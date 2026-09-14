using UnityEngine;

// ================================================================
// CONFIGURACION B1 - SOCIAL DWA
// ================================================================

[CreateAssetMenu(
    fileName = "B1_SocialDWA_Config",
    menuName = "Social Navigation/B1 Social DWA Config"
)]
public class B1_SocialDWA_Config : ScriptableObject
{
    // ============================================================
    // LIMITES CINEMATICOS
    // ============================================================

    [Header("Velocity limits")]

    public float vMin = 0.0f;

    public float vMax = 1.0f;

    public float omegaMax = 1.2f;

    public float aVMax = 0.8f;

    public float aOmegaMax = 1.5f;


    // ============================================================
    // MUESTREO DWA
    // ============================================================

    [Header("DWA Sampling")]

    public int velocitySamples = 10;

    public int omegaSamples = 21;

    public float predictionTime = 2.5f;

    public float simulationStep = 0.1f;

    public float localWaypointLookAhead = 3.0f;


    // ============================================================
    // PROXEMICA
    // ============================================================

    [Header("Proxemics")]

    public float dIntimate = 0.45f;

    public float dPersonal = 1.2f;

    public float dSocial = 3.6f;

    public float sigmaX = 1.5f;

    public float sigmaY = 0.9f;


    // ============================================================
    // ROBOT / OBSTACULOS / NAVMESH
    // ============================================================

    [Header("Robot and obstacles")]

    public float robotRadius = 0.35f;

    public float obstacleSafetyMargin = 0.15f;

    // Primera prueba: false.
    public bool rejectOffNavMesh = false;

    public float navMeshSampleRadius = 0.5f;


    // ============================================================
    // PESOS DE COSTE
    // ============================================================

    [Header("Cost weights")]

    public float wGoal = 1.0f;

    public float wVelocity = 0.3f;

    public float wSmooth = 0.5f;

    public float wProxemic = 2.0f;

    public float wAnisotropic = 2.0f;

    // Primera prueba: 0.
    public float wObstacle = 0.0f;
}