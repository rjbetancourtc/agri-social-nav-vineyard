using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

/// <summary>
/// Lightweight 360 deg LiDAR telemetry for MATLAB.
/// Single authoritative UDP sender for the unattended experimental campaign.
///
/// Base packet fields (required by MATLAB V7.5):
/// t,dist,vel,acc,px,py,pz,status,method,scenario
///
/// LiDAR extension:
/// headingX,headingZ,nHum,hx,hz...,nPlants,px,pz...,
/// nBushes,bx,bz...,nObjects,ox,oz...
///
/// In externalExperimentControl mode, ScenarioManager is authoritative for:
/// - mission status 0..6
/// - method ID 0..8
/// - scenario ID 1..4
/// - measurement enable/disable
///
/// IMPORTANT: this class remains the ONLY UDP sender on port 55000.
/// </summary>
public class UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV : MonoBehaviour
{
    // ============================================================
    // MATLAB / EXPERIMENT STATUS PROTOCOL
    // ============================================================
    public const int STATUS_RUNNING_TO_B = 0;
    public const int STATUS_RETURNING_TO_A = 1;
    public const int STATUS_SUCCESS = 2;
    public const int STATUS_TIMEOUT = 3;
    public const int STATUS_COLLISION = 4;
    public const int STATUS_STUCK = 5;
    public const int STATUS_ABORTED = 6;

    [Header("References")]
    public Transform robot;
    public Transform human;
    public Transform pointA;
    public Transform pointB;

    [Header("Mission")]
    public float missionEndThreshold = 0.6f;
    public MissionType missionType = MissionType.AtoBtoA;
    public enum MissionType { AtoB, AtoBtoA }

    [Header("Experimental IDs")]
    [Range(0, 8)] public int methodID = 0;
    [Range(1, 4)] public int scenarioID = 1;

    [Header("External experiment control")]
    [Tooltip("ON = ScenarioManager controls status, identity and run measurement. Keep ON for the automatic campaign.")]
    [SerializeField] private bool externalExperimentControl = true;

    [Tooltip("Physical velocity/acceleration are measured only during RUN. Heartbeat packets continue outside RUN with v=a=0.")]
    [SerializeField] private bool runMeasurementActive = false;

    [SerializeField, Range(0, 6)]
    private int missionStatus = STATUS_RUNNING_TO_B;

    public bool ExternalExperimentControl => externalExperimentControl;
    public bool RunMeasurementActive => runMeasurementActive;
    public int missionState => missionStatus;

    [Header("UDP")]
    public string ip = "127.0.0.1";
    public int port = 55000;

    [Header("Sampling")]
    public float sampleTime = 0.10f;
    public float warmupTime = 0.5f;
    public float minVelocityToStart = 0.10f;
    [Range(0f, 1f)] public float velSmoothing = 0.2f;

    [Header("360 LiDAR - lightweight")]
    public float lidarScanPeriod = 0.25f;
    public float lidarMaxRange = 22f;
    [Range(31, 361)] public int lidarBeamCount = 121;
    [Range(1, 4)] public int maxReturnsPerBeam = 2;
    public int maxTotalPointsPerPacket = 260;
    public LayerMask humanMask;
    public LayerMask plantMask;
    public LayerMask bushMask;
    public LayerMask objectMask;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Explicit corridor rows")]
    public bool useExplicitCorridorRows = true;
    public string[] explicitRowNames = new string[] { "Row4", "Row_Central" };
    public bool useRendererBoundsForRows = true;
    public float explicitRowPointSpacing = 0.45f;
    public int maxExplicitRowPoints = 120;
    public bool refreshRowsEverySecond = true;

    [Header("Human points")]
    public float humanSearchRadius = 22f;
    public int maxHumans = 16;
    public bool autoFindHumansByName = true;
    public string[] humanNameKeywords = new string[] { "Human", "Humano" };
    public Transform[] additionalHumans;

    [Header("Debug")]
    public bool verboseLog = true;
    public bool drawDebugRays = false;

