using System.Collections.Generic;
using UnityEngine;
public static class EligibleExperimentalHumans
{
    public static int StableID(ScenarioManager manager,Transform human)
    {
        foreach(var reference in manager.humans)if(reference!=null&&reference.human!=null&&reference.human.transform==human){
            var match=System.Text.RegularExpressions.Regex.Match(reference.id??"",@"\d+$");
            if(match.Success&&int.TryParse(match.Value,out int id)&&id>0)return id;
            throw new System.InvalidOperationException("Experimental human ID must have a positive unique numeric suffix");
        }
        throw new System.InvalidOperationException("Human outside experimental registry");
    }
    // Ground truth eligibility: serialized experimental inventory, active hierarchy.
    // No name guessing, geometric deduplication, or inactive explicit fallback.
    public static List<Transform> Collect(ScenarioManager manager)
    {
        var result = new List<Transform>();
        if(manager == null) return result;
        foreach(var reference in manager.humans)
            if(reference != null && reference.human != null && reference.human.activeInHierarchy && !result.Contains(reference.human.transform))
                result.Add(reference.human.transform);
        return result;
    }
}
