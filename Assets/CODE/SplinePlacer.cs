using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/// <summary>
/// Universal spline-based object placement tool.
/// 
/// Modes:
/// - Repeat: Places prefab modules end-to-end along the spline (pipes, fences, rails).
///   Each module rotates to follow spline. Configurable forward/up axes.
///   
/// - Deform: Takes a single mesh and bends it along the entire spline.
///   Vertices are repositioned to follow the curve. Works great with ProBuilder meshes.
///   
/// - Scatter: Places objects along the spline at intervals with randomization.
/// 
/// Editor-only: Use context menu "Generate" to build, results saved in prefab.
/// </summary>
[ExecuteInEditMode]
public class SplinePlacer : MonoBehaviour
{
    public enum PlacementMode
    {
        Repeat,  // Modules end-to-end (pipes, walls)
        Deform,  // Bend single mesh along spline (ProBuilder friendly)
        Scatter  // Objects at intervals (decor, lamps)
    }

    public enum MeshAxis
    {
        X,
        Y,
        Z,
        NegX,
        NegY,
        NegZ
    }

    // =================================================================
    // SETTINGS
    // =================================================================

    [Header("Spline")]
    [Tooltip("SplineContainer to follow. If empty, looks on this GameObject.")]
    public SplineContainer splineContainer;

    [Header("Mode")]
    public PlacementMode mode = PlacementMode.Repeat;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Orientation")]
    [Tooltip("Which axis of the prefab points along the spline direction")]
    public MeshAxis forwardAxis = MeshAxis.Z;
    [Tooltip("Which axis of the prefab points 'up' relative to the spline")]
    public MeshAxis upAxis = MeshAxis.Y;

    // --- Repeat Mode ---
    [Header("Repeat Settings")]
    [Tooltip("Length of one module along its forward axis. 0 = auto-detect from mesh bounds.")]
    public float moduleLength = 0f;
    [Tooltip("Gap between modules (can be negative for overlap)")]
    public float moduleGap = 0f;
    [Tooltip("Offset from spline in local space (perpendicular to spline)")]
    public Vector3 repeatOffset = Vector3.zero;
    [Tooltip("Extra rotation applied to each module (local)")]
    public Vector3 repeatRotationOffset = Vector3.zero;
    [Tooltip("Scale applied to each module")]
    public Vector3 repeatScale = Vector3.one;

    // --- Deform Mode ---
    [Header("Deform Settings")]
    [Tooltip("Number of subdivisions along the spline for deformed mesh")]
    public int deformSegments = 20;
    [Tooltip("Scale of the cross-section")]
    public float deformScale = 1f;
    [Tooltip("Offset from spline center")]
    public Vector3 deformOffset = Vector3.zero;

    // --- Scatter Mode ---
    [Header("Scatter Settings")]
    [Tooltip("If set, one of these is picked randomly per instance instead of using the single 'Prefab' field above.")]
    public GameObject[] scatterPrefabs;
    [Tooltip("Distance between scattered objects")]
    public float scatterSpacing = 5f;
    [Tooltip("Random offset perpendicular to spline")]
    public float scatterRandomOffset = 0f;
    [Tooltip("Random height variation")]
    public float scatterRandomHeight = 0f;
    [Tooltip("Align object rotation to spline direction")]
    public bool scatterAlignToSpline = true;
    [Tooltip("Only used when Align To Spline is on. Ignores the spline's local roll/pitch (upVector) — " +
             "objects always stand straight up (world Up), only their yaw follows the spline direction. " +
             "Use for terrain-following splines (e.g. along a mountain) that tilt unpredictably.")]
    public bool scatterKeepVertical = false;
    [Tooltip("Random Y rotation added on top of alignment")]
    public float scatterRandomYRotation = 0f;
    [Tooltip("Min/Max random uniform scale")]
    public Vector2 scatterScaleRange = new Vector2(1f, 1f);
    [Tooltip("Fixed offset from spline")]
    public Vector3 scatterOffset = Vector3.zero;
    [Tooltip("Random seed (0 = random each time)")]
    public int scatterSeed = 0;

