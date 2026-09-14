using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Referencia estable entre un ID experimental y el GameObject real de Unity.
/// Ejemplos:
/// E101 -> HumanF_Model
/// E102 -> HumanF_Model (1)
/// E201 -> HumanF_Model (7)
/// </summary>
[System.Serializable]



public class HumanReference
{
    [Tooltip("ID experimental estable, por ejemplo E101, E202, E303, E401.")]
    public string id;

    [Tooltip("GameObject raíz del humano correspondiente.")]
    public GameObject human;
}


/// <summary>
/// Fotografía del estado geométrico inicial de un humano.
/// En esta primera fase NO almacena todavía velocidad,
/// waypoint, NavMeshAgent, temporizadores ni semillas.
/// </summary>
[System.Serializable]
public class HumanState
{
    [Tooltip("ID experimental del humano.")]
    public string id;

    [Tooltip("Estado activo/inactivo cuando se capturó el escenario.")]
    public bool active;

    [Tooltip("Posición mundial original.")]
    public Vector3 position;

    [Tooltip("Rotación mundial original.")]
    public Quaternion rotation;

    [Tooltip("Escala local original.")]
    public Vector3 scale;
}


/// <summary>
/// Preset serializado de un escenario experimental.
/// Contiene los estados iniciales de los humanos
/// pertenecientes exclusivamente a ese escenario.
/// </summary>
[System.Serializable]
public class ScenarioState
{
    [Tooltip("Identificador del escenario: E1, E2, E3 o E4.")]
    public string scenarioID;

    [Tooltip("Estados iniciales capturados para los humanos de este escenario.")]
    public List<HumanState> humans = new List<HumanState>();
}


/// <summary>
/// Administrador de escenarios experimentales.
///
/// RESPONSABILIDADES:
/// 1. Mantiene y restaura los presets E1-E4.
/// 2. Reinicia robot, humanos y estados dinámicos.
/// 3. Ejecuta misiones A -> B -> A.
/// 4. Coordina identidad/status con MATLAB.
/// 5. Ejecuta validación de 2 corridas y campaña completa M0-B4.
/// </summary>
public class ScenarioManager : MonoBehaviour

{

    [ContextMenu("CAPTURE ROBOT INITIAL STATE")]
    public void CaptureRobotInitialState()
    {
        if (robotController == null)
        {
            Debug.LogError(
                "[ScenarioManager] No se puede capturar el robot." +
                " RobotController no está asignado."
            );

            return;
        }

        Transform robotTransform = robotController.transform;

        robotInitialPosition = robotTransform.position;
        robotInitialRotation = robotTransform.rotation;
        robotInitialScale = robotTransform.localScale;

        robotInitialStateCaptured = true;

        Debug.Log(
            "========================================\n" +
            "[ScenarioManager] ROBOT INITIAL STATE CAPTURADO\n" +
            "Robot = " + robotController.gameObject.name + "\n" +
            "Position = " + robotInitialPosition + "\n" +
            "Rotation = " + robotInitialRotation.eulerAngles + "\n" +
            "Scale = " + robotInitialScale + "\n" +
            "========================================"
        );
    }

    [Header("Reproducibilidad experimental")]
    [SerializeField]
    private int experimentSeed = 1001;


    [Header("Robot experimental")]
    [SerializeField]
    private RobotSocialNavController robotController;


    [Header("Estado inicial del robot")]
    [SerializeField]
    private Vector3 robotInitialPosition;

    [SerializeField]
    private Quaternion robotInitialRotation;

    [SerializeField]
    private Vector3 robotInitialScale;

    [SerializeField]
    private bool robotInitialStateCaptured = false;


    // ============================================================
    // ESTADO DE LA MAQUINA EXPERIMENTAL
    // ============================================================

    public enum ExperimentState
    {
        Idle,
        Prepare,
        Reset,
        Settle,
        Run,
        End,
        Export
    }


    // ============================================================
    // ETAPA FISICA DE LA MISION A -> B -> A
    // ============================================================

    public enum MissionLeg
    {
        GoingToB,
        ReturningToA
    }


    // ============================================================
    // RESULTADO DE UNA CORRIDA
    // ============================================================

    public enum RunTermination
    {
        None,
        Success,
        Timeout,
        Collision,
        Stuck,
        Aborted
    }


    [Header("Estado de la maquina experimental")]

    [SerializeField]
    private ExperimentState currentExperimentState =
        ExperimentState.Idle;

    [SerializeField]
    private MissionLeg currentMissionLeg =
        MissionLeg.GoingToB;

    [SerializeField]
    private RunTermination lastRunTermination =
        RunTermination.None;

    public ExperimentState CurrentExperimentState =>
        currentExperimentState;

    public MissionLeg CurrentMissionLeg =>
        currentMissionLeg;

    public RunTermination LastRunTermination =>
        lastRunTermination;


    // ============================================================
    // IDENTIFICACION DE LA CORRIDA ACTIVA
    // ============================================================

    [Header("Corrida experimental activa")]

    [SerializeField]
    private string activeScenarioID = "";

    [SerializeField]
    private int activeRunID = 0;

    [SerializeField]
    private float runStartTime = 0f;

    [SerializeField]
    private float runElapsedTime = 0f;

   [SerializeField]
    private bool experimentRunning = false;

    public bool ExperimentRunning =>
        experimentRunning;


    // ============================================================
    // CRITERIOS DE FINALIZACION A -> B -> A
    // ============================================================

    [Header("Mision A -> B -> A")]

    [Tooltip(
        "Distancia horizontal XZ al waypoint para considerarlo alcanzado."
    )]
    [SerializeField]
    private float waypointArrivalTolerance = 2.0f;

    [Tooltip(
        "Tiempo maximo permitido para completar A -> B -> A."
    )]
    [SerializeField]
    private float maximumRunTime = 180.0f;


    // ============================================================
    // CAMPANA AUTOMATICA
    // ============================================================

    [Header("Campana automatica")]

    [Tooltip(
        "Numero de repeticiones por Metodo x Escenario. " +
        "Para la campana final usar 10."
    )]
    [SerializeField]
    private int campaignRunsPerCell = 10;

    [Tooltip(
        "Semilla base de reproducibilidad. " +
        "Run01=base, Run02=base+1, etc."
    )]
    [SerializeField]
    private int campaignBaseSeed = 1001;

    [Tooltip(
        "Tiempo durante el cual se mantiene el status terminal " +
        "para que MATLAB guarde antes de la siguiente corrida."
    )]
    [SerializeField]
    private float interRunDelaySeconds = 2.0f;

    [Header("Computational Benchmark")]

    [SerializeField]
    private int benchmarkRunsPerCell = 3;

    [SerializeField]
    private float benchmarkInterRunDelaySeconds = 2.0f;

    [SerializeField]
    private bool autoStartComputationalBenchmarkInBuild = false;

    [SerializeField]
    private float benchmarkAutoStartDelaySeconds = 3.0f;

    [SerializeField]
    private bool campaignRunning = false;

    [SerializeField]
    private bool campaignStopRequested = false;

    [SerializeField]
    private int campaignCompletedRuns = 0;

    [SerializeField]
    private int campaignTotalRuns = 0;

    public bool CampaignRunning =>
        campaignRunning;

    public int CampaignCompletedRuns =>
        campaignCompletedRuns;

    public int CampaignTotalRuns =>
        campaignTotalRuns;


    // ============================================================
    // ORDEN OFICIAL DE LOS METODOS EXPERIMENTALES
    // ============================================================

    private static readonly
    RobotSocialNavController.ExperimentMode[] campaignMethods =
    {
        RobotSocialNavController.ExperimentMode.M0_NavMeshOnly,
        RobotSocialNavController.ExperimentMode.M1_ThresholdStop,
        RobotSocialNavController.ExperimentMode.M2_HysteresisSupervisor,
        RobotSocialNavController.ExperimentMode.M3_IsotropicProxemics,
        RobotSocialNavController.ExperimentMode.M4_FullAnisotropicHysteresis,
        RobotSocialNavController.ExperimentMode.B1_SocialDWA,
        RobotSocialNavController.ExperimentMode.B2_ORCA_RVO,
        RobotSocialNavController.ExperimentMode.B3_SocialForce,
        RobotSocialNavController.ExperimentMode.B4_CBF_SocialDWA
    };


    // ============================================================
    // TELEMETRIA EXPERIMENTAL UNITY -> MATLAB
    // ============================================================

    [Header("Telemetria experimental")]

    [SerializeField]
    private UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV telemetry;

    /// <summary>
    /// Auto-conecta el UNICO sender UDP LiDAR del Robot_ROOT.
    /// Evita depender de una asignacion manual en el Inspector.
    /// </summary>
    private void Awake()
    {
        if (telemetry == null && robotController != null)
        {
            telemetry = robotController.GetComponent<
                UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV>();
        }

        if (telemetry == null)
        {
            telemetry = UnityEngine.Object.FindFirstObjectByType<
                UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV>();
        }

        if (telemetry != null)
        {
            telemetry.SetExternalExperimentControl(true);
        }
        else
        {
            Debug.LogError(
                "[ScenarioManager] No se encontro el sender UDP LiDAR experimental."
            );
        }
    }

    // ============================================================
    // COMPUTATIONAL BENCHMARK - AUTO START EN BUILD
    // ============================================================

    private void Start()
    {
        if (autoStartComputationalBenchmarkInBuild)
        {
            StartCoroutine(
                AutoStartComputationalBenchmarkRoutine()
            );
        }
    }


    private IEnumerator AutoStartComputationalBenchmarkRoutine()
    {
        // Dar tiempo a que toda la escena termine de inicializarse.
        yield return new WaitForSecondsRealtime(
            benchmarkAutoStartDelaySeconds
        );

        // En un ejecutable no tendremos Context Menu del Inspector.
        // Si la pose inicial todavía no fue capturada,
        // la capturamos automáticamente.
        if (!robotInitialStateCaptured)
        {
            CaptureRobotInitialState();
        }

        RunComputationalBenchmark();
    }

    // ============================================================
    // APLICACIÓN / RESTAURACIÓN DE ESCENARIOS
    // ============================================================

    // [Header("Robot experimental")]
    // [SerializeField]
    //  private RobotSocialNavController robotController;

    //  [Header("Estado inicial del robot")]
    //  [SerializeField]
    //   private Vector3 robotInitialPosition;

    //   [SerializeField]
    //   private Quaternion robotInitialRotation;

    //   [SerializeField]
    //  private Vector3 robotInitialScale;

    //   [SerializeField]
    //   private bool robotInitialStateCaptured = false;

    // ============================================================
    // APLICACIÓN / RESTAURACIÓN DE ESCENARIOS
    // ============================================================

