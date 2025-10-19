using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using NUnit.Framework;

[RequireComponent(typeof(LineRenderer))]
public class AIPathDebugLine : MonoBehaviour
{
    public NavMeshAgent navAi;
    private LineRenderer _lineRenderer;

    private readonly List<GameObject> _markers = new();
    private Vector3[] _lastCorners = new Vector3[0];

    private Color _initialStartColor;
    private Color _initialEndColor;

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();

        _lineRenderer.startWidth = 0.05f;
        _lineRenderer.endWidth = 0.05f;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        _initialStartColor = _lineRenderer.startColor;
        _initialEndColor = _lineRenderer.endColor;
        if (!navAi)
        {
            navAi = GetComponentInParent<NavMeshAgent>();
        }

        Assert.IsTrue(navAi != null);
    }

    private void Update()
    {
        if (!navAi || !navAi.hasPath || navAi.pathPending || navAi.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            _lineRenderer.positionCount = 0;
            ClearMarkers();
            _lastCorners = Array.Empty<Vector3>();
            return;
        }

        if (navAi.isStopped)
        {
            _lineRenderer.startColor = Color.black;
            _lineRenderer.endColor = Color.black;
        }
        else
        {
            _lineRenderer.startColor = _initialStartColor;
            _lineRenderer.endColor = _initialEndColor;
        }

        var corners = navAi.path.corners;

        if (!CornersMatch(_lastCorners, corners))
        {
            _lineRenderer.positionCount = corners.Length;
            _lineRenderer.SetPositions(corners);
            _lastCorners = corners;
            UpdateMarkers(corners);
        }

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Debug.DrawLine(corners[i], corners[i + 1], Color.white);
        }

        if (corners.Length > 0)
        {
            Debug.DrawLine(corners[corners.Length - 1], navAi.destination, Color.yellow);
        }
    }

    private void UpdateMarkers(Vector3[] corners)
    {
        ClearMarkers();
        foreach (var corner in corners)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = corner;
            marker.transform.localScale = Vector3.one * 0.2f;
            Destroy(marker.GetComponent<Collider>());
            _markers.Add(marker);
        }
    }

    private void ClearMarkers()
    {
        foreach (var m in _markers) Destroy(m);
        _markers.Clear();
    }

    private bool CornersMatch(Vector3[] a, Vector3[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if ((a[i] - b[i]).sqrMagnitude > 0.001f * 0.001f) return false;
        }
        return true;
    }
}