    // --- General ---
    [Header("Generated")]
    public Transform generatedContainer;

    [Header("Asset Storage")]
    [Tooltip("Folder inside Assets/ for saved meshes. Auto-populated from room name.")]
    public string meshSavePath = "";

    // Track saved mesh assets for cleanup
    [SerializeField, HideInInspector]
    private List<string> savedMeshPaths = new List<string>();

    // =================================================================
    // CONTEXT MENU
    // =================================================================

    [ContextMenu("Generate")]
    public void Generate()
    {
        ClearGenerated();
        EnsureSplineContainer();

        bool hasScatterArray = mode == PlacementMode.Scatter && scatterPrefabs != null && scatterPrefabs.Length > 0;

        if (splineContainer == null || (prefab == null && !hasScatterArray))
        {
            Debug.LogWarning("[SplinePlacer] Missing SplineContainer or Prefab (or scatterPrefabs for Scatter mode)!");
            return;
        }

        CreateContainer();
        EnsureSavePath();

        switch (mode)
        {
            case PlacementMode.Repeat:
                GenerateRepeat();
                break;
            case PlacementMode.Deform:
                GenerateDeform();
                break;
            case PlacementMode.Scatter:
                GenerateScatter();
                break;
        }

        Debug.Log($"[SplinePlacer] Generated {generatedContainer.childCount} objects ({mode}), meshes saved to {meshSavePath}");
    }