    /// <summary>
    /// Restaura un escenario previamente capturado.
    ///
    /// Procedimiento:
    /// 1. Desactiva todos los humanos experimentales.
    /// 2. Busca cada humano almacenado en el preset.
    /// 3. Restaura posición, rotación y escala.
    /// 4. Restaura el estado activo original.
    ///
    /// IMPORTANTE:
    /// En esta fase todavía NO restaura:
    /// - NavMeshAgent
    /// - velocidad
    /// - destino
    /// - waypoint
    /// - temporizadores
    /// - semillas aleatorias
    /// - estados internos de scripts
    /// </summary>
    /// 
    /// 
    private int StableIdHash(string text)
    {
        unchecked
        {
            int hash = 17;

            if (string.IsNullOrEmpty(text))
                return hash;

            for (int i = 0; i < text.Length; i++)
            {
                hash = hash * 31 + text[i];
            }

            return hash;
        }
    }
    private bool ApplyScenario(ScenarioState scenario)
    {
        // ============================================================
        // 1. VALIDAR ESCENARIO
        // ============================================================

        if (scenario == null)
        {
            Debug.LogError(
                "[ScenarioManager] APPLY cancelado: " +
                "ScenarioState es NULL."
            );

            return false;
        }


        // ============================================================
        // 2. VALIDAR HUMANOS CAPTURADOS
        // ============================================================

        if (scenario.humans == null ||
            scenario.humans.Count == 0)
        {
            Debug.LogError(
                "[ScenarioManager] APPLY " +
                scenario.scenarioID +
                " cancelado: no existen humanos capturados."
            );

            return false;
        }


        // ============================================================
        // 3. VALIDAR ROBOT
        // ============================================================

        if (robotController == null)
        {
            Debug.LogError(
                "[ScenarioManager] APPLY cancelado: " +
                "RobotController no está asignado."
            );

            return false;
        }


        if (!robotInitialStateCaptured)
        {
            Debug.LogError(
                "[ScenarioManager] APPLY cancelado: " +
                "el estado inicial del robot no ha sido capturado. " +
                "Ejecute CAPTURE ROBOT INITIAL STATE."
            );

            return false;
        }


        // ============================================================
        // 4. LOG DE INICIO
        // ============================================================

        Debug.Log(
            "========================================\n" +
            "[ScenarioManager] APPLY INICIADO\n" +
            "Escenario: " + scenario.scenarioID + "\n" +
            "Humanos esperados: " +
            scenario.humans.Count +
            "\n========================================"
        );


        // ============================================================
        // 5. DESACTIVAR TODOS LOS HUMANOS
        // ============================================================

        foreach (HumanReference reference in humans)
        {
            if (reference == null)
                continue;

            if (reference.human == null)
                continue;

            reference.human.SetActive(false);
        }


        // ============================================================
        // 6. RESTAURAR ROBOT
        // ============================================================

        if (!RestoreRobotToInitialPose())
        {
            Debug.LogError(
                "[ScenarioManager] APPLY cancelado: " +
                "no fue posible restaurar el robot."
            );

            return false;
        }


        // ============================================================
        // 7. RESTAURAR HUMANOS
        // ============================================================

        int restoredCount = 0;

        foreach (HumanState state in scenario.humans)
        {
            if (state == null)
            {
                Debug.LogWarning(
                    "[ScenarioManager] Se encontró un " +
                    "HumanState NULL en " +
                    scenario.scenarioID
                );

                continue;
            }


            HumanReference reference =
                FindHumanReference(state.id);


            if (reference == null)
            {
                Debug.LogError(
                    "[ScenarioManager] No se encontró " +
                    "HumanReference para ID = " +
                    state.id
                );

                continue;
            }


            if (reference.human == null)
            {
                Debug.LogError(
                    "[ScenarioManager] El ID " +
                    state.id +
                    " no tiene GameObject asignado."
                );

                continue;
            }


            GameObject humanObject =
                reference.human;


            // --------------------------------------------------------
            // Restaurar Transform
            // --------------------------------------------------------

            humanObject.transform.position =
                state.position;

            humanObject.transform.rotation =
                state.rotation;

            humanObject.transform.localScale =
                state.scale;


            // --------------------------------------------------------
            // Reset HumanPatrol
            // --------------------------------------------------------

            HumanPatrol patrol =
                humanObject.GetComponent<HumanPatrol>();

            if (patrol != null)
            {
                patrol.ResetPatrolState();

                int humanSeed =
                    experimentSeed +
                    StableIdHash(state.id);

                patrol.ApplyExperimentSeed(
                    humanSeed
                );
            }


            // --------------------------------------------------------
            // Reset cinemática
            // --------------------------------------------------------

            HumanKinematics kinematics =
                humanObject.GetComponent<HumanKinematics>();

            if (kinematics != null)
            {
                kinematics.ResetKinematicsState();
            }


            // --------------------------------------------------------
            // Limpiar trayectoria
            // --------------------------------------------------------

            TrajectoryTrail trajectory =
                humanObject.GetComponent<TrajectoryTrail>();

            if (trajectory != null)
            {
                trajectory.ClearTrajectory();
            }


            // --------------------------------------------------------
            // Restaurar Active
            // --------------------------------------------------------

            humanObject.SetActive(
                state.active
            );


            restoredCount++;


            Debug.Log(
                "[ScenarioManager] " +
                scenario.scenarioID +
                " | Restaurado " +
                state.id +
                " | GameObject = " +
                humanObject.name +
                " | Position = " +
                humanObject.transform.position +
                " | Active = " +
                humanObject.activeSelf
            );
        }


        // ============================================================
        // 8. RESULTADO
        // ============================================================

        Debug.Log(
            "========================================\n" +
            "[ScenarioManager] APPLY COMPLETADO\n" +
            "Escenario: " +
            scenario.scenarioID + "\n" +
            "Humanos restaurados: " +
            restoredCount + "/" +
            scenario.humans.Count +
            "\nRobot Hold: " +
            robotController.ExperimentHold +
            "\n========================================"
        );

        return true;
    }
    private bool RestoreRobotToInitialPose()
    {
        // ============================================================
        // RESET FISICO REPRODUCIBLE DEL ROBOT
        // ============================================================


        // ------------------------------------------------------------
        // 1. Validaciones defensivas
        // ------------------------------------------------------------

        if (robotController == null)
        {
            Debug.LogError(
                "[ScenarioManager] RestoreRobotToInitialPose: " +
                "RobotController es NULL."
            );

            return false;
        }


        if (!robotInitialStateCaptured)
        {
            Debug.LogError(
                "[ScenarioManager] RestoreRobotToInitialPose: " +
                "estado inicial del robot no capturado."
            );

            return false;
        }


        // ------------------------------------------------------------
        // 2. Obtener componentes del robot
        // ------------------------------------------------------------

        Transform robotTransform =
            robotController.transform;

        NavMeshAgent robotAgent =
            robotController.GetComponent<NavMeshAgent>();


        // ------------------------------------------------------------
        // 3. TOMAR AUTORIDAD SOBRE EL ROBOT
        //
        // A partir de aquí RobotSocialNavController.Update()
        // no puede ejecutar ningún algoritmo M0-M4/B1-B4.
        // ------------------------------------------------------------

        robotController.SetExperimentHold(true);


        // ------------------------------------------------------------
        // 4. Limpiar memorias internas del controlador
        //
        // IMPORTANTE:
        // Este método ya NO realiza Warp().
        // ------------------------------------------------------------

        robotController.ResetExperimentalState();


        // ------------------------------------------------------------
        // 5. Posición objetivo inicial
        // ------------------------------------------------------------

        Vector3 resetPosition =
            robotInitialPosition;


        // ------------------------------------------------------------
        // 6. Preparar NavMeshAgent
        // ------------------------------------------------------------

        bool warpSuccess = false;


        if (robotAgent != null &&
            robotAgent.enabled &&
            robotAgent.gameObject.activeInHierarchy)
        {
            // --------------------------------------------------------
            // Durante RESET el agente NO controla el Transform.
            // --------------------------------------------------------

            robotAgent.updatePosition = false;
            robotAgent.updateRotation = false;


            // --------------------------------------------------------
            // Si el agente está actualmente sobre NavMesh,
            // eliminar movimiento y ruta residual.
            // --------------------------------------------------------

            if (robotAgent.isOnNavMesh)
            {
                robotAgent.isStopped = true;

                robotAgent.velocity =
                    Vector3.zero;

                robotAgent.ResetPath();
            }


            // --------------------------------------------------------
            // 7. Intentar primero la posición EXACTA capturada.
            // --------------------------------------------------------

            warpSuccess =
                robotAgent.Warp(
                    robotInitialPosition
                );


            // --------------------------------------------------------
            // 8. Si el Warp exacto falla, buscar NavMesh cercano.
            // --------------------------------------------------------

            if (!warpSuccess)
            {
                NavMeshHit hit;

                // IMPORTANTE:
                // buscar NavMesh compatible con el Agent Type
                // específico del Robot_ROOT.
                NavMeshQueryFilter filter =
                    new NavMeshQueryFilter();

                filter.agentTypeID =
                    robotAgent.agentTypeID;

                filter.areaMask =
                    robotAgent.areaMask;

                bool navMeshFound =
                    NavMesh.SamplePosition(
                        robotInitialPosition,
                        out hit,
                        2.0f,
                        filter
                    );


                if (navMeshFound)
                {
                    resetPosition =
                        hit.position;

                    // --------------------------------------------------------
                    // Primer intento: Warp normal.
                    // --------------------------------------------------------

                    warpSuccess =
                        robotAgent.Warp(
                            resetPosition
                        );


                    // --------------------------------------------------------
                    // FALLBACK ROBUSTO:
                    //
                    // Si el agente quedó desacoplado del NavMesh,
                    // deshabilitarlo, colocar físicamente el Transform
                    // sobre un NavMesh compatible y volver a habilitarlo.
                    // --------------------------------------------------------

                    if (!warpSuccess)
                    {
                        robotAgent.enabled =
                            false;

                        robotTransform.SetPositionAndRotation(
                            resetPosition,
                            robotInitialRotation
                        );

                        robotTransform.localScale =
                            robotInitialScale;

                        robotAgent.enabled =
                            true;

                        robotAgent.updatePosition =
                            false;

                        robotAgent.updateRotation =
                            false;

                        warpSuccess =
                            robotAgent.isOnNavMesh;
                    }


                    Debug.LogWarning(
                        "[ScenarioManager] Rebind NavMesh realizado." +
                        " | Original = " +
                        robotInitialPosition +
                        " | NavMesh = " +
                        resetPosition +
                        " | Success = " +
                        warpSuccess +
                        " | IsOnNavMesh = " +
                        robotAgent.isOnNavMesh
                    );


                    if (!warpSuccess ||
    !robotAgent.isOnNavMesh)
                    {
                        Debug.LogWarning(
                            "[ScenarioManager] Rebind NavMesh pendiente. " +
                            "Se verificara nuevamente durante SETTLE." +
                            " | AgentTypeID = " +
                            robotAgent.agentTypeID +
                            " | Position = " +
                            resetPosition
                        );
                    }
                }
                else
                {
                    Debug.LogError(
                        "[ScenarioManager] No existe NavMesh compatible " +
                        "cerca de la posicion inicial del robot." +
                        " | Position = " +
                        robotInitialPosition +
                        " | AgentTypeID = " +
                        robotAgent.agentTypeID
                    );

                    return false;
                }
            }
            
        }


        // ------------------------------------------------------------
        // 9. Imponer explícitamente Transform
        //
        // Esto hace que ScenarioManager sea la autoridad geométrica.
        // ------------------------------------------------------------

        robotTransform.SetPositionAndRotation(
            resetPosition,
            robotInitialRotation
        );

        robotTransform.localScale =
            robotInitialScale;


        // ------------------------------------------------------------
        // 10. Sincronización interna del NavMeshAgent
        // ------------------------------------------------------------

        if (robotAgent != null &&
            robotAgent.enabled &&
            robotAgent.gameObject.activeInHierarchy)
        {
            if (warpSuccess &&
                robotAgent.isOnNavMesh)
            {
                robotAgent.nextPosition =
                    robotTransform.position;

                robotAgent.isStopped =
                    true;

                robotAgent.velocity =
                    Vector3.zero;

                robotAgent.ResetPath();
            }
        }


        // ------------------------------------------------------------
        // 11. Reimponer orientación y escala
        //
        // Redundancia intencional después del Warp.
        // ------------------------------------------------------------

        robotTransform.rotation =
            robotInitialRotation;

        robotTransform.localScale =
            robotInitialScale;


        // ------------------------------------------------------------
        // 12. Error geométrico de reset
        // ------------------------------------------------------------

        float positionError =
            Vector3.Distance(
                robotTransform.position,
                resetPosition
            );


        float rotationError =
            Quaternion.Angle(
                robotTransform.rotation,
                robotInitialRotation
            );


        // ------------------------------------------------------------
        // 13. Diagnóstico
        // ------------------------------------------------------------

        Debug.Log(
            "========================================\n" +

            "[ScenarioManager] ROBOT RESET COMPLETADO\n" +

            "Robot = " +
            robotController.gameObject.name + "\n" +

            "InstanceID = " +
            robotController.gameObject.GetInstanceID() + "\n" +

            "InitialPosition = " +
            robotInitialPosition + "\n" +

            "ResetPosition = " +
            resetPosition + "\n" +

            "ActualPosition = " +
            robotTransform.position + "\n" +

            "PositionError = " +
            positionError.ToString("F6") +
            " m\n" +

            "RotationError = " +
            rotationError.ToString("F6") +
            " deg\n" +

            "WarpSuccess = " +
            warpSuccess + "\n" +

            "IsOnNavMesh = " +
            (
                robotAgent != null
                ? robotAgent.isOnNavMesh.ToString()
                : "NULL"
            ) +
            "\n" +

            "AgentNextPosition = " +
            (
                robotAgent != null
                ? robotAgent.nextPosition.ToString()
                : "NULL"
            ) +
            "\n" +

            "ExperimentHold = " +
            robotController.ExperimentHold +
            "\n" +

            "========================================"
        );


        return true;
    }

