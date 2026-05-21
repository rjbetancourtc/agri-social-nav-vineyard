using UnityEngine;

/// <summary>
/// Attach this component to each human agent used by Social-DWA.
/// The transform.forward direction is interpreted as the human frontal orientation.
/// </summary>
public class SocialDWAHuman : MonoBehaviour
{
    public Transform orientationReference;

    public Vector3 Position
    {
        get { return transform.position; }
    }

    public float HeadingRad
    {
        get
        {
            Vector3 fwd = orientationReference != null ? orientationReference.forward : transform.forward;
            return Mathf.Atan2(fwd.z, fwd.x);
        }
    }
}