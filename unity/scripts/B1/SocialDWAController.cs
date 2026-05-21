using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// B1 = Social-DWA baseline.
/// 
/// Arquitectura:
/// NavMesh global -> waypoint local -> Social-DWA -> comando (v, omega)
/// 
/// El NavMesh se usa como guía global. El movimiento real lo aplica este script
/// directamente sobre el Transform del robot.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SocialDWAController : MonoBehaviour
{
    [Header("Configuration")]
    public SocialDWAConfig config;

    [Header("Mission")]
    public Transform currentGoal;
    public bool autoDisableNavMeshAgentMotion = true;

    [Header("Humans")]
    public List<SocialDWAHuman> humans = new List<SocialDWAHuman>();

    [Header("Debug")]
    public bool drawCandidateTrajectories = false;
    public bool drawBestTrajectory = true;
    public bool debugConsole = true;

    public Color rejectedColor = new Color(1f, 0.2f, 0.2f, 0.25f);
    public Color candidateColor = new Color(0.2f, 0.6f, 1f, 0.15f);
    public Color bestColor = Color.green;

    public SocialDWAMetrics LastMetrics => lastMetrics;

    private NavMeshAgent agent;

    private float currentV = 0f;
    private float currentOmega = 0f;

    private SocialDWAMetrics lastMetrics;
    private readonly List<Vector3> bestTrajectory = new List<Vector3>();

    private const float EPS = 1e-5f;

    private struct Candidate
    {
        public float v;
        public float omega;

        public Candidate(float v, float omega)
        {
            this.v = v;
            this.omega = omega;
        }
    }

    private struct Pose2D
    {
        public Vector3 position;
        public float yawRad;

        public Pose2D(Vector3 position, float yawRad)
        {
            this.position = position;
            this.yawRad = yawRad;
        }
    }

    private struct TrajectoryEvaluation
    {
        public float cost;
        public bool rejected;
        public float minHumanDistance;
        public float minObstacleDistance;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null && autoDisableNavMeshAgentMotion)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.isStopped = true;
        }
    }

    private void FixedUpdate()
    {
        if (config == null || currentGoal == null)
        {
            return;
        }

        RunSocialDWA();
    }

    public void SetGoal(Transform goal)
    {
        currentGoal = goal;
    }

    public void SetHumans(List<SocialDWAHuman> humanList)
    {
        humans = humanList;
    }

    private void RunSocialDWA()
    {
        Stopwatch sw = null;

        if (config.logCpuTime)
        {
            sw = Stopwatch.StartNew();
        }

        Vector3 localWaypoint = GetLocalWaypointFromNavMesh();

        List<Candidate> candidates = GenerateCandidates();

        float bestCost = float.PositiveInfinity;
        Candidate best = new Candidate(0f, 0f);

        int evaluated = 0;
        int rejected = 0;

        float bestMinHumanDistance = float.PositiveInfinity;
        float bestMinObstacleDistance = float.PositiveInfinity;

        bestTrajectory.Clear();

        foreach (Candidate candidate in candidates)
        {
            List<Vector3> trajectory;
            Pose2D finalPose;

            TrajectoryEvaluation eval = EvaluateCandidate(
                candidate,
                localWaypoint,
                out trajectory,
                out finalPose
            );

            evaluated++;

            if (eval.rejected || float.IsInfinity(eval.cost) || float.IsNaN(eval.cost))
            {
                rejected++;

                if (drawCandidateTrajectories)
                {
                    DrawTrajectory(trajectory, rejectedColor);
                }

                continue;
            }

            if (drawCandidateTrajectories)
            {
                DrawTrajectory(trajectory, candidateColor);
            }

            if (eval.cost < bestCost)
            {
                bestCost = eval.cost;
                best = candidate;

                bestMinHumanDistance = eval.minHumanDistance;
                bestMinObstacleDistance = eval.minObstacleDistance;

                bestTrajectory.Clear();
                bestTrajectory.AddRange(trajectory);
            }
        }

        if (evaluated > 0 && rejected == evaluated)
        {
            best = EmergencyTurnToGoal();
            bestCost = 9999f;

            if (debugConsole)
            {
                UnityEngine.Debug.LogWarning(
                    $"DWA BLOQUEADO: todas las trayectorias rechazadas. " +
                    $"Se aplica giro de emergencia. Goal={currentGoal.name}"
                );
            }
        }

        if (Mathf.Abs(best.v) < 0.001f && Mathf.Abs(best.omega) < 0.001f)
        {
            best = EmergencyTurnToGoal();
        }

        if (drawBestTrajectory && bestTrajectory.Count > 1)
        {
            DrawTrajectory(bestTrajectory, bestColor);
        }

        Vector3 directToGoal = ProjectXZ(currentGoal.position - transform.position);

        if (directToGoal.magnitude > 0.1f)
        {
            float headingErrorDeg = Vector3.SignedAngle(
                transform.forward,
                directToGoal.normalized,
                Vector3.up
            );

            if (Mathf.Abs(headingErrorDeg) > 75f)
            {
                float headingErrorRad = headingErrorDeg * Mathf.Deg2Rad;

                best.v = 0f;
                best.omega = Mathf.Clamp(
                    headingErrorRad * 2.5f,
                    -config.omegaMax,
                    config.omegaMax
                );

                if (debugConsole)
                {
                    UnityEngine.Debug.Log(
                        "DWA giro forzado hacia objetivo. Angle=" +
                        headingErrorDeg.ToString("F1") +
                        " omega=" + best.omega.ToString("F2")
                    );
                }
            }
        }

        ApplyControl(best.v, best.omega);

        if (sw != null)
        {
            sw.Stop();
        }

        lastMetrics = new SocialDWAMetrics
        {
            bestCost = bestCost,
            cpuMs = sw != null ? (float)(sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency) : 0f,
            evaluatedCandidates = evaluated,
            rejectedCandidates = rejected,
            selectedV = best.v,
            selectedOmega = best.omega,
            minHumanDistance = bestMinHumanDistance,
            minObstacleDistance = bestMinObstacleDistance
        };

        if (debugConsole)
        {
            Vector3 toGoal = currentGoal.position - transform.position;
            float angleToGoal = Vector3.SignedAngle(transform.forward, ProjectXZ(toGoal), Vector3.up);

            UnityEngine.Debug.Log(
                $"DWA -> Goal={currentGoal.name}, " +
                $"Dist={ProjectXZ(toGoal).magnitude:F2}, " +
                $"Angle={angleToGoal:F1}, " +
                $"v={currentV:F3}, omega={currentOmega:F3}, " +
                $"Rejected={rejected}/{evaluated}"
            );
        }
    }

    private Vector3 GetLocalWaypointFromNavMesh()
    {
        if (agent == null || currentGoal == null)
        {
            return transform.position + transform.forward * config.localWaypointLookAhead;
        }

        NavMeshPath path = new NavMeshPath();

        bool ok = NavMesh.CalculatePath(
            transform.position,
            currentGoal.position,
            NavMesh.AllAreas,
            path
        );

        if (!ok || path.corners == null || path.corners.Length == 0)
        {
            return currentGoal.position;
        }

        if (path.corners.Length == 1)
        {
            return path.corners[0];
        }

        float accumulated = 0f;
        Vector3 previous = path.corners[0];

        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 current = path.corners[i];
            float segmentLength = Vector3.Distance(previous, current);

            if (accumulated + segmentLength >= config.localWaypointLookAhead)
            {
                float alpha = (config.localWaypointLookAhead - accumulated) / Mathf.Max(segmentLength, EPS);
                return Vector3.Lerp(previous, current, alpha);
            }

            accumulated += segmentLength;
            previous = current;
        }

        return path.corners[path.corners.Length - 1];
    }

    private List<Candidate> GenerateCandidates()
    {
        List<Candidate> candidates = new List<Candidate>(
            config.velocitySamples * config.omegaSamples
        );

        int nv = Mathf.Max(2, config.velocitySamples);
        int nw = Mathf.Max(3, config.omegaSamples);

        float vLow = Mathf.Max(0f, config.vMin);
        float vHigh = Mathf.Max(vLow, config.vMax);

        float omegaLow = -Mathf.Abs(config.omegaMax);
        float omegaHigh = Mathf.Abs(config.omegaMax);

        for (int i = 0; i < nv; i++)
        {
            float alphaV = (float)i / (nv - 1);
            float v = Mathf.Lerp(vLow, vHigh, alphaV);

            for (int j = 0; j < nw; j++)
            {
                float alphaW = (float)j / (nw - 1);
                float omega = Mathf.Lerp(omegaLow, omegaHigh, alphaW);

                candidates.Add(new Candidate(v, omega));
            }
        }

        return candidates;
    }

    private TrajectoryEvaluation EvaluateCandidate(
        Candidate candidate,
        Vector3 localWaypoint,
        out List<Vector3> trajectory,
        out Pose2D finalPose
    )
    {
        trajectory = SimulateTrajectory(candidate.v, candidate.omega, out finalPose);

        float jGoal = GoalCost(trajectory, finalPose, localWaypoint);
        float jVelocity = VelocityCost(candidate.v);
        float jSmooth = SmoothnessCost(candidate.v, candidate.omega);
        float jProxemic = ProxemicCost(trajectory);
        float jAnisotropic = AnisotropicCost(trajectory);
        float jObstacle = ObstacleCost(trajectory);

        bool rejected = false;

        if (float.IsInfinity(jProxemic) || float.IsInfinity(jObstacle))
        {
            rejected = true;
        }

        float totalCost =
            config.wGoal * jGoal +
            config.wVelocity * jVelocity +
            config.wSmooth * jSmooth +
            config.wProxemic * jProxemic +
            config.wAnisotropic * jAnisotropic +
            config.wObstacle * jObstacle;

        return new TrajectoryEvaluation
        {
            cost = totalCost,
            rejected = rejected,
            minHumanDistance = MinHumanDistance(trajectory),
            minObstacleDistance = MinObstacleDistance(trajectory)
        };
    }

    private List<Vector3> SimulateTrajectory(float v, float omega, out Pose2D finalPose)
    {
        int steps = Mathf.Max(1, Mathf.RoundToInt(config.predictionTime / config.simulationStep));
        List<Vector3> trajectory = new List<Vector3>(steps);

        Vector3 pos = transform.position;

        float yawRad = transform.eulerAngles.y * Mathf.Deg2Rad;

        for (int k = 0; k < steps; k++)
        {
            yawRad = NormalizeAngle(yawRad + omega * config.simulationStep);

            Vector3 forward = new Vector3(
                Mathf.Sin(yawRad),
                0f,
                Mathf.Cos(yawRad)
            );

            pos += forward * v * config.simulationStep;
            trajectory.Add(pos);
        }

        finalPose = new Pose2D(pos, yawRad);
        return trajectory;
    }

    private float GoalCost(List<Vector3> trajectory, Pose2D finalPose, Vector3 localWaypoint)
    {
        if (trajectory == null || trajectory.Count == 0)
        {
            return 1f;
        }

        Vector3 last = trajectory[trajectory.Count - 1];

        Vector3 toWaypoint = ProjectXZ(localWaypoint - last);

        float distance = toWaypoint.magnitude;
        float dRef = Mathf.Max(config.localWaypointLookAhead, 1f);
        float distanceCost = Mathf.Clamp01(distance / dRef);

        float angleCost = 0f;

        if (toWaypoint.magnitude > 0.05f)
        {
            Vector3 finalForward = new Vector3(
                Mathf.Sin(finalPose.yawRad),
                0f,
                Mathf.Cos(finalPose.yawRad)
            );

            angleCost = Vector3.Angle(finalForward, toWaypoint.normalized) / 180f;
        }

        return 0.60f * distanceCost + 0.40f * angleCost;
    }

    private float VelocityCost(float v)
    {
        if (config.vMax <= EPS)
        {
            return 1f;
        }

        return 1f - Mathf.Clamp01(v / config.vMax);
    }

    private float SmoothnessCost(float v, float omega)
    {
        float dv = Mathf.Abs(v - currentV) / Mathf.Max(config.vMax, EPS);
        float dw = Mathf.Abs(omega - currentOmega) / Mathf.Max(config.omegaMax, EPS);

        return dv + dw;
    }

    private float ProxemicCost(List<Vector3> trajectory)
    {
        if (humans == null || humans.Count == 0)
        {
            return 0f;
        }

        float total = 0f;
        int count = 0;

        foreach (Vector3 p in trajectory)
        {
            foreach (SocialDWAHuman h in humans)
            {
                if (h == null)
                {
                    continue;
                }

                float d = DistanceXZ(p, h.Position);

                if (d < config.dIntimate)
                {
                    return float.PositiveInfinity;
                }

                if (d < config.dPersonal)
                {
                    total += 10f;
                }
                else if (d < config.dSocial)
                {
                    total += 1f;
                }

                count++;
            }
        }

        return count > 0 ? total / count : 0f;
    }

    private float AnisotropicCost(List<Vector3> trajectory)
    {
        if (humans == null || humans.Count == 0)
        {
            return 0f;
        }

        float total = 0f;
        int count = 0;

        foreach (Vector3 p in trajectory)
        {
            foreach (SocialDWAHuman h in humans)
            {
                if (h == null)
                {
                    continue;
                }

                float dx = p.x - h.Position.x;
                float dz = p.z - h.Position.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);

                float beta = Mathf.Atan2(dz, dx);
                float phi = NormalizeAngle(beta - h.HeadingRad);

                float sx2 = Mathf.Max(config.sigmaX * config.sigmaX, EPS);
                float sy2 = Mathf.Max(config.sigmaY * config.sigmaY, EPS);

                float c = Mathf.Exp(
                    -d * d *
                    (
                        Mathf.Pow(Mathf.Cos(phi), 2f) / sx2 +
                        Mathf.Pow(Mathf.Sin(phi), 2f) / sy2
                    )
                );

                total += c;
                count++;
            }
        }

        return count > 0 ? total / count : 0f;
    }

    private float ObstacleCost(List<Vector3> trajectory)
    {
        float minDistance = float.PositiveInfinity;

        foreach (Vector3 p in trajectory)
        {
            if (config.rejectOffNavMesh)
            {
                NavMeshHit hit;
                bool onNavMesh = NavMesh.SamplePosition(
                    p,
                    out hit,
                    config.navMeshSampleRadius,
                    NavMesh.AllAreas
                );

                if (!onNavMesh)
                {
                    return float.PositiveInfinity;
                }
            }

            float clearance = ApproximateObstacleClearance(p);
            minDistance = Mathf.Min(minDistance, clearance);

            float minAllowed = config.robotRadius + config.obstacleSafetyMargin;

            if (clearance < minAllowed)
            {
                return float.PositiveInfinity;
            }
        }

        if (float.IsPositiveInfinity(minDistance))
        {
            return 0f;
        }

        return 1f / (minDistance + 0.05f);
    }

    private float ApproximateObstacleClearance(Vector3 p)
    {
        float radius = config.robotRadius + config.obstacleSafetyMargin;

        Collider[] hits = Physics.OverlapSphere(
            p,
            radius,
            config.obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.isTrigger)
            {
                continue;
            }

            return 0f;
        }

        return 5f;
    }

    private float MinHumanDistance(List<Vector3> trajectory)
    {
        if (humans == null || humans.Count == 0)
        {
            return float.PositiveInfinity;
        }

        float minD = float.PositiveInfinity;

        foreach (Vector3 p in trajectory)
        {
            foreach (SocialDWAHuman h in humans)
            {
                if (h == null)
                {
                    continue;
                }

                minD = Mathf.Min(minD, DistanceXZ(p, h.Position));
            }
        }

        return minD;
    }

    private float MinObstacleDistance(List<Vector3> trajectory)
    {
        float minD = float.PositiveInfinity;

        foreach (Vector3 p in trajectory)
        {
            minD = Mathf.Min(minD, ApproximateObstacleClearance(p));
        }

        return minD;
    }

    private Candidate EmergencyTurnToGoal()
    {
        if (currentGoal == null)
        {
            return new Candidate(0f, 0f);
        }

        Vector3 toGoal = ProjectXZ(currentGoal.position - transform.position);

        if (toGoal.magnitude < 0.05f)
        {
            return new Candidate(0f, 0f);
        }

        float angleDeg = Vector3.SignedAngle(transform.forward, toGoal.normalized, Vector3.up);
        float angleRad = angleDeg * Mathf.Deg2Rad;

        float omega = Mathf.Clamp(
            angleRad * 2.0f,
            -config.omegaMax,
            config.omegaMax
        );

        if (Mathf.Abs(angleDeg) > 25f)
        {
            return new Candidate(0f, omega);
        }

        float v = Mathf.Min(config.vMax * 0.35f, 0.25f);
        return new Candidate(v, omega);
    }

    private void ApplyControl(float targetV, float targetOmega)
    {
        float dt = Time.fixedDeltaTime;

        float v = Mathf.MoveTowards(
            currentV,
            targetV,
            config.aVMax * dt
        );

        float omega = Mathf.MoveTowards(
            currentOmega,
            targetOmega,
            config.aOmegaMax * dt
        );

        Quaternion newRot = transform.rotation *
                            Quaternion.Euler(0f, omega * Mathf.Rad2Deg * dt, 0f);

        Vector3 newForward = newRot * Vector3.forward;
        Vector3 newPos = transform.position + newForward * v * dt;

        transform.SetPositionAndRotation(newPos, newRot);

        if (agent != null)
        {
            agent.nextPosition = transform.position;
        }

        currentV = v;
        currentOmega = omega;
    }

    private void DrawTrajectory(List<Vector3> trajectory, Color color)
    {
        if (trajectory == null || trajectory.Count < 2)
        {
            return;
        }

        for (int i = 1; i < trajectory.Count; i++)
        {
            UnityEngine.Debug.DrawLine(
                trajectory[i - 1] + Vector3.up * 0.05f,
                trajectory[i] + Vector3.up * 0.05f,
                color,
                Time.fixedDeltaTime
            );
        }
    }

    private static Vector3 ProjectXZ(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static float NormalizeAngle(float angle)
    {
        return Mathf.Atan2(Mathf.Sin(angle), Mathf.Cos(angle));
    }
}