using System.Diagnostics;
using UnityEngine;

/// <summary>
/// B3 = Social Force baseline.
/// The robot is treated as an agent driven by a goal-attraction force plus
/// repulsive human/obstacle forces. This is intentionally isotropic and classical,
/// so it is methodologically different from M4's anisotropic proxemic field.
/// </summary>
public class B3_SocialForceController : SocialBaselineBase
{
    [Header("B3 Social Force parameters")]
    public float relaxationTime = 0.60f;
    public float humanRadius = 0.30f;
    public float socialForceGain = 2.25f;
    public float socialForceRange = 0.85f;
    public float personalZoneBoost = 2.0f;
    public float obstacleForceGain = 1.50f;
    public float obstacleForceRange = 1.00f;
    public float headingGain = 3.2f;

    protected override void RunController()
    {
        Stopwatch sw = null;
        if (config.logCpuTime) { sw = Stopwatch.StartNew(); }

        Vector3 waypoint = GetLocalWaypointFromNavMesh();
        Vector3 desiredDir = DesiredDirectionToWaypoint(waypoint);
        Vector3 desiredVelocity = desiredDir * config.vMax;
        Vector3 currentVelocity = ProjectXZ(transform.forward) * currentV;

        Vector3 force = (desiredVelocity - currentVelocity) / Mathf.Max(relaxationTime, EPS);
        float minHumanDistance = float.PositiveInfinity;
        int activeForces = 0;

        if (humans != null)
        {
            for (int i = 0; i < humans.Count; i++)
            {
                SocialDWAHuman h = humans[i];
                if (h == null) { continue; }

                Vector3 away = ProjectXZ(transform.position - h.Position);
                float d = away.magnitude;
                minHumanDistance = Mathf.Min(minHumanDistance, d);

                if (d < EPS || d > config.dSocial)
                {
                    continue;
                }

                Vector3 n = away.normalized;
                float combinedRadius = config.robotRadius + humanRadius;
                float magnitude = socialForceGain * Mathf.Exp((combinedRadius - d) / Mathf.Max(socialForceRange, EPS));

                if (d < config.dPersonal)
                {
                    float boost = 1f + personalZoneBoost * Mathf.Clamp01((config.dPersonal - d) / Mathf.Max(config.dPersonal, EPS));
                    magnitude *= boost;
                }

                force += n * magnitude;
                activeForces++;
            }
        }

        // Optional obstacle repulsion using the configured obstacle layer.
        if (config.obstacleMask.value != 0)
        {
            Collider[] nearObstacles = Physics.OverlapSphere(transform.position, config.dSocial, config.obstacleMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < nearObstacles.Length; i++)
            {
                Collider c = nearObstacles[i];
                if (c == null) { continue; }
                if (c.transform == transform || c.transform.IsChildOf(transform)) { continue; }
                if (c.isTrigger) { continue; }

                Vector3 closest = c.ClosestPoint(transform.position);
                Vector3 away = ProjectXZ(transform.position - closest);
                float d = away.magnitude;
                if (d < EPS || d > obstacleForceRange) { continue; }

                float mag = obstacleForceGain * Mathf.Exp((config.robotRadius - d) / Mathf.Max(obstacleForceRange, EPS));
                force += away.normalized * mag;
                activeForces++;
            }
        }

        Vector3 targetVelocity = currentVelocity + force * Time.fixedDeltaTime;
        if (targetVelocity.magnitude > config.vMax)
        {
            targetVelocity = targetVelocity.normalized * config.vMax;
        }

        ApplyVelocityVector(targetVelocity, 0f, headingGain);

        if (sw != null) { sw.Stop(); }

        lastMetrics = new SocialDWAMetrics
        {
            bestCost = force.magnitude,
            cpuMs = sw != null ? (float)(sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency) : 0f,
            evaluatedCandidates = humans != null ? humans.Count : 0,
            rejectedCandidates = activeForces,
            selectedV = currentV,
            selectedOmega = currentOmega,
            minHumanDistance = minHumanDistance,
            minObstacleDistance = ApproximateObstacleClearance(transform.position)
        };

        if (drawDebugVectors)
        {
            UnityEngine.Debug.DrawRay(transform.position + Vector3.up * 0.20f, desiredVelocity, Color.cyan, Time.fixedDeltaTime);
            UnityEngine.Debug.DrawRay(transform.position + Vector3.up * 0.35f, force, Color.magenta, Time.fixedDeltaTime);
        }

        if (debugConsole)
        {
            UnityEngine.Debug.Log("B3 Social Force -> v=" + currentV.ToString("F3") +
                                  " omega=" + currentOmega.ToString("F3") +
                                  " activeForces=" + activeForces.ToString());
        }
    }
}
