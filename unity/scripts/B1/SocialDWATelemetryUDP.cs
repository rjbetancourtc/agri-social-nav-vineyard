using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
/// Optional UDP telemetry sender for Social-DWA.
/// Packet format:
/// t, dmin, speed, accel_num, x, y, z, mission_state, method_id, scenario_id, cpu_ms, dwa_cost, rejected
/// 
/// Suggested method_id:
/// M0=0, M1=1, M2=2, M3=3, M4=4, B1 Social-DWA=5.
/// </summary>
public class SocialDWATelemetryUDP : MonoBehaviour
{
    public SocialDWAController controller;
    public string remoteIP = "127.0.0.1";
    public int remotePort = 5055;
    public int methodID = 5;
    public int scenarioID = 1;
    public int missionState = 0;

    private UdpClient udp;
    private IPEndPoint endPoint;
    private Vector3 previousPosition;
    private float previousSpeed;

    private void Start()
    {
        udp = new UdpClient();
        endPoint = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
        previousPosition = transform.position;
        previousSpeed = 0f;
    }

    private void FixedUpdate()
    {
        if (controller == null)
        {
            return;
        }

        float dt = Time.fixedDeltaTime;
        float speed = Vector3.Distance(transform.position, previousPosition) / Mathf.Max(dt, 1e-5f);
        float accelNum = (speed - previousSpeed) / Mathf.Max(dt, 1e-5f);

        SocialDWAMetrics m = controller.LastMetrics;

        string packet = string.Join(",",
            Time.time.ToString("F3", CultureInfo.InvariantCulture),
            m.minHumanDistance.ToString("F4", CultureInfo.InvariantCulture),
            speed.ToString("F4", CultureInfo.InvariantCulture),
            accelNum.ToString("F4", CultureInfo.InvariantCulture),
            transform.position.x.ToString("F4", CultureInfo.InvariantCulture),
            transform.position.y.ToString("F4", CultureInfo.InvariantCulture),
            transform.position.z.ToString("F4", CultureInfo.InvariantCulture),
            missionState.ToString(CultureInfo.InvariantCulture),
            methodID.ToString(CultureInfo.InvariantCulture),
            scenarioID.ToString(CultureInfo.InvariantCulture),
            m.cpuMs.ToString("F4", CultureInfo.InvariantCulture),
            m.bestCost.ToString("F6", CultureInfo.InvariantCulture),
            m.rejectedCandidates.ToString(CultureInfo.InvariantCulture)
        );

        byte[] data = Encoding.UTF8.GetBytes(packet);
        udp.Send(data, data.Length, endPoint);

        previousPosition = transform.position;
        previousSpeed = speed;
    }

    private void OnDestroy()
    {
        if (udp != null)
        {
            udp.Close();
            udp = null;
        }
    }
}