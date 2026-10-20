using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
/// ================================================================
/// SOCIAL NAVIGATION TELEMETRY UDP
/// ================================================================
///
/// Transmisor base de telemetría experimental Unity -> MATLAB.
///
/// Compatible con:
/// RobotExperimentDashboard V7.5 AutoCampaign.
///
/// PAQUETE BASE UDP:
///
///  1. t
///  2. dmin
///  3. speed
///  4. acceleration
///  5. x
///  6. y
///  7. z
///  8. mission_state
///  9. method_id
/// 10. scenario_id
///
/// IMPORTANTE:
/// Este componente transmite SOLO los 10 campos base.
///
/// Los campos posteriores a 10 quedan reservados para
/// transmisores LiDAR extendidos.
///
/// ---------------------------------------------------------------
/// METHOD ID
///
/// M0 = 0
/// M1 = 1
/// M2 = 2
/// M3 = 3
/// M4 = 4
/// B1 = 5
/// B2 = 6
/// B3 = 7
/// B4 = 8
///
/// ---------------------------------------------------------------
/// MISSION STATE / MATLAB PROTOCOL
///
/// 0 = A -> B / misión activa
/// 1 = B alcanzado / B -> A
/// 2 = SUCCESS / A -> B -> A completado
/// 3 = TIMEOUT
/// 4 = COLLISION
/// 5 = STUCK
/// 6 = ABORTED
///
/// ---------------------------------------------------------------
/// SCENARIO ID
///
/// E1 = 1
/// E2 = 2
/// E3 = 3
/// E4 = 4
///
/// ================================================================
/// </summary>
public class SocialNavigationTelemetryUDP : MonoBehaviour
{
    // ============================================================
    // PROTOCOLO MATLAB - MISSION STATUS
    // ============================================================

    public const int STATUS_RUNNING_TO_B = 0;
    public const int STATUS_RETURNING_TO_A = 1;
    public const int STATUS_SUCCESS = 2;
    public const int STATUS_TIMEOUT = 3;
    public const int STATUS_COLLISION = 4;
    public const int STATUS_STUCK = 5;
    public const int STATUS_ABORTED = 6;


    // ============================================================
    // CONTROLLER SOURCES
    // ============================================================

    [Header("Controller source")]

    [Tooltip(
        "Controlador principal del robot. " +
        "Se usa como fuente estándar de distancia al humano."
    )]
    public RobotSocialNavController robotSocialNavController;


    [Tooltip(
        "Controlador externo B1. " +
        "Se mantiene como fallback de métricas."
    )]
    public SocialDWAController b1SocialDWAController;


    [Tooltip(
        "Baseline externo si existe. " +
        "Se mantiene como fallback de métricas."
    )]
    public SocialBaselineBase baselineController;


    // ============================================================
    // UDP
    // ============================================================

    [Header("UDP")]

    [Tooltip("IP del receptor MATLAB.")]
    public string remoteIP = "127.0.0.1";


    [Tooltip("Puerto UDP utilizado por MATLAB.")]
    public int remotePort = 55000;


    // ============================================================
    // IDENTIDAD EXPERIMENTAL
    // ============================================================

    [Header("Identidad experimental MATLAB")]

    [Tooltip(
        "ID numérico del método experimental. " +
        "M0=0 ... B4=8."
    )]
    [Range(0, 8)]
    public int methodID = 0;


    [Tooltip(
        "ID numérico del escenario. " +
        "E1=1 ... E4=4."
    )]
    [Range(1, 4)]
    public int scenarioID = 1;


    [Tooltip(
        "Estado de misión enviado a MATLAB. " +
        "0=A->B, 1=B->A, 2=SUCCESS, " +
        "3=TIMEOUT, 4=COLLISION, " +
        "5=STUCK, 6=ABORTED."
    )]
    [Range(0, 6)]
    public int missionState = STATUS_RUNNING_TO_B;


    [Tooltip(
        "Si está activo, methodID se sincroniza " +
        "automáticamente con RobotSocialNavController.experimentMode."
    )]
    public bool autoReadMethodFromRobot = true;


    // ============================================================
    // CONTROL DE MEDICIÓN
    // ============================================================

    [Header("Control de medicion")]

    [Tooltip(
        "TRUE solamente durante la misión física A->B->A. " +
        "Durante RESET/SETTLE se mantiene FALSE para impedir " +
        "que un teleport genere velocidad/aceleración falsa."
    )]
    [SerializeField]
    private bool runMeasurementActive = false;


