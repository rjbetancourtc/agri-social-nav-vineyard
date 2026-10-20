using UnityEngine;
public static class VelocityObstacleMath
{
 // q=p_h-p_r; u=v_r-v_h; q(t)=q-u*t. Tangency is a risk.
 public static bool Risk(Vector3 q,Vector3 u,float radius,float horizon,out float t,out float distance)
 {
  float norm=Vector3.Dot(u,u);
  t=norm<1e-8f?0f:Mathf.Clamp(Vector3.Dot(q,u)/norm,0f,horizon);
  distance=(q-u*t).magnitude;
  return distance<=radius;
 }
}
