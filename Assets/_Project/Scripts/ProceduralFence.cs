using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ProceduralFence : MonoBehaviour
{
    [Header("Plaza shape (must match ProceduralPlazaShape)")]
    [Min(1f)] public float width = 50f;
    [Min(1f)] public float height = 28f;
    [Min(0.1f)] public float cornerRadius = 6f;
    [Min(1)] public int segmentsPerCorner = 16;

    [Header("Fence")]
    [Min(0.5f)] public float postSpacing = 2.5f;
    [Min(0.05f)] public float postRadius = 0.06f;
    [Min(0.3f)] public float postHeight = 1.0f;
    [Min(0.02f)] public float railRadius = 0.04f;
    [Range(1, 3)] public int railCount = 2;
    public Material material;

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

        List<Vector3> perimeter = BuildPerimeter(width, height,
            Mathf.Min(cornerRadius, Mathf.Min(width, height) / 2f),
            Mathf.Max(1, segmentsPerCorner));

        float totalLength = 0f;
        for (int i = 0; i < perimeter.Count; i++)
        {
            totalLength += Vector3.Distance(perimeter[i], perimeter[(i + 1) % perimeter.Count]);
        }

        int numPosts = Mathf.Max(4, Mathf.RoundToInt(totalLength / postSpacing));
        float actualSpacing = totalLength / numPosts;

        List<Vector3> postPositions = SampleEvenly(perimeter, numPosts, actualSpacing);

        for (int i = 0; i < postPositions.Count; i++)
        {
            CreatePost($"Post_{i:D3}", postPositions[i]);
        }

        for (int i = 0; i < postPositions.Count; i++)
        {
            Vector3 a = postPositions[i];
            Vector3 b = postPositions[(i + 1) % postPositions.Count];
            for (int r = 0; r < railCount; r++)
            {
                float t = railCount == 1 ? 0.5f : (r + 1) / (float)(railCount + 1);
                CreateRail($"Rail_{i:D3}_{r}", a, b, t * postHeight);
            }
        }

        // Generated geometry is preview/runtime-only: keep it out of the saved
        // scene so the .unity stays small and deterministic. OnEnable regenerates
        // it in every context (editor preview, play, build), so the serialized
        // copies were dead weight that churned the scene on each save.
        // Use DontSaveInEditor|DontSaveInBuild (not bare DontSave): consistent with
        // the other procedural generators. These are child GameObjects (built-in
        // primitives), not generated Mesh assets, so there's no asset leak here —
        // but keeping the same flags avoids copy-pasting bare DontSave into a script
        // that DOES create meshes (where DontSave's DontUnloadUnusedAsset leaks them).
        foreach (Transform child in transform)
            child.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
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

    private void CreatePost(string name, Vector3 basePos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = basePos + Vector3.up * (postHeight / 2f);
        go.transform.localScale = new Vector3(postRadius * 2f, postHeight / 2f, postRadius * 2f);
        var col = go.GetComponent<Collider>();
        if (col != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(col);
            else Destroy(col);
#else
            Destroy(col);
#endif
        }
        ApplyMaterial(go);
    }

    private void CreateRail(string name, Vector3 a, Vector3 b, float yOffset)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(transform, false);
        Vector3 mid = (a + b) / 2f + Vector3.up * yOffset;
        go.transform.localPosition = mid;
        Vector3 dir = b - a;
        float len = dir.magnitude;
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        go.transform.localScale = new Vector3(railRadius * 2f, len / 2f, railRadius * 2f);
        var col = go.GetComponent<Collider>();
        if (col != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(col);
            else Destroy(col);
#else
            Destroy(col);
#endif
        }
        ApplyMaterial(go);
    }

    private void ApplyMaterial(GameObject go)
    {
        if (material == null) return;
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    private static List<Vector3> SampleEvenly(List<Vector3> perimeter, int count, float spacing)
    {
        var result = new List<Vector3>(count);
        if (perimeter.Count < 2 || count == 0) return result;

        int currentIdx = 0;
        Vector3 currentStart = perimeter[0];
        Vector3 currentEnd = perimeter[1];
        float accumulated = 0f;

        for (int i = 0; i < count; i++)
        {
            float target = i * spacing;
            float segLen;
            while (accumulated + (segLen = Vector3.Distance(currentStart, currentEnd)) < target)
            {
                accumulated += segLen;
                currentIdx = (currentIdx + 1) % perimeter.Count;
                currentStart = perimeter[currentIdx];
                currentEnd = perimeter[(currentIdx + 1) % perimeter.Count];
            }
            float remaining = target - accumulated;
            float t = remaining / Vector3.Distance(currentStart, currentEnd);
            result.Add(Vector3.Lerp(currentStart, currentEnd, t));
        }

        return result;
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