    private UdpClient client;
    private float timer = 0f;
    private float lidarTimer = 0f;
    private float rowRefreshTimer = 0f;
    private float gameStartTime = 0f;
    private float missionStartTime = 0f;
    private bool missionStarted = false;
    private bool missionCompleted = false;
    private bool reachedB = false;

    private Vector3 prevPos;
    private Vector3 prevVel;
    private Vector3 smoothVel;

    private readonly Collider[] humanHits = new Collider[64];
    private readonly RaycastHit[] rayHits = new RaycastHit[16];
    private readonly List<Vector3> humanPoints = new List<Vector3>(32);
    private readonly List<Vector3> plantPoints = new List<Vector3>(256);
    private readonly List<Vector3> bushPoints = new List<Vector3>(256);
    private readonly List<Vector3> objectPoints = new List<Vector3>(256);
    private readonly List<Transform> explicitRows = new List<Transform>(16);
    private readonly List<Transform> explicitHumans = new List<Transform>(32);

    private int combinedObjectMask;
    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    void Start()
    {
        client = new UdpClient();
        gameStartTime = Time.time;

        if (robot == null) robot = transform;
        ResetTelemetryState();

        combinedObjectMask = humanMask.value | plantMask.value | bushMask.value | objectMask.value;
        RefreshExplicitRows();
        RefreshExplicitHumansByName();

        if (verboseLog)
        {
            Debug.Log("====================================");
            Debug.Log("[UDP LiDAR CSV] Started");
            Debug.Log("[UDP LiDAR CSV] SINGLE UDP SENDER | External control = " + externalExperimentControl);
            Debug.Log(string.Format("[UDP LiDAR CSV] Port {0} | beams {1} | range {2:F1} m", port, lidarBeamCount, lidarMaxRange));
            Debug.Log("====================================");
        }
    }

    // ============================================================
    // PUBLIC API USED BY ScenarioManager
    // ============================================================

    public void SetExternalExperimentControl(bool enabled)
    {
        externalExperimentControl = enabled;

        if (enabled)
        {
            runMeasurementActive = false;
            missionStarted = false;
            missionCompleted = false;
            reachedB = false;
            missionStatus = STATUS_RUNNING_TO_B;
            ResetTelemetryState();
        }
    }

    public void SetExperimentIdentity(
        RobotSocialNavController.ExperimentMode method,
        int scenarioCode)
    {
        SetExperimentIdentity((int)method, scenarioCode);
    }

    public void SetExperimentIdentity(int methodCode, int scenarioCode)
    {
        if (methodCode < 0 || methodCode > 8)
        {
            Debug.LogError("[UDP LiDAR CSV] Invalid methodID: " + methodCode);
            return;
        }

        if (scenarioCode < 1 || scenarioCode > 4)
        {
            Debug.LogError("[UDP LiDAR CSV] Invalid scenarioID: " + scenarioCode);
            return;
        }

        methodID = methodCode;
        scenarioID = scenarioCode;
    }

    public void SetMissionState(int status)
    {
        if (status < STATUS_RUNNING_TO_B || status > STATUS_ABORTED)
        {
            Debug.LogError("[UDP LiDAR CSV] Invalid mission status: " + status);
            return;
        }

        missionStatus = status;

        if (status == STATUS_RUNNING_TO_B)
        {
            reachedB = false;
            missionCompleted = false;
        }
        else if (status == STATUS_RETURNING_TO_A)
        {
            reachedB = true;
        }
        else if (status >= STATUS_SUCCESS)
        {
            missionCompleted = true;
        }
    }

    public void SetRunMeasurementActive(bool active)
    {
        if (runMeasurementActive == active)
        {
            if (!active)
                ResetTelemetryState();
            return;
        }

        runMeasurementActive = active;

        if (active)
        {
            missionStarted = true;
            missionStartTime = Time.time;
            ResetTelemetryState();

            if (verboseLog)
                Debug.Log("[UDP LiDAR CSV] RUN measurement ENABLED.");
        }
        else
        {
            ResetTelemetryState();

            if (verboseLog)
                Debug.Log("[UDP LiDAR CSV] RUN measurement DISABLED; heartbeat continues.");
        }
    }

