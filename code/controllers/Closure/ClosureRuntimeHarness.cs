using System;
using System.Collections;
using System.IO;
using UnityEngine;
[DefaultExecutionOrder(-10000)]
public class ClosureRuntimeHarness : MonoBehaviour
{
 public ScenarioManager manager; public ReliableSocialTelemetry channel;
 IEnumerator Start(){
  Time.timeScale=1; yield return null;yield return null;
  manager.CaptureRobotInitialState();
  Debug.Log("ASNV_PILOT_START two consecutive complete missions; M0 E1 development seeds");
  manager.StartCoroutine("TwoRunValidationRoutine");
  double start=Time.realtimeSinceStartupAsDouble;int commits=0;bool previous=false;
  while(Time.realtimeSinceStartupAsDouble-start<450){
   if(channel.Persisted&&!previous)commits++;
   previous=channel.Persisted;
   if(commits>=2){Debug.Log("ASNV_PILOT_DONE commits=2");Application.Quit(0);yield break;}
   yield return null;
  }
  Debug.LogError("ASNV_PILOT_TIMEOUT commits="+commits);Application.Quit(20);
 }
}
