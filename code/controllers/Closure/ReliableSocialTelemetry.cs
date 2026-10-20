using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
[DefaultExecutionOrder(10000)]
public class ReliableSocialTelemetry : MonoBehaviour
{
 public ScenarioManager manager;
 public RobotSocialNavController controller;
 public UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV legacy;
 public string campaignID="DEVELOPMENT";
 public int attemptID=1, receiverPort=55150;
 public float samplePeriod=.1f, ackTimeout=30f;
 public bool Persisted {get;private set;}
 public string CommitHash {get;private set;}
 public double LastRunSimDuration {get;private set;}
 public double LastRunWallDuration {get;private set;}
 double runBeginSim,runBeginMono;
 public string PacketIdentity => $"{campaignID}_M{legacy.methodID}_E{legacy.scenarioID}_R{legacy.runID}_S{legacy.experimentSeed}_A{attemptID}";
 UdpClient socket; ulong sequence; string terminalPacket;
 double missionOrigin,lastSample,lastSend; Vector3 previous,smooth;
 bool running; int leg; string identity; StreamWriter observer; long observations,totalObserverTicks,maxObserverTicks;
 readonly Queue<Action> events=new Queue<Action>(); bool endRequested; double lastPacketFixed=double.NegativeInfinity;
 readonly Queue<Action> postEvents=new Queue<Action>();bool captureRunOrigin;
 bool preControlCapture;double previousPostFixed;
 bool identityPrepared;
 double lastRenderedPoseTime;
 // NavMesh updates in the variable-frame loop, not FixedUpdate. Pair its pose
 // with the completed frame time; manual controllers integrate at fixed steps.
 bool ManualPose => legacy.methodID==5 || legacy.methodID==8;
 double PoseTime() => ManualPose ? (preControlCapture?previousPostFixed:Time.fixedTimeAsDouble) : lastRenderedPoseTime;
 void LateUpdate(){lastRenderedPoseTime=Time.timeAsDouble;}
 static CultureInfo CI=CultureInfo.InvariantCulture;
 void Awake(){socket=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));socket.Client.Blocking=false;gameObject.AddComponent<SocialAcquisitionPhasePump>();}
 void Start(){
  string path=Environment.GetEnvironmentVariable("ASNV_OUTPUT");
  if(string.IsNullOrEmpty(path))path=Application.persistentDataPath;
  Directory.CreateDirectory(path);observer=new StreamWriter(Path.Combine(path,"runtime_sidecar.jsonl"),true);observer.AutoFlush=true;
  previous=transform.position;lastSample=Time.timeAsDouble;
 }
 public void PreparePhase(){
  identityPrepared=true;
  string nextIdentity=$"{campaignID}_{legacy.methodID}_{legacy.scenarioID}_{legacy.runID}_{legacy.experimentSeed}_{attemptID}";
  if(identity!=nextIdentity){terminalPacket=null;Persisted=false;CommitHash=null;running=false;leg=0;}
  ClosureRunRecorder.Instance?.PauseHumans(true);
  int phase=Phase();string expected=PacketIdentity;events.Enqueue(()=>{if(PacketIdentity!=expected)throw new InvalidOperationException("Phase identity changed before fixed capture");Transmit(Build(phase,0));});
 }
 public void NotifyBReached(){if(!running||leg==1)return;leg=1;postEvents.Enqueue(()=>Transmit(Build(4,0)));}
 public void BeginRun(){
  events.Enqueue(BeginAtFixedBoundary);
 }
 void BeginAtFixedBoundary(){
  runBeginSim=PoseTime();runBeginMono=Time.realtimeSinceStartupAsDouble;endRequested=false;
  running=true; Persisted=false;CommitHash=null;terminalPacket=null;leg=0;
  lastSample=runBeginSim;previous=transform.position;smooth=Vector3.zero;
  ClosureRunRecorder.Instance?.PauseHumans(false);
  ClosureRunRecorder.Instance?.Begin();
  captureRunOrigin=true;Transmit(Build(3,0));captureRunOrigin=false;
 }
 public void EndRun(int result){
  if(terminalPacket!=null||endRequested)return;endRequested=true;postEvents.Enqueue(()=>EndAtFixedBoundary(result));
 }
 void EndAtFixedBoundary(int result){
  LastRunSimDuration=PoseTime()-runBeginSim;LastRunWallDuration=Time.realtimeSinceStartupAsDouble-runBeginMono;
  terminalPacket=Build(6,result);running=false;ClosureRunRecorder.Instance?.End();Transmit(terminalPacket);
 }
 public void FlushFixedEvents(){preControlCapture=true;try{while(events.Count>0)events.Dequeue()();}finally{preControlCapture=false;}}
 void FixedUpdate(){try{FixedSample();}finally{previousPostFixed=Time.fixedTimeAsDouble;}}
 void FixedSample(){
  ReceiveACK();
  while(postEvents.Count>0)postEvents.Dequeue()();
  if(!identityPrepared)return;
  if(legacy==null||manager==null||legacy.runID<1)return;
  double now=Time.realtimeSinceStartupAsDouble;
  if(terminalPacket!=null){if(!Persisted&&now-lastSend>=samplePeriod)Transmit(terminalPacket);return;}
  if(endRequested||Time.fixedTimeAsDouble-lastPacketFixed<samplePeriod-1e-6)return;
  int state=running?3:Phase();
  if(running && manager.CurrentMissionLeg==ScenarioManager.MissionLeg.ReturningToA && leg==0){leg=1;state=4;}
  Transmit(Build(state,0));
 }
 int Phase(){switch(manager.CurrentExperimentState){case ScenarioManager.ExperimentState.Prepare:return 0;case ScenarioManager.ExperimentState.Reset:return 1;case ScenarioManager.ExperimentState.Settle:return 2;default:return 5;}}
 void ReceiveACK(){
  while(socket!=null&&socket.Available>0){
   IPEndPoint from=new IPEndPoint(IPAddress.Any,0);var f=Encoding.ASCII.GetString(socket.Receive(ref from)).Split(',');
   if(from.Port!=receiverPort||!IPAddress.IsLoopback(from.Address)||f.Length!=9||f[0]!="ASNV_ACK"||f[1]!="4"||legacy==null)continue;
   if(f[2]!=campaignID||f[3]!=legacy.methodID.ToString(CI)||f[4]!=legacy.scenarioID.ToString(CI)||f[5]!=legacy.runID.ToString(CI)||f[6]!=legacy.experimentSeed.ToString(CI)||f[7]!=attemptID.ToString(CI)||f[8].Length!=64)continue;
   if(!System.Text.RegularExpressions.Regex.IsMatch(f[8],"^[0-9a-f]{64}$"))continue;
   if(Persisted&&CommitHash!=f[8]){Debug.LogError("ASNV_ACK_CONFLICT identity="+PacketIdentity);continue;}
   if(terminalPacket!=null){Persisted=true;CommitHash=f[8];Debug.Log("ASNV_ACK "+string.Join(",",f));}
  }
 }
 void Transmit(string packet){
  byte[] b=Encoding.ASCII.GetBytes(packet);socket.Send(b,b.Length,"127.0.0.1",receiverPort);lastSend=Time.realtimeSinceStartupAsDouble;
 }
 string Build(int state,int result){
  string nextIdentity=$"{campaignID}_{legacy.methodID}_{legacy.scenarioID}_{legacy.runID}_{legacy.experimentSeed}_{attemptID}";
  if(identity!=nextIdentity){identity=nextIdentity;sequence=0;missionOrigin=PoseTime();}
  long start=System.Diagnostics.Stopwatch.GetTimestamp();
  Vector3 pos=transform.position,fwd=transform.forward;fwd.y=0;fwd.Normalize();
  double now=PoseTime(),dt=now-lastSample;float speed=0,acc=0;
  if(running&&dt>0){var velocity=(pos-previous)/(float)dt;velocity.y=0;var next=Vector3.Lerp(smooth,velocity,1-legacy.velSmoothing);speed=next.magnitude;acc=((next-smooth)/(float)dt).magnitude;smooth=next;}
  var humans=EligibleExperimentalHumans.Collect(manager); float distance=float.PositiveInfinity;
  foreach(var h in humans){var d=h.position-pos;d.y=0;distance=Mathf.Min(distance,d.magnitude);}
  var sb=new StringBuilder();sb.Append("ASNV,4,").Append(campaignID).Append(',').Append(legacy.methodID).Append(',').Append(legacy.scenarioID).Append(',').Append(legacy.runID).Append(',').Append(legacy.experimentSeed).Append(',').Append(attemptID).Append(',').Append(++sequence);
  Add(sb,now-missionOrigin);Add(sb,Time.realtimeSinceStartupAsDouble);Add(sb,state);Add(sb,result);Add(sb,leg);
  Add(sb,pos.x);Add(sb,pos.y);Add(sb,pos.z);Add(sb,fwd.x);Add(sb,fwd.z);Add(sb,speed);Add(sb,acc);Add(sb,humans.Count>0?1:0);sb.Append(',');if(humans.Count>0)sb.Append(distance.ToString("R",CI));
  sb.Append(",0,,").Append(humans.Count); // sensor distance absent; GT is separate.
  foreach(var h in humans){var hf=h.forward;hf.y=0;hf.Normalize();Add(sb,EligibleExperimentalHumans.StableID(manager,h));Add(sb,h.position.x);Add(sb,h.position.z);Add(sb,hf.x);Add(sb,hf.z);}
  var blocks=new List<Vector3>[] {new List<Vector3>(),new List<Vector3>(),new List<Vector3>(),new List<Vector3>()};
  int mask=legacy.humanMask.value|legacy.plantMask.value|legacy.bushMask.value|legacy.objectMask.value;
  // Current nearest physics ray hit only. Explicit rows/map history are never sensor returns.
  for(int i=0;i<legacy.lidarBeamCount;i++){
   var dir=Quaternion.Euler(0,i*360f/legacy.lidarBeamCount,0)*Vector3.forward;
   if(Physics.Raycast(pos+Vector3.up*.55f,dir,out RaycastHit hit,legacy.lidarMaxRange,mask,legacy.triggerInteraction)&&!hit.transform.IsChildOf(transform)){
    int bit=1<<hit.collider.gameObject.layer;int kind=(legacy.humanMask.value&bit)!=0?0:(legacy.plantMask.value&bit)!=0?1:(legacy.bushMask.value&bit)!=0?2:3;blocks[kind].Add(hit.point);
   }
  }
  foreach(var block in blocks){Add(sb,block.Count);foreach(var hit in block){Add(sb,hit.x);Add(sb,hit.z);}}
  var agent=GetComponent<NavMeshAgent>();
  string record=JsonUtility.ToJson(new RuntimeSample{campaign=campaignID,method=legacy.methodID,scenario=legacy.scenarioID,run=legacy.runID,seed=legacy.experimentSeed,attempt=attemptID,sequence=sequence,sim=now-missionOrigin,mono=Time.realtimeSinceStartupAsDouble,state=state,result=result,leg=leg,position=pos,previous=previous,filteredSpeed=speed,distance=humans.Count>0?distance:-1,humanCount=humans.Count,hold=controller.ExperimentHold,agentSpeed=agent.speed,agentVelocity=agent.velocity,updatePosition=agent.updatePosition,updateRotation=agent.updateRotation,stopped=agent.isStopped,detected=controller.DetectedHumanCount,internalDistance=controller.VerificationDistance,selectedHuman=controller.VerificationHumanID,hysteresis=controller.VerificationHysteresis,field=controller.VerificationField});
  observer?.WriteLine(record);previous=pos;lastSample=now;lastPacketFixed=Time.fixedTimeAsDouble;
  long elapsed=System.Diagnostics.Stopwatch.GetTimestamp()-start;totalObserverTicks+=elapsed;maxObserverTicks=Math.Max(maxObserverTicks,elapsed);observations++;
  return sb.ToString();
 }
 static void Add(StringBuilder sb,double value){sb.Append(',').Append(value.ToString("R",CI));}
 void OnDestroy(){socket?.Dispose();observer?.Dispose();Debug.Log($"ASNV_OBSERVER samples={observations} totalTicks={totalObserverTicks} maxTicks={maxObserverTicks} frequency={System.Diagnostics.Stopwatch.Frequency}");}
 [Serializable]class RuntimeSample{public string campaign;public int method,scenario,run,seed,attempt,state,result,leg,humanCount,detected,selectedHuman;public ulong sequence;public double sim,mono;public Vector3 position,previous,agentVelocity,field;public float filteredSpeed,distance,agentSpeed,internalDistance;public bool hold,updatePosition,updateRotation,stopped,hysteresis;}
}
[DefaultExecutionOrder(-15000)]
public class SocialAcquisitionPhasePump:MonoBehaviour
{
 void FixedUpdate(){GetComponent<ReliableSocialTelemetry>().FlushFixedEvents();}
}