    public void ResetTelemetryState()
    {
        if (robot == null)
            robot = transform;

        if (robot != null)
            prevPos = robot.position;
        else
            prevPos = Vector3.zero;

        prevVel = Vector3.zero;
        smoothVel = Vector3.zero;
    }

    // ============================================================
    // UDP SAMPLING
    // ============================================================

    void Update()
    {
        if (robot == null) return;

        timer += Time.deltaTime;
        lidarTimer += Time.deltaTime;
        rowRefreshTimer += Time.deltaTime;

        if (refreshRowsEverySecond && rowRefreshTimer > 1.0f)
        {
            rowRefreshTimer = 0f;
            RefreshExplicitRows();
            RefreshExplicitHumansByName();
        }

        if (timer < sampleTime) return;

        float dt = timer;
        timer = 0f;

        if (Time.time - gameStartTime < warmupTime)
        {
            ResetTelemetryState();
            return;
        }

        if (externalExperimentControl)
        {
            SendExternallyControlledPacket(dt);
            return;
        }

        SendLegacyPacket(dt);
    }

    private void SendExternallyControlledPacket(float dt)
    {
        Vector3 pos = robot.position;
        float speed = 0f;
        float acceleration = 0f;

        if (runMeasurementActive)
        {
            Vector3 vel = (pos - prevPos) / Mathf.Max(dt, 1e-4f);
            vel.y = 0f;

            smoothVel = Vector3.Lerp(
                smoothVel,
                vel,
                1f - velSmoothing
            );

            Vector3 acc =
                (smoothVel - prevVel) /
                Mathf.Max(dt, 1e-4f);

            acc.y = 0f;

            speed = smoothVel.magnitude;
            acceleration = acc.magnitude;
        }
        else
        {
            // During PREPARE / RESET / SETTLE / END, heartbeat packets
            // continue but no teleport/reset velocity can contaminate data.
            smoothVel = Vector3.zero;
            prevVel = Vector3.zero;
            speed = 0f;
            acceleration = 0f;
        }

        if (lidarTimer >= lidarScanPeriod)
        {
            lidarTimer = 0f;
            ScanLidar360(pos);
        }

        float dist = ComputeClosestHumanDistance(pos);

        Vector3 fwd = robot.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f)
            fwd = Vector3.forward;
        fwd.Normalize();

        // IMPORTANT: global monotonically increasing time across the whole
        // Unity session. MATLAB clears run buffers between runs, but heartbeat
        // packets can arrive during the inter-run delay. A monotonic timestamp
        // prevents the next run from being rejected as out-of-order.
        float tPacket = Time.time - gameStartTime;

        SendPacket(
            BuildPacket(
                tPacket,
                dist,
                speed,
                acceleration,
                pos,
                fwd
            )
        );