    [ContextMenu("TEST FORCE ROBOT INITIAL TRANSFORM")]
    public void TestForceRobotInitialTransform()
    {
        if (robotController == null)
        {
            Debug.LogError(
                "[ScenarioManager] TEST FORCE: RobotController es NULL."
            );

            return;
        }

        if (!robotInitialStateCaptured)
        {
            Debug.LogError(
                "[ScenarioManager] TEST FORCE: " +
                "estado inicial del robot no capturado."
            );

            return;
        }

        Transform t = robotController.transform;

        NavMeshAgent a =
            robotController.GetComponent<NavMeshAgent>();


        // ============================================================
        // 1. Bloquear controlador
        // ============================================================

        robotController.SetExperimentHold(true);


        // ============================================================
        // 2. Desconectar NavMeshAgent del Transform
        // ============================================================

        if (a != null)
        {
            a.updatePosition = false;
            a.updateRotation = false;

            if (a.enabled &&
                a.gameObject.activeInHierarchy &&
                a.isOnNavMesh)
            {
                a.isStopped = true;
                a.velocity = Vector3.zero;
                a.ResetPath();
            }
        }


        // ============================================================
        // 3. MOVER DIRECTAMENTE EL TRANSFORM
        //
        // AQUÍ NO USAMOS WARP.
        // ============================================================

        Vector3 before =
            t.position;

        t.SetPositionAndRotation(
            robotInitialPosition,
            robotInitialRotation
        );

        t.localScale =
            robotInitialScale;


        // ============================================================
        // 4. Diagnóstico
        // ============================================================

        Debug.Log(
            "========================================\n" +

            "[TEST FORCE ROBOT TRANSFORM]\n" +

            "GameObject = " +
            t.gameObject.name + "\n" +

            "InstanceID = " +
            t.gameObject.GetInstanceID() + "\n" +

            "Parent = " +
            (
                t.parent != null
                ? t.parent.name
                : "NULL"
            ) + "\n" +

            "Before = " +
            before + "\n" +

            "Target = " +
            robotInitialPosition + "\n" +

            "After = " +
            t.position + "\n" +

            "LocalPosition = " +
            t.localPosition + "\n" +

            "Hold = " +
            robotController.ExperimentHold + "\n" +

            "Agent = " +
            (
                a != null
                ? a.gameObject.name
                : "NULL"
            ) + "\n" +

            "AgentInstanceID = " +
            (
                a != null
                ? a.gameObject.GetInstanceID().ToString()
                : "NULL"
            ) + "\n" +

            "========================================"
        );
    }
    [ContextMenu("APPLY E1")]
    public void ApplyE1()
    {
        ApplyScenario(E1);
    }

    [ContextMenu("APPLY E2")]
    public void ApplyE2()
    {
        ApplyScenario(E2);
    }

    [ContextMenu("APPLY E3")]
    public void ApplyE3()
    {
        ApplyScenario(E3);
    }

    [ContextMenu("APPLY E4")]
    public void ApplyE4()
    {
        ApplyScenario(E4);
    }


    // ============================================================
    // ESCENARIOS DE CAMPANA
    // ============================================================

    private ScenarioState[] GetCampaignScenarios()
    {
        return new ScenarioState[]
        {
            E1,
            E2,
            E3,
            E4
        };
    }
    // ============================================================
    // ESCENARIOS DEL BENCHMARK COMPUTACIONAL
    //
    // E1 = carga social relativamente baja
    // E4 = condición multi-humano más exigente
    // ============================================================

    // ============================================================
    // ORDEN REPRODUCIBLE DE METODOS PARA BENCHMARK
    // ============================================================

    private RobotSocialNavController.ExperimentMode[]
        GetBenchmarkMethodOrder(
            int shuffleSeed)
    {
        // Copiamos el array original.
        // NO modificamos campaignMethods.
        RobotSocialNavController.ExperimentMode[] order =
            (RobotSocialNavController.ExperimentMode[])
            campaignMethods.Clone();


        // Generador pseudoaleatorio reproducible.
        System.Random rng =
            new System.Random(
                shuffleSeed
            );


        // Fisher-Yates shuffle
        for (int i = order.Length - 1;
             i > 0;
             i--)
        {
            int j =
                rng.Next(
                    i + 1
                );

            RobotSocialNavController.ExperimentMode temp =
                order[i];

            order[i] =
                order[j];

            order[j] =
                temp;
        }


        return order;
    }

    private ScenarioState[] GetComputationalBenchmarkScenarios()
    {
        return new ScenarioState[]
        {
        E1,
        E4
        };
    }

    // ============================================================
    // CODIGO NUMERICO DE ESCENARIO PARA MATLAB
    //
    // E1 = 1
    // E2 = 2
    // E3 = 3
    // E4 = 4
    // ============================================================

    private int GetScenarioCode(
        ScenarioState scenario)
    {
        if (scenario == null)
        {
            return 0;
        }

        switch (scenario.scenarioID)
        {
            case "E1":
                return 1;

            case "E2":
                return 2;

            case "E3":
                return 3;

            case "E4":
                return 4;

            default:
                return 0;
        }
    }


    // ============================================================
    // DISTANCIA HORIZONTAL XZ
    // ============================================================

    private float HorizontalDistance(
        Vector3 a,
        Vector3 b)
    {
        float dx =
            a.x - b.x;

        float dz =
            a.z - b.z;

        return Mathf.Sqrt(
            dx * dx +
            dz * dz
        );
    }


    // ============================================================
    // VALIDACIONES DE UNA CORRIDA
    // ============================================================

    private bool ValidateRunConfiguration(
        ScenarioState scenario)
    {
        if (scenario == null)
        {
            Debug.LogError(
                "[EXPERIMENT] ScenarioState es NULL."
            );

            return false;
        }

        if (robotController == null)
        {
            Debug.LogError(
                "[EXPERIMENT] RobotController es NULL."
            );

            return false;
        }

        // ============================================================
        // AUTO-RECUPERAR TELEMETRIA SI LA REFERENCIA SE PERDIO
        // ============================================================

        if (telemetry == null)
        {
            if (robotController != null)
            {
                telemetry =
                    robotController.GetComponent<
                        UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV
                    >();
            }
        }

        if (telemetry == null)
        {
            telemetry =
                UnityEngine.Object.FindFirstObjectByType<
                    UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV
                >();
        }

        if (telemetry == null)
        {
            Debug.LogError(
                "[EXPERIMENT] No existe ningun sender UDP LiDAR activo en la escena."
            );

            return false;
        }

        // Garantizar que ScenarioManager sea la autoridad de la mision.
        telemetry.SetExternalExperimentControl(true);

        if (!robotInitialStateCaptured)
        {
            Debug.LogError(
                "[EXPERIMENT] El estado inicial del robot " +
                "no ha sido capturado."
            );

            return false;
        }

        if (scenario.humans == null ||
            scenario.humans.Count == 0)
        {
            Debug.LogError(
                "[EXPERIMENT] El escenario " +
                scenario.scenarioID +
                " no tiene humanos capturados."
            );

            return false;
        }

        if (robotController.pointA_R == null ||
            robotController.pointB_R == null)
        {
            Debug.LogError(
                "[EXPERIMENT] Point_A_R o Point_B_R son NULL."
            );

            return false;
        }

        int scenarioCode =
            GetScenarioCode(scenario);

        if (scenarioCode < 1 ||
            scenarioCode > 4)
        {
            Debug.LogError(
                "[EXPERIMENT] ScenarioID no reconocido: " +
                scenario.scenarioID
            );

            return false;
        }

        return true;
    }


    // ============================================================
    // TELEMETRIA: PREPARE / RESET / SETTLE
    // ============================================================

    private void PrepareTelemetryForRun(
        ScenarioState scenario)
    {
        if (telemetry == null ||
            robotController == null)
        {
            return;
        }

        // Durante PREPARE / RESET / SETTLE no se calcula
        // velocidad física. Esto evita picos por teleport/Warp.
        telemetry.SetRunMeasurementActive(false);

        telemetry.SetExperimentIdentity(
            robotController.experimentMode,
            GetScenarioCode(scenario)
        );

        telemetry.SetMissionState(
            UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV.STATUS_RUNNING_TO_B
        );
    }


    // ============================================================
    // TELEMETRIA: INICIO FISICO DE RUN
    // ============================================================

    private void StartRunTelemetry()
{
    // ============================================================
    // BENCHMARK COMPUTACIONAL
    // Comienza exactamente con la ventana valida de la mision.
    // ============================================================

    ControllerBenchmarkRecorder.BeginRun(
        robotController != null
            ? robotController.experimentMode.ToString()
            : "UNKNOWN",
        activeScenarioID,
        activeRunID
    );


    if (telemetry == null)
    {
        return;
    }

    telemetry.ResetTelemetryState();

    telemetry.SetMissionState(
        UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV.STATUS_RUNNING_TO_B
    );

    telemetry.SetRunMeasurementActive(true);
}


    // ============================================================
    // TELEMETRIA: FIN DE CORRIDA
    // ============================================================