    [ContextMenu("Clear")]
    public void ClearGenerated()
    {
        // Delete saved mesh assets
#if UNITY_EDITOR
        foreach (string path in savedMeshPaths)
        {
            if (!string.IsNullOrEmpty(path) && UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
            {
                UnityEditor.AssetDatabase.DeleteAsset(path);
            }
        }
        if (savedMeshPaths.Count > 0)
            UnityEditor.AssetDatabase.Refresh();
#endif
        savedMeshPaths.Clear();

        if (generatedContainer != null)
            DestroyImmediate(generatedContainer.gameObject);
        generatedContainer = null;
    }

    [ContextMenu("Regenerate")]
    public void Regenerate() => Generate();

    // =================================================================
    // ASSET SAVING
    // =================================================================

    /// <summary>
    /// Build save path from room hierarchy:
    /// Assets/GeneratedMeshes/{RoomName}/{SplinePlacerGameObjectName}/
    /// </summary>
    void EnsureSavePath()
    {
        if (!string.IsNullOrEmpty(meshSavePath)) return;

        string roomName = FindRoomName();
        string placerName = SanitizeFileName(gameObject.name);

        meshSavePath = $"Assets/GeneratedMeshes/{roomName}/{placerName}";
    }

    string FindRoomName()
    {
            return "Justkek";
    }

    string SanitizeFileName(string name)
    {
        char[] invalid = System.IO.Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
            name = name.Replace(c, '_');
        return name.Replace(' ', '_');
    }

    /// <summary>
    /// Save a deformed mesh as an asset file. Returns the saved mesh (now an asset reference).
    /// </summary>
    Mesh SaveMeshAsset(Mesh mesh, int index)
    {
#if UNITY_EDITOR
        // Ensure directory exists
        string fullDir = meshSavePath;
        string[] parts = fullDir.Split('/');
        string buildPath = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = buildPath + "/" + parts[i];
            if (!UnityEditor.AssetDatabase.IsValidFolder(next))
            {
                UnityEditor.AssetDatabase.CreateFolder(buildPath, parts[i]);
            }
            buildPath = next;
        }

        string assetName = $"{SanitizeFileName(prefab.name)}_{mode}_{index}.asset";
        string assetPath = $"{meshSavePath}/{assetName}";

        // Delete existing asset at this path
        if (UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(assetPath) != null)
        {
            UnityEditor.AssetDatabase.DeleteAsset(assetPath);
        }

        mesh.name = $"{prefab.name}_{mode}_{index}";
        UnityEditor.AssetDatabase.CreateAsset(mesh, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();

        savedMeshPaths.Add(assetPath);

        // Return the asset reference (not the in-memory copy)
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
#else
        return mesh;
#endif
    }

    // =================================================================
    // AXIS HELPERS
    // =================================================================

    /// <summary>
    /// Get the rotation that maps the chosen forward/up axes to world Z-forward/Y-up.
    /// This corrects for meshes whose "forward" is along X or Y instead of Z.
    /// </summary>
    Quaternion GetAxisCorrectionRotation()
    {
        Vector3 fwd = AxisToVector(forwardAxis);
        Vector3 up = AxisToVector(upAxis);

        // Safety: if forward == up, fix it
        if (Vector3.Dot(fwd, up) > 0.99f)
            up = Vector3.up;

        // This rotation maps: fwd -> Vector3.forward, up -> Vector3.up
        // We need the inverse: Vector3.forward -> fwd
        // So the correction is: Inverse(LookRotation(fwd, up)) — 
        // this rotates the mesh so its fwd axis aligns with spline tangent
        return Quaternion.Inverse(Quaternion.LookRotation(fwd, up));
    }

    Vector3 AxisToVector(MeshAxis axis)
    {
        return axis switch
        {
            MeshAxis.X => Vector3.right,
            MeshAxis.Y => Vector3.up,
            MeshAxis.Z => Vector3.forward,
            MeshAxis.NegX => Vector3.left,
            MeshAxis.NegY => Vector3.down,
            MeshAxis.NegZ => Vector3.back,
            _ => Vector3.forward
        };
    }

    /// <summary>
    /// Get the size of the prefab along the chosen forward axis.
    /// </summary>
    float GetSizeAlongForwardAxis()
    {
        if (prefab == null) return 1f;

        Mesh mesh = GetMeshFromObject(prefab);
        if (mesh != null)
        {
            Vector3 size = mesh.bounds.size;
            Vector3 scale = prefab.transform.localScale;

            return forwardAxis switch
            {
                MeshAxis.X or MeshAxis.NegX => size.x * scale.x,
                MeshAxis.Y or MeshAxis.NegY => size.y * scale.y,
                MeshAxis.Z or MeshAxis.NegZ => size.z * scale.z,
                _ => size.z * scale.z
            };
        }

        return 1f;
    }

    /// <summary>
    /// Get the component of a Vector3 along the chosen forward axis.
    /// Used for deform mode to know how far along the mesh a vertex is.
    /// </summary>
    float GetComponentAlongAxis(Vector3 v, MeshAxis axis)
    {
        return axis switch
        {
            MeshAxis.X => v.x,
            MeshAxis.Y => v.y,
            MeshAxis.Z => v.z,
            MeshAxis.NegX => -v.x,
            MeshAxis.NegY => -v.y,
            MeshAxis.NegZ => -v.z,
            _ => v.z
        };
    }

    // =================================================================
    // REPEAT MODE (with per-module deformation)
    // =================================================================

    void GenerateRepeat()
    {
        Mesh sourceMesh = GetMeshFromObject(prefab);
        if (sourceMesh == null)
        {
            Debug.LogWarning("[SplinePlacer] Repeat mode requires a prefab with a mesh!");
            return;
        }

        Spline spline = splineContainer.Spline;
        float splineLength = spline.GetLength();

        float effectiveLength = moduleLength > 0f ? moduleLength : GetSizeAlongForwardAxis();
        if (effectiveLength <= 0.001f)
        {
            Debug.LogWarning("[SplinePlacer] Module length is 0!");
            return;
        }

        float step = effectiveLength + moduleGap;
        float distance = 0f;
        int index = 0;

        while (distance + effectiveLength <= splineLength + 0.01f)
        {
            // This module covers spline from distance to distance+effectiveLength
            float tStart = distance / splineLength;
            float tEnd = (distance + effectiveLength) / splineLength;
            tStart = Mathf.Clamp01(tStart);
            tEnd = Mathf.Clamp01(tEnd);

            // Deform mesh for this segment
            Mesh deformedMesh = DeformMeshAlongSpline(
                sourceMesh, spline, splineLength,
                tStart, tEnd, repeatScale, repeatOffset
            );

            if (deformedMesh != null)
            {
                // Save mesh as asset
                Mesh savedMesh = SaveMeshAsset(deformedMesh, index);

                GameObject obj = new GameObject($"{prefab.name}_{index}");
                obj.transform.SetParent(generatedContainer);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;

                MeshFilter mf = obj.AddComponent<MeshFilter>();
                mf.sharedMesh = savedMesh;

                MeshRenderer sourceRenderer = prefab.GetComponentInChildren<MeshRenderer>();
                if (sourceRenderer != null)
                {
                    MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = sourceRenderer.sharedMaterials;
                }
            }

            distance += step;
            index++;
        }
    }

    // =================================================================
    // DEFORM MODE (single mesh along entire spline)
    // =================================================================

    void GenerateDeform()
    {
        Mesh sourceMesh = GetMeshFromObject(prefab);
        if (sourceMesh == null)
        {
            Debug.LogWarning("[SplinePlacer] Deform mode requires a prefab with a mesh!");
            return;
        }

        Spline spline = splineContainer.Spline;
        float splineLength = spline.GetLength();

        Mesh deformedMesh = DeformMeshAlongSpline(
            sourceMesh, spline, splineLength,
            0f, 1f, Vector3.one * deformScale, deformOffset
        );

        if (deformedMesh == null) return;

        // Save mesh as asset
        Mesh savedMesh = SaveMeshAsset(deformedMesh, 0);

        GameObject deformObj = new GameObject($"{prefab.name}_Deformed");
        deformObj.transform.SetParent(generatedContainer);
        deformObj.transform.localPosition = Vector3.zero;
        deformObj.transform.localRotation = Quaternion.identity;
        deformObj.transform.localScale = Vector3.one;

        MeshFilter mf = deformObj.AddComponent<MeshFilter>();
        mf.sharedMesh = savedMesh;

        MeshRenderer sourceRenderer = prefab.GetComponentInChildren<MeshRenderer>();
        if (sourceRenderer != null)
        {
            MeshRenderer mr = deformObj.AddComponent<MeshRenderer>();
            mr.sharedMaterials = sourceRenderer.sharedMaterials;
        }

        MeshCollider sourceCollider = prefab.GetComponentInChildren<MeshCollider>();
        if (sourceCollider != null)
        {
            MeshCollider mc = deformObj.AddComponent<MeshCollider>();
            mc.sharedMesh = savedMesh;
        }
    }

    // =================================================================
    // SHARED DEFORMATION
    // =================================================================

    /// <summary>
    /// Deform a mesh to follow a segment of the spline.
    /// tStart/tEnd define which portion of the spline this mesh covers (0..1).
    /// Each vertex's position along the forward axis maps to a spline parameter,
    /// while the perpendicular cross-section is preserved and rotated to follow the curve.
    /// </summary>
    Mesh DeformMeshAlongSpline(Mesh sourceMesh, Spline spline, float splineLength,
        float tStart, float tEnd, Vector3 scale, Vector3 offset)
    {
        Vector3[] sourceVerts = sourceMesh.vertices;
        Vector3[] sourceNormals = sourceMesh.normals;

        // Find bounds along forward axis
        float minForward = float.MaxValue;
        float maxForward = float.MinValue;

        Vector3 prefabScale = prefab.transform.localScale;

        for (int i = 0; i < sourceVerts.Length; i++)
        {
            Vector3 scaledVert = Vector3.Scale(sourceVerts[i], prefabScale);
            float f = GetComponentAlongAxis(scaledVert, forwardAxis);
            if (f < minForward) minForward = f;
            if (f > maxForward) maxForward = f;
        }

        float meshLength = maxForward - minForward;
        if (meshLength < 0.001f)
        {
            Debug.LogWarning("[SplinePlacer] Mesh has no extent along forward axis!");
            return null;
        }

        Mesh deformedMesh = Instantiate(sourceMesh);
        deformedMesh.name = "DeformedMesh";

        Vector3[] newVerts = new Vector3[sourceVerts.Length];
        Vector3[] newNormals = new Vector3[sourceNormals.Length];

        Quaternion axisCorrection = GetAxisCorrectionRotation();

        for (int i = 0; i < sourceVerts.Length; i++)
        {
            Vector3 scaledVert = Vector3.Scale(sourceVerts[i], prefabScale);

            // How far along the forward axis (0..1 within this module)
            float forwardPos = GetComponentAlongAxis(scaledVert, forwardAxis);
            float localT = (forwardPos - minForward) / meshLength;
            localT = Mathf.Clamp01(localT);

            // Map to spline segment
            float splineT = Mathf.Lerp(tStart, tEnd, localT);
            splineT = Mathf.Clamp01(splineT);

            // Get cross-section (perpendicular to forward axis)
            Vector3 corrected = axisCorrection * scaledVert;
            Vector2 crossSection = new Vector2(corrected.x * scale.x, corrected.y * scale.y);

            // Sample spline
            SampleSplineWorld(spline, splineT, out Vector3 splinePos, out Quaternion splineRot);

            // Position vertex
            Vector3 localOffset = new Vector3(crossSection.x, crossSection.y, 0f) + offset;
            newVerts[i] = transform.InverseTransformPoint(splinePos + splineRot * localOffset);

            // Transform normals
            if (i < sourceNormals.Length)
            {
                Vector3 correctedNormal = axisCorrection * sourceNormals[i];
                newNormals[i] = transform.InverseTransformDirection(splineRot * correctedNormal);
            }
        }

        deformedMesh.vertices = newVerts;
        if (newNormals.Length == newVerts.Length)
            deformedMesh.normals = newNormals;
        deformedMesh.RecalculateBounds();

        return deformedMesh;
    }

    // =================================================================
    // SCATTER MODE
    // =================================================================

    /// <summary>
    /// Picks a random prefab from scatterPrefabs if it's populated, otherwise falls back to the single prefab field.
    /// Null entries in scatterPrefabs are skipped (re-rolled) so a stray empty slot doesn't produce empty instances.
    /// </summary>
    GameObject GetScatterPrefab()
    {
        if (scatterPrefabs == null || scatterPrefabs.Length == 0)
            return prefab;

        // Try a few times in case of null slots, then give up rather than looping forever
        for (int attempt = 0; attempt < scatterPrefabs.Length; attempt++)
        {
            GameObject candidate = scatterPrefabs[Random.Range(0, scatterPrefabs.Length)];
            if (candidate != null) return candidate;
        }

        return prefab;
    }

    /// <summary>
    /// Builds a rotation from the spline tangent projected onto the horizontal plane, with world Up as up vector.
    /// Ignores the spline's own upVector/roll entirely — used for scatterKeepVertical on terrain-following splines.
    /// </summary>
    Quaternion GetVerticalSplineRotation(Spline spline, float t)
    {
        SplineUtility.Evaluate(spline, t, out float3 localPos, out float3 tangent, out float3 upVector);
        Vector3 worldTangent = splineContainer.transform.TransformDirection(math.normalize(tangent));

        Vector3 flatTangent = Vector3.ProjectOnPlane(worldTangent, Vector3.up);
        if (flatTangent.sqrMagnitude < 0.0001f)
            flatTangent = Vector3.forward; // near-vertical spline segment — not expected, just avoiding NaN

        return Quaternion.LookRotation(flatTangent.normalized, Vector3.up);
    }

    void GenerateScatter()
    {
        Spline spline = splineContainer.Spline;
        float splineLength = spline.GetLength();

        if (scatterSpacing <= 0.01f)
        {
            Debug.LogWarning("[SplinePlacer] Scatter spacing too small!");
            return;
        }

        Random.State prevState = Random.state;
        if (scatterSeed != 0)
            Random.InitState(scatterSeed);
        else
            Random.InitState(System.Environment.TickCount);

        Quaternion axisCorrection = GetAxisCorrectionRotation();
        float distance = 0f;
        int index = 0;

        while (distance <= splineLength)
        {
            float t = distance / splineLength;
            t = Mathf.Clamp01(t);

            SampleSplineWorld(spline, t, out Vector3 worldPos, out Quaternion splineRot);

            if (scatterAlignToSpline && scatterKeepVertical)
                splineRot = GetVerticalSplineRotation(spline, t);

            // Base rotation
            Quaternion baseRot = scatterAlignToSpline
                ? splineRot * axisCorrection
                : Quaternion.identity;

            // Random offset perpendicular to spline
            Vector3 right = splineRot * Vector3.right;
            float lateralOffset = Random.Range(-scatterRandomOffset, scatterRandomOffset);
            float heightOffset = Random.Range(-scatterRandomHeight, scatterRandomHeight);

            Vector3 finalPos = worldPos
                + right * lateralOffset
                + Vector3.up * heightOffset
                + splineRot * scatterOffset;

            // Random rotation
            float randomY = Random.Range(-scatterRandomYRotation, scatterRandomYRotation);
            Quaternion finalRot = baseRot * Quaternion.Euler(0f, randomY, 0f);

            // Random scale
            float scale = Random.Range(scatterScaleRange.x, scatterScaleRange.y);

            GameObject sourcePrefab = GetScatterPrefab();
            if (sourcePrefab == null)
            {
                index++;
                distance += scatterSpacing;
                continue;
            }

            GameObject obj = InstantiatePrefab(sourcePrefab, generatedContainer);
            obj.transform.position = finalPos;
            obj.transform.rotation = finalRot;
            obj.transform.localScale = Vector3.one * scale;
            obj.name = $"{sourcePrefab.name}_{index}";

            distance += scatterSpacing;
            index++;
        }

        Random.state = prevState;
    }

    // =================================================================
    // SPLINE SAMPLING
    // =================================================================

    /// <summary>
    /// Sample the spline at parameter t, returning world position and rotation.
    /// Rotation has Z-forward along tangent, Y-up along spline up vector.
    /// </summary>
    void SampleSplineWorld(Spline spline, float t, out Vector3 worldPos, out Quaternion worldRot)
    {
        SplineUtility.Evaluate(spline, t, out float3 localPos, out float3 tangent, out float3 upVector);

        worldPos = splineContainer.transform.TransformPoint(localPos);

        Vector3 worldTangent = splineContainer.transform.TransformDirection(math.normalize(tangent));
        Vector3 worldUp = splineContainer.transform.TransformDirection(math.normalize(upVector));

        if (math.lengthsq(tangent) > 0.0001f)
            worldRot = Quaternion.LookRotation(worldTangent, worldUp);
        else
            worldRot = splineContainer.transform.rotation;
    }

    // =================================================================
    // HELPERS
    // =================================================================

    void EnsureSplineContainer()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();
    }