    public bool RunMeasurementActive =>
        runMeasurementActive;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]

    [Tooltip(
        "Muestra cambios importantes de identidad, " +
        "estado y medición en Console."
    )]
    public bool verboseLogs = true;


    // ============================================================
    // UDP INTERNAL STATE
    // ============================================================

    private UdpClient udp;

    private IPEndPoint endPoint;

    private bool udpReady = false;


    // ============================================================
    // TELEMETRY INTERNAL STATE
    // ============================================================

    private Vector3 previousPosition;

    private float previousSpeed = 0f;


    // ============================================================
    // ERROR LOG THROTTLING
    // ============================================================

    private float nextUdpErrorLogTime = 0f;

    private const float udpErrorLogInterval = 2.0f;


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // Intentar encontrar automáticamente el controlador
        // principal si no fue asignado por Inspector.
        // --------------------------------------------------------

        if (robotSocialNavController == null)
        {
            robotSocialNavController =
                GetComponent<RobotSocialNavController>();
        }


        // --------------------------------------------------------
        // B1 fallback
        // --------------------------------------------------------

        if (b1SocialDWAController == null)
        {
            b1SocialDWAController =
                GetComponent<SocialDWAController>();
        }


        // --------------------------------------------------------
        // Baseline fallback
        // --------------------------------------------------------

        if (baselineController == null)
        {
            baselineController =
                GetComponent<SocialBaselineBase>();
        }
    }


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        InitializeUdp();

        previousPosition =
            transform.position;

        previousSpeed =
            0f;


        // --------------------------------------------------------
        // Al comenzar Unity todavía NO estamos midiendo una
        // corrida experimental.
        //
        // Seguimos enviando paquetes, pero speed/acceleration
        // serán cero hasta que ScenarioManager active RUN.
        // --------------------------------------------------------

        runMeasurementActive =
            false;


        if (verboseLogs)
        {
            Debug.Log(
                "========================================\n" +
                "[SocialNavigationTelemetryUDP] START\n" +
                "Remote = " +
                remoteIP +
                ":" +
                remotePort +
                "\nMethodID = " +
                methodID +
                "\nScenarioID = " +
                scenarioID +
                "\nMissionState = " +
                missionState +
                "\nRunMeasurementActive = " +
                runMeasurementActive +
                "\n========================================"
            );
        }
    }


    // ============================================================
    // INITIALIZE UDP
    // ============================================================

    private void InitializeUdp()
    {
        udpReady =
            false;


        // --------------------------------------------------------
        // Validar puerto
        // --------------------------------------------------------

        if (remotePort <= 0 ||
            remotePort > 65535)
        {
            Debug.LogError(
                "[SocialNavigationTelemetryUDP] " +
                "Puerto UDP inválido: " +
                remotePort
            );

            return;
        }


        // --------------------------------------------------------
        // Validar IP
        // --------------------------------------------------------

        IPAddress ipAddress;

        if (!IPAddress.TryParse(
                remoteIP,
                out ipAddress))
        {
            Debug.LogError(
                "[SocialNavigationTelemetryUDP] " +
                "IP inválida: " +
                remoteIP
            );

            return;
        }


        // --------------------------------------------------------
        // Crear socket UDP
        // --------------------------------------------------------

        try
        {
            udp =
                new UdpClient();

            endPoint =
                new IPEndPoint(
                    ipAddress,
                    remotePort
                );

            udpReady =
                true;


            if (verboseLogs)
            {
                Debug.Log(
                    "[SocialNavigationTelemetryUDP] " +
                    "UDP preparado correctamente -> " +
                    remoteIP +
                    ":" +
                    remotePort
                );
            }
        }
        catch (System.Exception ex)
        {
            udpReady =
                false;

            Debug.LogError(
                "[SocialNavigationTelemetryUDP] " +
                "No se pudo inicializar UDP: " +
                ex.Message
            );
        }
    }


    // ============================================================
    // SET EXPERIMENT IDENTITY
    // ============================================================

    /// <summary>
    /// Configura la identidad de la corrida que MATLAB recibirá.
    ///
    /// method:
    /// 0..8 = M0..B4
    ///
    /// scenario:
    /// 1..4 = E1..E4
    /// </summary>
    public void SetExperimentIdentity(
        int methodCode,
        int scenarioCode)
    {
        // --------------------------------------------------------
        // Validar método
        // --------------------------------------------------------

        if (methodCode < 0 ||
            methodCode > 8)
        {
            Debug.LogError(
                "[SocialNavigationTelemetryUDP] " +
                "MethodID inválido: " +
                methodCode +
                ". Debe estar entre 0 y 8."
            );

            return;
        }


        // --------------------------------------------------------
        // Validar escenario
        // --------------------------------------------------------

        if (scenarioCode < 1 ||
            scenarioCode > 4)
        {
            Debug.LogError(
                "[SocialNavigationTelemetryUDP] " +
                "ScenarioID inválido: " +
                scenarioCode +
                ". Debe estar entre 1 y 4."
            );

            return;
        }


        bool changed =
            methodID != methodCode ||
            scenarioID != scenarioCode;


        methodID =
            methodCode;

        scenarioID =
            scenarioCode;


        if (verboseLogs &&
            changed)
        {
            Debug.Log(
                "========================================\n" +
                "[SocialNavigationTelemetryUDP] " +
                "EXPERIMENT IDENTITY UPDATED\n" +
                "MethodID = " +
                methodID +
                "\nScenarioID = " +
                scenarioID +
                "\n========================================"
            );
        }
    }


    // ============================================================
    // OVERLOAD CON ENUM
    // ============================================================

    public void SetExperimentIdentity(
        RobotSocialNavController.ExperimentMode method,
        int scenarioCode)
    {
        SetExperimentIdentity(
            (int)method,
            scenarioCode
        );
    }


    // ============================================================
    // SET MISSION STATE
    // ============================================================

    public void SetMissionState(
        int newState)
    {
        if (newState < STATUS_RUNNING_TO_B ||
            newState > STATUS_ABORTED)
        {
            Debug.LogError(
                "[SocialNavigationTelemetryUDP] " +
                "MissionState inválido: " +
                newState
            );

            return;
        }


        bool changed =
            missionState != newState;


        missionState =
            newState;


        if (verboseLogs &&
            changed)
        {
            Debug.Log(
                "[SocialNavigationTelemetryUDP] " +
                "MissionState = " +
                missionState
            );
        }
    }


    // ============================================================
    // CONTROL DE MEDICIÓN DE LA CORRIDA
    // ============================================================

    /// <summary>
    /// Activa/desactiva el cálculo físico de velocidad
    /// y aceleración.
    ///
    /// FALSE:
    /// RESET / SETTLE / entre corridas.
    /// Se transmiten paquetes con speed=0 y acceleration=0.
    ///
    /// TRUE:
    /// RUN A->B->A.
    /// </summary>
    public void SetRunMeasurementActive(
        bool active)
    {
        runMeasurementActive =
            active;


        // --------------------------------------------------------
        // Al cambiar de fase sincronizamos la posición anterior.
        //
        // Esto elimina:
        // - velocidad falsa de teleport
        // - aceleración falsa de teleport
        // - memoria de la corrida anterior
        // --------------------------------------------------------

        ResetTelemetryState();


        if (verboseLogs)
        {
            Debug.Log(
                "[SocialNavigationTelemetryUDP] " +
                "RunMeasurementActive = " +
                runMeasurementActive
            );
        }
    }


    // ============================================================
    // RESET DE TELEMETRIA ENTRE CORRIDAS
    // ============================================================

    public void ResetTelemetryState()
    {
        previousPosition =
            transform.position;

        previousSpeed =
            0f;


        if (verboseLogs)
        {
            Debug.Log(
                "[SocialNavigationTelemetryUDP] " +
                "Estado de telemetria reiniciado" +
                " | Position = " +
                previousPosition
            );
        }
    }


    // ============================================================
    // FIXED UPDATE
    // ============================================================

    private void FixedUpdate()
    {
        // --------------------------------------------------------
        // Si UDP no pudo inicializarse, no intentar enviar.
        // --------------------------------------------------------

        if (!udpReady ||
            udp == null ||
            endPoint == null)
        {
            return;
        }


        // ========================================================
        // 1. SINCRONIZAR METHOD ID CON ROBOT
        // ========================================================

        if (autoReadMethodFromRobot &&
            robotSocialNavController != null)
        {
            methodID =
                (int)
                robotSocialNavController.experimentMode;
        }


        // ========================================================
        // 2. LEER DISTANCIA SOCIAL
        // ========================================================

        float minHumanDistance =
            ReadMinHumanDistance();


        // ========================================================
        // 3. CALCULAR VELOCIDAD / ACELERACIÓN
        // ========================================================

        float speed =
            0f;

        float accelNum =
            0f;


        float dt =
            Mathf.Max(
                Time.fixedDeltaTime,
                1e-5f
            );


        if (runMeasurementActive)
        {
            speed =
                Vector3.Distance(
                    transform.position,
                    previousPosition
                ) /
                dt;


            accelNum =
                (
                    speed -
                    previousSpeed
                ) /
                dt;
        }
        else
        {
            // ----------------------------------------------------
            // RESET / SETTLE / END:
            //
            // MATLAB sigue recibiendo heartbeat e identidad,
            // pero NO detectará un falso MISSION START
            // por teleport.
            // ----------------------------------------------------

            speed =
                0f;

            accelNum =
                0f;
        }


        // ========================================================
        // 4. CREAR PAQUETE BASE MATLAB
        // ========================================================
        //
        // EXACTAMENTE 10 CAMPOS:
        //
        // 1  time
        // 2  minHumanDistance
        // 3  speed
        // 4  acceleration
        // 5  x
        // 6  y
        // 7  z
        // 8  missionState
        // 9  methodID
        // 10 scenarioID
        //
        // ========================================================

        string packet =
            string.Join(
                ",",

                Time.time.ToString(
                    "F3",
                    CultureInfo.InvariantCulture
                ),

                minHumanDistance.ToString(
                    "F4",
                    CultureInfo.InvariantCulture
                ),

                speed.ToString(
                    "F4",
                    CultureInfo.InvariantCulture
                ),

                accelNum.ToString(
                    "F4",
                    CultureInfo.InvariantCulture
                ),

                transform.position.x.ToString(
                    "F4",
                    CultureInfo.InvariantCulture
                ),

                transform.position.y.ToString(
                    "F4",
                    CultureInfo.InvariantCulture
                ),

                transform.position.z.ToString(
                    "F4",
                    CultureInfo.InvariantCulture
                ),

                missionState.ToString(
                    CultureInfo.InvariantCulture
                ),

                methodID.ToString(
                    CultureInfo.InvariantCulture
                ),

                scenarioID.ToString(
                    CultureInfo.InvariantCulture
                )
            );


        // ========================================================
        // 5. ENVIAR UDP
        // ========================================================

        try
        {
            byte[] data =
                Encoding.UTF8.GetBytes(
                    packet
                );


            udp.Send(
                data,
                data.Length,
                endPoint
            );
        }
        catch (System.Exception ex)
        {
            // ----------------------------------------------------
            // Evitar inundar Console a 50 Hz si MATLAB está
            // cerrado o existe un error de red.
            // ----------------------------------------------------

            if (Time.unscaledTime >=
                nextUdpErrorLogTime)
            {
                nextUdpErrorLogTime =
                    Time.unscaledTime +
                    udpErrorLogInterval;


                Debug.LogWarning(
                    "[SocialNavigationTelemetryUDP] " +
                    "Error enviando UDP: " +
                    ex.Message
                );
            }
        }


        // ========================================================
        // 6. ACTUALIZAR MEMORIA
        // ========================================================
        //
        // Incluso durante RESET sincronizamos previousPosition
        // con el Transform actual.
        //
        // Así un teleport jamás queda almacenado como un
        // desplazamiento físico de la siguiente muestra.
        // ========================================================

        previousPosition =
            transform.position;


        if (runMeasurementActive)
        {
            previousSpeed =
                speed;
        }
        else
        {
            previousSpeed =
                0f;
        }
    }


    // ============================================================
    // DISTANCIA MÍNIMA AL HUMANO
    // ============================================================

    private float ReadMinHumanDistance()
    {
        // ========================================================
        // FUENTE PRINCIPAL
        //
        // RobotSocialNavController usa el mismo detector para
        // M0-M4 y B1-B4.
        //
        // Esto permite comparar todos los métodos utilizando
        // exactamente la misma variable de distancia.
        // ========================================================

        if (robotSocialNavController != null)
        {
            float distance =
                robotSocialNavController
                    .NearestHumanDistance;


            return SanitizeDistance(
                distance
            );
        }


        // ========================================================
        // FALLBACK 1:
        // SocialBaselineBase
        // ========================================================

        if (baselineController != null)
        {
            SocialDWAMetrics metrics =
                baselineController.LastMetrics;


            return SanitizeDistance(
                metrics.minHumanDistance
            );
        }


        // ========================================================
        // FALLBACK 2:
        // B1 Social DWA externo
        // ========================================================

        if (b1SocialDWAController != null)
        {
            SocialDWAMetrics metrics =
                b1SocialDWAController.LastMetrics;


            return SanitizeDistance(
                metrics.minHumanDistance
            );
        }


        // ========================================================
        // SIN SENSOR DISPONIBLE
        // ========================================================

        return 999f;
    }


    // ============================================================
    // SANITIZE DISTANCE
    // ============================================================

    private float SanitizeDistance(
        float distance)
    {
        if (float.IsNaN(distance) ||
            float.IsInfinity(distance) ||
            distance < 0f)
        {
            return 999f;
        }


        return distance;
    }


    // ============================================================
    // ON DESTROY
    // ============================================================

    private void OnDestroy()
    {
        udpReady =
            false;


        if (udp != null)
        {
            try
            {
                udp.Close();
            }
            catch
            {
                // No hacer nada durante shutdown.
            }


            udp =
                null;
        }
    }
}