    private void FinishRunTelemetry(
     int terminalStatus)
    {
        // ============================================================
        // BENCHMARK COMPUTACIONAL
        // Se cierra exactamente al terminar la mision.
        // ============================================================

        ControllerBenchmarkRecorder.EndRun(
            terminalStatus
        );


        if (telemetry == null)
        {
            return;
        }

        telemetry.SetMissionState(
            terminalStatus
        );

        telemetry.SetRunMeasurementActive(false);
    }


    // ============================================================
    // ABORTO INTERNO POR ERROR DE CONFIGURACION / RESET
    // ============================================================

    private void AbortCurrentRunBecauseOfError(
        string message)
    {
        lastRunTermination =
            RunTermination.Aborted;

        FinishRunTelemetry(
            UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV.STATUS_ABORTED
        );

        if (robotController != null)
        {
            robotController.SetExperimentHold(true);
            robotController.SetExternalMissionControl(false);
        }

        currentExperimentState =
            ExperimentState.Idle;

        experimentRunning =
            false;

        if (campaignRunning)
        {
            campaignStopRequested =
                true;
        }

        Debug.LogError(
            "[EXPERIMENT] ABORTADO: " +
            message
        );
    }


    // ============================================================
    // UNA CORRIDA EXPERIMENTAL COMPLETA
    //
    // MISION:
    // A -> B -> A
    //
    // PROTOCOLO MATLAB:
    // 0 = A -> B / activa
    // 1 = B alcanzado / B -> A
    // 2 = SUCCESS
    // 3 = TIMEOUT
    // 4 = COLLISION (reservado)
    // 5 = STUCK     (reservado)
    // 6 = ABORTED
    // ============================================================

    private IEnumerator AutomaticExperimentRoutine(
        ScenarioState scenario,
        int requestedRunID)
    {
        // ========================================================
        // 0. VALIDACIONES
        // ========================================================

        if (!ValidateRunConfiguration(scenario))
        {
            yield break;
        }

        if (requestedRunID < 1)
        {
            Debug.LogError(
                "[EXPERIMENT] requestedRunID debe ser >= 1."
            );

            yield break;
        }


        // ========================================================
        // 1. PREPARE
        // ========================================================

        currentExperimentState =
            ExperimentState.Prepare;

        experimentRunning =
            true;

        activeScenarioID =
            scenario.scenarioID;

        activeRunID =
            requestedRunID;

        runStartTime =
            0f;

        runElapsedTime =
            0f;

        currentMissionLeg =
            MissionLeg.GoingToB;

        lastRunTermination =
            RunTermination.None;

        PrepareTelemetryForRun(
            scenario
        );

        Debug.Log(
            "========================================\n" +
            "[EXPERIMENT] PREPARE\n" +
            "Scenario = " +
            activeScenarioID + "\n" +
            "ScenarioCode = " +
            GetScenarioCode(scenario) + "\n" +
            "Run = " +
            activeRunID + "\n" +
            "Seed = " +
            experimentSeed + "\n" +
            "Method = " +
            robotController.experimentMode + "\n" +
            "MethodCode = " +
            ((int)robotController.experimentMode) + "\n" +
            "MATLAB Status = 0\n" +
            "========================================"
        );


        // ========================================================
        // 2. RESET
        // ========================================================

        currentExperimentState =
            ExperimentState.Reset;

        PrepareTelemetryForRun(
            scenario
        );

        bool scenarioApplied =
            ApplyScenario(
                scenario
            );

        if (!scenarioApplied)
        {
            AbortCurrentRunBecauseOfError(
                "ApplyScenario fallo para " +
                scenario.scenarioID
            );

            yield break;
        }


        // ========================================================
        // 3. SETTLE
        // ========================================================

        currentExperimentState =
            ExperimentState.Settle;

        PrepareTelemetryForRun(
            scenario
        );

        // Tres frames completos con robot en HOLD.
        yield return null;
        yield return null;
        yield return null;




        // ========================================================
        // 4. SINCRONIZACION FINAL NAVMESH ANTES DE RUN
        // ========================================================

        NavMeshAgent robotAgent =
            robotController.GetComponent<NavMeshAgent>();


        if (robotAgent == null)
        {
            AbortCurrentRunBecauseOfError(
                "Robot_ROOT no tiene NavMeshAgent."
            );

            yield break;
        }


        // --------------------------------------------------------
        // Si despues del RESET aun no esta vinculado,
        // hacer un REBIND ASINCRONO.
        //
        // IMPORTANTE:
        // deshabilitamos -> esperamos un frame ->
        // posicionamos -> habilitamos -> esperamos otro frame.
        //
        // Esto evita comprobar isOnNavMesh en el mismo frame
        // en que el componente fue reactivado.
        // --------------------------------------------------------

        if (!robotAgent.isOnNavMesh)
        {
            NavMeshHit navHit;

            NavMeshQueryFilter filter =
                new NavMeshQueryFilter();

            filter.agentTypeID =
                robotAgent.agentTypeID;

            filter.areaMask =
                robotAgent.areaMask;


            bool navMeshFound =
                NavMesh.SamplePosition(
                    robotController.transform.position,
                    out navHit,
                    2.0f,
                    filter
                );


            if (!navMeshFound)
            {
                AbortCurrentRunBecauseOfError(
                    "No se encontro NavMesh compatible " +
                    "durante SETTLE."
                );

                yield break;
            }


            // ====================================================
            // DESACTIVAR AGENT
            // ====================================================

            robotAgent.enabled =
                false;


            // Dar un frame a Unity.
            yield return null;


            // ====================================================
            // COLOCAR ROBOT EXACTAMENTE SOBRE EL NAVMESH
            // ====================================================

            robotController.transform.position =
                navHit.position;


            // ====================================================
            // REACTIVAR AGENT
            // ====================================================

            robotAgent.enabled =
                true;

            robotAgent.updatePosition =
                false;

            robotAgent.updateRotation =
                false;


            // MUY IMPORTANTE:
            // permitir que Unity registre el agente nuevamente.
            yield return null;
        }


        // ========================================================
        // VERIFICACION DEFINITIVA
        // ========================================================

        if (!robotAgent.enabled ||
            !robotAgent.gameObject.activeInHierarchy ||
            !robotAgent.isOnNavMesh)
        {
            Debug.LogError(
                "[NAVMESH READY] FAILED" +
                " | Enabled = " +
                robotAgent.enabled +
                " | IsOnNavMesh = " +
                robotAgent.isOnNavMesh +
                " | Position = " +
                robotController.transform.position +
                " | AgentTypeID = " +
                robotAgent.agentTypeID
            );


            AbortCurrentRunBecauseOfError(
                "NavMeshAgent continua fuera del NavMesh " +
                "despues del SETTLE."
            );

            yield break;
        }


        // ========================================================
        // AGENT CORRECTAMENTE SINCRONIZADO
        // ========================================================

        robotAgent.nextPosition =
            robotController.transform.position;

        robotAgent.velocity =
            Vector3.zero;

        robotAgent.isStopped =
            true;

        robotAgent.ResetPath();


        Debug.Log(
            "========================================\n" +
            "[NAVMESH READY]\n" +
            "IsOnNavMesh = " +
            robotAgent.isOnNavMesh + "\n" +
            "Position = " +
            robotController.transform.position + "\n" +
            "NextPosition = " +
            robotAgent.nextPosition + "\n" +
            "AgentTypeID = " +
            robotAgent.agentTypeID + "\n" +
            "========================================"
        );

        // ========================================================
        // 5. RUN
        // ========================================================

        currentExperimentState =
            ExperimentState.Run;

        currentMissionLeg =
            MissionLeg.GoingToB;

        // Usamos tiempo no escalado para que el timeout sea real y
        // no dependa de Time.timeScale.
        runStartTime =
            Time.realtimeSinceStartup;

        runElapsedTime =
            0f;

        // Congelar geometricamente los dos extremos para esta corrida.
        // Aunque un Transform de waypoint se mueva accidentalmente, la
        // definicion experimental A/B no cambia durante el run.
        Vector3 missionPointA =
            robotController.pointA_R.position;

        Vector3 missionPointB =
            robotController.pointB_R.position;

        // ========================================================
        // ScenarioManager pasa a ser la UNICA autoridad
        // de la mision fisica A -> B -> A.
        // ========================================================

        robotController.SetExternalMissionControl(true);

        robotController.SetExternalMissionTarget(
            robotController.pointB_R
        );

        float nextMissionWatchLogTime = 0f;

        // Nueva referencia cinemática y medición válida.
        StartRunTelemetry();

        // Aquí comienza físicamente la corrida.
        robotController.SetExperimentHold(false);

        Debug.Log(
            "========================================\n" +
            "[EXPERIMENT] RUN START\n" +
            "Mission = A -> B -> A\n" +
            "Scenario = " +
            activeScenarioID + "\n" +
            "Run = " +
            activeRunID + "\n" +
            "Method = " +
            robotController.experimentMode + "\n" +
            "Seed = " +
            experimentSeed + "\n" +
            "Position = " +
            robotController.transform.position + "\n" +
            "MATLAB Status = 0\n" +
            "========================================"
        );


        // ========================================================
        // 6. EJECUCION REAL A -> B -> A
        // ========================================================

        while (lastRunTermination ==
               RunTermination.None)
        {
            runElapsedTime =
                Time.realtimeSinceStartup -
                runStartTime;

            Vector3 robotPosition =
                robotController.transform.position;

            float distanceToA =
                HorizontalDistance(
                    robotPosition,
                    missionPointA
                );

            float distanceToB =
                HorizontalDistance(
                    robotPosition,
                    missionPointB
                );

            // Diagnostico de baja frecuencia: permite comprobar exactamente
            // si el robot se aproxima a B y luego a A sin inundar la Console.
            if (runElapsedTime >= nextMissionWatchLogTime)
            {
                nextMissionWatchLogTime =
                    runElapsedTime + 2.0f;

                Debug.Log(
                    "[MISSION WATCH] Leg=" +
                    currentMissionLeg +
                    " | dA=" +
                    distanceToA.ToString("F2") +
                    " | dB=" +
                    distanceToB.ToString("F2") +
                    " | Pos=" +
                    robotPosition +
                    " | Target=" +
                    robotController.ExternalMissionTargetPosition
                );
            }


            // ====================================================
            // STOP MANUAL
            // ====================================================

            if (campaignStopRequested)
            {
                lastRunTermination =
                    RunTermination.Aborted;

                robotController.SetExperimentHold(true);

                FinishRunTelemetry(
                    UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV.STATUS_ABORTED
                );

                Debug.LogWarning(
                    "[EXPERIMENT] RUN ABORTED."
                );

                break;
            }


            // ====================================================
            // ETAPA 1: A -> B
            // ====================================================

            if (currentMissionLeg ==
                MissionLeg.GoingToB)
            {
                if (distanceToB <=
                    waypointArrivalTolerance)
                {
                    currentMissionLeg =
                        MissionLeg.ReturningToA;

                    // Cambio explícito de objetivo:
                    // desde este instante B -> A.
                    robotController.SetExternalMissionTarget(
                        robotController.pointA_R
                    );

                    telemetry.SetMissionState(
                        UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV
                            .STATUS_RETURNING_TO_A
                    );

                    Debug.Log(
                        "========================================\n" +
                        "[EXPERIMENT] POINT B REACHED\n" +
                        "Scenario = " +
                        activeScenarioID + "\n" +
                        "Run = " +
                        activeRunID + "\n" +
                        "Method = " +
                        robotController.experimentMode + "\n" +
                        "Time = " +
                        runElapsedTime.ToString("F3") +
                        " s\n" +
                        "DistanceToB = " +
                        distanceToB.ToString("F3") +
                        " m\n" +
                        "MATLAB Status = 1\n" +
                        "Returning B -> A\n" +
                        "========================================"
                    );
                }
            }


            // ====================================================
            // ETAPA 2: B -> A
            // ====================================================

            else if (currentMissionLeg ==
                     MissionLeg.ReturningToA)
            {
                if (distanceToA <=
                    waypointArrivalTolerance)
                {
                    lastRunTermination =
                        RunTermination.Success;

                    FinishRunTelemetry(
                        UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV.STATUS_SUCCESS
                    );

                    robotController.SetExperimentHold(true);

                    Debug.Log(
                        "========================================\n" +
                        "[EXPERIMENT] MISSION SUCCESS\n" +
                        "A -> B -> A COMPLETED\n" +
                        "Scenario = " +
                        activeScenarioID + "\n" +
                        "Run = " +
                        activeRunID + "\n" +
                        "Method = " +
                        robotController.experimentMode + "\n" +
                        "Time = " +
                        runElapsedTime.ToString("F3") +
                        " s\n" +
                        "DistanceToA = " +
                        distanceToA.ToString("F3") +
                        " m\n" +
                        "MATLAB Status = 2\n" +
                        "========================================"
                    );

                    break;
                }
            }


            // ====================================================
            // TIMEOUT
            // ====================================================

            if (runElapsedTime >=
                maximumRunTime)
            {
                lastRunTermination =
                    RunTermination.Timeout;

                robotController.SetExperimentHold(true);

                FinishRunTelemetry(
                    UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV.STATUS_TIMEOUT
                );

                Debug.LogWarning(
                    "========================================\n" +
                    "[EXPERIMENT] TIMEOUT\n" +
                    "Scenario = " +
                    activeScenarioID + "\n" +
                    "Run = " +
                    activeRunID + "\n" +
                    "Method = " +
                    robotController.experimentMode + "\n" +
                    "Time = " +
                    runElapsedTime.ToString("F3") +
                    " s\n" +
                    "MissionLeg = " +
                    currentMissionLeg + "\n" +
                    "MATLAB Status = 3\n" +
                    "========================================"
                );

                break;
            }

            yield return null;
        }


        // ========================================================
        // 7. END
        // ========================================================

        runElapsedTime =
            Time.realtimeSinceStartup -
            runStartTime;

        currentExperimentState =
            ExperimentState.End;

        // Detener inmediatamente al robot.
        robotController.SetExperimentHold(true);

        // Protección: la medición debe quedar apagada incluso si
        // apareció una salida futura no contemplada arriba.
        if (telemetry.RunMeasurementActive)
        {
            telemetry.SetRunMeasurementActive(false);
        }

        // Mantener el status terminal durante varias transmisiones UDP
        // antes de liberar el control externo. MATLAB recibe a 10 Hz, por
        // lo que 0.35 s entrega normalmente 3-4 paquetes terminales.
        yield return new WaitForSecondsRealtime(0.35f);

        robotController.SetExternalMissionControl(false);

        Debug.Log(
            "========================================\n" +
            "[EXPERIMENT] RUN END\n" +
            "Scenario = " +
            activeScenarioID + "\n" +
            "Run = " +
            activeRunID + "\n" +
            "Method = " +
            robotController.experimentMode + "\n" +
            "Result = " +
            lastRunTermination + "\n" +
            "Duration = " +
            runElapsedTime.ToString("F3") +
            " s\n" +
            "FinalPosition = " +
            robotController.transform.position + "\n" +
            "MATLAB Status = " +
            telemetry.missionState + "\n" +
            "========================================"
        );


        // ========================================================
        // 8. EXPORT
        // ========================================================
        //
        // Unity NO exporta CSV/MAT.
        // MATLAB recibe el status terminal, calcula y guarda.
        // El status terminal NO se pone a 0 aquí.
        // ========================================================

        currentExperimentState =
            ExperimentState.Export;

        yield return null;


        // ========================================================
        // 9. IDLE
        // ========================================================

        currentExperimentState =
            ExperimentState.Idle;

        experimentRunning =
            false;

        Debug.Log(
            "[EXPERIMENT] Corrida finalizada." +
            " Result = " +
            lastRunTermination
        );
    }


