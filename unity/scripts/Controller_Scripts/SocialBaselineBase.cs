using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Base class for external social-navigation baselines B2, B3 and B4.
/// It keeps the same architecture used by B1:
/// NavMesh global path -> local waypoint -> local social controller -> unicycle command.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public abstract class SocialBaselineBase : MonoBehaviour
{
    [Header("Configuration")]
    public SocialDWAConfig config;

    [Header("Mission")]
    public Transform currentGoal;
    public bool autoDisableNavMeshAgentMotion = true;

    [Header("Humans")]
    public List<SocialDWAHuman> humans = new List<SocialDWAHuman>();

    [Header("Debug")]
    public bool debugConsole = false;
    public bool drawDebugVectors = true;

    public SocialDWAMetrics LastMetrics { get { return lastMetrics; } }

    protected NavMeshAgent agent;
    protected float currentV = 0f;
    protected float currentOmega = 0f;
    protected SocialDWAMetrics lastMetrics;

    protected const float EPS = 1e-5f;

    private readonly Dictionary<SocialDWAHuman, Vector3> previousHumanPositions = new Dictionary<SocialDWAHuman, Vector3>();
    private readonly Dictionary<SocialDWAHuman, Vector3> estimatedHumanVelocities = new Dictionary<SocialDWAHuman, Vector3>();

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null && autoDisableNavMeshAgentMotion)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.isStopped = true;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (config == null || currentGoal == null)
        {
            return;
        }

        UpdateHumanVelocityCache();
        RunController();
    }

    protected abstract void RunController();

    public void SetGoal(Transform goal)
    {
        currentGoal = goal;
    }

    public void SetHumans(List<SocialDWAHuman> humanList)
    {
        humans = humanList;
    }

    protected Vector3 GetLocalWaypointFromNavMesh()
    {
        if (agent == null || currentGoal == null)
        {
            return transform.position + transform.forward * Mathf.Max(config.localWaypointLookAhead, 1f);
        }

        NavMeshPath path = new NavMeshPath();
        bool ok = NavMesh.CalculatePath(transform.position, currentGoal.position, NavMesh.AllAreas, path);

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
            float segmentLength = Vector3.Distance(ProjectXZ(previous), ProjectXZ(current));

            if (accumulated + segmentLength >= config.localWaypointLookAhead)
            {
                float alpha = (config.localWaypointLookAhead - accumulated) / Mathf.Max(segmentLength, EPS);
                return Vector3.Lerp(previous, current, Mathf.Clamp01(alpha));
            }

            accumulated += segmentLength;
            previous = current;
        }

        return path.corners[path.corners.Length - 1];
    }

    protected Vector3 DesiredDirectionToWaypoint(Vector3 waypoint)
    {
        Vector3 toWaypoint = ProjectXZ(waypoint - transform.position);

        if (toWaypoint.magnitude < 0.05f && currentGoal != null)
        {
            toWaypoint = ProjectXZ(currentGoal.position - transform.position);
        }

        if (toWaypoint.magnitude < 0.05f)
        {
            return ProjectXZ(transform.forward).normalized;
        }

        return toWaypoint.normalized;
    }

    protected void ApplyVelocityVector(Vector3 targetVelocity, float omegaBias, float headingGain)
    {
        Vector3 vXZ = ProjectXZ(targetVelocity);
        float desiredSpeed = Mathf.Clamp(vXZ.magnitude, 0f, config.vMax);

        float targetOmega = omegaBias;
        float targetV = 0f;

        if (desiredSpeed > 0.001f)
        {
            Vector3 desiredDir = vXZ.normalized;
            float headingErrorDeg = Vector3.SignedAngle(ProjectXZ(transform.forward).normalized, desiredDir, Vector3.up);
            float headingErrorRad = headingErrorDeg * Mathf.Deg2Rad;

            targetOmega += headingGain * headingErrorRad;
            targetOmega = Mathf.Clamp(targetOmega, -config.omegaMax, config.omegaMax);

            float alignment = Mathf.Clamp01(Mathf.Cos(headingErrorRad));
            targetV = desiredSpeed * alignment;
        }

        ApplyControl(targetV, targetOmega);
    }

    protected void ApplyControl(float targetV, float targetOmega)
    {
        float dt = Time.fixedDeltaTime;

        float v = Mathf.MoveTowards(currentV, Mathf.Clamp(targetV, Mathf.Max(0f, config.vMin), config.vMax), config.aVMax * dt);
        float omega = Mathf.MoveTowards(currentOmega, Mathf.Clamp(targetOmega, -config.omegaMax, config.omegaMax), config.aOmegaMax * dt);

        Quaternion newRot = transform.rotation * Quaternion.Euler(0f, omega * Mathf.Rad2Deg * dt, 0f);
        Vector3 newForward = newRot * Vector3.forward;
        Vector3 newPos = transform.position + newForward * v * dt;

        if (config.rejectOffNavMesh)
        {
            NavMeshHit hit;
            bool onNavMesh = NavMesh.SamplePosition(newPos, out hit, config.navMeshSampleRadius, NavMesh.AllAreas);
            if (!onNavMesh)
            {
                v = 0f;
                newPos = transform.position;
            }
        }

        transform.SetPositionAndRotation(newPos, newRot);

        if (agent != null)
        {
            agent.nextPosition = transform.position;
        }

        currentV = v;
        currentOmega = omega;
    }

    protected Vector3 GetHumanVelocity(SocialDWAHuman h)
    {
        if (h == null || !estimatedHumanVelocities.ContainsKey(h))
        {
            return Vector3.zero;
        }

        return estimatedHumanVelocities[h];
    }

    protected float MinCurrentHumanDistance()
    {
        if (humans == null || humans.Count == 0)
        {
            return float.PositiveInfinity;
        }

        float dMin = float.PositiveInfinity;
        for (int i = 0; i < humans.Count; i++)
        {
            SocialDWAHuman h = humans[i];
            if (h == null) { continue; }
            dMin = Mathf.Min(dMin, DistanceXZ(transform.position, h.Position));
        }

        return dMin;
    }

    protected float ApproximateObstacleClearance(Vector3 p)
    {
        float radius = config.robotRadius + config.obstacleSafetyMargin;

        Collider[] hits = Physics.OverlapSphere(p, radius, config.obstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null) { continue; }
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) { continue; }
            if (hit.isTrigger) { continue; }
            return 0f;
        }

        return 5f;
    }

    protected bool IsInsideObstacle(Vector3 p)
    {
        return ApproximateObstacleClearance(p) <= 0.001f;
    }

    private void UpdateHumanVelocityCache()
    {
        if (humans == null)
        {
            return;
        }

        float dt = Mathf.Max(Time.fixedDeltaTime, EPS);

        for (int i = 0; i < humans.Count; i++)
        {
            SocialDWAHuman h = humans[i];
            if (h == null) { continue; }

            Vector3 pNow = h.Position;
            Vector3 pPrev;

            if (previousHumanPositions.TryGetValue(h, out pPrev))
            {
                Vector3 v = ProjectXZ(pNow - pPrev) / dt;
                estimatedHumanVelocities[h] = v;
                previousHumanPositions[h] = pNow;
            }
            else
            {
                previousHumanPositions.Add(h, pNow);
                estimatedHumanVelocities[h] = Vector3.zero;
            }
        }
    }

    protected static Vector3 ProjectXZ(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    protected static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    protected static float NormalizeAngle(float angle)
    {
        return Mathf.Atan2(Mathf.Sin(angle), Mathf.Cos(angle));
    }

    protected static Vector3 Rotate90(Vector3 v, float sign)
    {
        Vector3 p = ProjectXZ(v);
        return new Vector3(-p.z * sign, 0f, p.x * sign);
    }
}