        prevPos = pos;
        prevVel = runMeasurementActive
            ? smoothVel
            : Vector3.zero;
    }

    private void SendLegacyPacket(float dt)
    {
        Vector3 pos = robot.position;
        Vector3 vel = (pos - prevPos) / Mathf.Max(dt, 1e-4f);
        vel.y = 0f;
        smoothVel = Vector3.Lerp(smoothVel, vel, 1f - velSmoothing);
        Vector3 acc = (smoothVel - prevVel) / Mathf.Max(dt, 1e-4f);
        acc.y = 0f;

        if (!missionStarted && smoothVel.magnitude > minVelocityToStart)
        {
            missionStarted = true;
            missionStartTime = Time.time;
            if (verboseLog)
                Debug.Log("[UDP LiDAR CSV] Mission started (legacy mode).");
        }

        if (!missionStarted)
        {
            prevPos = pos;
            prevVel = smoothVel;
            return;
        }

        UpdateMissionStatus(pos);

        if (lidarTimer >= lidarScanPeriod)
        {
            lidarTimer = 0f;
            ScanLidar360(pos);
        }

        float dist = ComputeClosestHumanDistance(pos);
        Vector3 fwd = robot.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f)
            fwd = Vector3.forward;
        fwd.Normalize();

        float tMission = Time.time - missionStartTime;

        SendPacket(
            BuildPacket(
                tMission,
                dist,
                smoothVel.magnitude,
                acc.magnitude,
                pos,
                fwd
            )
        );

        prevPos = pos;
        prevVel = smoothVel;
    }

    private void SendPacket(string data)
    {
        if (client == null)
            return;

        byte[] bytes = Encoding.ASCII.GetBytes(data);

        try
        {
            client.Send(bytes, bytes.Length, ip, port);
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "[UDP LiDAR CSV] UDP send error: " +
                e.Message
            );
        }
    }

    private void RefreshExplicitRows()
    {
        explicitRows.Clear();
        if (!useExplicitCorridorRows || explicitRowNames == null) return;

        for (int i = 0; i < explicitRowNames.Length; i++)
        {
            string nm = explicitRowNames[i];
            if (string.IsNullOrEmpty(nm)) continue;
            GameObject go = GameObject.Find(nm);
            if (go != null && !explicitRows.Contains(go.transform)) explicitRows.Add(go.transform);
        }
    }

    private void RefreshExplicitHumansByName()
    {
        explicitHumans.Clear();
        if (human != null) explicitHumans.Add(human);
        if (additionalHumans != null)
        {
            for (int i = 0; i < additionalHumans.Length; i++)
            {
                if (additionalHumans[i] != null && !explicitHumans.Contains(additionalHumans[i]))
                    explicitHumans.Add(additionalHumans[i]);
            }
        }
        if (!autoFindHumansByName || humanNameKeywords == null) return;

        Transform[] all;
#if UNITY_2023_1_OR_NEWER
        all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
#else
        all = UnityEngine.Object.FindObjectsOfType<Transform>();
#endif
        for (int i = 0; i < all.Length; i++)
        {
            Transform tr = all[i];
            if (tr == null || tr == robot || tr.root == robot.root) continue;
            string nm = tr.name;
            bool match = false;
            for (int k = 0; k < humanNameKeywords.Length; k++)
            {
                string key = humanNameKeywords[k];
                if (!string.IsNullOrEmpty(key) && nm.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    match = true; break;
                }
            }
            if (!match) continue;
            Transform root = tr.root;
            if (root == robot.root) continue;
            if (!explicitHumans.Contains(root)) explicitHumans.Add(root);
        }
    }

    private void ScanLidar360(Vector3 origin)
    {
        humanPoints.Clear();
        plantPoints.Clear();
        bushPoints.Clear();
        objectPoints.Clear();

        CollectHumanPoints(origin);
        ScanRays(origin);
        CollectExplicitRowPoints(origin);
        LimitTotalPoints();
    }

    private void ScanRays(Vector3 origin)
    {
        if (combinedObjectMask == 0 || lidarBeamCount < 2) return;

        Vector3 rayOrigin = origin + Vector3.up * 0.55f;
        float startDeg = -180f;
        float step = 360f / (lidarBeamCount - 1);

        for (int i = 0; i < lidarBeamCount; i++)
        {
            float a = startDeg + step * i;
            Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
            int hitCount = Physics.RaycastNonAlloc(rayOrigin, dir, rayHits, lidarMaxRange, combinedObjectMask, triggerInteraction);
            if (hitCount <= 0) continue;

            SortHitsByDistance(hitCount);
            int used = 0;
            for (int h = 0; h < hitCount && used < maxReturnsPerBeam; h++)
            {
                Collider c = rayHits[h].collider;
                if (c == null) continue;
                if (c.transform.root == robot.root) continue;

                Vector3 p = rayHits[h].point;
                p.y = 0f;

                int layerBit = 1 << c.gameObject.layer;
                if ((humanMask.value & layerBit) != 0)
                {
                    // humans are handled as one point by CollectHumanPoints
                }
                else if ((plantMask.value & layerBit) != 0)
                {
                    plantPoints.Add(p); used++;
                }
                else if ((bushMask.value & layerBit) != 0)
                {
                    bushPoints.Add(p); used++;
                }
                else if ((objectMask.value & layerBit) != 0)
                {
                    objectPoints.Add(p); used++;
                }

                if (drawDebugRays) Debug.DrawLine(rayOrigin, rayHits[h].point, Color.cyan, lidarScanPeriod);
            }
        }
    }

    private void SortHitsByDistance(int hitCount)
    {
        // Small insertion sort; rayHits length is small, avoids LINQ allocations.
        for (int i = 1; i < hitCount; i++)
        {
            RaycastHit key = rayHits[i];
            int j = i - 1;
            while (j >= 0 && rayHits[j].distance > key.distance)
            {
                rayHits[j + 1] = rayHits[j];
                j--;
            }
            rayHits[j + 1] = key;
        }
    }

    private void CollectExplicitRowPoints(Vector3 origin)
    {
        if (!useExplicitCorridorRows || explicitRows.Count == 0) return;

        int added = 0;
        for (int r = 0; r < explicitRows.Count && added < maxExplicitRowPoints; r++)
        {
            Transform row = explicitRows[r];
            if (row == null) continue;

            Bounds b;
            if (!TryGetRowBounds(row, out b)) continue;

            Vector3 center = b.center;
            float sx = b.size.x;
            float sz = b.size.z;
            bool longAlongZ = sz >= sx;
            float length = longAlongZ ? sz : sx;
            int samples = Mathf.Clamp(Mathf.CeilToInt(length / Mathf.Max(0.1f, explicitRowPointSpacing)), 3, 80);

            for (int i = 0; i < samples && added < maxExplicitRowPoints; i++)
            {
                float u = samples == 1 ? 0.5f : (float)i / (samples - 1);
                Vector3 p = center;
                if (longAlongZ)
                    p.z = Mathf.Lerp(b.min.z, b.max.z, u);
                else
                    p.x = Mathf.Lerp(b.min.x, b.max.x, u);

                // Use nearest side face to the robot to make the corridor walls visible.
                if (longAlongZ)
                {
                    float leftX = b.min.x;
                    float rightX = b.max.x;
                    p.x = Mathf.Abs(origin.x - leftX) < Mathf.Abs(origin.x - rightX) ? leftX : rightX;
                }
                else
                {
                    float lowZ = b.min.z;
                    float highZ = b.max.z;
                    p.z = Mathf.Abs(origin.z - lowZ) < Mathf.Abs(origin.z - highZ) ? lowZ : highZ;
                }

                p.y = 0f;
                float d = Vector3.Distance(new Vector3(origin.x, 0f, origin.z), p);
                if (d <= lidarMaxRange)
                {
                    plantPoints.Add(p);
                    added++;
                }
            }
        }
    }

    private bool TryGetRowBounds(Transform row, out Bounds bounds)
    {
        Renderer[] renderers = row.GetComponentsInChildren<Renderer>();
        Collider[] colliders = row.GetComponentsInChildren<Collider>();
        bool has = false;
        bounds = new Bounds(row.position, Vector3.zero);

        if (useRendererBoundsForRows)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (!has) { bounds = renderers[i].bounds; has = true; }
                else bounds.Encapsulate(renderers[i].bounds);
            }
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;
            if (!has) { bounds = colliders[i].bounds; has = true; }
            else bounds.Encapsulate(colliders[i].bounds);
        }
        return has;
    }

    private void CollectHumanPoints(Vector3 origin)
    {
        // 1) Explicit / name-based humans. This avoids LayerMask problems.
        for (int i = 0; i < explicitHumans.Count && humanPoints.Count < maxHumans; i++)
        {
            Transform h = explicitHumans[i];
            if (h == null || h.root == robot.root) continue;
            Vector3 hp = Flat(h.position);
            if (Vector3.Distance(Flat(origin), hp) > humanSearchRadius) continue;
            AddUniquePoint(humanPoints, hp, 0.45f);
        }

        // 2) LayerMask humans, if configured.
        if (humanMask.value != 0 && humanPoints.Count < maxHumans)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, humanSearchRadius, humanHits, humanMask, triggerInteraction);
            for (int i = 0; i < count && humanPoints.Count < maxHumans; i++)
            {
                Collider c = humanHits[i];
                if (c == null || c.transform.root == robot.root) continue;
                Vector3 hp = Flat(c.transform.root.position);
                AddUniquePoint(humanPoints, hp, 0.45f);
            }
        }
    }

    private void AddUniquePoint(List<Vector3> list, Vector3 p, float minSeparation)
    {
        for (int k = 0; k < list.Count; k++)
            if (Vector3.Distance(list[k], p) < minSeparation) return;
        list.Add(p);
    }

    private Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

    private void LimitTotalPoints()
    {
        TrimList(plantPoints, maxTotalPointsPerPacket / 2);
        TrimList(bushPoints, maxTotalPointsPerPacket / 4);
        TrimList(objectPoints, maxTotalPointsPerPacket / 3);
        TrimList(humanPoints, maxHumans);
    }

    private void TrimList(List<Vector3> list, int maxCount)
    {
        if (maxCount < 1) maxCount = 1;
        while (list.Count > maxCount) list.RemoveAt(list.Count - 1);
    }

    private float ComputeClosestHumanDistance(Vector3 pos)
    {
        float minDist = Mathf.Infinity;
        for (int i = 0; i < humanPoints.Count; i++)
        {
            float d = Vector3.Distance(Flat(pos), humanPoints[i]);
            if (d < minDist) minDist = d;
        }
        if (!float.IsInfinity(minDist)) return minDist;
        if (human != null) return Vector3.Distance(pos, human.position);
        return humanSearchRadius;
    }

    private string BuildPacket(float t, float dist, float vel, float acc, Vector3 pos, Vector3 heading)
    {
        // IMPORTANT: Numeric comma-separated CSV only, because the MATLAB dashboard parser
        // reads the packet with str2double(split(data, ',')). No labels or semicolons.
        StringBuilder sb = new StringBuilder(8192);
        sb.Append(t.ToString("F4", CI)).Append(',')
          .Append(dist.ToString("F4", CI)).Append(',')
          .Append(vel.ToString("F4", CI)).Append(',')
          .Append(acc.ToString("F4", CI)).Append(',')
          .Append(pos.x.ToString("F4", CI)).Append(',')
          .Append(pos.y.ToString("F4", CI)).Append(',')
          .Append(pos.z.ToString("F4", CI)).Append(',')
          .Append(missionStatus.ToString(CI)).Append(',')
          .Append(methodID.ToString(CI)).Append(',')
          .Append(scenarioID.ToString(CI)).Append(',')
          .Append(heading.x.ToString("F4", CI)).Append(',')
          .Append(heading.z.ToString("F4", CI));

        AppendNumericPointSection(sb, humanPoints);
        AppendNumericPointSection(sb, plantPoints);
        AppendNumericPointSection(sb, bushPoints);
        AppendNumericPointSection(sb, objectPoints);
        return sb.ToString();
    }

    private void AppendNumericPointSection(StringBuilder sb, List<Vector3> pts)
    {
        sb.Append(',').Append(pts.Count.ToString(CI));
        for (int i = 0; i < pts.Count; i++)
        {
            sb.Append(',')
              .Append(pts[i].x.ToString("F3", CI)).Append(',')
              .Append(pts[i].z.ToString("F3", CI));
        }
    }

    private void UpdateMissionStatus(Vector3 robotPos)
    {
        if (missionCompleted) return;
        if (pointA == null || pointB == null) return;

        float distToA = Vector3.Distance(robotPos, pointA.position);
        float distToB = Vector3.Distance(robotPos, pointB.position);

        if (missionType == MissionType.AtoB)
        {
            if (distToB < missionEndThreshold)
            {
                missionStatus = 2;
                missionCompleted = true;
            }
        }
        else
        {
            if (!reachedB && distToB < missionEndThreshold)
            {
                reachedB = true;
                missionStatus = 1;
            }
            if (reachedB && distToA < missionEndThreshold)
            {
                missionStatus = 2;
                missionCompleted = true;
            }
        }
    }

    void OnApplicationQuit() { CloseClient(); }
    void OnDestroy() { CloseClient(); }

    private void CloseClient()
    {
        if (client != null)
        {
            client.Close();
            client = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (robot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(robot.position, lidarMaxRange);
        }
        if (pointA != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pointA.position, missionEndThreshold);
        }
        if (pointB != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pointB.position, missionEndThreshold);
        }
    }
}