    // ============================================================
    // PRUEBA DE UNA CORRIDA E1
    // ============================================================

    [ContextMenu("RUN AUTOMATIC TEST E1")]
    public void RunAutomaticTestE1()
    {
        if (experimentRunning ||
            campaignRunning)
        {
            Debug.LogWarning(
                "[ScenarioManager] Ya existe una corrida/campana activa."
            );

            return;
        }

        campaignStopRequested =
            false;

        StartCoroutine(
            AutomaticExperimentRoutine(
                E1,
                1
            )
        );
    }


    // ============================================================
    // VALIDACION DE DOS CORRIDAS CONSECUTIVAS
    //
    // Usa el metodo actualmente seleccionado en RobotController
    // y el escenario E1. Sirve para validar que MATLAB guarde una
    // corrida, limpie buffers y reciba la segunda sin pulsar START.
    // ============================================================

    private IEnumerator TwoRunValidationRoutine()
    {
        if (!ValidateRunConfiguration(E1))
        {
            yield break;
        }

        campaignRunning =
            true;

        campaignStopRequested =
            false;

        campaignCompletedRuns =
            0;

        campaignTotalRuns =
            2;

        RobotSocialNavController.ExperimentMode validationMethod =
            robotController.experimentMode;

        Debug.Log(
            "========================================\n" +
            "[VALIDATION] TWO CONSECUTIVE RUNS START\n" +
            "Method = " +
            validationMethod + "\n" +
            "Scenario = E1\n" +
            "Runs = 2\n" +
            "========================================"
        );

        for (int run = 1;
             run <= 2;
             run++)
        {
            if (campaignStopRequested)
            {
                break;
            }

            experimentSeed =
                campaignBaseSeed +
                (run - 1);

            robotController.SetExperimentHold(true);

            robotController.experimentMode =
                validationMethod;

            yield return StartCoroutine(
                AutomaticExperimentRoutine(
                    E1,
                    run
                )
            );

            if (campaignStopRequested)
            {
                break;
            }

            campaignCompletedRuns++;

            Debug.Log(
                "[VALIDATION] RUN COMPLETE " +
                campaignCompletedRuns +
                "/2 | Result=" +
                lastRunTermination
            );

            if (run < 2)
            {
                yield return new WaitForSecondsRealtime(
                    interRunDelaySeconds
                );
            }
        }

        robotController.SetExperimentHold(true);

        campaignRunning =
            false;

        currentExperimentState =
            ExperimentState.Idle;

        if (campaignStopRequested)
        {
            Debug.LogWarning(
                "[VALIDATION] TWO RUN TEST ABORTED."
            );
        }
        else
        {
            Debug.Log(
                "========================================\n" +
                "[VALIDATION] TWO CONSECUTIVE RUNS COMPLETE\n" +
                "Completed = " +
                campaignCompletedRuns +
                "/2\n" +
                "========================================"
            );
        }
    }


    [ContextMenu("RUN 2-RUN VALIDATION E1")]
    public void RunTwoRunValidationE1()
    {
        if (experimentRunning ||
            campaignRunning)
        {
            Debug.LogWarning(
                "[VALIDATION] Ya existe una corrida/campana activa."
            );

            return;
        }

        StartCoroutine(
            TwoRunValidationRoutine()
        );
    }
    // ============================================================
    // VALIDACION DE LOS 4 ESCENARIOS
    //
    // Ejecuta UNA corrida del metodo actualmente seleccionado:
    //
    // E1 -> E2 -> E3 -> E4
    //
    // Sirve para comprobar:
    // 1. Cambio automatico de escenario.
    // 2. Reset correcto entre escenarios.
    // 3. Identidad correcta enviada a MATLAB.
    // 4. Guardado en carpetas E1/E2/E3/E4.
    // ============================================================

    private IEnumerator FourScenarioValidationRoutine()
    {
        ScenarioState[] scenarios =
            GetCampaignScenarios();

        // --------------------------------------------------------
        // Validar los cuatro escenarios antes de comenzar.
        // --------------------------------------------------------

        for (int i = 0;
             i < scenarios.Length;
             i++)
        {
            if (!ValidateRunConfiguration(
                    scenarios[i]))
            {
                Debug.LogError(
                    "[4-SCENARIO VALIDATION] " +
                    "Configuracion invalida en escenario " +
                    scenarios[i].scenarioID
                );

                yield break;
            }
        }


        campaignRunning = true;

        campaignStopRequested = false;

        campaignCompletedRuns = 0;

        campaignTotalRuns =
            scenarios.Length;


        // Mantener exactamente el metodo
        // seleccionado actualmente en Robot_ROOT.
        RobotSocialNavController.ExperimentMode validationMethod =
            robotController.experimentMode;


        Debug.Log(
            "========================================\n" +
            "[4-SCENARIO VALIDATION] START\n" +
            "Method = " +
            validationMethod + "\n" +
            "Sequence = E1 -> E2 -> E3 -> E4\n" +
            "Runs = 4\n" +
            "========================================"
        );


        // ========================================================
        // E1 -> E2 -> E3 -> E4
        // ========================================================

        for (int scenarioIndex = 0;
             scenarioIndex < scenarios.Length;
             scenarioIndex++)
        {
            if (campaignStopRequested)
            {
                break;
            }


            ScenarioState selectedScenario =
                scenarios[scenarioIndex];


            // Misma repeticion = misma semilla.
            experimentSeed =
                campaignBaseSeed;


            robotController.SetExperimentHold(
                true
            );


            // No cambiar metodo durante esta validacion.
            robotController.experimentMode =
                validationMethod;


            // Un frame para estabilizar.
            yield return null;


            Debug.Log(
                "========================================\n" +
                "[4-SCENARIO VALIDATION] START SCENARIO\n" +
                "Scenario = " +
                selectedScenario.scenarioID + "\n" +
                "Method = " +
                validationMethod + "\n" +
                "Run = 1\n" +
                "Seed = " +
                experimentSeed + "\n" +
                "========================================"
            );


            yield return StartCoroutine(
                AutomaticExperimentRoutine(
                    selectedScenario,
                    1
                )
            );


            if (campaignStopRequested)
            {
                break;
            }


            campaignCompletedRuns++;


            Debug.Log(
                "[4-SCENARIO VALIDATION] COMPLETE " +
                campaignCompletedRuns +
                "/4" +
                " | Scenario=" +
                selectedScenario.scenarioID +
                " | Result=" +
                lastRunTermination
            );


            // Dar tiempo a MATLAB para guardar
            // y rearmarse antes del siguiente escenario.
            if (scenarioIndex <
                scenarios.Length - 1)
            {
                yield return new WaitForSecondsRealtime(
                    interRunDelaySeconds
                );
            }
        }


        robotController.SetExperimentHold(
            true
        );


        campaignRunning = false;

        currentExperimentState =
            ExperimentState.Idle;


        if (campaignStopRequested)
        {
            Debug.LogWarning(
                "[4-SCENARIO VALIDATION] ABORTED."
            );
        }
        else
        {
            Debug.Log(
                "========================================\n" +
                "[4-SCENARIO VALIDATION] COMPLETE\n" +
                "Completed = " +
                campaignCompletedRuns +
                "/4\n" +
                "========================================"
            );
        }
    }


