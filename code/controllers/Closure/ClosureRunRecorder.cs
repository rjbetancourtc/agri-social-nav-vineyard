using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using Stopwatch=System.Diagnostics.Stopwatch;
[DefaultExecutionOrder(11000)]
public class ClosureRunRecorder:MonoBehaviour
{
 public ScenarioManager manager;public RobotSocialNavController robot;public ReliableSocialTelemetry channel;
 public static ClosureRunRecorder Instance;
 StreamWriter cycles,frames,b2;bool running;long cycleStart,detectionEnd,algorithmStart,actuatorTick;Vector3 before,proposed;string writer="",branch="";double origin;long cyclesObserved,observerTicks;
 readonly Dictionary<HumanPatrol,bool> savedEnabled=new Dictionary<HumanPatrol,bool>();
 static readonly CultureInfo CI=CultureInfo.InvariantCulture;
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 void Awake(){Instance=this;}
 public void PauseHumans(bool paused){
  foreach(var reference in manager.humans){if(reference==null||reference.human==null)continue;
   foreach(var patrol in reference.human.GetComponentsInChildren<HumanPatrol>(true)){
    if(paused){if(!savedEnabled.ContainsKey(patrol))savedEnabled[patrol]=patrol.enabled;patrol.enabled=false;}
    else if(savedEnabled.TryGetValue(patrol,out bool enabled)){patrol.enabled=enabled;}
   }
  }
  if(!paused)savedEnabled.Clear();
 }
 public void Begin(){
  string folder=Environment.GetEnvironmentVariable("ASNV_OUTPUT");string id=channel.PacketIdentity;
  var a=robot.GetComponent<NavMeshAgent>();var dwa=robot.GetComponent<SocialDWAController>();
  var humans=new List<HumanSnapshot>();
  foreach(var reference in manager.humans){if(reference==null||reference.human==null)continue;var h=reference.human;
   var patrol=h.GetComponentInChildren<HumanPatrol>(true);var kin=h.GetComponentInChildren<HumanKinematics>(true);
   humans.Add(new HumanSnapshot{id=reference.id,wireID=EligibleExperimentalHumans.StableID(manager,h.transform),runtimeID=h.transform.GetInstanceID(),active=h.activeInHierarchy,position=h.transform.position,rotation=h.transform.rotation,forward=h.transform.forward,patrolConfig=patrol==null?null:JsonUtility.ToJson(patrol),seed=patrol==null?0:(int)typeof(HumanPatrol).GetField("currentSeed",Flags).GetValue(patrol),unstuck=patrol==null?0:(int)typeof(HumanPatrol).GetField("unstuckDirection",Flags).GetValue(patrol),target=patrol==null?null:((Transform)typeof(HumanPatrol).GetField("currentTarget",Flags).GetValue(patrol))?.name,velocity=kin==null?Vector3.zero:kin.PlanarVelocity});
  }
  var components=new List<string>();foreach(var c in robot.GetComponents<Component>()){if(c==null)continue;components.Add(c.GetType().FullName+" enabled="+(c is Behaviour b?b.enabled:true));}
  var manifest=new RuntimeManifest{identity=id,protocol=4,sim=Time.timeAsDouble,mono=Time.realtimeSinceStartupAsDouble,pointA=robot.pointA_R.position,pointB=robot.pointB_R.position,start=robot.transform.position,arrivalRadius=(float)typeof(ScenarioManager).GetField("waypointArrivalTolerance",Flags).GetValue(manager),timeoutWall=(float)typeof(ScenarioManager).GetField("maximumRunTime",Flags).GetValue(manager),fixedDt=Time.fixedDeltaTime,timeScale=Time.timeScale,targetFrameRate=Application.targetFrameRate,robotConfig=JsonUtility.ToJson(robot),dwaConfig=dwa==null||dwa.config==null?null:JsonUtility.ToJson(dwa.config),navRadius=a.radius,navAcceleration=a.acceleration,navAngularDeg=a.angularSpeed,humans=humans.ToArray(),components=components.ToArray(),buildManifest=File.ReadAllText(Environment.GetEnvironmentVariable("ASNV_BUILD_MANIFEST"))};
  File.WriteAllText(Path.Combine(folder,id+".runtime_manifest.json"),JsonUtility.ToJson(manifest,true));
  cycles=new StreamWriter(Path.Combine(folder,id+".control.csv"));cycles.WriteLine("FixedTime_s,Monotonic_s,Frame,Writer,Branch,XBefore,ZBefore,XAfter,ZAfter,ProposedX,ProposedZ,LinearCommand,AgentVelocity,ManualV,ManualOmega,Stopped,UpdatePosition,UpdateRotation,InternalDistance,SelectedRuntimeHuman,Hysteresis,FieldX,FieldZ,DetectedHumans,Detection_ms,Kernel_ms,SensorToCommandAPI_ms,Observer_ms,SelectionValid,SelectedWireHuman,SelectedX,SelectedZ,SelectedHeadingRad,HeadingFromKinematics,DetectorWireHumans,TelemetryWireHumans,B1WireHumans,FieldComputedM4,FieldMagnitude,GC0,GC1,GC2");
  frames=new StreamWriter(Path.Combine(folder,id+".frames.jsonl"));
  b2=new StreamWriter(Path.Combine(folder,id+".b2.csv"));b2.WriteLine("FixedTime_s,Qx,Qz,Ux,Uz,Radius,Horizon,TStar,Closest,Risk");
  origin=Time.timeAsDouble;cyclesObserved=0;observerTicks=0;running=true;
 }
 public void End(){running=false;cycles?.Dispose();frames?.Dispose();b2?.Dispose();cycles=null;frames=null;b2=null;PauseHumans(true);}
 public static void StartCycle(RobotSocialNavController r){var o=Instance;if(o==null||!o.running||r!=o.robot)return;o.cycleStart=Stopwatch.GetTimestamp();o.before=r.transform.position;o.proposed=o.before;o.branch="entry";o.writer="none";o.actuatorTick=0;}
 public static void DetectionComplete(){var o=Instance;if(o==null||!o.running)return;o.detectionEnd=Stopwatch.GetTimestamp();o.algorithmStart=o.detectionEnd;}
 public static void DwaStart(){var o=Instance;if(o==null||!o.running)return;o.algorithmStart=Stopwatch.GetTimestamp();}
 public static void Branch(Component component,string text,[CallerMemberName]string member=""){var o=Instance;if(o==null||!o.running||component.gameObject!=o.robot.gameObject)return;o.branch=member+":"+text;o.writer=component.GetType().Name;}
 public static void Applied(Component component,Vector3 commanded,[CallerMemberName]string member=""){var o=Instance;if(o==null||!o.running||component.gameObject!=o.robot.gameObject)return;o.writer=component.GetType().Name+"."+member;o.proposed=commanded;o.actuatorTick=Stopwatch.GetTimestamp();}
 public static void B2(Vector3 q,Vector3 u,float radius,float horizon,float t,float distance,bool risk){var o=Instance;if(o==null||!o.running)return;o.b2.WriteLine(string.Join(",",new[]{F(Time.fixedTimeAsDouble),F(q.x),F(q.z),F(u.x),F(u.z),F(radius),F(horizon),F(t),F(distance),risk?"1":"0"}));}
 public static void FinishCycle(bool dwa=false){var o=Instance;if(o==null||!o.running)return;if((int)o.robot.experimentMode==5&&!dwa)return;
  long stop=Stopwatch.GetTimestamp();long observe=stop;var a=o.robot.GetComponent<NavMeshAgent>();Vector3 after=o.robot.transform.position;var d=o.robot.GetComponent<SocialDWAController>();
  float manualV=(int)o.robot.experimentMode==5?d.CurrentLinearVelocity:(float)typeof(RobotSocialNavController).GetField("manualCurrentV",Flags).GetValue(o.robot);
  float manualW=(int)o.robot.experimentMode==5?d.CurrentAngularVelocity:(float)typeof(RobotSocialNavController).GetField("manualCurrentOmega",Flags).GetValue(o.robot);
  double detection=(o.detectionEnd-o.cycleStart)*1000.0/Stopwatch.Frequency,kernel=(stop-o.algorithmStart)*1000.0/Stopwatch.Frequency,chain=(o.actuatorTick>0?o.actuatorTick:stop)-o.cycleStart;chain=chain*1000.0/Stopwatch.Frequency;
  var f=o.robot.VerificationField;
  string row=string.Join(",",new[]{F(Time.fixedTimeAsDouble),F(Time.realtimeSinceStartupAsDouble),Time.frameCount.ToString(CI),o.writer,o.branch,F(o.before.x),F(o.before.z),F(after.x),F(after.z),F(o.proposed.x),F(o.proposed.z),F(a.speed),F(a.velocity.magnitude),F(manualV),F(manualW),a.isStopped?"1":"0",a.updatePosition?"1":"0",a.updateRotation?"1":"0",F(o.robot.VerificationDistance),o.robot.VerificationHumanID.ToString(CI),o.robot.VerificationHysteresis?"1":"0",F(f.x),F(f.z),o.robot.DetectedHumanCount.ToString(CI),F(detection),F(kernel),F(chain)});
  var selected=(Transform)typeof(RobotSocialNavController).GetField("nearestHuman",Flags).GetValue(o.robot);var kin=(HumanKinematics)typeof(RobotSocialNavController).GetField("nearestHumanKin",Flags).GetValue(o.robot);
  var detector=(List<Transform>)typeof(RobotSocialNavController).GetField("detectedHumans",Flags).GetValue(o.robot);var eligible=EligibleExperimentalHumans.Collect(o.manager);var b1=new List<Transform>();if((int)o.robot.experimentMode==5&&d!=null&&d.enabled&&d.humans!=null)foreach(var h in d.humans)if(h!=null)b1.Add(h.transform);
  string heading=selected==null?"":F(kin!=null?kin.HeadingRad:Mathf.Atan2(selected.forward.z,selected.forward.x));
  string extra=string.Join(",",new[]{selected!=null?"1":"0",selected==null?"":EligibleExperimentalHumans.StableID(o.manager,selected).ToString(CI),selected==null?"":F(selected.position.x),selected==null?"":F(selected.position.z),heading,kin!=null?"1":"0",o.WireIDs(detector),o.WireIDs(eligible),o.WireIDs(b1),(int)o.robot.experimentMode==4?"1":"0",F(f.magnitude),GC.CollectionCount(0).ToString(CI),GC.CollectionCount(1).ToString(CI),GC.CollectionCount(2).ToString(CI)});
  double cost=(Stopwatch.GetTimestamp()-observe)*1000.0/Stopwatch.Frequency;o.cycles.WriteLine(row+","+F(cost)+","+extra);o.cyclesObserved++;o.observerTicks+=Stopwatch.GetTimestamp()-observe;
 }
 void LateUpdate(){if(!running)return;
  var active=EligibleExperimentalHumans.Collect(manager);var humans=new List<FrameHuman>();foreach(var h in active){var kin=h.GetComponentInChildren<HumanKinematics>(true);humans.Add(new FrameHuman{id=EligibleExperimentalHumans.StableID(manager,h),position=h.position,forward=h.forward,velocity=kin==null?Vector3.zero:kin.PlanarVelocity,headingFromKinematics=kin!=null,headingRad=kin==null?Mathf.Atan2(h.forward.z,h.forward.x):kin.HeadingRad});}
  frames.WriteLine(JsonUtility.ToJson(new FrameSnapshot{sim=Time.timeAsDouble-origin,mono=Time.realtimeSinceStartupAsDouble,frame=Time.frameCount,leg=(int)manager.CurrentMissionLeg,position=robot.transform.position,forward=robot.transform.forward,velocity=robot.GetComponent<NavMeshAgent>().velocity,humans=humans.ToArray()}));
 }
 void OnDestroy(){End();if(Instance==this)Instance=null;}
 static string F(double n){return n.ToString("R",CI);}
 string WireIDs(List<Transform> list){var ids=new List<int>();foreach(var h in list)if(h!=null)ids.Add(EligibleExperimentalHumans.StableID(manager,h));ids.Sort();return string.Join(";",ids);}
 [Serializable]class HumanSnapshot{public string id,patrolConfig,target;public int wireID,runtimeID,seed,unstuck;public bool active;public Vector3 position,forward,velocity;public Quaternion rotation;}
 [Serializable]class RuntimeManifest{public string identity,robotConfig,dwaConfig,buildManifest;public int protocol,targetFrameRate;public double sim,mono;public float arrivalRadius,timeoutWall,fixedDt,timeScale,navRadius,navAcceleration,navAngularDeg;public Vector3 pointA,pointB,start;public HumanSnapshot[] humans;public string[] components;}
 [Serializable]class FrameHuman{public int id;public Vector3 position,forward,velocity;public float headingRad;public bool headingFromKinematics;}
 [Serializable]class FrameSnapshot{public double sim,mono;public int frame,leg;public Vector3 position,forward,velocity;public FrameHuman[] humans;}
}
