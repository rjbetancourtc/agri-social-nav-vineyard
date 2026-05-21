using UnityEngine;

[CreateAssetMenu(fileName = "SocialDWAConfig", menuName = "Navigation/Social DWA Config")]
public class SocialDWAConfig : ScriptableObject
{
    [Header("Robot limits")]
    public float vMin = 0.0f;
    public float vMax = 1.0f;
    public float omegaMax = 1.2f;          // rad/s
    public float aVMax = 0.8f;             // m/s^2
    public float aOmegaMax = 1.5f;         // rad/s^2

    [Header("DWA sampling")]
    public int velocitySamples = 10;
    public int omegaSamples = 21;
    public float predictionTime = 2.5f;    // s
    public float simulationStep = 0.1f;    // s
    public float localWaypointLookAhead = 3.0f; // m

    [Header("Proxemic zones")]
    public float dIntimate = 0.45f;
    public float dPersonal = 1.20f;
    public float dSocial = 3.60f;

    [Header("Anisotropic social field")]
    public float sigmaX = 1.50f;           // frontal axis, m
    public float sigmaY = 0.90f;           // lateral axis, m

    [Header("Collision / validity")]
    public float robotRadius = 0.35f;
    public float obstacleSafetyMargin = 0.15f;
    public LayerMask obstacleMask;
    public bool rejectOffNavMesh = true;
    public float navMeshSampleRadius = 0.25f;

    [Header("Cost weights")]
    public float wGoal = 1.0f;
    public float wVelocity = 0.3f;
    public float wSmooth = 0.5f;
    public float wProxemic = 2.0f;
    public float wAnisotropic = 2.0f;
    public float wObstacle = 3.0f;

    [Header("Telemetry")]
    public bool logCpuTime = true;
}