    // ============================================================
    // BOTON DEL INSPECTOR
    // ============================================================

    [ContextMenu("RUN 4-SCENARIO VALIDATION")]
    public void RunFourScenarioValidation()
    {
        if (experimentRunning ||
            campaignRunning)
        {
            Debug.LogWarning(
                "[4-SCENARIO VALIDATION] " +
                "Ya existe una corrida/campana activa."
            );

            return;
        }


        campaignStopRequested =
            false;


        StartCoroutine(
            FourScenarioValidationRoutine()
        );
    }

    // ============================================================
    // VALIDACION DE LOS 9 METODOS EN E1
    //
    // Ejecuta UNA corrida por metodo:
    //
    // M0 -> M1 -> M2 -> M3 -> M4 -> B1 -> B2 -> B3 -> B4
    //
    // Escenario fijo: E1
    // Semilla fija: campaignBaseSeed
    //
    // Objetivo:
    // validar cambio automatico de metodo antes de lanzar
    // la campana completa de 360 corridas.
    // ============================================================

    private IEnumerator NineMethodValidationE1Routine()
    {
        // ========================================================
        // 0. VALIDAR E1
        // ========================================================

        if (!ValidateRunConfiguration(E1))
        {
            Debug.LogError(
                "[9-METHOD VALIDATION] E1 no tiene configuracion valida."
            );

            yield break;
        }


        // ========================================================
        // 1. INICIALIZAR VALIDACION
        // ========================================================

        campaignRunning = true;

        campaignStopRequested = false;

        campaignCompletedRuns = 0;

        campaignTotalRuns =
            campaignMethods.Length;


        Debug.Log(
            "========================================\n" +
            "[9-METHOD VALIDATION] START\n" +
            "Scenario = E1\n" +
            "Methods = " +
            campaignMethods.Length + "\n" +
            "Sequence = M0 -> M1 -> M2 -> M3 -> M4 -> B1 -> B2 -> B3 -> B4\n" +
            "Runs = 9\n" +
            "Seed = " +
            campaignBaseSeed + "\n" +
            "========================================"
        );


        // ========================================================
        // 2. RECORRER LOS 9 METODOS
        // ========================================================

        for (int methodIndex = 0;
             methodIndex < campaignMethods.Length;
             methodIndex++)
        {
            if (campaignStopRequested)
            {
                break;
            }


            RobotSocialNavController.ExperimentMode
                selectedMethod =
                    campaignMethods[methodIndex];


            // ----------------------------------------------------
            // MISMA SEMILLA PARA TODOS LOS METODOS
            //
            // Esto permite una comparacion pareada:
            // misma repeticion / misma condicion experimental.
            // ----------------------------------------------------

            experimentSeed =
                campaignBaseSeed;


            // ----------------------------------------------------
            // Detener robot antes de cambiar controlador
            // ----------------------------------------------------

            robotController.SetExperimentHold(
                true
            );


            // ----------------------------------------------------
            // CAMBIO AUTOMATICO DEL METODO
            // ----------------------------------------------------

            robotController.experimentMode =
                selectedMethod;


            Debug.Log(
                "========================================\n" +
                "[9-METHOD VALIDATION] METHOD SELECTED\n" +
                "Progress = " +
                (methodIndex + 1) +
                "/" +
                campaignMethods.Length + "\n" +
                "Method = " +
                selectedMethod + "\n" +
                "RawCode = " +
                ((int)selectedMethod) + "\n" +
                "Scenario = E1\n" +
                "Seed = " +
                experimentSeed + "\n" +
                "========================================"
            );


            // Un frame para que RobotSocialNavController
            // aplique correctamente el nuevo modo.
            yield return null;


            // ====================================================
            // 3. EJECUTAR UNA MISION A -> B -> A
            // ====================================================

            yield return StartCoroutine(
                AutomaticExperimentRoutine(
                    E1,
                    1
                )
            );


            if (campaignStopRequested)
            {
                break;
            }


            campaignCompletedRuns++;


            Debug.Log(
                "[9-METHOD VALIDATION] COMPLETE " +
                campaignCompletedRuns +
                "/" +
                campaignMethods.Length +
                " | Method=" +
                selectedMethod +
                " | Scenario=E1" +
                " | Result=" +
                lastRunTermination
            );


            // ====================================================
            // 4. ESPERA PARA MATLAB
            //
            // Permite:
            // - guardar CSV
            // - guardar MAT
            // - guardar metrics
            // - actualizar master_log
            // - limpiar buffers
            // - rearmarse
            // ====================================================

            if (methodIndex <
                campaignMethods.Length - 1)
            {
                yield return new WaitForSecondsRealtime(
                    interRunDelaySeconds
                );
            }
        }


        // ========================================================
        // 5. FINAL
        // ========================================================

        robotController.SetExperimentHold(
            true
        );


        campaignRunning = false;

        currentExperimentState =
            ExperimentState.Idle;


        if (campaignStopRequested)
        {
            Debug.LogWarning(
                "[9-METHOD VALIDATION] ABORTED."
            );
        }
        else
        {
            Debug.Log(
                "========================================\n" +
                "[9-METHOD VALIDATION] COMPLETE\n" +
                "Completed = " +
                campaignCompletedRuns +
                "/" +
                campaignMethods.Length + "\n" +
                "========================================"
            );
        }
    }


    // ============================================================
    // BOTON DEL INSPECTOR
    // ============================================================

    [ContextMenu("RUN 9-METHOD VALIDATION E1")]
    public void RunNineMethodValidationE1()
    {
        if (experimentRunning ||
            campaignRunning)
        {
            Debug.LogWarning(
                "[9-METHOD VALIDATION] " +
                "Ya existe una corrida/campana activa."
            );

            return;
        }


        campaignStopRequested =
            false;


        StartCoroutine(
            NineMethodValidationE1Routine()
        );
    }

    // ============================================================
    // CAMPANA AUTOMATICA COMPLETA
    //
    // 9 metodos x 4 escenarios x N repeticiones.
    // Con N=10 => 360 corridas.
    // ============================================================

    private IEnumerator FullCampaignRoutine()
    {
        // ========================================================
        // 0. VALIDACIONES GLOBALES
        // ========================================================

        if (robotController == null)
        {
            Debug.LogError(
                "[CAMPAIGN] RobotController es NULL."
            );

            yield break;
        }

        if (telemetry == null)
        {
            Debug.LogError(
                "[CAMPAIGN] Telemetry es NULL."
            );

            yield break;
        }

        if (!robotInitialStateCaptured)
        {
            Debug.LogError(
                "[CAMPAIGN] Capture primero ROBOT INITIAL STATE."
            );

            yield break;
        }

        if (campaignRunsPerCell < 1)
        {
            Debug.LogError(
                "[CAMPAIGN] campaignRunsPerCell debe ser >= 1."
            );

            yield break;
        }

        if (interRunDelaySeconds < 0f)
        {
            Debug.LogError(
                "[CAMPAIGN] interRunDelaySeconds no puede ser negativo."
            );

            yield break;
        }

        ScenarioState[] scenarios =
            GetCampaignScenarios();

        for (int i = 0;
             i < scenarios.Length;
             i++)
        {
            if (!ValidateRunConfiguration(scenarios[i]))
            {
                Debug.LogError(
                    "[CAMPAIGN] Configuracion invalida en escenario index " +
                    i
                );

                yield break;
            }
        }


        // ========================================================
        // 1. INICIALIZAR CAMPANA
        // ========================================================

        campaignRunning =
            true;

        campaignStopRequested =
            false;

        campaignCompletedRuns =
            0;

        campaignTotalRuns =
            campaignMethods.Length *
            scenarios.Length *
            campaignRunsPerCell;

        robotController.SetExperimentHold(true);

        telemetry.SetRunMeasurementActive(false);

        Debug.Log(
            "========================================\n" +
            "[CAMPAIGN] FULL CAMPAIGN START\n" +
            "Methods = " +
            campaignMethods.Length + "\n" +
            "Scenarios = " +
            scenarios.Length + "\n" +
            "RunsPerCell = " +
            campaignRunsPerCell + "\n" +
            "TOTAL = " +
            campaignTotalRuns + "\n" +
            "========================================"
        );


        // ========================================================
        // 2. METODOS M0 ... B4
        // ========================================================

        for (int methodIndex = 0;
             methodIndex < campaignMethods.Length;
             methodIndex++)
        {
            if (campaignStopRequested)
            {
                break;
            }

            RobotSocialNavController.ExperimentMode selectedMethod =
                campaignMethods[
                    methodIndex
                ];

            // Cambiar método siempre con el robot detenido.
            robotController.SetExperimentHold(true);

            robotController.experimentMode =
                selectedMethod;

            Debug.Log(
                "========================================\n" +
                "[CAMPAIGN] METHOD SELECTED\n" +
                "Method = " +
                selectedMethod + "\n" +
                "RawCode = " +
                ((int)selectedMethod) + "\n" +
                "========================================"
            );

            // Un frame para estabilizar el cambio lógico.
            yield return null;


            // ====================================================
            // 3. ESCENARIOS E1 ... E4
            // ====================================================

            for (int scenarioIndex = 0;
                 scenarioIndex < scenarios.Length;
                 scenarioIndex++)
            {
                if (campaignStopRequested)
                {
                    break;
                }

                ScenarioState selectedScenario =
                    scenarios[
                        scenarioIndex
                    ];


                // =================================================
                // 4. REPETICIONES 1 ... N
                // =================================================

                for (int run = 1;
                     run <= campaignRunsPerCell;
                     run++)
                {
                    if (campaignStopRequested)
                    {
                        break;
                    }

                    // Misma semilla de repeticion para todos los
                    // métodos: comparación pareada.
                    experimentSeed =
                        campaignBaseSeed +
                        (run - 1);

                    Debug.Log(
                        "========================================\n" +
                        "[CAMPAIGN] PREPARING RUN\n" +
                        "Progress = " +
                        (campaignCompletedRuns + 1) +
                        "/" +
                        campaignTotalRuns + "\n" +
                        "Method = " +
                        selectedMethod + "\n" +
                        "Scenario = " +
                        selectedScenario.scenarioID + "\n" +
                        "Run = " +
                        run +
                        "/" +
                        campaignRunsPerCell + "\n" +
                        "Seed = " +
                        experimentSeed + "\n" +
                        "========================================"
                    );

                    yield return StartCoroutine(
                        AutomaticExperimentRoutine(
                            selectedScenario,
                            run
                        )
                    );

                    // Error grave o STOP manual.
                    if (campaignStopRequested)
                    {
                        break;
                    }

                    campaignCompletedRuns++;

                    Debug.Log(
                        "[CAMPAIGN] RUN COMPLETE" +
                        " | " +
                        campaignCompletedRuns +
                        "/" +
                        campaignTotalRuns +
                        " | Method=" +
                        selectedMethod +
                        " | Scenario=" +
                        selectedScenario.scenarioID +
                        " | Run=" +
                        run +
                        " | Result=" +
                        lastRunTermination
                    );

                    // Mantener status terminal (2/3/6) para MATLAB.
                    yield return new WaitForSecondsRealtime(
                        interRunDelaySeconds
                    );
                }
            }
        }


        // ========================================================
        // 5. FIN DE CAMPANA
        // ========================================================

        robotController.SetExperimentHold(true);

        telemetry.SetRunMeasurementActive(false);

        campaignRunning =
            false;

        currentExperimentState =
            ExperimentState.Idle;

        if (campaignStopRequested)
        {
            Debug.LogWarning(
                "========================================\n" +
                "[CAMPAIGN] CAMPAIGN ABORTED\n" +
                "Completed = " +
                campaignCompletedRuns +
                "/" +
                campaignTotalRuns + "\n" +
                "========================================"
            );
        }
        else
        {
            Debug.Log(
                "========================================\n" +
                "[CAMPAIGN] CAMPAIGN COMPLETE\n" +
                "Completed = " +
                campaignCompletedRuns +
                "/" +
                campaignTotalRuns + "\n" +
                "========================================"
            );
        }
    }