    void CreateContainer()
    {
        GameObject container = new GameObject($"SplinePlacer_Generated");
        container.transform.SetParent(transform);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;
        generatedContainer = container.transform;
    }

    GameObject InstantiatePrefab(GameObject source, Transform parent)
    {
#if UNITY_EDITOR
        GameObject obj = UnityEditor.PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
        if (obj != null) return obj;
#endif
        return Instantiate(source, parent);
    }

    /// <summary>
    /// Get mesh from prefab — supports both regular MeshFilter and ProBuilder meshes.
    /// ProBuilder stores mesh data in its own component; MeshFilter.sharedMesh may be null.
    /// </summary>
    Mesh GetMeshFromObject(GameObject obj)
    {
        if (obj == null) return null;

        // Try regular MeshFilter first
        MeshFilter mf = obj.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            return mf.sharedMesh;

        // Try ProBuilder — it exposes mesh via MeshFilter but may need a poke
        // ProBuilderMesh lives in UnityEngine.ProBuilder namespace
        // We access it generically to avoid hard dependency
        var pbMesh = obj.GetComponentInChildren(System.Type.GetType("UnityEngine.ProBuilder.ProBuilderMesh, Unity.ProBuilder"));
        if (pbMesh != null)
        {
            // ProBuilder populates MeshFilter on Awake/Refresh — try getting it after
            mf = pbMesh.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                return mf.sharedMesh;

            // Last resort: instantiate temporarily to force ProBuilder to build the mesh
            GameObject temp = Instantiate(obj, Vector3.one * 99999f, Quaternion.identity);
            temp.hideFlags = HideFlags.HideAndDontSave;

            MeshFilter tempMF = temp.GetComponentInChildren<MeshFilter>();
            Mesh result = null;
            if (tempMF != null && tempMF.sharedMesh != null)
                result = tempMF.sharedMesh;

            // Also try getting mesh via Renderer
            if (result == null)
            {
                MeshRenderer mr = temp.GetComponentInChildren<MeshRenderer>();
                if (mr != null)
                {
                    tempMF = mr.GetComponent<MeshFilter>();
                    if (tempMF != null && tempMF.sharedMesh != null)
                        result = tempMF.sharedMesh;
                }
            }

            if (result != null)
            {
                // Clone mesh so we keep it after destroying temp
                result = Instantiate(result);
            }

            DestroyImmediate(temp);
            return result;
        }

        // Try Renderer bounds as absolute fallback
        Renderer rend = obj.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            mf = rend.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                return mf.sharedMesh;
        }

