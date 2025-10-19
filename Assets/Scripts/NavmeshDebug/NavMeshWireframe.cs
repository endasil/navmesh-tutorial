#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[ExecuteAlways]
public sealed class NavMeshWireframe : MonoBehaviour
{
    private struct Edge
    {
        public readonly int A;
        public readonly int B;
        public Edge(int i, int j)
        {
            if (i < j) { A = i; B = j; } else { A = j; B = i; }
        }
        public override int GetHashCode() { unchecked { return (A * 397) ^ B; } }
        public override bool Equals(object obj) => obj is Edge e && e.A == A && e.B == B;
    }

    private sealed class EdgeInfo
    {
        public int Count;               // how many triangles reference this edge
        public int AreaA = -1;          // area id of first triangle
        public int AreaB = -1;          // area id of second triangle (if any)
    }

    private static void Accumulate(Dictionary<Edge, EdgeInfo> map, Edge e, int area)
    {
        if (!map.TryGetValue(e, out var info))
        {
            info = new EdgeInfo { Count = 1, AreaA = area };
            map[e] = info;
            return;
        }
        info.Count++;
        if (info.AreaB == -1 && area != info.AreaA) info.AreaB = area;
    }

    private void OnDrawGizmos()
    {
        var tri = NavMesh.CalculateTriangulation();
        var edges = new Dictionary<Edge, EdgeInfo>(tri.indices.Length);

        for (int i = 0; i < tri.indices.Length; i += 3)
        {
            int i0 = tri.indices[i];
            int i1 = tri.indices[i + 1];
            int i2 = tri.indices[i + 2];
            int area = tri.areas[i / 3];

            Accumulate(edges, new Edge(i0, i1), area);
            Accumulate(edges, new Edge(i1, i2), area);
            Accumulate(edges, new Edge(i2, i0), area);
        }

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        Handles.color = new Color(0f, 0f, 0f, 1f);

        foreach (var kv in edges)
        {
            var info = kv.Value;

            bool isOuterBorder = info.Count == 1;
            bool isAreaBoundary = info.Count >= 2 && info.AreaB != -1; // adjacent triangles have different area ids

            if (!(isOuterBorder || isAreaBoundary)) continue;

            var e = kv.Key;
            Vector3 a = tri.vertices[e.A];
            Vector3 b = tri.vertices[e.B];
            Handles.DrawAAPolyLine(4f, a, b);
        }
    }
}
#endif
