using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HumanPatrol : MonoBehaviour
{
    [Header("Patrol Points")]
    public Transform pointA;
    public Transform pointB;

    [Header("Motion Parameters")]
    public float speed = 0.9f;
    public float reachDistance = 0.2f;
    public float turnSpeed = 6f;
    public float gravity = -9.81f;

    [Header("Human-Human Avoidance")]
    public LayerMask humanMask;
    public float humanDetectionRadius = 1.8f;
    public float separationRadius = 0.9f;
    public float avoidanceStrength = 1.25f;
    public float lateralPassingStrength = 0.85f;
    public bool passOnRightSide = true;

    [Header("Anti-Stuck")]
    public float stuckSpeedThreshold = 0.03f;
    public float stuckTimeLimit = 1.0f;
    public float unstuckLateralStrength = 1.2f;

    private Transform currentTarget;
    private CharacterController controller;
    private float verticalVelocity;

    private readonly Collider[] humanHits = new Collider[24];

    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private int unstuckDirection = 1;
    private int currentSeed = 0;
    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentTarget = pointB;
        lastPosition = transform.position;

        unstuckDirection = Random.value > 0.5f ? 1 : -1;
    }

    void Update()
    {
        if (pointA == null || pointB == null)
            return;

        Vector3 direction = currentTarget.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= reachDistance)
        {
            currentTarget = (currentTarget == pointA) ? pointB : pointA;
            return;
        }

        Vector3 routeDir = direction.normalized;

        Vector3 avoidanceDir = ComputeHumanAvoidance(routeDir);
        Vector3 stuckDir = ComputeAntiStuckDirection(routeDir);

        Vector3 finalDir = routeDir + avoidanceDir + stuckDir;

        if (finalDir.sqrMagnitude < 1e-6f)
            finalDir = routeDir;

        finalDir.y = 0f;
        finalDir.Normalize();

        RotateTowards(finalDir);
        MoveWithGravity(finalDir);

        UpdateStuckState();
    }

    Vector3 ComputeHumanAvoidance(Vector3 routeDir)
    {
        Vector3 avoidance = Vector3.zero;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            humanDetectionRadius,
            humanHits,
            humanMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            Collider c = humanHits[i];

            if (c == null)
                continue;

            Transform otherRoot = c.transform.root;

            if (otherRoot == transform.root)
                continue;

            Vector3 toOther = otherRoot.position - transform.position;
            toOther.y = 0f;

            float distance = toOther.magnitude;

            if (distance < 1e-4f || distance > humanDetectionRadius)
                continue;

            Vector3 away = -toOther.normalized;

            float proximityFactor = Mathf.Clamp01(
                (humanDetectionRadius - distance) / humanDetectionRadius
            );

            avoidance += away * proximityFactor * avoidanceStrength;

            if (distance <= separationRadius)
            {
                Vector3 lateralDir;

                if (passOnRightSide)
                    lateralDir = Vector3.Cross(Vector3.up, routeDir).normalized;
                else
                    lateralDir = Vector3.Cross(routeDir, Vector3.up).normalized;

                avoidance += lateralDir * lateralPassingStrength;
            }
        }

        avoidance.y = 0f;
        return avoidance;
    }

    Vector3 ComputeAntiStuckDirection(Vector3 routeDir)
    {
        if (stuckTimer < stuckTimeLimit)
            return Vector3.zero;

        Vector3 lateralDir = Vector3.Cross(Vector3.up, routeDir).normalized;
        return lateralDir * unstuckDirection * unstuckLateralStrength;
    }

    void UpdateStuckState()
    {
        Vector3 displacement = transform.position - lastPosition;
        displacement.y = 0f;

        float realSpeed = displacement.magnitude / Mathf.Max(Time.deltaTime, 1e-5f);

        if (realSpeed < stuckSpeedThreshold)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    void RotateTowards(Vector3 moveDir)
    {
        if (moveDir.sqrMagnitude < 1e-8f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    void MoveWithGravity(Vector3 moveDir)
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = moveDir * speed;
        motion.y = verticalVelocity;

        controller.Move(motion * Time.deltaTime);
    }


    /// <summary>
    /// Restaura el estado dinámico interno de HumanPatrol
    /// al estado inicial de una nueva corrida experimental.
    ///
    /// IMPORTANTE:
    /// Este método NO modifica los parámetros físicos
    /// ni de comportamiento del humano.
    /// </summary>
    public void ResetPatrolState()
    {
        // Recuperar el CharacterController si por alguna razón
        // todavía no está referenciado.
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        // Al inicio de la patrulla el objetivo original es Point B,
        // igual que ocurre en Start().
        currentTarget = pointB;

        // Eliminar velocidad vertical residual
        // de la corrida anterior.
        verticalVelocity = 0f;

        // Eliminar tiempo acumulado del sistema anti-stuck.
        stuckTimer = 0f;

        // Sincronizar lastPosition con la posición que el
        // ScenarioManager acaba de restaurar.
        lastPosition = transform.position;

        Debug.Log(
            "[HumanPatrol] Reset dinámico completado | " +
            "Human = " + gameObject.name +
            " | Target = " +
            (currentTarget != null ? currentTarget.name : "NULL") +
            " | Position = " +
            transform.position
        );
    }
    /// <summary>
    /// Configura de forma determinista el estado aleatorio
    /// de este humano para una repetición experimental.
    /// </summary>
    public void ApplyExperimentSeed(int seed)
    {
        currentSeed = seed;

        // Generador local independiente.
        // No modifica UnityEngine.Random global.
        System.Random rng = new System.Random(seed);

        // Reproduce la decisión binaria original:
        // dirección lateral +1 o -1.
        unstuckDirection =
            rng.NextDouble() > 0.5 ? 1 : -1;

        Debug.Log(
            "[HumanPatrol] Seed aplicada | " +
            "Human = " + gameObject.name +
            " | Seed = " + currentSeed +
            " | UnstuckDirection = " + unstuckDirection
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, humanDetectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}