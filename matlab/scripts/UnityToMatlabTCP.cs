using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Globalization;

/// <summary>
/// UnityToMatlabUDP V3 - Telemetria con deteccion de inicio/fin de mision A->B->A
/// 
/// Formato del paquete UDP enviado a MATLAB:
///   t,dist,vel,acc,px,py,pz,status,method,scenario
/// 
/// Donde:
///   status:    0 = navegando hacia B
///              1 = paso por B (yendo a A)
///              2 = MISION COMPLETADA (regreso a A)
///   method:    0..4 (M0..M4)
///   scenario:  1..4 (E1..E4)
/// 
/// Compatible con RobotExperimentDashboard_V7 (MATLAB).
/// 
/// FLUJO:
///   1. Pulsa Play en Unity (este script empieza a transmitir)
///   2. Pulsa INICIAR en MATLAB (empieza a recibir)
///   3. El robot patrulla A->B->A
///   4. Al regresar a A, status=2 -> MATLAB auto-guarda
///   5. Detener Unity, preparar siguiente corrida
/// </summary>
public class UnityToMatlabUDP : MonoBehaviour
{
    // =========================================================================
    // REFERENCIAS
    // =========================================================================
    [Header("Referencias")]
    public Transform robot;
    [Tooltip("Humano de respaldo si no se encuentra ninguno por LayerMask.")]
    public Transform human;

    [Header("Puntos de patrulla A<->B")]
    [Tooltip("Punto inicial Y FINAL del recorrido (Point_A_R)")]
    public Transform pointA;
    [Tooltip("Punto intermedio del recorrido (Point_B_R)")]
    public Transform pointB;
    [Tooltip("Distancia al punto destino para considerar que llego")]
    public float missionEndThreshold = 0.6f;
    [Tooltip("Tipo de mision: A->B (un trayecto) o A->B->A (ida y vuelta)")]
    public MissionType missionType = MissionType.AtoBtoA;

    public enum MissionType { AtoB, AtoBtoA }

    // =========================================================================
    // DETECCION MULTI-HUMANO
    // =========================================================================
    [Header("Deteccion multi-humano")]
    public bool useClosestHuman = true;
    public LayerMask humanMask;
    public float humanSearchRadius = 12f;

    // =========================================================================
    // CONFIGURACION EXPERIMENTAL
    // =========================================================================
    [Header("Configuracion experimental")]
    [Tooltip("Metodo activo (0=M0, 1=M1, ..., 4=M4)")]
    [Range(0, 4)]
    public int methodID = 0;
    [Tooltip("Escenario activo (1=E1, 2=E2, 3=E3, 4=E4)")]
    [Range(1, 4)]
    public int scenarioID = 1;

    // =========================================================================
    // CONEXION UDP
    // =========================================================================
    [Header("Conexion UDP")]
    public string ip = "127.0.0.1";
    public int port = 55000;

    // =========================================================================
    // MUESTREO
    // =========================================================================
    [Header("Muestreo")]
    public float sampleTime = 0.1f;   // 10 Hz
    private float timer = 0f;

    // =========================================================================
    // DETECCION DE INICIO DE MISION
    // =========================================================================
    [Header("Inicio de mision")]
    [Tooltip("Velocidad minima para considerar que el robot empezo a moverse")]
    public float minVelocityToStart = 0.10f;
    [Tooltip("Tiempo de espera antes de empezar a transmitir (deja que Unity inicialice)")]
    public float warmupTime = 0.5f;

    // =========================================================================
    // SUAVIZADO
    // =========================================================================
    [Header("Suavizado")]
    [Range(0f, 1f)]
    public float velSmoothing = 0.2f;

    // =========================================================================
    // FEEDBACK
    // =========================================================================
    [Header("Debug")]
    public bool verboseLog = true;

    // =========================================================================
    // ESTADO INTERNO
    // =========================================================================
    private UdpClient client;
    private Vector3 prevPos;
    private Vector3 prevVel;
    private Vector3 smoothVel;
    private readonly Collider[] humanHits = new Collider[64];

