#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class NavMeshPathHighlighter : MonoBehaviour
{
    [Range(0.01f, 1f)]
    public float sampleStep = 0.1f;                // metres

    private Mesh _triMesh;
    private Material _mat;
    private static readonly Color START = new(0f, 0.4f, 0f, 1f); // dark green
    private static readonly Vector3 LABEL_OFFSET = Vector3.up * 0.05f;

    void OnEnable()
    {
        _triMesh ??= new Mesh();
        _mat ??= new Material(Shader.Find("Unlit/Color"));
    }

    void OnDrawGizmos()
    {
        var agent = GetComponent<NavMeshAgent>();
        if (agent.pathStatus != NavMeshPathStatus.PathComplete) return;

        var tri = NavMesh.CalculateTriangulation();
        var verts = tri.vertices;
        var idx = tri.indices;

        List<int> ordered = CollectPathTriangles(agent.path, verts, idx);
        int n = ordered.Count;
        if (n == 0) return;

        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 1f : i / (float)(n - 1);
            Color col = Color.Lerp(START, Color.white, t);

            int k = ordered[i];
            Vector3 a = verts[idx[k]];
            Vector3 b = verts[idx[k + 1]];
            Vector3 c = verts[idx[k + 2]];

            DrawSolid(a, b, c, col);

            // number label
            Vector3 center = (a + b + c) * 0.333333f + LABEL_OFFSET;
            Handles.Label(center, (i + 1).ToString());
        }
    }

    //------------------------------------------------------------------

    List<int> CollectPathTriangles(NavMeshPath path, Vector3[] v, int[] idx)
    {
        var list = new List<int>(128);
        var seen = new HashSet<int>();
        Vector3[] c = path.corners;

        for (int seg = 0; seg < c.Length - 1; seg++)
        {
            float len = Vector3.Distance(c[seg], c[seg + 1]);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len / sampleStep));

            for (int s = 0; s <= steps; s++)
            {
                Vector3 p = Vector3.Lerp(c[seg], c[seg + 1], s / (float)steps);
                if (!NavMesh.SamplePosition(p, out var hit, 0.2f, NavMesh.AllAreas)) continue;

                int triStart = FindContainingTriangle(hit.position, v, idx);
                if (triStart < 0) triStart = ClosestTriStart(hit.position, v, idx);

                if (triStart >= 0 && seen.Add(triStart)) list.Add(triStart);
            }
        }
        return list;
    }

    static int FindContainingTriangle(Vector3 p, Vector3[] v, int[] idx)
    {
        Vector2 p2 = new(p.x, p.z);
        for (int k = 0; k < idx.Length; k += 3)
        {
            Vector2 a = new(v[idx[k]].x, v[idx[k]].z);
            Vector2 b = new(v[idx[k + 1]].x, v[idx[k + 1]].z);
            Vector2 c = new(v[idx[k + 2]].x, v[idx[k + 2]].z);
            if (PointInTri2D(p2, a, b, c)) return k;
        }
        return -1;
    }

    static bool PointInTri2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool neg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
        bool pos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
        return !(neg && pos);
    }

    static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) =>
        (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

    static int ClosestTriStart(Vector3 p, Vector3[] v, int[] idx)
    {
        float best = float.MaxValue;
        int bestStart = -1;
        for (int k = 0; k < idx.Length; k += 3)
        {
            Vector3 c = (v[idx[k]] + v[idx[k + 1]] + v[idx[k + 2]]) * 0.333333f;
            float d = (c - p).sqrMagnitude;
            if (d < best) { best = d; bestStart = k; }
        }
        return bestStart;
    }

    void DrawSolid(Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        _triMesh.Clear();
        _triMesh.vertices = new[] { a, b, c };
        _triMesh.triangles = new[] { 0, 1, 2 };
        _triMesh.RecalculateNormals();
        _mat.color = color;
        _mat.SetPass(0);
        Graphics.DrawMeshNow(_triMesh, Matrix4x4.identity);
    }
}
#endif