    // ============================================================
    // COMPUTATIONAL BENCHMARK
    //
    // 9 metodos
    // x 2 escenarios: E1 y E4
    // x 3 repeticiones
    //
    // TOTAL = 54 runs
    // ============================================================

    private IEnumerator ComputationalBenchmarkRoutine()
    {
        // ============================================================
        // CONDICIONES COMPUTACIONALES CONTROLADAS
        // ============================================================

        // Mantener el benchmark ejecutándose aunque la ventana
        // pierda temporalmente el foco.
        Application.runInBackground = true;

        // Evitar sincronización con el monitor.
        QualitySettings.vSyncCount = 0;

        // Frecuencia objetivo común para los métodos ejecutados en Update().
        Application.targetFrameRate = 50;

        // B1 usa FixedUpdate(). 0.02 s = 50 Hz.
        Time.fixedDeltaTime = 0.02f;

        Debug.Log(
            "[COMPUTATIONAL BENCHMARK] " +
            "TargetFrameRate = " +
            Application.targetFrameRate +
            " Hz | FixedDeltaTime = " +
            Time.fixedDeltaTime.ToString("F3") +
            " s | VSync = " +
            QualitySettings.vSyncCount
        );


        // ========================================================
        // 0. VALIDACIONES
        // ========================================================

        if (robotController == null)
        {
            Debug.LogError(
                "[COMPUTATIONAL BENCHMARK] RobotController es NULL."
            );

            yield break;
        }


        if (telemetry == null)
        {
            Debug.LogError(
                "[COMPUTATIONAL BENCHMARK] Telemetry es NULL."
            );

            yield break;
        }


        if (ControllerBenchmarkRecorder.Instance == null)
        {
            Debug.LogError(
                "[COMPUTATIONAL BENCHMARK] " +
                "No existe ControllerBenchmarkRecorder en la escena."
            );

            yield break;
        }


        if (!robotInitialStateCaptured)
        {
            Debug.LogError(
                "[COMPUTATIONAL BENCHMARK] " +
                "Capture primero ROBOT INITIAL STATE."
            );

            yield break;
        }


        if (benchmarkRunsPerCell < 1)
        {
            Debug.LogError(
                "[COMPUTATIONAL BENCHMARK] " +
                "benchmarkRunsPerCell debe ser >= 1."
            );

            yield break;
        }


        if (benchmarkInterRunDelaySeconds < 0f)
        {
            Debug.LogError(
                "[COMPUTATIONAL BENCHMARK] " +
                "benchmarkInterRunDelaySeconds no puede ser negativo."
            );

            yield break;
        }


        // ========================================================
        // 1. OBTENER SOLO E1 Y E4
        // ========================================================

        ScenarioState[] benchmarkScenarios =
            GetComputationalBenchmarkScenarios();


        // Validar que ambos escenarios estén correctamente capturados.
        for (int i = 0;
             i < benchmarkScenarios.Length;
             i++)
        {
            if (!ValidateRunConfiguration(
                    benchmarkScenarios[i]))
            {
                Debug.LogError(
                    "[COMPUTATIONAL BENCHMARK] " +
                    "Configuracion invalida en " +
                    benchmarkScenarios[i].scenarioID
                );

                yield break;
            }
        }


        // ========================================================
        // 2. INICIALIZAR BENCHMARK
        // ========================================================

        campaignRunning = true;

        campaignStopRequested = false;

        campaignCompletedRuns = 0;


        campaignTotalRuns =
            campaignMethods.Length *
            benchmarkScenarios.Length *
            benchmarkRunsPerCell;


        robotController.SetExperimentHold(
            true
        );


        telemetry.SetRunMeasurementActive(
            false
        );


        Debug.Log(
            "========================================\n" +
            "[COMPUTATIONAL BENCHMARK] START\n" +
            "Methods = " +
            campaignMethods.Length + "\n" +
            "Scenarios = 2 (E1,E4)\n" +
            "RunsPerCell = " +
            benchmarkRunsPerCell + "\n" +
            "TOTAL = " +
            campaignTotalRuns + "\n" +
            "Seeds = " +
            campaignBaseSeed + "..." +
            (
                campaignBaseSeed +
                benchmarkRunsPerCell -
                1
            ) + "\n" +
            "========================================"
        );


        // ========================================================
        // 3. ESCENARIOS
        // ========================================================

        for (int scenarioIndex = 0;
             scenarioIndex < benchmarkScenarios.Length;
             scenarioIndex++)
        {
            if (campaignStopRequested)
            {
                break;
            }


            ScenarioState selectedScenario =
                benchmarkScenarios[
                    scenarioIndex
                ];


            // ====================================================
            // 4. REPETICIONES
            // ====================================================

            for (int run = 1;
                 run <= benchmarkRunsPerCell;
                 run++)
            {
                if (campaignStopRequested)
                {
                    break;
                }


                // ------------------------------------------------
                // Semilla experimental:
                //
                // run 1 = 1001
                // run 2 = 1002
                // run 3 = 1003
                //
                // MISMA para los nueve métodos.
                // ------------------------------------------------

                experimentSeed =
                    campaignBaseSeed +
                    (run - 1);


                // ------------------------------------------------
                // Orden reproducible de métodos.
                // ------------------------------------------------

                int shuffleSeed =
                    100000 +
                    scenarioIndex * 1000 +
                    experimentSeed;


                RobotSocialNavController.ExperimentMode[]
                    benchmarkOrder =
                        GetBenchmarkMethodOrder(
                            shuffleSeed
                        );


                // ================================================
                // 5. NUEVE METODOS
                // ================================================

                for (int methodIndex = 0;
                     methodIndex <
                     benchmarkOrder.Length;
                     methodIndex++)
                {
                    if (campaignStopRequested)
                    {
                        break;
                    }


                    RobotSocialNavController.ExperimentMode
                        selectedMethod =
                            benchmarkOrder[
                                methodIndex
                            ];


                    // Detener el robot antes de cambiar controlador.
                    robotController.SetExperimentHold(
                        true
                    );


                    // Cambiar método.
                    robotController.experimentMode =
                        selectedMethod;


                    // Un frame para estabilizar el cambio.
                    yield return null;


                    Debug.Log(
                        "========================================\n" +
                        "[COMPUTATIONAL BENCHMARK] PREPARING RUN\n" +
                        "Progress = " +
                        (campaignCompletedRuns + 1) +
                        "/" +
                        campaignTotalRuns + "\n" +
                        "Method = " +
                        selectedMethod + "\n" +
                        "Scenario = " +
                        selectedScenario.scenarioID + "\n" +
                        "Run = " +
                        run +
                        "/" +
                        benchmarkRunsPerCell + "\n" +
                        "Seed = " +
                        experimentSeed + "\n" +
                        "========================================"
                    );


                    // ============================================
                    // AQUÍ reutilizamos exactamente
                    // AutomaticExperimentRoutine
                    //
                    // Esta función ya:
                    // - resetea robot
                    // - resetea humanos
                    // - aplica escenario
                    // - configura semilla
                    // - ejecuta A -> B -> A
                    // - inicia benchmark
                    // - termina benchmark
                    // ============================================

                    yield return StartCoroutine(
                        AutomaticExperimentRoutine(
                            selectedScenario,
                            run
                        )
                    );


                    if (campaignStopRequested)
                    {
                        break;
                    }


                    campaignCompletedRuns++;


                    Debug.Log(
                        "[COMPUTATIONAL BENCHMARK] RUN COMPLETE" +
                        " | " +
                        campaignCompletedRuns +
                        "/" +
                        campaignTotalRuns +
                        " | Method=" +
                        selectedMethod +
                        " | Scenario=" +
                        selectedScenario.scenarioID +
                        " | Run=" +
                        run +
                        " | Seed=" +
                        experimentSeed +
                        " | Result=" +
                        lastRunTermination
                    );


                    // Esperar antes de comenzar siguiente corrida.
                    yield return new WaitForSecondsRealtime(
                        benchmarkInterRunDelaySeconds
                    );
                }
            }
        }


        // ========================================================
        // 6. FIN
        // ========================================================

        robotController.SetExperimentHold(
            true
        );


        telemetry.SetRunMeasurementActive(
            false
        );


        campaignRunning =
            false;


        currentExperimentState =
            ExperimentState.Idle;


        if (campaignStopRequested)
        {
            Debug.LogWarning(
                "========================================\n" +
                "[COMPUTATIONAL BENCHMARK] ABORTED\n" +
                "Completed = " +
                campaignCompletedRuns +
                "/" +
                campaignTotalRuns + "\n" +
                "========================================"
            );
        }
        else
        {
            Debug.Log(
                "========================================\n" +
                "[COMPUTATIONAL BENCHMARK] COMPLETE\n" +
                "Completed = " +
                campaignCompletedRuns +
                "/" +
                campaignTotalRuns + "\n" +
                "========================================"
            );
        }
    }
    // ============================================================
    // BOTON: RUN COMPUTATIONAL BENCHMARK
    // ============================================================

    [ContextMenu("RUN COMPUTATIONAL BENCHMARK")]
    public void RunComputationalBenchmark()
    {
        if (campaignRunning ||
            experimentRunning)
        {
            Debug.LogWarning(
                "[COMPUTATIONAL BENCHMARK] " +
                "Ya existe una campana/corrida activa."
            );

            return;
        }

        campaignStopRequested =
            false;

        StartCoroutine(
            ComputationalBenchmarkRoutine()
        );
    }
    // ============================================================
    // BOTON: STOP COMPUTATIONAL BENCHMARK
    // ============================================================

    [ContextMenu("STOP COMPUTATIONAL BENCHMARK")]
    public void StopComputationalBenchmark()
    {
        if (!campaignRunning &&
            !experimentRunning)
        {
            Debug.LogWarning(
                "[COMPUTATIONAL BENCHMARK] " +
                "No existe benchmark activo."
            );

            return;
        }

        campaignStopRequested =
            true;

        Debug.LogWarning(
            "[COMPUTATIONAL BENCHMARK] STOP solicitado."
        );
    }

    [ContextMenu("RUN FULL CAMPAIGN")]
    public void RunFullCampaign()
    {
        if (campaignRunning ||
            experimentRunning)
        {
            Debug.LogWarning(
                "[CAMPAIGN] Ya existe una campana/corrida activa."
            );

            return;
        }

        campaignStopRequested =
            false;

        StartCoroutine(
            FullCampaignRoutine()
        );
    }


