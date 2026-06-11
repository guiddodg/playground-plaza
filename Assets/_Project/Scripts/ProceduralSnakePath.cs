using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class ProceduralSnakePath : MonoBehaviour
{
    [Header("Waypoints (path goes through these in order)")]
    public Vector3[] waypoints =
    {
        new Vector3(-20f, 0f, 0f),
        new Vector3(-10f, 0f, 4f),
        new Vector3(0f, 0f, -3f),
        new Vector3(10f, 0f, 3f),
        new Vector3(20f, 0f, 0f),
    };

    [Header("Path properties")]
    [Min(0.2f)] public float pathWidth = 2.5f;
    [Min(2)] public int subdivisionsPerSegment = 12;

    [Header("Editor")]
    public bool regenerateOnValidate = true;
    [Tooltip("Draw the smoothed curve in the Scene view as a yellow line.")]
    public bool drawDebugGizmo = true;

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
        if (waypoints == null || waypoints.Length < 2) return;

        var meshFilter = GetComponent<MeshFilter>();
        if (mesh == null)
        {
            mesh = new Mesh { name = "SnakePath (procedural)" };
            // Procedural mesh: never serialize it into the scene. OnEnable rebuilds
            // it in every context, so a saved copy only churned the .unity.
            mesh.hideFlags = HideFlags.DontSave;
        }
        else mesh.Clear();

        List<Vector3> centerLine = BuildCatmullRomPath(waypoints, subdivisionsPerSegment);

        var vertices = new List<Vector3>(centerLine.Count * 2);
        var triangles = new List<int>((centerLine.Count - 1) * 6);
        var uvs = new List<Vector2>(centerLine.Count * 2);
        var normals = new List<Vector3>(centerLine.Count * 2);

        float halfW = pathWidth / 2f;
        for (int i = 0; i < centerLine.Count; i++)
        {
            Vector3 forward;
            if (i == 0) forward = (centerLine[1] - centerLine[0]).normalized;
            else if (i == centerLine.Count - 1) forward = (centerLine[i] - centerLine[i - 1]).normalized;
            else forward = (centerLine[i + 1] - centerLine[i - 1]).normalized;

            Vector3 right = new Vector3(forward.z, 0f, -forward.x).normalized;
            Vector3 left = centerLine[i] - right * halfW;
            Vector3 rightP = centerLine[i] + right * halfW;
            vertices.Add(left);
            vertices.Add(rightP);

            float v = i / (float)(centerLine.Count - 1);
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));

            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
        }

        for (int i = 0; i < centerLine.Count - 1; i++)
        {
            int a = i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;

            triangles.Add(a); triangles.Add(c); triangles.Add(b);
            triangles.Add(b); triangles.Add(c); triangles.Add(d);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
    }

    private static List<Vector3> BuildCatmullRomPath(Vector3[] points, int subdivisions)
    {
        var result = new List<Vector3>();
        int n = points.Length;
        for (int i = 0; i < n - 1; i++)
        {
            Vector3 p0 = i == 0 ? points[0] : points[i - 1];
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];
            Vector3 p3 = i + 2 < n ? points[i + 2] : points[n - 1];

            int steps = i == n - 2 ? subdivisions + 1 : subdivisions;
            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)subdivisions;
                result.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }
        return result;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmo || waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.yellow;
        var path = BuildCatmullRomPath(waypoints, subdivisionsPerSegment);
        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(transform.TransformPoint(path[i]),
                            transform.TransformPoint(path[i + 1]));
        }
        Gizmos.color = Color.red;
        foreach (var w in waypoints)
        {
            Gizmos.DrawSphere(transform.TransformPoint(w), 0.4f);
        }
    }
#endif
}