        return null;
    }

    // =================================================================
    // GIZMOS
    // =================================================================

    void OnDrawGizmosSelected()
    {
        EnsureSplineContainer();
        if (splineContainer == null) return;

        Spline spline = splineContainer.Spline;
        if (spline == null) return;

        float splineLength = spline.GetLength();
        if (splineLength < 0.01f) return;

        float spacing;
        switch (mode)
        {
            case PlacementMode.Repeat:
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.6f);
                float modLen = moduleLength > 0f ? moduleLength : GetSizeAlongForwardAxis();
                spacing = modLen + moduleGap;
                break;
            case PlacementMode.Deform:
                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.6f);
                spacing = splineLength / Mathf.Max(1, deformSegments);
                break;
            default:
                Gizmos.color = new Color(1f, 0.7f, 0f, 0.6f);
                spacing = scatterSpacing;
                break;
        }

        if (spacing <= 0.01f) return;

        float dist = 0f;
        while (dist <= splineLength)
        {
            float t = dist / splineLength;
            SampleSplineWorld(spline, Mathf.Clamp01(t), out Vector3 pos, out Quaternion rot);

            Vector3 offset = mode == PlacementMode.Repeat ? repeatOffset :
                             mode == PlacementMode.Deform ? deformOffset : scatterOffset;
            Vector3 displayPos = pos + rot * offset;

            if (mode == PlacementMode.Repeat)
            {
                // Draw oriented box showing module orientation
                float modLen2 = moduleLength > 0f ? moduleLength : GetSizeAlongForwardAxis();
                Gizmos.matrix = Matrix4x4.TRS(displayPos, rot * GetAxisCorrectionRotation(), Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.3f, 0.3f, modLen2));
                Gizmos.matrix = Matrix4x4.identity;
            }
            else
            {
                Gizmos.DrawWireSphere(displayPos, 0.2f);
            }

            dist += spacing;
        }
    }
}