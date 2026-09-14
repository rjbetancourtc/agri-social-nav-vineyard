using System.Diagnostics;
using UnityEngine;

/// <summary>
/// B2 = ORCA/RVO-inspired geometric velocity-obstacle baseline.
/// This implementation keeps the same Unity/NavMesh protocol as B1 and M4.
/// It computes a preferred velocity toward the NavMesh waypoint and projects it
/// away from predicted human collision cones over a finite time horizon.
/// </summary>
public class B2_ORCAController : SocialBaselineBase
{
    [Header("B2 ORCA/RVO parameters")]
    public float humanRadius = 0.30f;
    public float additionalSafetyMargin = 0.25f;
    public float neighborDistance = 5.0f;
    public float timeHorizon = 3.0f;
    public float orcaGain = 1.25f;
    public float emergencyGain = 2.5f;
    public float headingGain = 3.0f;
    public float sidePreference = 1.0f; // +1 right-hand pass, -1 left-hand pass.

    protected override void RunController()
    {
        Stopwatch sw = null;
        if (config.logCpuTime) { sw = Stopwatch.StartNew(); }

        Vector3 waypoint = GetLocalWaypointFromNavMesh();
        Vector3 desiredDir = DesiredDirectionToWaypoint(waypoint);
        Vector3 desiredVel = desiredDir * config.vMax;

        Vector3 safeVel = desiredVel;
        int constraints = 0;
        float minHumanDistance = float.PositiveInfinity;

        if (humans != null)
        {
            for (int i = 0; i < humans.Count; i++)
            {
                SocialDWAHuman h = humans[i];
                if (h == null) { continue; }

                Vector3 relPos = ProjectXZ(h.Position - transform.position);
                float d = relPos.magnitude;
                minHumanDistance = Mathf.Min(minHumanDistance, d);

                if (d > neighborDistance || d < EPS)
                {
                    continue;
                }

                float combinedRadius = config.robotRadius + humanRadius + additionalSafetyMargin;
                Vector3 away = -relPos.normalized;

                Vector3 humanVel = GetHumanVelocity(h);
                Vector3 relVel = safeVel - humanVel;

                float ttc;
                float closestDistance;
                bool collisionCone = ComputeVelocityObstacleRisk(relPos, relVel, combinedRadius, out ttc, out closestDistance);

                if (d < combinedRadius)
                {
                    float penetration = Mathf.Clamp01((combinedRadius - d) / Mathf.Max(combinedRadius, EPS));
                    safeVel += away * emergencyGain * config.vMax * (0.5f + penetration);
                    constraints++;
                    continue;
                }

                if (collisionCone)
                {
                    Vector3 lateral = ChoosePassingSide(relPos, desiredDir);
                    float timeRisk = Mathf.Clamp01((timeHorizon - ttc) / Mathf.Max(timeHorizon, EPS));
                    float distanceRisk = Mathf.Clamp01((combinedRadius - closestDistance) / Mathf.Max(combinedRadius, EPS));
                    float risk = Mathf.Clamp01(0.55f * timeRisk + 0.45f * distanceRisk);

                    safeVel += (0.35f * away + 0.65f * lateral) * orcaGain * config.vMax * risk;
                    constraints++;
                }
                else if (d < config.dPersonal)
                {
                    float proximity = Mathf.Clamp01((config.dPersonal - d) / Mathf.Max(config.dPersonal, EPS));
                    safeVel += away * 0.65f * config.vMax * proximity;
                    constraints++;
                }
            }
        }

        if (safeVel.magnitude > config.vMax)
        {
            safeVel = safeVel.normalized * config.vMax;
        }

        ApplyVelocityVector(safeVel, 0f, headingGain);

        if (sw != null) { sw.Stop(); }

        lastMetrics = new SocialDWAMetrics
        {
            bestCost = safeVel.magnitude,
            cpuMs = sw != null ? (float)(sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency) : 0f,
            evaluatedCandidates = humans != null ? humans.Count : 0,
            rejectedCandidates = constraints,
            selectedV = currentV,
            selectedOmega = currentOmega,
            minHumanDistance = minHumanDistance,
            minObstacleDistance = ApproximateObstacleClearance(transform.position)
        };

        if (drawDebugVectors)
        {
            UnityEngine.Debug.DrawRay(transform.position + Vector3.up * 0.25f, desiredVel, Color.cyan, Time.fixedDeltaTime);
            UnityEngine.Debug.DrawRay(transform.position + Vector3.up * 0.35f, safeVel, Color.green, Time.fixedDeltaTime);
        }

        if (debugConsole)
        {
            UnityEngine.Debug.Log("B2 ORCA -> v=" + currentV.ToString("F3") +
                                  " omega=" + currentOmega.ToString("F3") +
                                  " constraints=" + constraints.ToString());
        }
    }

    private bool ComputeVelocityObstacleRisk(Vector3 relPos, Vector3 relVel, float radius, out float ttc, out float closestDistance)
    {
        ttc = float.PositiveInfinity;
        closestDistance = relPos.magnitude;

        float relSpeed2 = Vector3.Dot(relVel, relVel);
        if (relSpeed2 < EPS)
        {
            return false;
        }

        // relPos is human - robot. Positive ttc means the relative distance is decreasing.
        ttc = -Vector3.Dot(relPos, relVel) / relSpeed2;
        if (ttc < 0f || ttc > timeHorizon)
        {
            return false;
        }

        Vector3 closest = relPos + relVel * ttc;
        closestDistance = closest.magnitude;
        return closestDistance < radius;
    }

    private Vector3 ChoosePassingSide(Vector3 relPos, Vector3 desiredDir)
    {
        float crossY = Vector3.Cross(desiredDir, relPos.normalized).y;
        float sign = Mathf.Abs(crossY) > 0.05f ? Mathf.Sign(crossY) : Mathf.Sign(sidePreference);
        Vector3 lateral = Rotate90(relPos.normalized, -sign).normalized;
        return lateral;
    }
}
