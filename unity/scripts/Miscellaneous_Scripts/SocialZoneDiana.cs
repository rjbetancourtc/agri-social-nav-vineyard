using UnityEngine;

[ExecuteAlways]
public class SocialZoneDiana : MonoBehaviour
{
    [Header("Zone Radii")]
    public float intimateRadius = 0.45f;
    public float personalRadius = 1.20f;
    public float socialRadius = 3.60f;

    [Header("Visibility")]
    public bool showIntimate = true;
    public bool showPersonal = true;
    public bool showSocial = true;

    [Header("Appearance")]
    [Range(24, 256)]
    public int segments = 100;
    public float yOffset = 0.05f;
    public float lineWidth = 0.04f;

    public Color intimateColor = new Color(1f, 0f, 0f, 0.95f);
    public Color personalColor = new Color(1f, 0.5f, 0f, 0.95f);
    public Color socialColor = new Color(0f, 1f, 1f, 0.95f);

    private LineRenderer intimateLR;
    private LineRenderer personalLR;
    private LineRenderer socialLR;

    private void OnEnable()
    {
        EnsureAllRings();
        RebuildAll();
    }

    private void OnValidate()
    {
        segments = Mathf.Max(24, segments);
        lineWidth = Mathf.Max(0.001f, lineWidth);
        yOffset = Mathf.Max(0f, yOffset);

        EnsureAllRings();
        RebuildAll();
    }

    private void LateUpdate()
    {
        EnsureAllRings();
        UpdateVisibility();
    }

    private void EnsureAllRings()
    {
        intimateLR = GetOrCreateRing("IntimateZoneRing", ref intimateLR, intimateColor);
        personalLR = GetOrCreateRing("PersonalZoneRing", ref personalLR, personalColor);
        socialLR = GetOrCreateRing("SocialZoneRing", ref socialLR, socialColor);
    }

    private LineRenderer GetOrCreateRing(string childName, ref LineRenderer lr, Color color)
    {
        Transform child = transform.Find(childName);

        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            lr = go.AddComponent<LineRenderer>();
        }
        else
        {
            lr = child.GetComponent<LineRenderer>();
            if (lr == null)
                lr = child.gameObject.AddComponent<LineRenderer>();
        }

        lr.useWorldSpace = false;
        lr.loop = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (lr.sharedMaterial == null || lr.sharedMaterial.shader != shader)
        {
            Material mat = new Material(shader);
            mat.color = color;
            lr.sharedMaterial = mat;
        }
        else
        {
            lr.sharedMaterial.color = color;
        }

        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        return lr;
    }

    private void RebuildAll()
    {
        if (intimateLR != null)
        {
            intimateLR.startWidth = lineWidth;
            intimateLR.endWidth = lineWidth;
            intimateLR.startColor = intimateColor;
            intimateLR.endColor = intimateColor;
            if (intimateLR.sharedMaterial != null) intimateLR.sharedMaterial.color = intimateColor;
            DrawCircle(intimateLR, intimateRadius);
        }

        if (personalLR != null)
        {
            personalLR.startWidth = lineWidth;
            personalLR.endWidth = lineWidth;
            personalLR.startColor = personalColor;
            personalLR.endColor = personalColor;
            if (personalLR.sharedMaterial != null) personalLR.sharedMaterial.color = personalColor;
            DrawCircle(personalLR, personalRadius);
        }

        if (socialLR != null)
        {
            socialLR.startWidth = lineWidth;
            socialLR.endWidth = lineWidth;
            socialLR.startColor = socialColor;
            socialLR.endColor = socialColor;
            if (socialLR.sharedMaterial != null) socialLR.sharedMaterial.color = socialColor;
            DrawCircle(socialLR, socialRadius);
        }

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (intimateLR != null) intimateLR.enabled = showIntimate;
        if (personalLR != null) personalLR.enabled = showPersonal;
        if (socialLR != null) socialLR.enabled = showSocial;
    }

    private void DrawCircle(LineRenderer lr, float radius)
    {
        int count = segments + 1;
        lr.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / segments;
            float angle = t * Mathf.PI * 2f;

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            lr.SetPosition(i, new Vector3(x, yOffset, z));
        }
    }
}