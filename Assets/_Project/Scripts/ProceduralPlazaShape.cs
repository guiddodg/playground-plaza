using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class ProceduralPlazaShape : MonoBehaviour
{
    [Header("Dimensions (meters)")]
    [Min(1f)] public float width = 50f;
    [Min(1f)] public float height = 28f;
    [Min(0.1f)] public float cornerRadius = 6f;
    [Min(1)] public int segmentsPerCorner = 16;

    [Header("Editor")]
    public bool regenerateOnValidate = true;

    private Mesh mesh;

    private void OnEnable() => Generate();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!regenerateOnValidate) return;
        EditorApplication.delayCall += () => { if (this != null) Generate(); };
    }
#endif

    public void Generate()
    {
        var meshFilter = GetComponent<MeshFilter>();
        if (mesh == null)
        {
            mesh = new Mesh { name = "PlazaShape (procedural)" };
            // Procedural mesh: never serialize it into the scene. OnEnable rebuilds
            // it in every context, so a saved copy only churned the .unity.
            mesh.hideFlags = HideFlags.DontSave;
        }
        else
        {
            mesh.Clear();
        }

        float w = width;
        float h = height;
        float r = Mathf.Min(cornerRadius, Mathf.Min(w, h) / 2f);
        int seg = Mathf.Max(1, segmentsPerCorner);

        List<Vector3> perimeter = BuildPerimeter(w, h, r, seg);

        // Vertex 0 = center, then the perimeter
        var vertices = new List<Vector3>(perimeter.Count + 1) { Vector3.zero };
        vertices.AddRange(perimeter);

        // Fan triangulation from the center
        var triangles = new List<int>(perimeter.Count * 3);
        for (int i = 1; i < vertices.Count; i++)
        {
            int next = i == vertices.Count - 1 ? 1 : i + 1;
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(next);
        }

        // Planar UVs projected from XZ to [0,1]
        var uvs = new List<Vector2>(vertices.Count);
        float halfW = w / 2f;
        float halfH = h / 2f;
        foreach (var v in vertices)
        {
            uvs.Add(new Vector2((v.x + halfW) / w, (v.z + halfH) / h));
        }

        var normals = new Vector3[vertices.Count];
        for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
    }

    private static List<Vector3> BuildPerimeter(float w, float h, float r, int seg)
    {
        var points = new List<Vector3>();
        float halfW = w / 2f;
        float halfH = h / 2f;

        // Clockwise from top-left of the top straight edge
        points.Add(new Vector3(-halfW + r, 0f, halfH));
        points.Add(new Vector3(halfW - r, 0f, halfH));

        AddArc(points, new Vector3(halfW - r, 0f, halfH - r), r, 90f, 0f, seg);
        points.Add(new Vector3(halfW, 0f, -halfH + r));

        AddArc(points, new Vector3(halfW - r, 0f, -halfH + r), r, 0f, -90f, seg);
        points.Add(new Vector3(-halfW + r, 0f, -halfH));

        AddArc(points, new Vector3(-halfW + r, 0f, -halfH + r), r, -90f, -180f, seg);
        points.Add(new Vector3(-halfW, 0f, halfH - r));

        AddArc(points, new Vector3(-halfW + r, 0f, halfH - r), r, 180f, 90f, seg);
        return points;
    }

    private static void AddArc(List<Vector3> points, Vector3 center, float radius, float startDeg, float endDeg, int segments)
    {
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(startDeg, endDeg, t) * Mathf.Deg2Rad;
            points.Add(center + new Vector3(radius * Mathf.Cos(angle), 0f, radius * Mathf.Sin(angle)));
        }
    }
}