    [ContextMenu("STOP FULL CAMPAIGN")]
    public void StopFullCampaign()
    {
        if (!campaignRunning &&
            !experimentRunning)
        {
            Debug.LogWarning(
                "[CAMPAIGN] No existe campana/corrida activa."
            );

            return;
        }

        campaignStopRequested =
            true;

        // Si hay una corrida físicamente activa, ella misma
        // detectará campaignStopRequested en el siguiente frame
        // y enviará status=6 a MATLAB.
        // Si estamos entre corridas, no generamos un falso ABORTED.
        if (!experimentRunning)
        {
            if (telemetry != null)
            {
                telemetry.SetRunMeasurementActive(false);
            }

            if (robotController != null)
            {
                robotController.SetExperimentHold(true);
                robotController.SetExternalMissionControl(false);
            }
        }

        Debug.LogWarning(
            "[CAMPAIGN] STOP solicitado."
        );
    }

    [ContextMenu("START / RELEASE ROBOT")]
    public void ReleaseRobotAfterReset()
    {
        if (robotController == null)
        {
            Debug.LogError(
                "[ScenarioManager] START cancelado: " +
                "RobotController no está asignado."
            );

            return;
        }


        NavMeshAgent robotAgent =
            robotController.GetComponent<NavMeshAgent>();


        if (robotAgent != null &&
            robotAgent.enabled &&
            robotAgent.gameObject.activeInHierarchy &&
            robotAgent.isOnNavMesh)
        {
            robotAgent.nextPosition =
                robotController.transform.position;

            robotAgent.velocity =
                Vector3.zero;
        }


        robotController.SetExperimentHold(
            false
        );


        Debug.Log(
            "========================================\n" +
            "[ScenarioManager] ROBOT RELEASED\n" +
            "Robot = " +
            robotController.gameObject.name + "\n" +
            "Position = " +
            robotController.transform.position + "\n" +
            "Mode = " +
            robotController.experimentMode + "\n" +
            "Hold = " +
            robotController.ExperimentHold + "\n" +
            "========================================"
        );
    }


    // ============================================================
    // INVENTARIO GLOBAL
    // ============================================================

    [Header("Humanos del experimento")]
    [Tooltip(
        "Inventario global de referencias " +
        "de los humanos experimentales."
    )]
    public List<HumanReference> humans =
        new List<HumanReference>();


    // ============================================================
    // PRESETS DE LOS CUATRO ESCENARIOS
    // ============================================================

    [Header("Presets de escenarios")]

    public ScenarioState E1 = new ScenarioState
    {
        scenarioID = "E1"
    };

    public ScenarioState E2 = new ScenarioState
    {
        scenarioID = "E2"
    };

    public ScenarioState E3 = new ScenarioState
    {
        scenarioID = "E3"
    };

    public ScenarioState E4 = new ScenarioState
    {
        scenarioID = "E4"
    };


    // ============================================================
    // VALIDACIÓN BÁSICA
    // ============================================================

    /// <summary>
    /// Determina si un ID humano pertenece al escenario indicado.
    ///
    /// Ejemplos:
    /// E101 empieza por E1 -> pertenece a E1.
    /// E202 empieza por E2 -> pertenece a E2.
    /// E303 empieza por E3 -> pertenece a E3.
    /// E401 empieza por E4 -> pertenece a E4.
    /// </summary>
    private bool BelongsToScenario(string humanId, string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(humanId))
            return false;

        if (string.IsNullOrWhiteSpace(scenarioId))
            return false;

        return humanId.StartsWith(scenarioId);
    }


    /// <summary>
    /// Busca una referencia humana por su ID experimental.
    /// </summary>
    private HumanReference FindHumanReference(string id)
    {
        foreach (HumanReference reference in humans)
        {
            if (reference == null)
                continue;

            if (reference.id == id)
                return reference;
        }

        return null;
    }


    // ============================================================
    // CAPTURA DE ESCENARIOS
    // ============================================================

    /// <summary>
    /// Captura el estado geométrico actual de todos los humanos
    /// pertenecientes al escenario solicitado.
    ///
    /// IMPORTANTE:
    /// Esta función solamente LEE la escena.
    /// No mueve, activa, desactiva ni modifica ningún GameObject.
    /// </summary>
    private void CaptureScenario(
        string scenarioId,
        ScenarioState targetScenario)
    {
        if (targetScenario == null)
        {
            Debug.LogError(
                "[ScenarioManager] El ScenarioState de " +
                scenarioId +
                " es NULL."
            );

            return;
        }


        // --------------------------------------------------------
        // Establecer ID del escenario
        // --------------------------------------------------------

        targetScenario.scenarioID = scenarioId;


        // --------------------------------------------------------
        // Borrar captura anterior
        // --------------------------------------------------------

        targetScenario.humans.Clear();


        // --------------------------------------------------------
        // Recorrer inventario global
        // --------------------------------------------------------

        foreach (HumanReference reference in humans)
        {
            // Referencia inexistente
            if (reference == null)
            {
                Debug.LogWarning(
                    "[ScenarioManager] Se encontró una " +
                    "HumanReference NULL."
                );

                continue;
            }


            // ID vacío
            if (string.IsNullOrWhiteSpace(reference.id))
            {
                Debug.LogWarning(
                    "[ScenarioManager] Existe una referencia " +
                    "con ID vacío."
                );

                continue;
            }


            // GameObject no asignado
            if (reference.human == null)
            {
                Debug.LogWarning(
                    "[ScenarioManager] El ID " +
                    reference.id +
                    " no tiene GameObject asignado."
                );

                continue;
            }


            // ----------------------------------------------------
            // Filtrar por escenario
            // ----------------------------------------------------

            if (!BelongsToScenario(reference.id, scenarioId))
                continue;


            // ----------------------------------------------------
            // Crear fotografía del estado actual
            // ----------------------------------------------------

            HumanState state = new HumanState();

            state.id = reference.id;

            state.active =
                reference.human.activeSelf;

            state.position =
                reference.human.transform.position;

            state.rotation =
                reference.human.transform.rotation;

            state.scale =
                reference.human.transform.localScale;


            // ----------------------------------------------------
            // Guardar estado dentro del preset
            // ----------------------------------------------------

            targetScenario.humans.Add(state);


            // ----------------------------------------------------
            // Información individual en Console
            // ----------------------------------------------------

            Debug.Log(
                "[ScenarioManager] " +
                scenarioId +
                " | Capturado " +
                reference.id +
                " | GameObject = " +
                reference.human.name +
                " | Position = " +
                state.position +
                " | Active = " +
                state.active
            );
        }


        // --------------------------------------------------------
        // Resultado final
        // --------------------------------------------------------

        Debug.Log(
            "========================================\n" +
            "[ScenarioManager] CAPTURA COMPLETADA\n" +
            "Escenario: " + scenarioId + "\n" +
            "Humanos capturados: " +
            targetScenario.humans.Count +
            "\n========================================"
        );
    }


    // ============================================================
    // BOTONES MEDIANTE CONTEXT MENU
    // ============================================================

    [ContextMenu("CAPTURE E1")]
    public void CaptureE1()
    {
        CaptureScenario("E1", E1);

        if (E1.humans.Count != 2)
        {
            Debug.LogWarning(
                "[ScenarioManager] ATENCION: " +
                "E1 debería contener 2 humanos, " +
                "pero se capturaron " +
                E1.humans.Count +
                "."
            );
        }
    }


    [ContextMenu("CAPTURE E2")]
    public void CaptureE2()
    {
        CaptureScenario("E2", E2);

        if (E2.humans.Count != 2)
        {
            Debug.LogWarning(
                "[ScenarioManager] ATENCION: " +
                "E2 debería contener 2 humanos, " +
                "pero se capturaron " +
                E2.humans.Count +
                "."
            );
        }
    }


    [ContextMenu("CAPTURE E3")]
    public void CaptureE3()
    {
        CaptureScenario("E3", E3);

        if (E3.humans.Count != 3)
        {
            Debug.LogWarning(
                "[ScenarioManager] ATENCION: " +
                "E3 debería contener 3 humanos, " +
                "pero se capturaron " +
                E3.humans.Count +
                "."
            );
        }
    }


    [ContextMenu("CAPTURE E4")]
    public void CaptureE4()
    {
        CaptureScenario("E4", E4);

        if (E4.humans.Count != 9)
        {
            Debug.LogWarning(
                "[ScenarioManager] ATENCION: " +
                "E4 debería contener 9 humanos, " +
                "pero se capturaron " +
                E4.humans.Count +
                "."
            );
        }
    }


    // ============================================================
    // VALIDACIÓN DEL INVENTARIO
    // ============================================================

    [ContextMenu("VALIDATE HUMAN REFERENCES")]
    public void ValidateHumanReferences()
    {
        Debug.Log(
            "========================================\n" +
            "[ScenarioManager] VALIDANDO REFERENCIAS\n" +
            "Total registrado: " +
            humans.Count +
            "\n========================================"
        );


        HashSet<string> ids =
            new HashSet<string>();

        HashSet<GameObject> objects =
            new HashSet<GameObject>();


        foreach (HumanReference reference in humans)
        {
            if (reference == null)
            {
                Debug.LogError(
                    "[ScenarioManager] " +
                    "HumanReference NULL."
                );

                continue;
            }


            // ----------------------------------------------------
            // Validar ID
            // ----------------------------------------------------

            if (string.IsNullOrWhiteSpace(reference.id))
            {
                Debug.LogError(
                    "[ScenarioManager] " +
                    "Existe un ID vacío."
                );
            }
            else
            {
                if (!ids.Add(reference.id))
                {
                    Debug.LogError(
                        "[ScenarioManager] ID DUPLICADO: " +
                        reference.id
                    );
                }
            }


            // ----------------------------------------------------
            // Validar GameObject
            // ----------------------------------------------------

            if (reference.human == null)
            {
                Debug.LogError(
                    "[ScenarioManager] " +
                    reference.id +
                    " no tiene GameObject."
                );
            }
            else
            {
                if (!objects.Add(reference.human))
                {
                    Debug.LogWarning(
                        "[ScenarioManager] El GameObject '" +
                        reference.human.name +
                        "' aparece asociado a más de un ID. " +
                        "Verificar si esto es intencional."
                    );
                }
            }
        }


        Debug.Log(
            "[ScenarioManager] Validación terminada."
        );
    }


    // ============================================================
    // INFORMACIÓN GENERAL
    // ============================================================

    [ContextMenu("SHOW EXPERIMENT SUMMARY")]
    public void ShowExperimentSummary()
    {
        int countE1 = 0;
        int countE2 = 0;
        int countE3 = 0;
        int countE4 = 0;


        foreach (HumanReference reference in humans)
        {
            if (reference == null)
                continue;

            if (BelongsToScenario(reference.id, "E1"))
                countE1++;

            else if (BelongsToScenario(reference.id, "E2"))
                countE2++;

            else if (BelongsToScenario(reference.id, "E3"))
                countE3++;

            else if (BelongsToScenario(reference.id, "E4"))
                countE4++;
        }


        Debug.Log(
            "========================================\n" +
            "EXPERIMENT SCENARIO SUMMARY\n" +
            "----------------------------------------\n" +
            "E1 = " + countE1 + " humanos\n" +
            "E2 = " + countE2 + " humanos\n" +
            "E3 = " + countE3 + " humanos\n" +
            "E4 = " + countE4 + " humanos\n" +
            "----------------------------------------\n" +
            "TOTAL = " +
            (countE1 + countE2 + countE3 + countE4) +
            "\n========================================"
        );
    }
}