using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ProceduralTreeRing : MonoBehaviour
{
    [Header("Plaza shape (must match ProceduralPlazaShape)")]
    [Min(1f)] public float width = 50f;
    [Min(1f)] public float height = 28f;
    [Min(0.1f)] public float cornerRadius = 6f;
    [Min(1)] public int segmentsPerCorner = 16;

    [Header("Distribution")]
    [Min(1f)] public float treeSpacing = 4f;
    [Tooltip("Distance from the perimeter outwards. Positive = outside the plaza.")]
    public float outwardOffset = 3f;
    [Tooltip("Random perpendicular jitter for natural look.")]
    [Min(0f)] public float jitter = 1.5f;
    [Range(0.5f, 1.5f)] public float scaleMin = 0.7f;
    [Range(0.5f, 2f)] public float scaleMax = 1.3f;
    public int randomSeed = 42;

    [Header("Tree prefab")]
    public GameObject treePrefab;

    [Header("Editor")]
    public bool regenerateOnValidate = true;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!regenerateOnValidate) return;
        EditorApplication.delayCall += () => { if (this != null) Generate(); };
    }
#endif

    private void OnEnable() => Generate();

    public void Generate()
    {
        ClearChildren();

        if (treePrefab == null)
        {
            return;
        }

        List<Vector3> perimeter = BuildPerimeter(width, height,
            Mathf.Min(cornerRadius, Mathf.Min(width, height) / 2f),
            Mathf.Max(1, segmentsPerCorner));

        float totalLength = 0f;
        for (int i = 0; i < perimeter.Count; i++)
        {
            totalLength += Vector3.Distance(perimeter[i], perimeter[(i + 1) % perimeter.Count]);
        }

        int numTrees = Mathf.Max(4, Mathf.RoundToInt(totalLength / treeSpacing));
        float actualSpacing = totalLength / numTrees;

        var rng = new System.Random(randomSeed);

        for (int i = 0; i < numTrees; i++)
        {
            float target = i * actualSpacing;
            (Vector3 pos, Vector3 tangent) = SampleAt(perimeter, target);

            Vector3 outwardNormal = new Vector3(tangent.z, 0f, -tangent.x).normalized;
            float jitterAmount = ((float)rng.NextDouble() * 2f - 1f) * jitter;
            Vector3 finalPos = pos + outwardNormal * (outwardOffset + jitterAmount);

            float scale = Mathf.Lerp(scaleMin, scaleMax, (float)rng.NextDouble());
            float rotY = (float)rng.NextDouble() * 360f;

            CreateTree(i, finalPos, scale, rotY);
        }
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child);
            else Destroy(child);
#else
            Destroy(child);
#endif
        }
    }

    private void CreateTree(int index, Vector3 pos, float scale, float rotY)
    {
        GameObject tree;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, transform);
        }
        else
        {
            tree = Instantiate(treePrefab, transform);
        }
#else
        tree = Instantiate(treePrefab, transform);
#endif
        tree.name = $"Tree_{index:D3}";
        tree.transform.localPosition = pos;
        tree.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        tree.transform.localScale = Vector3.one * scale;
    }

    private static (Vector3 pos, Vector3 tangent) SampleAt(List<Vector3> perimeter, float target)
    {
        float accumulated = 0f;
        for (int i = 0; i < perimeter.Count; i++)
        {
            Vector3 a = perimeter[i];
            Vector3 b = perimeter[(i + 1) % perimeter.Count];
            float segLen = Vector3.Distance(a, b);
            if (accumulated + segLen >= target)
            {
                float t = (target - accumulated) / segLen;
                Vector3 pos = Vector3.Lerp(a, b, t);
                Vector3 tangent = (b - a).normalized;
                return (pos, tangent);
            }
            accumulated += segLen;
        }
        return (perimeter[0], (perimeter[1] - perimeter[0]).normalized);
    }

    private static List<Vector3> BuildPerimeter(float w, float h, float r, int seg)
    {
        var points = new List<Vector3>();
        float halfW = w / 2f;
        float halfH = h / 2f;

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
