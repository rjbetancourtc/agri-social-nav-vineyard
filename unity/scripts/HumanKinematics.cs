using UnityEngine;

/// <summary>
/// Estima cinemática planar del humano en el plano XZ:
/// - velocidad
/// - aceleración
/// - heading (orientación yaw)
/// </summary>
public class HumanKinematics : MonoBehaviour
{
    [Header("Suavizado")]
    [Range(0f, 0.99f)]
    public float velocitySmoothing = 0.15f;

    [Header("Depuración")]
    public bool drawDebug = true;
    public float debugScale = 0.75f;

    private Vector3 previousPosition;
    private Vector3 previousVelocity;

    public Vector3 PlanarVelocity { get; private set; }
    public Vector3 PlanarAcceleration { get; private set; }
    public float HeadingRad { get; private set; }

    public float HeadingDeg
    {
        get { return HeadingRad * Mathf.Rad2Deg; }
    }

    private void Start()
    {
        previousPosition = transform.position;
        previousVelocity = Vector3.zero;

        Vector3 fwd = transform.forward;
        fwd.y = 0f;

        if (fwd.sqrMagnitude < 1e-8f)
            fwd = Vector3.forward;

        fwd.Normalize();
        HeadingRad = Mathf.Atan2(fwd.z, fwd.x);
    }

    private void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-5f);

        Vector3 currentPosition = transform.position;
        Vector3 rawVelocity = (currentPosition - previousPosition) / dt;
        rawVelocity.y = 0f;

        Vector3 smoothedVelocity = Vector3.Lerp(rawVelocity, previousVelocity, velocitySmoothing);
        smoothedVelocity.y = 0f;
        PlanarVelocity = smoothedVelocity;

        Vector3 acc = (PlanarVelocity - previousVelocity) / dt;
        acc.y = 0f;
        PlanarAcceleration = acc;

        if (PlanarVelocity.magnitude > 0.02f)
        {
            HeadingRad = Mathf.Atan2(PlanarVelocity.z, PlanarVelocity.x);
        }
        else
        {
            Vector3 fwd = transform.forward;
            fwd.y = 0f;

            if (fwd.sqrMagnitude > 1e-8f)
            {
                fwd.Normalize();
                HeadingRad = Mathf.Atan2(fwd.z, fwd.x);
            }
        }

        previousPosition = currentPosition;
        previousVelocity = PlanarVelocity;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
            return;

        Vector3 origin = transform.position + Vector3.up * 1.5f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + PlanarVelocity * debugScale);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(origin, origin + PlanarAcceleration * debugScale);

        Vector3 heading = new Vector3(Mathf.Cos(HeadingRad), 0f, Mathf.Sin(HeadingRad));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + heading * (debugScale * 1.2f));
    }
}