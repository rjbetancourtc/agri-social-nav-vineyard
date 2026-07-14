using UnityEngine;

[System.Serializable]
public struct SocialDWAMetrics
{
    public float bestCost;
    public float cpuMs;
    public int evaluatedCandidates;
    public int rejectedCandidates;
    public float selectedV;
    public float selectedOmega;
    public float minHumanDistance;
    public float minObstacleDistance;
}