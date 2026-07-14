using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controlador completo de navegación social para robot agrícola en Unity.
/// 
/// Incluye modos:
/// M0_NavMeshOnly
/// M1_ThresholdStop
/// M2_HysteresisSupervisor
/// M3_IsotropicProxemics
/// M4_FullAnisotropicHysteresis
/// B1_SocialDWA
/// B2_ORCA_RVO
/// B3_SocialForce
/// B4_CBF_SocialDWA
/// 
/// En B1_SocialDWA:
/// - Usa Point_A_R y Point_B_R.
/// - Envía automáticamente el objetivo actual al SocialDWAController.
/// - Cambia de objetivo si llega al punto o si lo pasa de largo.
/// - No modifica la lógica M0-M4.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RobotSocialNavController : MonoBehaviour
{
    public enum SocialMode
    {
        Patrol,
        AvoidOnly,
        ApproachNearestHuman,
        FollowNearestHuman,
        Auto
    }

    public enum ExperimentMode
    {
        M0_NavMeshOnly = 0,
        M1_ThresholdStop = 1,
        M2_HysteresisSupervisor = 2,
        M3_IsotropicProxemics = 3,
        M4_FullAnisotropicHysteresis = 4,
        B1_SocialDWA = 5,
        B2_ORCA_RVO = 6,
        B3_SocialForce = 7,
        B4_CBF_SocialDWA = 8
    }

    [Header("Modo principal")]
    public SocialMode mode = SocialMode.Auto;

    [Header("Modo experimental")]
    public ExperimentMode experimentMode = ExperimentMode.M4_FullAnisotropicHysteresis;

    [Header("Baseline externo")]
    private SocialDWAController socialDWAController;

    [Header("Baseline")]
    public float b2HumanRadius = 0.30f;
    public float b2SafetyMargin = 0.25f;
    public float b2NeighborDistance = 5.0f;
    public float b2TimeHorizon = 3.0f;
    public float b2OrcaGain = 1.25f;
    public float b2EmergencyGain = 2.5f;
    public float b2SidePreference = 1.0f; // +1 derecha, -1 izquierda

    [Header("Baseline B3 - Social Force")]
    public float b3RelaxationTime = 0.60f;
    public float b3HumanRadius = 0.30f;
    public float b3SocialForceGain = 2.25f;
    public float b3SocialForceRange = 0.85f;
    public float b3PersonalZoneBoost = 2.0f;
    public float b3EmergencyGain = 3.0f;

    [Header("Baseline B4 - CBF-Social-DWA")]
    public float b4PredictionTime = 2.0f;
    public float b4SimulationStep = 0.10f;
    public int b4VelocitySamples = 8;
    public int b4OmegaSamples = 17;
    public float b4OmegaMax = 1.20f;
    public float b4HeadingWeight = 0.50f;
    public float b4ClearanceWeight = 0.30f;
    public float b4VelocityWeight = 0.10f;
    public float b4SocialWeight = 0.10f;
    public float b4CbfSafeDistance = 1.20f;
    public float b4CbfGamma = 1.50f;
    public float b4CbfSteerGain = 2.50f;
    public float b4CbfInfluenceDistance = 2.20f;
    public bool b4UseHumanVelocity = true;

    [Header("Movimiento manual B4")]
    public float manualMaxLinearAcceleration = 2.0f;
    public float manualMaxAngularAcceleration = 3.0f;
    public float manualNavMeshSnapRadius = 0.80f;

    [Header("Patrulla global")]
    public Transform pointA_R;
    public Transform pointB_R;
    public float switchDistance = 2.0f;

    [Header("Detección de humanos")]
    public LayerMask humanMask;
    public float detectionRadius = 8f;
    public bool useClosestPoint = false;

    [Header("Zonas sociales")]
    public float intimateZone = 0.45f;
    public float personalZone = 1.20f;
    public float socialZone = 3.60f;

    [Header("Velocidades por zona")]
    public float nominalSpeed = 1.0f;
    public float slowSpeed = 0.45f;
    public float stopSpeed = 0.0f;

    [Header("Aproximación / seguimiento social")]
    public float dMin = 1.8f;

    [Range(-180f, 180f)]
    public float alphaDeg = -36f;

    public bool updateReferenceContinuously = true;

    [Header("Campo social anisotrópico")]
    public float sigmaX = 1.2f;
    public float sigmaY = 0.8f;
    public float avoidanceGain = 3.0f;
    public float avoidanceInfluenceDistance = 4.0f;

    [Header("Waypoint local para NavMesh")]
    public float localProbeDistance = 1.25f;
    public float navMeshSampleRadius = 1.5f;

    [Header("Auto social")]
    public bool stopInsideIntimateZone = true;
    public bool slowInsideSocialZone = true;

    [Header("Debug")]
    public bool drawDebug = true;
    public Color colorGlobal = Color.green;
    public Color colorAvoid = Color.red;
    public Color colorFinal = Color.cyan;

    private NavMeshAgent agent;
    private Transform patrolTarget;

    private readonly Collider[] humanHits = new Collider[64];
    private readonly List<Transform> detectedHumans = new List<Transform>();
    private readonly Dictionary<Transform, Vector3> humanPreviousPositions = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Vector3> humanEstimatedVelocities = new Dictionary<Transform, Vector3>();

    private Transform nearestHuman;
    private HumanKinematics nearestHumanKin;
    private float nearestHumanDistance = Mathf.Infinity;

    private Vector3 lastGlobalTarget;
    private Vector3 lastAvoidVector;
    private Vector3 lastFinalDirection;

    private bool wasUsingManualController = false;
    private bool b1PatrolInitialized = false;

    private float b1PreviousDistanceToTarget = Mathf.Infinity;
    private float b1MinimumDistanceToTarget = Mathf.Infinity;

    private float manualCurrentV = 0f;
    private float manualCurrentOmega = 0f;

    private struct DwaCandidate
    {
        public float v;
        public float omega;

        public DwaCandidate(float v, float omega)
        {
            this.v = v;
            this.omega = omega;
        }
    }

    public Transform NearestHuman => nearestHuman;
    public float NearestHumanDistance => nearestHumanDistance;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (socialDWAController == null)
        {
            socialDWAController = GetComponent<SocialDWAController>();
        }

        if (socialDWAController != null)
        {
            socialDWAController.enabled = false;
        }

        if (agent != null)
        {
            agent.speed = nominalSpeed;

            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning("RobotSocialNavController: el robot no arrancó exactamente sobre el NavMesh.");
            }
        }

        patrolTarget = pointB_R;

        if (!IsManualExperimentMode(experimentMode))
        {
            Vector3 initialTarget = GetCurrentGlobalTarget();
            SetNavmeshDestination(initialTarget);
        }

        ApplyControllerMode();
    }

    private void Update()
    {
        ApplyControllerMode();

        UpdateNearestHuman();

        if (experimentMode == ExperimentMode.B1_SocialDWA)
        {
            UpdateB1SocialDWAStandalone();
            return;
        }

        UpdatePatrolSwitch();

        switch (experimentMode)
        {
            case ExperimentMode.M0_NavMeshOnly:
                UpdateM0_NavMeshOnly();
                break;

            case ExperimentMode.M1_ThresholdStop:
                UpdateM1_ThresholdStop();
                break;

            case ExperimentMode.M2_HysteresisSupervisor:
                UpdateM2_HysteresisSupervisor();
                break;

            case ExperimentMode.M3_IsotropicProxemics:
                UpdateM3_IsotropicProxemics();
                break;

            case ExperimentMode.M4_FullAnisotropicHysteresis:
                UpdateM4_FullAnisotropicHysteresis();
                break;

            case ExperimentMode.B1_SocialDWA:
                UpdateB1SocialDWAStandalone();
                break;

            case ExperimentMode.B2_ORCA_RVO:
                UpdateB2_ORCA_RVO();
                break;

            case ExperimentMode.B3_SocialForce:
                UpdateB3_SocialForce();
                break;

            case ExperimentMode.B4_CBF_SocialDWA:
                UpdateB4_CBF_SocialDWA();
                break;
        }
    }

    private void ApplyControllerMode()
    {
        bool useB1 = experimentMode == ExperimentMode.B1_SocialDWA;
        bool useManual = IsManualExperimentMode(experimentMode);

        if (socialDWAController != null)
        {
            socialDWAController.enabled = useB1;

            if (useB1 && patrolTarget != null)
            {
                socialDWAController.SetGoal(patrolTarget);
                socialDWAController.currentGoal = patrolTarget;
            }
        }

        if (agent == null)
        {
            return;
        }

        if (useManual)
        {
            if (!wasUsingManualController)
            {
                b1PatrolInitialized = false;
                b1PreviousDistanceToTarget = Mathf.Infinity;
                b1MinimumDistanceToTarget = Mathf.Infinity;
                manualCurrentV = 0f;
                manualCurrentOmega = 0f;
            }

            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            wasUsingManualController = true;
        }
        else
        {
            agent.updatePosition = true;
            agent.updateRotation = true;

            if (wasUsingManualController)
            {
                agent.Warp(transform.position);
                agent.isStopped = false;
                agent.speed = nominalSpeed;
                SetNavmeshDestination(GetCurrentGlobalTarget());

                wasUsingManualController = false;
                b1PatrolInitialized = false;
                b1PreviousDistanceToTarget = Mathf.Infinity;
                b1MinimumDistanceToTarget = Mathf.Infinity;
                manualCurrentV = 0f;
                manualCurrentOmega = 0f;
            }
        }
    }

    private bool IsManualExperimentMode(ExperimentMode m)
    {
        return m == ExperimentMode.B1_SocialDWA || m == ExperimentMode.B4_CBF_SocialDWA;
    }

    private void UpdateB1SocialDWAStandalone()
    {
        if (socialDWAController == null)
        {
            Debug.LogWarning("B1_SocialDWA activo, pero SocialDWAController no está asignado.");
            return;
        }

        if (pointA_R == null || pointB_R == null)
        {
            Debug.LogWarning("B1_SocialDWA activo, pero Point_A_R o Point_B_R no están asignados.");
            return;
        }

        if (!b1PatrolInitialized || patrolTarget == null)
        {
            InitializeB1PatrolTarget();
            b1PatrolInitialized = true;

            b1PreviousDistanceToTarget = Mathf.Infinity;
            b1MinimumDistanceToTarget = Mathf.Infinity;
        }

        Vector3 robotXZ = ProjectXZ(transform.position);
        Vector3 targetXZ = ProjectXZ(patrolTarget.position);

        float distanceToTarget = Vector3.Distance(robotXZ, targetXZ);

        if (distanceToTarget < b1MinimumDistanceToTarget)
        {
            b1MinimumDistanceToTarget = distanceToTarget;
        }

        bool insideSwitchRadius = distanceToTarget <= switchDistance;

        bool passedTarget =
            b1MinimumDistanceToTarget <= switchDistance * 1.8f &&
            distanceToTarget > b1PreviousDistanceToTarget + 0.15f;

        bool shouldSwitch = insideSwitchRadius || passedTarget;

        if (shouldSwitch)
        {
            patrolTarget = patrolTarget == pointA_R ? pointB_R : pointA_R;

            Debug.Log(
                "B1_SocialDWA cambió objetivo a: " +
                patrolTarget.name +
                " | distancia actual=" + distanceToTarget.ToString("F2") +
                " | mínimo=" + b1MinimumDistanceToTarget.ToString("F2")
            );

            b1PreviousDistanceToTarget = Mathf.Infinity;
            b1MinimumDistanceToTarget = Mathf.Infinity;
        }
        else
        {
            b1PreviousDistanceToTarget = distanceToTarget;
        }

        socialDWAController.enabled = true;
        socialDWAController.SetGoal(patrolTarget);
        socialDWAController.currentGoal = patrolTarget;

        UpdateB1DetectedHumans();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        Vector3 globalTarget = patrolTarget.position;
        lastGlobalTarget = globalTarget;
        lastAvoidVector = Vector3.zero;

        Vector3 toTarget = globalTarget - transform.position;
        toTarget.y = 0f;
        lastFinalDirection = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : Vector3.zero;

        if (drawDebug)
        {
            Debug.DrawLine(
                transform.position + Vector3.up * 0.3f,
                patrolTarget.position + Vector3.up * 0.3f,
                Color.green
            );

            Debug.DrawRay(
                transform.position + Vector3.up * 0.4f,
                transform.forward * 2.0f,
                Color.cyan
            );
        }
    }

    private void InitializeB1PatrolTarget()
    {
        float dA = Vector3.Distance(
            ProjectXZ(transform.position),
            ProjectXZ(pointA_R.position)
        );

        float dB = Vector3.Distance(
            ProjectXZ(transform.position),
            ProjectXZ(pointB_R.position)
        );

        patrolTarget = dA <= dB ? pointB_R : pointA_R;

        Debug.Log(
            "B1_SocialDWA objetivo inicial: " +
            patrolTarget.name +
            " | dA=" + dA.ToString("F2") +
            " | dB=" + dB.ToString("F2")
        );
    }

    private void UpdateB1DetectedHumans()
    {
        if (socialDWAController == null)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            humanMask,
            QueryTriggerInteraction.Ignore
        );

        List<SocialDWAHuman> detectedHumans = new List<SocialDWAHuman>();

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            SocialDWAHuman human = hit.GetComponentInParent<SocialDWAHuman>();

            if (human != null && !detectedHumans.Contains(human))
            {
                detectedHumans.Add(human);
            }
        }

        socialDWAController.SetHumans(detectedHumans);
    }

    private void UpdateM0_NavMeshOnly()
    {
        Vector3 globalTarget = GetCurrentGlobalTarget();

        agent.isStopped = false;
        agent.speed = nominalSpeed;

        SetNavmeshDestination(globalTarget);

        lastGlobalTarget = globalTarget;
        lastAvoidVector = Vector3.zero;
        lastFinalDirection = SafeDirection(globalTarget - transform.position);
    }

    private void UpdateM1_ThresholdStop()
    {
        Vector3 globalTarget = GetCurrentGlobalTarget();

        if (nearestHuman != null && nearestHumanDistance <= personalZone)
        {
            agent.speed = stopSpeed;
            agent.isStopped = true;

            lastGlobalTarget = globalTarget;
            lastAvoidVector = Vector3.zero;
            lastFinalDirection = Vector3.zero;
            return;
        }

        agent.isStopped = false;
        agent.speed = nominalSpeed;

        SetNavmeshDestination(globalTarget);

        lastGlobalTarget = globalTarget;
        lastAvoidVector = Vector3.zero;
        lastFinalDirection = SafeDirection(globalTarget - transform.position);
    }

    private void UpdateM2_HysteresisSupervisor()
    {
        Vector3 globalTarget = GetCurrentGlobalTarget();

        if (nearestHuman != null)
        {
            if (!agent.isStopped && nearestHumanDistance <= personalZone)
            {
                agent.speed = stopSpeed;
                agent.isStopped = true;
            }
            else if (agent.isStopped && nearestHumanDistance >= socialZone)
            {
                agent.isStopped = false;
                agent.speed = nominalSpeed;
            }
        }
        else
        {
            agent.isStopped = false;
            agent.speed = nominalSpeed;
        }

        if (!agent.isStopped)
        {
            SetNavmeshDestination(globalTarget);
            lastFinalDirection = SafeDirection(globalTarget - transform.position);
        }
        else
        {
            lastFinalDirection = Vector3.zero;
        }

        lastGlobalTarget = globalTarget;
        lastAvoidVector = Vector3.zero;
    }

    private void UpdateM3_IsotropicProxemics()
    {
        Vector3 globalTarget = GetCurrentGlobalTarget();

        Vector3 robotPos = transform.position;
        Vector3 toTarget = globalTarget - robotPos;
        toTarget.y = 0f;

        Vector3 globalDir = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : Vector3.zero;
        Vector3 avoidDir = ComputeIsotropicAvoidanceVector();
        Vector3 finalDir = SafeDirection(globalDir + avoidDir);

        if (nearestHuman != null && nearestHumanDistance <= intimateZone)
        {
            agent.speed = stopSpeed;
            agent.isStopped = true;
        }
        else
        {
            agent.isStopped = false;

            if (nearestHuman != null && nearestHumanDistance <= socialZone)
            {
                agent.speed = slowSpeed;
            }
            else
            {
                agent.speed = nominalSpeed;
            }
        }

        if (!agent.isStopped && finalDir.sqrMagnitude > 1e-8f)
        {
            Vector3 localWaypoint = robotPos + finalDir.normalized * localProbeDistance;
            SetNavmeshDestination(localWaypoint);
        }

        lastGlobalTarget = globalTarget;
        lastAvoidVector = avoidDir;
        lastFinalDirection = finalDir;
    }

    private void UpdateM4_FullAnisotropicHysteresis()
    {
        Vector3 globalTarget = GetCurrentGlobalTarget();

        Vector3 robotPos = transform.position;
        Vector3 toTarget = globalTarget - robotPos;
        toTarget.y = 0f;

        Vector3 globalDir = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : Vector3.zero;
        Vector3 avoidDir = ComputeSocialAvoidanceVector();
        Vector3 finalDir = ComputeFinalDirection(globalDir, avoidDir);

        UpdateSocialSpeed();

        if (finalDir.sqrMagnitude > 1e-8f)
        {
            Vector3 localWaypoint = robotPos + finalDir.normalized * localProbeDistance;
            SetNavmeshDestination(localWaypoint);
        }
        else
        {
            SetNavmeshDestination(globalTarget);
        }

        lastGlobalTarget = globalTarget;
        lastAvoidVector = avoidDir;
        lastFinalDirection = finalDir;
    }


    private void UpdateB2_ORCA_RVO()
    {
        Vector3 globalTarget = GetCurrentGlobalTarget();
        Vector3 localWaypoint = GetLocalWaypointTowards(globalTarget, Mathf.Max(localProbeDistance, 2.0f));

        Vector3 desiredDir = SafeDirection(localWaypoint - transform.position);
        Vector3 desiredVel = desiredDir * nominalSpeed;
        Vector3 safeVel = desiredVel;

        for (int i = 0; i < detectedHumans.Count; i++)
        {
            Transform h = detectedHumans[i];
            if (h == null)
            {
                continue;
            }

            Vector3 robotPos = ProjectXZ(transform.position);
            Vector3 humanPos = ProjectXZ(h.position);
            Vector3 relPos = humanPos - robotPos;
            float d = relPos.magnitude;

            if (d < 1e-5f || d > b2NeighborDistance)
            {
                continue;
            }

            Vector3 humanVel = GetEstimatedHumanVelocity(h);
            Vector3 relVel = safeVel - humanVel;

            float combinedRadius = 0.30f + b2HumanRadius + b2SafetyMargin;
            Vector3 away = -relPos.normalized;

            if (d < combinedRadius)
            {
                float penetration = Mathf.Clamp01((combinedRadius - d) / Mathf.Max(combinedRadius, 1e-5f));
                safeVel += away * b2EmergencyGain * nominalSpeed * (0.5f + penetration);
                continue;
            }

            float ttc;
            float closestDistance;
            bool risk = ComputeVelocityObstacleRisk(relPos, relVel, combinedRadius, b2TimeHorizon, out ttc, out closestDistance);

            if (risk)
            {
                Vector3 lateral = ChooseB2PassingSide(relPos, desiredDir);
                float timeRisk = Mathf.Clamp01((b2TimeHorizon - ttc) / Mathf.Max(b2TimeHorizon, 1e-5f));
                float distanceRisk = Mathf.Clamp01((combinedRadius - closestDistance) / Mathf.Max(combinedRadius, 1e-5f));
                float riskLevel = Mathf.Clamp01(0.55f * timeRisk + 0.45f * distanceRisk);

                safeVel += (0.35f * away + 0.65f * lateral) * b2OrcaGain * nominalSpeed * riskLevel;
            }
            else if (d < personalZone)
            {
                float proximity = Mathf.Clamp01((personalZone - d) / Mathf.Max(personalZone, 1e-5f));
                safeVel += away * 0.65f * nominalSpeed * proximity;
            }
        }

        if (safeVel.magnitude > nominalSpeed)
        {
            safeVel = safeVel.normalized * nominalSpeed;
        }

        ApplyVelocityViaNavMesh(safeVel);

        lastGlobalTarget = globalTarget;
        lastAvoidVector = safeVel - desiredVel;
        lastFinalDirection = safeVel.sqrMagnitude > 1e-8f ? safeVel.normalized : desiredDir;

        if (drawDebug)
        {
            Debug.DrawRay(transform.position + Vector3.up * 0.30f, desiredVel, Color.cyan);
            Debug.DrawRay(transform.position + Vector3.up * 0.45f, safeVel, Color.green);
        }
    }

    private void UpdateB3_SocialForce()
    {
        Vector3 globalTarget = GetCurrentGlobalTarget();
        Vector3 localWaypoint = GetLocalWaypointTowards(globalTarget, Mathf.Max(localProbeDistance, 2.0f));

        Vector3 desiredDir = SafeDirection(localWaypoint - transform.position);
        Vector3 desiredVelocity = desiredDir * nominalSpeed;
        Vector3 currentVelocity = agent != null ? ProjectXZ(agent.velocity) : Vector3.zero;

        Vector3 force = (desiredVelocity - currentVelocity) / Mathf.Max(b3RelaxationTime, 1e-5f);

        for (int i = 0; i < detectedHumans.Count; i++)
        {
            Transform h = detectedHumans[i];
            if (h == null)
            {
                continue;
            }

            Vector3 away = ProjectXZ(transform.position - h.position);
            float d = away.magnitude;

            if (d < 1e-5f || d > socialZone)
            {
                continue;
            }

            Vector3 n = away.normalized;
            float combinedRadius = 0.30f + b3HumanRadius;
            float magnitude = b3SocialForceGain * Mathf.Exp((combinedRadius - d) / Mathf.Max(b3SocialForceRange, 1e-5f));

            if (d < personalZone)
            {
                float boost = 1.0f + b3PersonalZoneBoost * Mathf.Clamp01((personalZone - d) / Mathf.Max(personalZone, 1e-5f));
                magnitude *= boost;
            }

            if (d < intimateZone)
            {
                magnitude += b3EmergencyGain;
            }

            force += n * magnitude;
        }

        Vector3 targetVelocity = currentVelocity + force * Time.deltaTime;

        if (targetVelocity.magnitude > nominalSpeed)
        {
            targetVelocity = targetVelocity.normalized * nominalSpeed;
        }

        ApplyVelocityViaNavMesh(targetVelocity);

        lastGlobalTarget = globalTarget;
        lastAvoidVector = force;
        lastFinalDirection = targetVelocity.sqrMagnitude > 1e-8f ? targetVelocity.normalized : desiredDir;

        if (drawDebug)
        {
            Debug.DrawRay(transform.position + Vector3.up * 0.30f, desiredVelocity, Color.cyan);
            Debug.DrawRay(transform.position + Vector3.up * 0.45f, force, Color.magenta);
        }
    }

    private void UpdateB4_CBF_SocialDWA()
    {
        if (pointA_R == null || pointB_R == null)
        {
            Debug.LogWarning("B4_CBF_SocialDWA activo, pero Point_A_R o Point_B_R no están asignados.");
            return;
        }

        Vector3 globalTarget = GetCurrentGlobalTarget();
        Vector3 localWaypoint = GetLocalWaypointTowards(globalTarget, Mathf.Max(localProbeDistance * 2.0f, 2.0f));

        DwaCandidate nominal = SelectB4DwaCandidate(localWaypoint);
        DwaCandidate safe = ApplyB4CbfFilter(nominal);

        ApplyManualUnicycleControl(safe.v, safe.omega);

        lastGlobalTarget = globalTarget;
        lastAvoidVector = Vector3.zero;
        lastFinalDirection = ProjectXZ(transform.forward).normalized;

        if (drawDebug)
        {
            Debug.DrawLine(transform.position + Vector3.up * 0.35f, localWaypoint + Vector3.up * 0.35f, Color.yellow);
            Debug.DrawRay(transform.position + Vector3.up * 0.45f, transform.forward * safe.v, Color.green);
        }
    }

    private void ApplyVelocityViaNavMesh(Vector3 velocity)
    {
        if (agent == null)
        {
            return;
        }

        velocity.y = 0f;
        float speed = Mathf.Clamp(velocity.magnitude, 0f, nominalSpeed);

        if (speed <= 1e-3f)
        {
            agent.speed = 0f;
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        agent.speed = speed;

        Vector3 localWaypoint = transform.position + velocity.normalized * localProbeDistance;
        SetNavmeshDestination(localWaypoint);
    }

    private Vector3 GetLocalWaypointTowards(Vector3 globalTarget, float lookAhead)
    {
        if (agent == null)
        {
            return globalTarget;
        }

        NavMeshPath path = new NavMeshPath();

        if (NavMesh.CalculatePath(transform.position, globalTarget, NavMesh.AllAreas, path) &&
            path.corners != null &&
            path.corners.Length >= 2)
        {
            Vector3 p0 = transform.position;
            float remaining = Mathf.Max(0.25f, lookAhead);

            for (int i = 1; i < path.corners.Length; i++)
            {
                Vector3 p1 = path.corners[i];
                Vector3 segment = p1 - p0;
                segment.y = 0f;

                float length = segment.magnitude;
                if (length < 1e-5f)
                {
                    p0 = p1;
                    continue;
                }

                if (length >= remaining)
                {
                    Vector3 candidate = p0 + segment.normalized * remaining;
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(candidate, out hit, navMeshSampleRadius, NavMesh.AllAreas))
                    {
                        return hit.position;
                    }

                    return candidate;
                }

                remaining -= length;
                p0 = p1;
            }
        }

        NavMeshHit fallbackHit;
        if (NavMesh.SamplePosition(globalTarget, out fallbackHit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            return fallbackHit.position;
        }

        return globalTarget;
    }

    private bool ComputeVelocityObstacleRisk(
        Vector3 relPos,
        Vector3 relVel,
        float radius,
        float horizon,
        out float ttc,
        out float closestDistance)
    {
        ttc = Mathf.Infinity;
        closestDistance = relPos.magnitude;

        float relSpeed2 = Vector3.Dot(relVel, relVel);

        if (relSpeed2 < 1e-8f)
        {
            return false;
        }

        ttc = -Vector3.Dot(relPos, relVel) / relSpeed2;

        if (ttc < 0f || ttc > horizon)
        {
            return false;
        }

        Vector3 closest = relPos + relVel * ttc;
        closestDistance = closest.magnitude;

        return closestDistance < radius;
    }

    private Vector3 ChooseB2PassingSide(Vector3 relPos, Vector3 desiredDir)
    {
        if (desiredDir.sqrMagnitude < 1e-8f)
        {
            desiredDir = transform.forward;
        }

        float crossY = Vector3.Cross(desiredDir.normalized, relPos.normalized).y;
        float sign = Mathf.Abs(crossY) > 0.05f ? Mathf.Sign(crossY) : Mathf.Sign(b2SidePreference);

        return Rotate90(relPos.normalized, -sign).normalized;
    }

    private DwaCandidate SelectB4DwaCandidate(Vector3 localWaypoint)
    {
        int nv = Mathf.Max(2, b4VelocitySamples);
        int nw = Mathf.Max(3, b4OmegaSamples);

        float bestCost = Mathf.Infinity;
        DwaCandidate best = new DwaCandidate(0f, 0f);

        for (int i = 0; i < nv; i++)
        {
            float av = (float)i / (nv - 1);
            float v = Mathf.Lerp(0f, nominalSpeed, av);

            for (int j = 0; j < nw; j++)
            {
                float aw = (float)j / (nw - 1);
                float omega = Mathf.Lerp(-b4OmegaMax, b4OmegaMax, aw);

                float cost = EvaluateB4Candidate(v, omega, localWaypoint);

                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = new DwaCandidate(v, omega);
                }
            }
        }

        return best;
    }

    private float EvaluateB4Candidate(float v, float omega, Vector3 localWaypoint)
    {
        int steps = Mathf.Max(1, Mathf.RoundToInt(b4PredictionTime / Mathf.Max(b4SimulationStep, 1e-4f)));

        Vector3 pos = transform.position;
        float yawRad = transform.eulerAngles.y * Mathf.Deg2Rad;

        float minHumanDistance = Mathf.Infinity;
        float socialCost = 0f;
        float clearancePenalty = 0f;

        for (int k = 0; k < steps; k++)
        {
            yawRad = NormalizeAngleRad(yawRad + omega * b4SimulationStep);
            Vector3 fwd = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
            pos += fwd * v * b4SimulationStep;

            NavMeshHit navHit;
            bool onNavMesh = NavMesh.SamplePosition(pos, out navHit, Mathf.Max(0.20f, navMeshSampleRadius), NavMesh.AllAreas);
            if (!onNavMesh)
            {
                clearancePenalty += 10.0f;
            }

            for (int i = 0; i < detectedHumans.Count; i++)
            {
                Transform h = detectedHumans[i];
                if (h == null)
                {
                    continue;
                }

                float d = Vector3.Distance(ProjectXZ(pos), ProjectXZ(h.position));
                minHumanDistance = Mathf.Min(minHumanDistance, d);

                if (d < intimateZone)
                {
                    socialCost += 20.0f;
                }
                else if (d < personalZone)
                {
                    socialCost += 1.0f - d / Mathf.Max(personalZone, 1e-5f);
                }
                else if (d < socialZone)
                {
                    socialCost += 0.15f * (1.0f - d / Mathf.Max(socialZone, 1e-5f));
                }
            }
        }

        Vector3 toGoal = ProjectXZ(localWaypoint - pos);
        float distanceCost = Mathf.Clamp01(toGoal.magnitude / Mathf.Max(localProbeDistance * 2.0f, 1.0f));

        float headingCost = 0f;
        if (toGoal.sqrMagnitude > 1e-8f)
        {
            Vector3 finalForward = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
            headingCost = Vector3.Angle(finalForward, toGoal.normalized) / 180f;
        }

        float goalCost = 0.60f * distanceCost + 0.40f * headingCost;
        float velocityCost = 1.0f - Mathf.Clamp01(v / Mathf.Max(nominalSpeed, 1e-5f));
        float clearanceCost = clearancePenalty;

        if (!float.IsInfinity(minHumanDistance))
        {
            clearanceCost += 1.0f / Mathf.Max(minHumanDistance, 0.05f);
        }

        return
            b4HeadingWeight * goalCost +
            b4ClearanceWeight * clearanceCost +
            b4VelocityWeight * velocityCost +
            b4SocialWeight * socialCost;
    }

    private DwaCandidate ApplyB4CbfFilter(DwaCandidate nominal)
    {
        float safeV = Mathf.Clamp(nominal.v, 0f, nominalSpeed);
        float safeOmega = Mathf.Clamp(nominal.omega, -b4OmegaMax, b4OmegaMax);

        Vector3 robotPos = ProjectXZ(transform.position);
        Vector3 forward = ProjectXZ(transform.forward);

        if (forward.sqrMagnitude < 1e-8f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        float dSafe = Mathf.Max(b4CbfSafeDistance, intimateZone + 0.05f);

        for (int i = 0; i < detectedHumans.Count; i++)
        {
            Transform h = detectedHumans[i];
            if (h == null)
            {
                continue;
            }

            Vector3 humanPos = ProjectXZ(h.position);
            Vector3 r = robotPos - humanPos;
            float d = r.magnitude;

            if (d < 1e-5f || d > b4CbfInfluenceDistance)
            {
                continue;
            }

            Vector3 vHuman = b4UseHumanVelocity ? GetEstimatedHumanVelocity(h) : Vector3.zero;
            float hValue = d * d - dSafe * dSafe;

            float a = 2.0f * Vector3.Dot(r, forward);
            float b = 2.0f * Vector3.Dot(r, vHuman) - b4CbfGamma * hValue;

            if (Mathf.Abs(a) > 1e-5f)
            {
                float bound = b / a;

                if (a < 0f)
                {
                    safeV = Mathf.Min(safeV, Mathf.Max(0f, bound));
                }
                else if (hValue < 0f)
                {
                    safeV = Mathf.Max(safeV, Mathf.Min(nominalSpeed, bound));
                }
            }

            float proximity = Mathf.Clamp01((b4CbfInfluenceDistance - d) / Mathf.Max(b4CbfInfluenceDistance - dSafe, 1e-5f));
            Vector3 away = r.normalized;
            float steerSign = Mathf.Sign(Vector3.SignedAngle(forward, away, Vector3.up));

            if (Mathf.Abs(steerSign) < 0.1f)
            {
                steerSign = 1.0f;
            }

            safeOmega += steerSign * b4CbfSteerGain * proximity;

            if (d < dSafe)
            {
                safeV = Mathf.Min(safeV, nominalSpeed * 0.15f);
            }
        }

        safeV = Mathf.Clamp(safeV, 0f, nominalSpeed);
        safeOmega = Mathf.Clamp(safeOmega, -b4OmegaMax, b4OmegaMax);

        return new DwaCandidate(safeV, safeOmega);
    }

    private void ApplyManualUnicycleControl(float targetV, float targetOmega)
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);

        manualCurrentV = Mathf.MoveTowards(
            manualCurrentV,
            targetV,
            Mathf.Max(0.01f, manualMaxLinearAcceleration) * dt
        );

        manualCurrentOmega = Mathf.MoveTowards(
            manualCurrentOmega,
            targetOmega,
            Mathf.Max(0.01f, manualMaxAngularAcceleration) * dt
        );

        transform.Rotate(0f, manualCurrentOmega * Mathf.Rad2Deg * dt, 0f, Space.World);

        Vector3 nextPos = transform.position + transform.forward * manualCurrentV * dt;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(nextPos, out hit, manualNavMeshSnapRadius, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        else
        {
            transform.position = nextPos;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(transform.position);
        }
    }

    private Vector3 GetEstimatedHumanVelocity(Transform h)
    {
        if (h == null)
        {
            return Vector3.zero;
        }

        Vector3 v;
        if (humanEstimatedVelocities.TryGetValue(h, out v))
        {
            return v;
        }

        return Vector3.zero;
    }

    private static Vector3 Rotate90(Vector3 v, float sign)
    {
        Vector3 r = new Vector3(-v.z, 0f, v.x);

        if (sign < 0f)
        {
            r = -r;
        }

        return r;
    }

    private static float NormalizeAngleRad(float angle)
    {
        while (angle > Mathf.PI)
        {
            angle -= 2f * Mathf.PI;
        }

        while (angle < -Mathf.PI)
        {
            angle += 2f * Mathf.PI;
        }

        return angle;
    }

    private void UpdateNearestHuman()
    {
        nearestHuman = null;
        nearestHumanKin = null;
        nearestHumanDistance = Mathf.Infinity;
        detectedHumans.Clear();

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRadius,
            humanHits,
            humanMask,
            QueryTriggerInteraction.Ignore
        );

        float dt = Mathf.Max(Time.deltaTime, 1e-4f);

        for (int i = 0; i < count; i++)
        {
            Collider c = humanHits[i];

            if (c == null)
            {
                continue;
            }

            Transform root = c.transform.root;

            if (root == transform)
            {
                continue;
            }

            if (!detectedHumans.Contains(root))
            {
                detectedHumans.Add(root);

                Vector3 currentHumanPos = ProjectXZ(root.position);
                Vector3 previousHumanPos;

                if (humanPreviousPositions.TryGetValue(root, out previousHumanPos))
                {
                    humanEstimatedVelocities[root] = (currentHumanPos - previousHumanPos) / dt;
                }
                else
                {
                    humanEstimatedVelocities[root] = Vector3.zero;
                }

                humanPreviousPositions[root] = currentHumanPos;
            }

            Vector3 refPoint = useClosestPoint ? c.ClosestPoint(transform.position) : root.position;
            float d = Vector3.Distance(ProjectXZ(transform.position), ProjectXZ(refPoint));

            if (d < nearestHumanDistance)
            {
                nearestHumanDistance = d;
                nearestHuman = root;
                nearestHumanKin = root.GetComponent<HumanKinematics>();
            }
        }
    }

    private void UpdatePatrolSwitch()
    {
        if (pointA_R == null || pointB_R == null)
        {
            return;
        }

        if (patrolTarget == null)
        {
            patrolTarget = pointB_R;
        }

        Vector3 robotPos = ProjectXZ(transform.position);
        Vector3 patrolPos = ProjectXZ(patrolTarget.position);

        if (Vector3.Distance(robotPos, patrolPos) <= switchDistance)
        {
            patrolTarget = patrolTarget == pointA_R ? pointB_R : pointA_R;
        }
    }

    private Vector3 GetCurrentGlobalTarget()
    {
        if (mode == SocialMode.Patrol || mode == SocialMode.AvoidOnly || mode == SocialMode.Auto)
        {
            if (patrolTarget != null)
            {
                return patrolTarget.position;
            }

            return transform.position;
        }

        if (nearestHuman == null)
        {
            if (patrolTarget != null)
            {
                return patrolTarget.position;
            }

            return transform.position;
        }

        return ComputeSocialReferencePoint(nearestHuman, nearestHumanKin);
    }

    private Vector3 ComputeSocialReferencePoint(Transform human, HumanKinematics kin)
    {
        Vector3 hPos = human.position;
        float thetaH = GetHumanHeadingRad(human, kin);
        float alpha = alphaDeg * Mathf.Deg2Rad;

        float xd = hPos.x - dMin * Mathf.Cos(alpha + thetaH);
        float zd = hPos.z - dMin * Mathf.Sin(alpha + thetaH);

        return new Vector3(xd, transform.position.y, zd);
    }

    private float GetHumanHeadingRad(Transform human, HumanKinematics kin)
    {
        if (kin != null)
        {
            return kin.HeadingRad;
        }

        Vector3 fwd = human.forward;
        fwd.y = 0f;

        if (fwd.sqrMagnitude < 1e-8f)
        {
            fwd = Vector3.forward;
        }

        fwd.Normalize();

        return Mathf.Atan2(fwd.z, fwd.x);
    }

    private Vector3 ComputeIsotropicAvoidanceVector()
    {
        if (nearestHuman == null)
        {
            return Vector3.zero;
        }

        if (nearestHumanDistance > avoidanceInfluenceDistance)
        {
            return Vector3.zero;
        }

        Vector3 r = ProjectXZ(transform.position);
        Vector3 h = ProjectXZ(nearestHuman.position);

        Vector3 away = r - h;

        if (away.sqrMagnitude < 1e-8f)
        {
            return Vector3.zero;
        }

        float d = Mathf.Max(nearestHumanDistance, 1e-5f);
        float exponent = -(d * d) / Mathf.Max(1e-5f, sigmaX * sigmaX);
        float F = Mathf.Exp(exponent);

        float gain = avoidanceGain * F;

        if (nearestHumanDistance <= personalZone)
        {
            gain *= 2.0f;
        }

        if (nearestHumanDistance <= intimateZone)
        {
            gain *= 4.0f;
        }

        return away.normalized * gain;
    }

    private Vector3 ComputeSocialAvoidanceVector()
    {
        if (nearestHuman == null)
        {
            return Vector3.zero;
        }

        if (nearestHumanDistance > avoidanceInfluenceDistance)
        {
            return Vector3.zero;
        }

        Vector3 r = ProjectXZ(transform.position);
        Vector3 h = ProjectXZ(nearestHuman.position);

        float xr = r.x;
        float yr = r.z;
        float xh = h.x;
        float yh = h.z;

        float thetaH = GetHumanHeadingRad(nearestHuman, nearestHumanKin);

        float d = Mathf.Sqrt((xr - xh) * (xr - xh) + (yr - yh) * (yr - yh));

        if (d < 1e-5f)
        {
            d = 1e-5f;
        }

        float beta = Mathf.Atan2((yr - yh), (xr - xh));
        float delta = beta - thetaH;

        float cosTerm = Mathf.Cos(delta);
        float sinTerm = Mathf.Sin(delta);

        float exponent =
            -d * d *
            (
                (cosTerm * cosTerm) / Mathf.Max(1e-5f, sigmaX * sigmaX) +
                (sinTerm * sinTerm) / Mathf.Max(1e-5f, sigmaY * sigmaY)
            );

        float F = Mathf.Exp(exponent);

        Vector3 away = (r - h).normalized;
        float gain = avoidanceGain * F;

        if (nearestHumanDistance <= personalZone)
        {
            gain *= 2.0f;
        }

        if (nearestHumanDistance <= intimateZone)
        {
            gain *= 4.0f;
        }

        return away * gain;
    }

    private Vector3 ComputeFinalDirection(Vector3 globalDir, Vector3 avoidDir)
    {
        Vector3 finalDir = Vector3.zero;

        switch (mode)
        {
            case SocialMode.Patrol:
                finalDir = globalDir;
                break;

            case SocialMode.AvoidOnly:
            case SocialMode.ApproachNearestHuman:
            case SocialMode.FollowNearestHuman:
            case SocialMode.Auto:
                finalDir = SafeDirection(globalDir + avoidDir);
                break;
        }

        if (float.IsNaN(finalDir.x) || float.IsNaN(finalDir.y) || float.IsNaN(finalDir.z))
        {
            return Vector3.zero;
        }

        return finalDir;
    }

    private void UpdateSocialSpeed()
    {
        if (agent == null)
        {
            return;
        }

        float speedToApply = nominalSpeed;

        if (nearestHuman != null)
        {
            if (stopInsideIntimateZone && nearestHumanDistance <= intimateZone)
            {
                speedToApply = stopSpeed;
            }
            else if (nearestHumanDistance <= personalZone)
            {
                speedToApply = slowSpeed;
            }
            else if (slowInsideSocialZone && nearestHumanDistance <= socialZone)
            {
                speedToApply = Mathf.Min(nominalSpeed, slowSpeed * 1.35f);
            }
        }

        agent.speed = speedToApply;
        agent.isStopped = speedToApply <= 1e-3f;
    }

    private void SetNavmeshDestination(Vector3 desiredPoint)
    {
        if (agent == null)
        {
            return;
        }

        if (IsManualExperimentMode(experimentMode))
        {
            return;
        }

        NavMeshHit hit;

        if (NavMesh.SamplePosition(desiredPoint, out hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private static Vector3 ProjectXZ(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    private static Vector3 SafeDirection(Vector3 v)
    {
        v.y = 0f;

        if (v.sqrMagnitude < 1e-8f)
        {
            return Vector3.zero;
        }

        return v.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, detectionRadius);

        Gizmos.color = new Color(1f, 0f, 0f, 0.45f);
        Gizmos.DrawWireSphere(p, intimateZone);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.45f);
        Gizmos.DrawWireSphere(p, personalZone);

        Gizmos.color = new Color(0f, 1f, 1f, 0.45f);
        Gizmos.DrawWireSphere(p, socialZone);

        Gizmos.color = colorGlobal;
        Gizmos.DrawLine(p + Vector3.up * 0.4f, lastGlobalTarget + Vector3.up * 0.4f);

        Gizmos.color = colorAvoid;
        Gizmos.DrawLine(p + Vector3.up * 0.6f, p + Vector3.up * 0.6f + lastAvoidVector);

        Gizmos.color = colorFinal;
        Gizmos.DrawLine(p + Vector3.up * 0.8f, p + Vector3.up * 0.8f + lastFinalDirection * 2f);

        if (nearestHuman != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(p, nearestHuman.position);
        }
    }
}