using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// B4 = CBF-Social-DWA baseline.
/// First, a Social-DWA nominal command is selected by velocity sampling.
/// Second, a Control Barrier Function safety filter clips the selected linear
/// velocity and adds a steering correction when a human safety set is at risk.
/// Safety set per human: h_i(x) = ||p_r - p_h||^2 - d_safe^2 >= 0.
/// </summary>
public class B4_CBFSocialDWAController : SocialBaselineBase
{
    [Header("B4 CBF parameters")]
    public float cbfSafeDistance = 1.20f;
    public float cbfGamma = 1.50f;
    public float cbfSteerGain = 2.50f;
    public float cbfInfluenceDistance = 2.20f;
    public float humanRadius = 0.30f;
    public bool useHumanVelocityInCBF = true;

    [Header("B4 DWA debug")]
    public bool drawBestTrajectory = true;
    public Color bestColor = Color.yellow;

    private readonly List<Vector3> bestTrajectory = new List<Vector3>();

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

    private struct Evaluation
    {
        public float cost;
        public bool rejected;
        public float minHumanDistance;
        public float minObstacleDistance;
    }

    protected override void RunController()
    {
        Stopwatch sw = null;
        if (config.logCpuTime) { sw = Stopwatch.StartNew(); }

        Vector3 localWaypoint = GetLocalWaypointFromNavMesh();
        List<Candidate> candidates = GenerateCandidates();

        Candidate best = new Candidate(0f, 0f);
        float bestCost = float.PositiveInfinity;
        float bestMinHumanDistance = float.PositiveInfinity;
        float bestMinObstacleDistance = float.PositiveInfinity;
        int evaluated = 0;
        int rejected = 0;

        bestTrajectory.Clear();

        for (int i = 0; i < candidates.Count; i++)
        {
            Candidate candidate = candidates[i];
            List<Vector3> trajectory;
            Pose2D finalPose;
            Evaluation eval = EvaluateCandidate(candidate, localWaypoint, out trajectory, out finalPose);

            evaluated++;
            if (eval.rejected || float.IsInfinity(eval.cost) || float.IsNaN(eval.cost))
            {
                rejected++;
                continue;
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
        }

        Candidate filtered = ApplyCBFFilter(best);
        ApplyControl(filtered.v, filtered.omega);

        if (drawBestTrajectory && bestTrajectory.Count > 1)
        {
            DrawTrajectory(bestTrajectory, bestColor);
        }

        if (sw != null) { sw.Stop(); }

        lastMetrics = new SocialDWAMetrics
        {
            bestCost = bestCost,
            cpuMs = sw != null ? (float)(sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency) : 0f,
            evaluatedCandidates = evaluated,
            rejectedCandidates = rejected,
            selectedV = currentV,
            selectedOmega = currentOmega,
            minHumanDistance = Mathf.Min(bestMinHumanDistance, MinCurrentHumanDistance()),
            minObstacleDistance = bestMinObstacleDistance
        };

        if (debugConsole)
        {
            UnityEngine.Debug.Log("B4 CBF-Social-DWA -> v=" + currentV.ToString("F3") +
                                  " omega=" + currentOmega.ToString("F3") +
                                  " rejected=" + rejected.ToString() + "/" + evaluated.ToString());
        }
    }

    private List<Candidate> GenerateCandidates()
    {
        List<Candidate> candidates = new List<Candidate>(config.velocitySamples * config.omegaSamples);

        int nv = Mathf.Max(2, config.velocitySamples);
        int nw = Mathf.Max(3, config.omegaSamples);

        float vLow = Mathf.Max(0f, config.vMin);
        float vHigh = Mathf.Max(vLow, config.vMax);
        float omegaLow = -Mathf.Abs(config.omegaMax);
        float omegaHigh = Mathf.Abs(config.omegaMax);

        for (int i = 0; i < nv; i++)
        {
            float av = (float)i / (nv - 1);
            float v = Mathf.Lerp(vLow, vHigh, av);

            for (int j = 0; j < nw; j++)
            {
                float aw = (float)j / (nw - 1);
                float omega = Mathf.Lerp(omegaLow, omegaHigh, aw);
                candidates.Add(new Candidate(v, omega));
            }
        }

        return candidates;
    }

    private Evaluation EvaluateCandidate(Candidate candidate, Vector3 localWaypoint, out List<Vector3> trajectory, out Pose2D finalPose)
    {
        trajectory = SimulateTrajectory(candidate.v, candidate.omega, out finalPose);

        float jGoal = GoalCost(trajectory, finalPose, localWaypoint);
        float jVelocity = VelocityCost(candidate.v);
        float jSmooth = SmoothnessCost(candidate.v, candidate.omega);
        float jProxemic = ProxemicCost(trajectory);
        float jAnisotropic = AnisotropicCost(trajectory);
        float jObstacle = ObstacleCost(trajectory);

        bool rejected = float.IsInfinity(jProxemic) || float.IsInfinity(jObstacle);

        float totalCost =
            config.wGoal * jGoal +
            config.wVelocity * jVelocity +
            config.wSmooth * jSmooth +
            config.wProxemic * jProxemic +
            config.wAnisotropic * jAnisotropic +
            config.wObstacle * jObstacle;

        return new Evaluation
        {
            cost = totalCost,
            rejected = rejected,
            minHumanDistance = MinHumanDistance(trajectory),
            minObstacleDistance = MinObstacleDistance(trajectory)
        };
    }

    private List<Vector3> SimulateTrajectory(float v, float omega, out Pose2D finalPose)
    {
        int steps = Mathf.Max(1, Mathf.RoundToInt(config.predictionTime / Mathf.Max(config.simulationStep, EPS)));
        List<Vector3> trajectory = new List<Vector3>(steps);

        Vector3 pos = transform.position;
        float yawRad = transform.eulerAngles.y * Mathf.Deg2Rad;

        for (int k = 0; k < steps; k++)
        {
            yawRad = NormalizeAngle(yawRad + omega * config.simulationStep);
            Vector3 forward = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
            pos += forward * v * config.simulationStep;
            trajectory.Add(pos);
        }

        finalPose = new Pose2D(pos, yawRad);
        return trajectory;
    }

    private float GoalCost(List<Vector3> trajectory, Pose2D finalPose, Vector3 localWaypoint)
    {
        if (trajectory == null || trajectory.Count == 0) { return 1f; }

        Vector3 last = trajectory[trajectory.Count - 1];
        Vector3 toWaypoint = ProjectXZ(localWaypoint - last);
        float distanceCost = Mathf.Clamp01(toWaypoint.magnitude / Mathf.Max(config.localWaypointLookAhead, 1f));

        float angleCost = 0f;
        if (toWaypoint.magnitude > 0.05f)
        {
            Vector3 finalForward = new Vector3(Mathf.Sin(finalPose.yawRad), 0f, Mathf.Cos(finalPose.yawRad));
            angleCost = Vector3.Angle(finalForward, toWaypoint.normalized) / 180f;
        }

        return 0.60f * distanceCost + 0.40f * angleCost;
    }

    private float VelocityCost(float v)
    {
        return 1f - Mathf.Clamp01(v / Mathf.Max(config.vMax, EPS));
    }

    private float SmoothnessCost(float v, float omega)
    {
        float dv = Mathf.Abs(v - currentV) / Mathf.Max(config.vMax, EPS);
        float dw = Mathf.Abs(omega - currentOmega) / Mathf.Max(config.omegaMax, EPS);
        return dv + dw;
    }

    private float ProxemicCost(List<Vector3> trajectory)
    {
        if (humans == null || humans.Count == 0) { return 0f; }

        float total = 0f;
        int count = 0;

        for (int i = 0; i < trajectory.Count; i++)
        {
            Vector3 p = trajectory[i];
            for (int j = 0; j < humans.Count; j++)
            {
                SocialDWAHuman h = humans[j];
                if (h == null) { continue; }

                float d = DistanceXZ(p, h.Position);
                if (d < config.dIntimate) { return float.PositiveInfinity; }
                if (d < config.dPersonal) { total += 10f; }
                else if (d < config.dSocial) { total += 1f; }
                count++;
            }
        }

        return count > 0 ? total / count : 0f;
    }

    private float AnisotropicCost(List<Vector3> trajectory)
    {
        if (humans == null || humans.Count == 0) { return 0f; }

        float total = 0f;
        int count = 0;

        for (int i = 0; i < trajectory.Count; i++)
        {
            Vector3 p = trajectory[i];
            for (int j = 0; j < humans.Count; j++)
            {
                SocialDWAHuman h = humans[j];
                if (h == null) { continue; }

                float dx = p.x - h.Position.x;
                float dz = p.z - h.Position.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                float beta = Mathf.Atan2(dz, dx);
                float phi = NormalizeAngle(beta - h.HeadingRad);

                float sx2 = Mathf.Max(config.sigmaX * config.sigmaX, EPS);
                float sy2 = Mathf.Max(config.sigmaY * config.sigmaY, EPS);

                float c = Mathf.Exp(-d * d *
                    (Mathf.Pow(Mathf.Cos(phi), 2f) / sx2 + Mathf.Pow(Mathf.Sin(phi), 2f) / sy2));

                total += c;
                count++;
            }
        }

        return count > 0 ? total / count : 0f;
    }

    private float ObstacleCost(List<Vector3> trajectory)
    {
        float minDistance = float.PositiveInfinity;

        for (int i = 0; i < trajectory.Count; i++)
        {
            Vector3 p = trajectory[i];

            if (config.rejectOffNavMesh)
            {
                NavMeshHit hit;
                bool onNavMesh = NavMesh.SamplePosition(p, out hit, config.navMeshSampleRadius, NavMesh.AllAreas);
                if (!onNavMesh) { return float.PositiveInfinity; }
            }

            float clearance = ApproximateObstacleClearance(p);
            minDistance = Mathf.Min(minDistance, clearance);

            float minAllowed = config.robotRadius + config.obstacleSafetyMargin;
            if (clearance < minAllowed) { return float.PositiveInfinity; }
        }

        if (float.IsPositiveInfinity(minDistance)) { return 0f; }
        return 1f / (minDistance + 0.05f);
    }

    private float MinHumanDistance(List<Vector3> trajectory)
    {
        if (humans == null || humans.Count == 0) { return float.PositiveInfinity; }

        float minD = float.PositiveInfinity;
        for (int i = 0; i < trajectory.Count; i++)
        {
            Vector3 p = trajectory[i];
            for (int j = 0; j < humans.Count; j++)
            {
                SocialDWAHuman h = humans[j];
                if (h == null) { continue; }
                minD = Mathf.Min(minD, DistanceXZ(p, h.Position));
            }
        }

        return minD;
    }

    private float MinObstacleDistance(List<Vector3> trajectory)
    {
        float minD = float.PositiveInfinity;
        for (int i = 0; i < trajectory.Count; i++)
        {
            minD = Mathf.Min(minD, ApproximateObstacleClearance(trajectory[i]));
        }
        return minD;
    }

    private Candidate ApplyCBFFilter(Candidate nominal)
    {
        float safeV = Mathf.Clamp(nominal.v, 0f, config.vMax);
        float safeOmega = Mathf.Clamp(nominal.omega, -config.omegaMax, config.omegaMax);

        if (humans == null || humans.Count == 0)
        {
            return new Candidate(safeV, safeOmega);
        }

        Vector3 robotPos = ProjectXZ(transform.position);
        Vector3 forward = ProjectXZ(transform.forward).normalized;
        float dSafe = Mathf.Max(cbfSafeDistance, config.robotRadius + humanRadius + 0.05f);

        for (int i = 0; i < humans.Count; i++)
        {
            SocialDWAHuman h = humans[i];
            if (h == null) { continue; }

            Vector3 humanPos = ProjectXZ(h.Position);
            Vector3 r = robotPos - humanPos;
            float d = r.magnitude;
            if (d < EPS || d > cbfInfluenceDistance) { continue; }

            Vector3 vHuman = useHumanVelocityInCBF ? GetHumanVelocity(h) : Vector3.zero;
            float hValue = d * d - dSafe * dSafe;

            // hdot + gamma*h >= 0, h = ||p_r - p_h||^2 - d_safe^2
            float a = 2f * Vector3.Dot(r, forward);
            float b = 2f * Vector3.Dot(r, vHuman) - cbfGamma * hValue;

            if (Mathf.Abs(a) > EPS)
            {
                float bound = b / a;

                if (a < 0f)
                {
                    // Robot is heading toward the human: this becomes an upper bound on v.
                    safeV = Mathf.Min(safeV, bound);
                }
                else if (bound > safeV && hValue < 0f)
                {
                    // Inside the unsafe set and facing away: enforce a small escape motion.
                    safeV = Mathf.Max(safeV, Mathf.Min(config.vMax, bound));
                }
            }

            float proximity = Mathf.Clamp01((cbfInfluenceDistance - d) / Mathf.Max(cbfInfluenceDistance - dSafe, EPS));
            Vector3 away = r.normalized;
            float steerSign = Mathf.Sign(Vector3.SignedAngle(forward, away, Vector3.up));
            if (Mathf.Abs(steerSign) < 0.1f) { steerSign = 1f; }
            safeOmega += steerSign * cbfSteerGain * proximity;

            if (d < dSafe)
            {
                safeV = Mathf.Min(safeV, config.vMax * 0.15f);
            }
        }

        safeV = Mathf.Clamp(safeV, 0f, config.vMax);
        safeOmega = Mathf.Clamp(safeOmega, -config.omegaMax, config.omegaMax);
        return new Candidate(safeV, safeOmega);
    }

    private Candidate EmergencyTurnToGoal()
    {
        if (currentGoal == null) { return new Candidate(0f, 0f); }

        Vector3 toGoal = ProjectXZ(currentGoal.position - transform.position);
        if (toGoal.magnitude < 0.05f) { return new Candidate(0f, 0f); }

        float angleDeg = Vector3.SignedAngle(ProjectXZ(transform.forward).normalized, toGoal.normalized, Vector3.up);
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float omega = Mathf.Clamp(angleRad * 2.0f, -config.omegaMax, config.omegaMax);

        if (Mathf.Abs(angleDeg) > 25f)
        {
            return new Candidate(0f, omega);
        }

        return new Candidate(Mathf.Min(config.vMax * 0.35f, 0.25f), omega);
    }

    private void DrawTrajectory(List<Vector3> trajectory, Color color)
    {
        if (trajectory == null || trajectory.Count < 2) { return; }

        for (int i = 1; i < trajectory.Count; i++)
        {
            UnityEngine.Debug.DrawLine(trajectory[i - 1] + Vector3.up * 0.05f,
                                       trajectory[i] + Vector3.up * 0.05f,
                                       color,
                                       Time.fixedDeltaTime);
        }
    }
}
