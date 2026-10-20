using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
[DefaultExecutionOrder(-10000)]
public class ClosureControlHarness:MonoBehaviour
{
 public ScenarioManager manager;public RobotSocialNavController robot;
 readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 StreamWriter output,trace;int failures;
 object Invoke(object instance,string name,params object[] args){return instance.GetType().GetMethod(name,flags).Invoke(instance,args);}
 IEnumerator Start(){
  yield return null;yield return null;
  string folder=Environment.GetEnvironmentVariable("ASNV_OUTPUT");Directory.CreateDirectory(folder);
  output=new StreamWriter(Path.Combine(folder,"CONTROL_TESTS.csv"));output.AutoFlush=true;output.WriteLine("Test,Expected,Observed,Tolerance,Status");
  trace=new StreamWriter(Path.Combine(folder,"stopping_trace.csv"));trace.AutoFlush=true;trace.WriteLine("Test,Time_s,X_m,Z_m,AgentVelocity_mps,Command_mps,Stopped,InternalDistance_m");
  manager.CaptureRobotInitialState();
  foreach(var item in manager.humans)if(item!=null&&item.human!=null)foreach(var p in item.human.GetComponentsInChildren<HumanPatrol>(true))p.enabled=false;
  // Repeat each scene preset immediately with the same existing requested seed.
  var presets=new[]{manager.E1,manager.E2,manager.E3,manager.E4};int[] expected={2,2,3,9};
  for(int k=0;k<4;k++){
   Invoke(manager,"ApplyScenario",presets[k]);Physics.SyncTransforms();var first=Snapshot();
   Invoke(manager,"ApplyScenario",presets[k]);Physics.SyncTransforms();var second=Snapshot();
   Check("E"+(k+1)+"_same_seed_initialization","identical poses/target/seed/unstuck",first==second?"identical":"different","exact serialized state",first==second);
   var active=EligibleExperimentalHumans.Collect(manager);Check("E"+(k+1)+"_active_count",expected[k].ToString(),active.Count.ToString(),"exact",active.Count==expected[k]);
   GameObject inactive=null;foreach(var item in manager.humans)if(item!=null&&item.human!=null&&!item.human.activeInHierarchy){inactive=item.human;break;}
   if(inactive!=null){Vector3 previous=inactive.transform.position;float before=Minimum(active);inactive.transform.position=robot.transform.position;float after=Minimum(EligibleExperimentalHumans.Collect(manager));inactive.transform.position=previous;Check("E"+(k+1)+"_inactive_GT","unchanged minimum",(after-before).ToString("R",CultureInfo.InvariantCulture),"1e-6 m",Mathf.Abs(after-before)<1e-6f);}
  }
  Invoke(manager,"ApplyScenario",manager.E1);yield return null;
  var pool=EligibleExperimentalHumans.Collect(manager);if(pool.Count==0){Check("fixture_human","active","none","exact",false);Finish();yield break;}
  Transform human=pool[0];foreach(var item in manager.humans)if(item!=null&&item.human!=null&&item.human.transform!=human)item.human.SetActive(false);
  robot.SetExternalMissionControl(true);robot.SetExternalMissionTarget(robot.pointB_R);robot.SetExperimentHold(false);
  yield return Encounter(human,RobotSocialNavController.ExperimentMode.M1_ThresholdStop,3.61f,false,1f,"M1_outside");
  yield return Encounter(human,RobotSocialNavController.ExperimentMode.M1_ThresholdStop,1.19f,true,0f,"M1_personal_stop");
  yield return Encounter(human,RobotSocialNavController.ExperimentMode.M1_ThresholdStop,.44f,true,0f,"M1_intimate_stop");
  robot.ResetExperimentalState();robot.SetExperimentHold(false);robot.SetExternalMissionControl(true);robot.SetExternalMissionTarget(robot.pointB_R);
  yield return Encounter(human,RobotSocialNavController.ExperimentMode.M2_HysteresisSupervisor,1.19f,true,0f,"M2_entry");
  yield return Encounter(human,RobotSocialNavController.ExperimentMode.M2_HysteresisSupervisor,2f,true,0f,"M2_memory");
  yield return Encounter(human,RobotSocialNavController.ExperimentMode.M2_HysteresisSupervisor,3.61f,false,1f,"M2_exit");
  yield return Encounter(human,RobotSocialNavController.ExperimentMode.M4_FullAnisotropicHysteresis,1.19f,false,.45f,"M4_personal_slow");
  yield return Encounter(human,RobotSocialNavController.ExperimentMode.M4_FullAnisotropicHysteresis,.44f,true,0f,"M4_declared_intimate_stop");
  // Compiled field function, orientation forced through its documented Transform fallback.
  human.position=robot.transform.position+Vector3.right;human.rotation=Quaternion.LookRotation(Vector3.right);Physics.SyncTransforms();Invoke(robot,"UpdateNearestHuman");typeof(RobotSocialNavController).GetField("nearestHumanKin",flags).SetValue(robot,null);
  float longitudinal=((Vector3)Invoke(robot,"ComputeSocialAvoidanceVector")).magnitude;
  human.rotation=Quaternion.LookRotation(Vector3.left);float reverse=((Vector3)Invoke(robot,"ComputeSocialAvoidanceVector")).magnitude;
  human.rotation=Quaternion.LookRotation(Vector3.forward);float lateral=((Vector3)Invoke(robot,"ComputeSocialAvoidanceVector")).magnitude;
  Check("M4_cos2_symmetry","front=back",Mathf.Abs(longitudinal-reverse).ToString("R",CultureInfo.InvariantCulture),"1e-5",Mathf.Abs(longitudinal-reverse)<1e-5f);
  Check("M4_longitudinal_lateral","longitudinal > lateral",longitudinal.ToString("R",CultureInfo.InvariantCulture)+" / "+lateral.ToString("R",CultureInfo.InvariantCulture),"1e-5",longitudinal>lateral+1e-5f);
  robot.SetExperimentHold(true);
  var dwa=robot.GetComponent<SocialDWAController>();
  if(dwa!=null&&dwa.config!=null){
   Invoke(dwa,"ResetControllerState");var candidates=(System.Collections.IEnumerable)Invoke(dwa,"GenerateCandidates");float v=0,w=0;bool feasible=true;int count=0;
   foreach(var c in candidates){float cv=(float)c.GetType().GetField("v").GetValue(c),cw=(float)c.GetType().GetField("omega").GetValue(c);feasible&=cv>=-1e-6f&&cv<=dwa.config.aVMax*Time.fixedDeltaTime+1e-6f&&Mathf.Abs(cw)<=dwa.config.aOmegaMax*Time.fixedDeltaTime+1e-6f;v=cv;w=cw;count++;}
   Check("B1_dynamic_window","all candidates reachable",count.ToString(),"1e-6 command units",feasible&&count>0);
   object[] args={v,w,null};var trajectory=(List<Vector3>)dwa.GetType().GetMethod("SimulateTrajectory",flags).Invoke(dwa,args);
   robot.GetComponent<NavMeshAgent>().updatePosition=false;Invoke(dwa,"ApplyControl",v,w);float error=Vector3.Distance(robot.transform.position,trajectory[0]);Check("B1_rollout_actuator_first_step","same discrete state",error.ToString("R",CultureInfo.InvariantCulture),"1e-6 m",error<=1e-6f);
  }else Check("B1_config","serialized config","missing","exact",false);
  var clone=new GameObject("SeedStartFixture");clone.SetActive(false);clone.AddComponent<CharacterController>();var patrol=clone.AddComponent<HumanPatrol>();patrol.pointA=robot.pointA_R;patrol.pointB=robot.pointB_R;patrol.ApplyExperimentSeed(1007);int seedDirection=(int)typeof(HumanPatrol).GetField("unstuckDirection",flags).GetValue(patrol);clone.SetActive(true);yield return null;
  int firstDirection=(int)typeof(HumanPatrol).GetField("unstuckDirection",flags).GetValue(patrol);Check("seed_before_first_Start","seeded direction retained",firstDirection.ToString(),"exact",firstDirection==seedDirection);clone.SetActive(false);Destroy(clone);
  Finish();
 }
 IEnumerator Encounter(Transform h,RobotSocialNavController.ExperimentMode method,float distance,bool stopped,float speed,string name){
  robot.experimentMode=method;h.position=robot.transform.position+Vector3.right*distance;Physics.SyncTransforms();
  Invoke(robot,"UpdateNearestHuman");
  string function=method==RobotSocialNavController.ExperimentMode.M1_ThresholdStop?"UpdateM1_ThresholdStop":method==RobotSocialNavController.ExperimentMode.M2_HysteresisSupervisor?"UpdateM2_HysteresisSupervisor":"UpdateM4_FullAnisotropicHysteresis";
  var agent=robot.GetComponent<NavMeshAgent>();float entryVelocity=agent.velocity.magnitude;
  // Finite braking replaces the inappropriate instantaneous-displacement assumption.
  // Defined from pre-command velocity and serialized acceleration BEFORE observation.
  float brakingBound=entryVelocity*entryVelocity/(2*Mathf.Max(agent.acceleration,1e-6f))+entryVelocity*Mathf.Max(Time.deltaTime,Time.fixedDeltaTime)+.005f;
  Invoke(robot,function);Vector3 before=robot.transform.position;
  float d=robot.VerificationDistance;Check(name+"_input",distance.ToString(CultureInfo.InvariantCulture),d.ToString("R",CultureInfo.InvariantCulture),"0.05 m",Mathf.Abs(d-distance)<=.05f);
  Check(name+"_command",stopped?"stopped":"moving",agent.isStopped?"stopped":"moving","exact",agent.isStopped==stopped);
  if(!stopped)Check(name+"_speed",speed.ToString(CultureInfo.InvariantCulture),agent.speed.ToString("R",CultureInfo.InvariantCulture),"1e-5 m/s",Mathf.Abs(agent.speed-speed)<1e-5f);
  Vector3 lastStoppedPosition=before;bool lawConsistent=true;
  double start=Time.realtimeSinceStartupAsDouble,until=start+.25;while(Time.realtimeSinceStartupAsDouble<until){
   if(agent.isStopped)lastStoppedPosition=robot.transform.position;
   if(method==RobotSocialNavController.ExperimentMode.M4_FullAnisotropicHysteresis){float internalDistance=robot.VerificationDistance;if(internalDistance>=0)lawConsistent&=agent.isStopped==(internalDistance<=robot.intimateZone);}
   trace.WriteLine(string.Join(",",new[]{name,(Time.realtimeSinceStartupAsDouble-start).ToString("R",CultureInfo.InvariantCulture),robot.transform.position.x.ToString("R",CultureInfo.InvariantCulture),robot.transform.position.z.ToString("R",CultureInfo.InvariantCulture),agent.velocity.magnitude.ToString("R",CultureInfo.InvariantCulture),agent.speed.ToString("R",CultureInfo.InvariantCulture),agent.isStopped?"1":"0",robot.VerificationDistance.ToString("R",CultureInfo.InvariantCulture)}));yield return null;
  }
  float movement=Vector3.Distance(before,robot.transform.position);
  if(stopped){float brakingMovement=Vector3.Distance(before,lastStoppedPosition);Check(name+"_actuation","finite envelope during stop command",brakingMovement.ToString("R",CultureInfo.InvariantCulture),"v0="+entryVelocity.ToString("R",CultureInfo.InvariantCulture)+";a="+agent.acceleration.ToString("R",CultureInfo.InvariantCulture)+";bound="+brakingBound.ToString("R",CultureInfo.InvariantCulture)+" m",brakingMovement<=brakingBound);}
  if(method==RobotSocialNavController.ExperimentMode.M4_FullAnisotropicHysteresis)Check(name+"_threshold_law","stop iff inside declared intimate zone",lawConsistent?"consistent":"inconsistent","threshold 0.45 m; exact flags",lawConsistent);
 }
 float Minimum(List<Transform> humans){float d=float.PositiveInfinity;foreach(var h in humans){Vector3 delta=h.position-robot.transform.position;delta.y=0;d=Mathf.Min(d,delta.magnitude);}return d;}
 string Snapshot(){var s=new System.Text.StringBuilder();foreach(var item in manager.humans)if(item!=null&&item.human!=null){s.Append(item.id).Append(item.human.activeInHierarchy).Append(JsonUtility.ToJson(new PoseState{position=item.human.transform.position,rotation=item.human.transform.rotation}));foreach(var p in item.human.GetComponentsInChildren<HumanPatrol>(true))foreach(string field in new[]{"currentSeed","unstuckDirection","currentTarget"})s.Append(field).Append(p.GetType().GetField(field,flags).GetValue(p));}return s.ToString();}
 void Check(string name,string expected,string actual,string tolerance,bool pass){output.WriteLine(string.Join(",",new[]{name,expected,actual,tolerance,pass?"PASS":"FAIL"}));if(!pass)failures++;Debug.Log("ASNV_CONTROL "+name+" "+(pass?"PASS":"FAIL")+" "+actual);}
 void Finish(){output.Dispose();trace.Dispose();Debug.Log("ASNV_CONTROLS_DONE failures="+failures);Application.Quit(failures==0?0:21);}
 [Serializable]class PoseState{public Vector3 position;public Quaternion rotation;}
}
