using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotNavPatrol : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float switchDistance = 0.3f;

    private NavMeshAgent agent;
    private Transform currentTarget;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        Debug.Log("RobotNavPatrol: Start ejecutado en " + gameObject.name);

        if (pointA == null || pointB == null)
        {
            Debug.LogError("RobotNavPatrol: faltan Point_A_R o Point_B_R");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("RobotNavPatrol: el robot NO está sobre el NavMesh");
            return;
        }

        currentTarget = pointB;
        agent.SetDestination(currentTarget.position);

        Debug.Log("RobotNavPatrol: destino inicial asignado a " + currentTarget.name);
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;
        if (!agent.isOnNavMesh) return;
        if (agent.pathPending) return;

        if (agent.remainingDistance <= switchDistance)
        {
            currentTarget = (currentTarget == pointA) ? pointB : pointA;
            agent.SetDestination(currentTarget.position);
            Debug.Log("RobotNavPatrol: cambio de destino -> " + currentTarget.name);
        }
    }
}