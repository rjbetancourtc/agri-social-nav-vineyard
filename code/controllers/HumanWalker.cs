using UnityEngine;

public class HumanWalker : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 1.5f;
    public float reachThreshold = 0.1f;

    private Transform target;

    void Start()
    {
        target = pointB;
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;

        Vector3 direction = (target.position - transform.position);
        direction.y = 0f;

        if (direction.magnitude > reachThreshold)
        {
            Vector3 moveDir = direction.normalized;
            transform.position += moveDir * speed * Time.deltaTime;

            if (moveDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(moveDir),
                    5f * Time.deltaTime
                );
            }
        }
        else
        {
            target = target == pointA ? pointB : pointA;
        }
    }
}