    private int missionStatus = 0;
    private bool reachedB = false;
    private bool missionStarted = false;
    private bool missionCompleted = false;
    private float missionStartTime = 0f;
    private float gameStartTime = 0f;

    // =========================================================================
    // START
    // =========================================================================
    void Start()
    {
        client = new UdpClient();

        if (robot != null)
        {
            prevPos = robot.position;
            prevVel = Vector3.zero;
            smoothVel = Vector3.zero;
        }

        // Reset completo de estado de mision
        missionStatus = 0;
        reachedB = false;
        missionStarted = false;
        missionCompleted = false;
        missionStartTime = 0f;
        gameStartTime = Time.time;

        Debug.Log("====================================");
        Debug.Log("[UDP V3] Telemetria iniciada");
        Debug.Log(string.Format(
            "[UDP V3] M{0} - E{1} - Tipo: {2} - Puerto: {3}",
            methodID, scenarioID, missionType, port));
        Debug.Log("[UDP V3] Esperando movimiento del robot...");
        Debug.Log("====================================");
    }

    // =========================================================================
    // UPDATE
    // =========================================================================
    void Update()
    {
        if (robot == null) return;

        timer += Time.deltaTime;
        if (timer < sampleTime) return;

        float dt = timer;
        timer = 0f;

        // --- Periodo de calentamiento ---
        // No transmitir nada los primeros 0.5s para que Unity estabilice fisica
        float timeSinceGameStart = Time.time - gameStartTime;
        if (timeSinceGameStart < warmupTime) return;

        // --- Calculos cinematicos ---
        Vector3 pos = robot.position;
        Vector3 vel = (pos - prevPos) / dt;
        vel.y = 0f;

        smoothVel = Vector3.Lerp(smoothVel, vel, 1f - velSmoothing);

        Vector3 acc = (smoothVel - prevVel) / dt;
        acc.y = 0f;

        float dist = ComputeHumanDistance();

        // --- Deteccion de inicio de mision ---
        if (!missionStarted && smoothVel.magnitude > minVelocityToStart)
        {
            missionStarted = true;
            missionStartTime = Time.time;
            if (verboseLog)
            {
                Debug.Log("====================================");
                Debug.Log(string.Format(
                    "[UDP V3] >>> MISION INICIADA en t={0:F2}s",
                    missionStartTime));
                Debug.Log(string.Format(
                    "[UDP V3] Pos inicial: ({0:F2}, {1:F2}, {2:F2})",
                    pos.x, pos.y, pos.z));
                Debug.Log("====================================");
            }
        }

        // No enviar nada antes de que arranque la mision
        if (!missionStarted) return;

        // --- Actualizar estado de mision ---
        UpdateMissionStatus(pos);

        // --- Construir paquete UDP ---
        // Formato: t,dist,vel,acc,px,py,pz,status,method,scenario
        // Tiempo relativo al inicio de mision (mas limpio que Time.time)
        float tMission = Time.time - missionStartTime;

        string data =
            tMission.ToString("F4", CultureInfo.InvariantCulture) + "," +
            dist.ToString("F4", CultureInfo.InvariantCulture) + "," +
            smoothVel.magnitude.ToString("F4", CultureInfo.InvariantCulture) + "," +
            acc.magnitude.ToString("F4", CultureInfo.InvariantCulture) + "," +
            pos.x.ToString("F4", CultureInfo.InvariantCulture) + "," +
            pos.y.ToString("F4", CultureInfo.InvariantCulture) + "," +
            pos.z.ToString("F4", CultureInfo.InvariantCulture) + "," +
            missionStatus.ToString(CultureInfo.InvariantCulture) + "," +
            methodID.ToString(CultureInfo.InvariantCulture) + "," +
            scenarioID.ToString(CultureInfo.InvariantCulture);

        byte[] bytes = Encoding.ASCII.GetBytes(data);

        try
        {
            client.Send(bytes, bytes.Length, ip, port);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[UDP V3] Error enviando paquete: " + e.Message);
        }

        prevPos = pos;
        prevVel = smoothVel;

        // --- Si mision termino, dejar de transmitir despues de unos paquetes finales ---
        if (missionCompleted && timeSinceGameStart > missionStartTime + 1.0f)
        {
            // Continuar transmitiendo unos segundos despues del fin para que MATLAB
            // procese los ultimos paquetes con status=2
        }
    }

