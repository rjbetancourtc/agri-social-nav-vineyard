using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class RobotTelemetryPanel : MonoBehaviour
{
    [Header("Referencias")]
    public Transform robotRoot;
    public NavMeshAgent agent;
    public TextMeshProUGUI telemetryText;
    public Image panelBackground;

    [Header("Detección de humanos")]
    public LayerMask humanMask;
    public float detectionRadius = 10f;
    public float alertDistance = 2f;

    private Vector3 previousPosition;
    private Vector3 previousVelocity;
    private readonly Collider[] hits = new Collider[64];

    void Start()
    {
        if (robotRoot == null)
        {
            Debug.LogError("RobotTelemetryPanel: falta asignar robotRoot.");
            enabled = false;
            return;
        }

        if (agent == null)
            agent = robotRoot.GetComponent<NavMeshAgent>();

        previousPosition = robotRoot.position;
        previousVelocity = Vector3.zero;
    }

    void Update()
    {
        if (telemetryText == null || robotRoot == null)
            return;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        Vector3 position = robotRoot.position;

        Vector3 velocity = (position - previousPosition) / dt;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            velocity = agent.velocity;

        Vector3 acceleration = (velocity - previousVelocity) / dt;

        previousPosition = position;
        previousVelocity = velocity;

        string humanName = "Ninguno";
        string distanceTextValue = "---";
        string proximityState = "SIN HUMANOS";
        float nearestDistance = float.PositiveInfinity;

        int count = Physics.OverlapSphereNonAlloc(
            position,
            detectionRadius,
            hits,
            humanMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            if (hits[i] == null) continue;

            Transform root = hits[i].transform.root;

            if (robotRoot != null && root == robotRoot)
                continue;

            Vector3 closest = hits[i].ClosestPoint(position);
            float d = Vector3.Distance(position, closest);

            if (d < nearestDistance)
            {
                nearestDistance = d;
                humanName = root.name;
            }
        }

        if (nearestDistance < float.PositiveInfinity)
        {
            distanceTextValue = nearestDistance.ToString("F2") + " m";
            proximityState = nearestDistance <= alertDistance ? "ALERTA" : "MONITOREO";
        }

        string navDestination = "---";
        string remainingDistance = "---";

        if (agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath)
        {
            navDestination = $"({agent.destination.x:F2}, {agent.destination.y:F2}, {agent.destination.z:F2})";
            remainingDistance = agent.remainingDistance.ToString("F2") + " m";
        }

        telemetryText.text =
            $"POSICIÓN XYZ: ({position.x:F2}, {position.y:F2}, {position.z:F2})\n" +
            $"VELOCIDAD |v|: {velocity.magnitude:F2} m/s\n" +
            $"ACELERACIÓN |a|: {acceleration.magnitude:F2} m/s²\n" +
            $"VELOCIDAD XYZ: ({velocity.x:F2}, {velocity.y:F2}, {velocity.z:F2})\n" +
            $"HUMANO MÁS CERCANO: {humanName}\n" +
            $"DISTANCIA HUMANO: {distanceTextValue}\n" +
            $"ESTADO PROXIMIDAD: {proximityState}\n" +
            $"DESTINO NAVMESH: {navDestination}\n" +
            $"DISTANCIA RESTANTE: {remainingDistance}";

        if (panelBackground != null)
        {
            if (proximityState == "ALERTA")
                panelBackground.color = new Color(0.45f, 0.12f, 0.12f, 0.85f);
            else if (proximityState == "MONITOREO")
                panelBackground.color = new Color(0.12f, 0.12f, 0.12f, 0.80f);
            else
                panelBackground.color = new Color(0.10f, 0.10f, 0.10f, 0.75f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (robotRoot == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(robotRoot.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(robotRoot.position, alertDistance);
    }
}