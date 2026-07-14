using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryTrail : MonoBehaviour
{
    [Header("Trajectory Source")]
    public Transform trackedObject;

    [Header("Sampling")]
    public float minDistanceBetweenPoints = 0.08f;
    public float heightOffset = 0.05f;
    public int maxPoints = 5000;

    [Header("Line Appearance")]
    public float lineWidth = 0.08f;
    public Color lineColor = Color.cyan;

    [Header("Runtime")]
    public bool recordTrajectory = true;
    public bool clearOnStart = true;

    private LineRenderer lineRenderer;
    private readonly List<Vector3> points = new List<Vector3>();
    private Vector3 lastPoint;
    private bool hasLastPoint = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = lineColor;
        lineRenderer.material = mat;

        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    private void Start()
    {
        if (trackedObject == null)
        {
            trackedObject = transform;
        }

        if (clearOnStart)
        {
            ClearTrajectory();
        }

        AddPoint(GetTrackedPosition());
    }

    private void Update()
    {
        if (!recordTrajectory || trackedObject == null)
            return;

        Vector3 currentPoint = GetTrackedPosition();

        if (!hasLastPoint)
        {
            AddPoint(currentPoint);
            return;
        }

        float distance = Vector3.Distance(ProjectXZ(currentPoint), ProjectXZ(lastPoint));

        if (distance >= minDistanceBetweenPoints)
        {
            AddPoint(currentPoint);
        }
    }

    private Vector3 GetTrackedPosition()
    {
        Vector3 p = trackedObject.position;
        p.y += heightOffset;
        return p;
    }

    private void AddPoint(Vector3 p)
    {
        if (points.Count >= maxPoints)
        {
            points.RemoveAt(0);
        }

        points.Add(p);
        lastPoint = p;
        hasLastPoint = true;

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    public void ClearTrajectory()
    {
        points.Clear();
        lineRenderer.positionCount = 0;
        hasLastPoint = false;
    }

    private Vector3 ProjectXZ(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}