    // =========================================================================
    // ACTUALIZAR ESTADO DE MISION
    // =========================================================================
    void UpdateMissionStatus(Vector3 robotPos)
    {
        if (missionCompleted) return;
        if (pointA == null || pointB == null) return;

        float distToA = Vector3.Distance(robotPos, pointA.position);
        float distToB = Vector3.Distance(robotPos, pointB.position);

        if (missionType == MissionType.AtoB)
        {
            // Mision termina al llegar a B
            if (distToB < missionEndThreshold)
            {
                missionStatus = 2;  // usar 2 para A->B tambien (consistencia con MATLAB)
                missionCompleted = true;
                float elapsed = Time.time - missionStartTime;
                if (verboseLog)
                {
                    Debug.Log("====================================");
                    Debug.Log(string.Format(
                        "[UDP V3] *** MISION COMPLETADA (A->B) en {0:F2}s ***",
                        elapsed));
                    Debug.Log(string.Format(
                        "[UDP V3] Distancia final a B: {0:F3}m",
                        distToB));
                    Debug.Log("====================================");
                }
            }
        }
        else  // AtoBtoA
        {
            // Fase 1: ir de A a B
            if (!reachedB && distToB < missionEndThreshold)
            {
                reachedB = true;
                missionStatus = 1;  // marca paso por B
                if (verboseLog)
                {
                    Debug.Log(string.Format(
                        "[UDP V3] === FASE 1: llego a B en {0:F2}s, regresando a A ===",
                        Time.time - missionStartTime));
                }
            }

            // Fase 2: regresar a A
            if (reachedB && distToA < missionEndThreshold)
            {
                missionStatus = 2;  // mision completada
                missionCompleted = true;
                float elapsed = Time.time - missionStartTime;
                if (verboseLog)
                {
                    Debug.Log("====================================");
                    Debug.Log(string.Format(
                        "[UDP V3] *** MISION COMPLETADA (A->B->A) en {0:F2}s ***",
                        elapsed));
                    Debug.Log(string.Format(
                        "[UDP V3] Distancia final a A: {0:F3}m",
                        distToA));
                    Debug.Log("[UDP V3] MATLAB debe haber auto-guardado.");
                    Debug.Log("[UDP V3] Detener Unity y empezar siguiente corrida.");
                    Debug.Log("====================================");
                }
            }
        }
    }

    // =========================================================================
    // DISTANCIA AL HUMANO MAS CERCANO
    // =========================================================================
    float ComputeHumanDistance()
    {
        if (!useClosestHuman)
        {
            if (human != null)
                return Vector3.Distance(robot.position, human.position);
            return humanSearchRadius;
        }

        int count = Physics.OverlapSphereNonAlloc(
            robot.position,
            humanSearchRadius,
            humanHits,
            humanMask,
            QueryTriggerInteraction.Ignore
        );

        float minDist = Mathf.Infinity;

        for (int i = 0; i < count; i++)
        {
            Collider c = humanHits[i];
            if (c == null) continue;

            Transform root = c.transform.root;
            if (root == robot.root) continue;

            Vector3 refPoint = c.ClosestPoint(robot.position);
            float d = Vector3.Distance(robot.position, refPoint);

            if (d < minDist)
                minDist = d;
        }

        if (float.IsInfinity(minDist))
        {
            // Si ningun humano dentro del radio, saturar al radio (sensor limitado)
            return humanSearchRadius;
        }

        return minDist;
    }

    // =========================================================================
    // CIERRE
    // =========================================================================
    void OnApplicationQuit() { CloseClient(); }
    void OnDestroy() { CloseClient(); }

    void CloseClient()
    {
        if (client != null)
        {
            client.Close();
            client = null;
        }
    }

    // =========================================================================
    // VISUALIZACION GIZMO
    // =========================================================================
    void OnDrawGizmosSelected()
    {
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
