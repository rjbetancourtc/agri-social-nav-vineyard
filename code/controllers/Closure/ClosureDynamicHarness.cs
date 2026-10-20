using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
[DefaultExecutionOrder(-10000)]
public class ClosureDynamicHarness:MonoBehaviour
{
 public ScenarioManager manager;public RobotSocialNavController robot;public ReliableSocialTelemetry channel;public ClosureRunRecorder recorder;
 static BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 void Awake(){robot.SetExperimentHold(true);recorder.PauseHumans(true);string campaign=Environment.GetEnvironmentVariable("ASNV_CAMPAIGN_ID");if(!string.IsNullOrEmpty(campaign))channel.campaignID=campaign;}
 IEnumerator Start(){
  Time.timeScale=1;string step=Environment.GetEnvironmentVariable("ASNV_FIXED_STEP_S");Time.fixedDeltaTime=string.IsNullOrEmpty(step)?.02f:float.Parse(step,System.Globalization.CultureInfo.InvariantCulture);Application.targetFrameRate=120;QualitySettings.vSyncCount=0;
  if(Environment.GetEnvironmentVariable("ASNV_RUNTIME_MODE")=="controls"){
   channel.enabled=false;recorder.enabled=false;var controls=manager.gameObject.AddComponent<ClosureControlHarness>();controls.manager=manager;controls.robot=robot;enabled=false;yield break;
  }
  yield return null;yield return null;
  if(Environment.GetEnvironmentVariable("ASNV_START_AT_A")=="1")PrepareStartAtA();
  manager.CaptureRobotInitialState();
  string path=Environment.GetEnvironmentVariable("ASNV_OUTPUT");int[][] plan={new[]{6,1,5001},new[]{7,1,5001},new[]{8,1,5001},new[]{4,4,5004},new[]{5,1,5001}};
  if(Environment.GetEnvironmentVariable("ASNV_PERFORMANCE_GATE_FIRST")=="1")plan=new[]{new[]{5,1,5101},new[]{6,1,5101},new[]{7,1,5101},new[]{8,1,5101},new[]{4,4,5104}};
  if(Environment.GetEnvironmentVariable("ASNV_ACQUISITION_FIXED_TEST")=="1")plan=new[]{new[]{5,1,5201},new[]{8,1,5201},new[]{6,1,5201}};
  if(Environment.GetEnvironmentVariable("ASNV_BROAD_TEST")=="1")plan=new[]{new[]{0,2,5302},new[]{1,2,5302},new[]{2,3,5303},new[]{3,3,5303}};
  string planPath=Environment.GetEnvironmentVariable("ASNV_RUN_PLAN");
  if(!string.IsNullOrEmpty(planPath)){
   var declared=JsonUtility.FromJson<RunPlan>(File.ReadAllText(planPath));plan=new int[declared.runs.Length][];
   for(int j=0;j<plan.Length;j++){var r=declared.runs[j];plan[j]=new[]{r.method,r.scenario,r.seed,r.run,r.attempt};}
  }
  string pilotLimit=Environment.GetEnvironmentVariable("ASNV_PILOT_LIMIT");int limit=string.IsNullOrEmpty(pilotLimit)?plan.Length:int.Parse(pilotLimit);
  using(var log=new StreamWriter(Path.Combine(path,"attempts.jsonl"),true)){
   for(int n=0;n<Math.Min(limit,plan.Length);n++){
    var p=plan[n];robot.SetExperimentHold(true);robot.experimentMode=(RobotSocialNavController.ExperimentMode)p[0];
    int runID=p.Length>3?p[3]:1;channel.attemptID=p.Length>4?p[4]:1;
    string identity=channel.campaignID+"_M"+p[0]+"_E"+p[1]+"_R"+runID+"_S"+p[2]+"_A"+channel.attemptID;
    if(File.Exists(Path.Combine(path,identity,"COMMIT.json"))){Debug.Log("ASNV_DYNAMIC_SKIP_VERIFIED "+identity);continue;}
    typeof(ScenarioManager).GetField("experimentSeed",Flags).SetValue(manager,p[2]);yield return null;
    var scenario=p[1]==4?manager.E4:p[1]==3?manager.E3:p[1]==2?manager.E2:manager.E1;
    log.WriteLine("{\"identity\":\""+identity+"\",\"method\":"+p[0]+",\"scenario\":"+p[1]+",\"seed\":"+p[2]+",\"run\":"+runID+",\"attempt\":"+channel.attemptID+",\"event\":\"EXECUTION_REQUESTED\"}");log.Flush();
    var routine=(IEnumerator)typeof(ScenarioManager).GetMethod("AutomaticExperimentRoutine",Flags).Invoke(manager,new object[]{scenario,runID});
    yield return manager.StartCoroutine(routine);
    bool committed=channel.Persisted;
    log.WriteLine("{\"identity\":\""+identity+"\",\"method\":"+p[0]+",\"scenario\":"+p[1]+",\"seed\":"+p[2]+",\"run\":"+runID+",\"attempt\":"+channel.attemptID+",\"event\":\"RETURNED\",\"committed\":"+(committed?"true":"false")+",\"result\":\""+manager.LastRunTermination+"\"}");log.Flush();
    if(!committed){Debug.LogError("ASNV_DYNAMIC_FAILED no durable ACK; no next mission");Application.Quit(22);yield break;}
    if(n==0&&Environment.GetEnvironmentVariable("ASNV_PERFORMANCE_GATE_FIRST")=="1"&&Math.Abs(channel.LastRunSimDuration/channel.LastRunWallDuration-1)>.05){Debug.LogError("ASNV_CLOCK_GATE_FAILED; preserve B1 attempt; no other pilots");Application.Quit(23);yield break;}
    Debug.Log("ASNV_DYNAMIC_COMMITTED index="+(n+1)+" method="+p[0]+" scenario="+p[1]+" result="+manager.LastRunTermination);
   }
  }
  Debug.Log("ASNV_DYNAMIC_DONE");Application.Quit(0);
 }
 [Serializable]class RunPlan{public PlannedRun[] runs;}
 [Serializable]class PlannedRun{public int method,scenario,seed,run,attempt;}
 void PrepareStartAtA(){
  var a=robot.GetComponent<NavMeshAgent>();Vector3 old=robot.transform.position;
  if(!NavMesh.SamplePosition(robot.pointA_R.position,out NavMeshHit ha,robot.navMeshSampleRadius,a.areaMask))throw new InvalidOperationException("A has no NavMesh point within the existing sample radius");
  if(!NavMesh.SamplePosition(robot.pointB_R.position,out NavMeshHit hb,robot.navMeshSampleRadius,a.areaMask))throw new InvalidOperationException("B has no NavMesh point within the existing sample radius");
  float radius=(float)typeof(ScenarioManager).GetField("waypointArrivalTolerance",Flags).GetValue(manager);
  if(Vector2.Distance(new Vector2(ha.position.x,ha.position.z),new Vector2(robot.pointA_R.position.x,robot.pointA_R.position.z))>radius)throw new InvalidOperationException("Projected start is outside A arrival region");
  var path=new NavMeshPath();if(!NavMesh.CalculatePath(ha.position,hb.position,a.areaMask,path)||path.status!=NavMeshPathStatus.PathComplete)throw new InvalidOperationException("A/B NavMesh path incomplete");
  var widths=new System.Collections.Generic.List<CorridorSample>();float length=0;
  for(int k=0;k<path.corners.Length-1;k++){
   Vector3 start=path.corners[k],end=path.corners[k+1],direction=(end-start).normalized,right=Vector3.Cross(Vector3.up,direction).normalized;length+=Vector3.Distance(start,end);
   for(int j=0;j<=4;j++){Vector3 p=Vector3.Lerp(start,end,j/4f);NavMeshHit leftHit,rightHit;bool left=NavMesh.Raycast(p,p-right*10,out leftHit,a.areaMask),r=NavMesh.Raycast(p,p+right*10,out rightHit,a.areaMask);widths.Add(new CorridorSample{point=p,hitBothBoundaries=left&&r,centerTravelWidth=left&&r?Vector3.Distance(p,leftHit.position)+Vector3.Distance(p,rightHit.position):-1});}
  }
  if(!a.Warp(ha.position))throw new InvalidOperationException("A start Warp failed");robot.transform.position=ha.position;a.nextPosition=ha.position;robot.SetExperimentHold(true);Physics.SyncTransforms();
  var geo=new GeometrySnapshot{sourceStart=old,pointA=robot.pointA_R.position,pointB=robot.pointB_R.position,projectedA=ha.position,projectedB=hb.position,start=robot.transform.position,arrivalRadius=radius,navSampleRadius=robot.navMeshSampleRadius,agentRadius=a.radius,bakeRadius=NavMesh.GetSettingsByID(a.agentTypeID).agentRadius,pathLength=length,corners=path.corners,widths=widths.ToArray(),definition="XZ positions in metres; measured NavMesh centre-travel width, not physical vineyard width; original rotation retained; setup Warp is before RUN"};
  File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("ASNV_OUTPUT"),"GEOMETRY_PRE_RUN.json"),JsonUtility.ToJson(geo,true));
 }
 [Serializable]class CorridorSample{public Vector3 point;public bool hitBothBoundaries;public float centerTravelWidth;}
 [Serializable]class GeometrySnapshot{public Vector3 sourceStart,pointA,pointB,projectedA,projectedB,start;public float arrivalRadius,navSampleRadius,agentRadius,bakeRadius,pathLength;public Vector3[] corners;public CorridorSample[] widths;public string definition;}
}
