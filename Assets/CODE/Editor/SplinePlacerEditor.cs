using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplinePlacer))]
public class SplinePlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SplinePlacer placer = (SplinePlacer)target;
        serializedObject.Update();

        // Core
        EditorGUILayout.PropertyField(serializedObject.FindProperty("splineContainer"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("prefab"));

        EditorGUILayout.Space(5);

        // Orientation (all modes)
        EditorGUILayout.LabelField("Orientation", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("forwardAxis"),
            new GUIContent("Forward Axis", "Which axis of the mesh points along the spline"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("upAxis"),
            new GUIContent("Up Axis", "Which axis of the mesh points 'up'"));
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(10);

        // Mode-specific
        switch (placer.mode)
        {
            case SplinePlacer.PlacementMode.Repeat:
                DrawRepeatSettings();
                break;
            case SplinePlacer.PlacementMode.Deform:
                DrawDeformSettings();
                break;
            case SplinePlacer.PlacementMode.Scatter:
                DrawScatterSettings();
                break;
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("generatedContainer"));

        // Mesh save path
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Mesh Assets", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("meshSavePath"),
            new GUIContent("Save Path"));
        EditorGUI.EndDisabledGroup();

        var savedPaths = serializedObject.FindProperty("savedMeshPaths");
        if (savedPaths.arraySize > 0)
        {
            EditorGUILayout.LabelField($"Saved meshes: {savedPaths.arraySize}");
        }

        if (GUILayout.Button("Reset Path (auto-detect)"))
        {
            serializedObject.FindProperty("meshSavePath").stringValue = "";
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();

        // === BUTTONS ===
        EditorGUILayout.Space(15);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("Generate", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(placer.gameObject, "SplinePlacer Generate");
            placer.Generate();
        }

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("Clear", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(placer.gameObject, "SplinePlacer Clear");
            placer.ClearGenerated();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // Info
        if (placer.generatedContainer != null)
        {
            EditorGUILayout.Space(5);
            int count = placer.generatedContainer.childCount;
            string modeStr = placer.mode.ToString();
            EditorGUILayout.HelpBox($"{modeStr}: {count} object(s) generated", MessageType.Info);
        }
    }

    void DrawRepeatSettings()
    {
        EditorGUILayout.LabelField("Repeat Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("moduleLength"),
            new GUIContent("Module Length", "0 = auto-detect from mesh bounds"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("moduleGap"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("repeatOffset"),
            new GUIContent("Offset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("repeatRotationOffset"),
            new GUIContent("Rotation Offset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("repeatScale"),
            new GUIContent("Scale"));
        EditorGUI.indentLevel--;
    }

    void DrawDeformSettings()
    {
        EditorGUILayout.LabelField("Deform Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("deformSegments"),
            new GUIContent("Segments", "Higher = smoother bending, heavier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("deformScale"),
            new GUIContent("Cross-Section Scale"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("deformOffset"),
            new GUIContent("Offset"));

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Deform mode bends the prefab's mesh to follow the spline curve.\n" +
            "Works best with meshes that have enough vertices along the forward axis.\n" +
            "ProBuilder meshes with edge loops work great!",
            MessageType.None);
        EditorGUI.indentLevel--;
    }

    void DrawScatterSettings()
    {
        EditorGUILayout.LabelField("Scatter Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        var scatterPrefabsProp = serializedObject.FindProperty("scatterPrefabs");
        EditorGUILayout.PropertyField(scatterPrefabsProp, new GUIContent("Scatter Prefabs",
            "If populated, one is picked randomly per instance. If empty, the single 'Prefab' field above is used instead."), true);

        if (scatterPrefabsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Empty — will use the single 'Prefab' field above for every instance.", MessageType.None);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scatterSpacing"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scatterRandomOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scatterRandomHeight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scatterAlignToSpline"));

        bool alignToSpline = serializedObject.FindProperty("scatterAlignToSpline").boolValue;
        EditorGUI.BeginDisabledGroup(!alignToSpline);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scatterKeepVertical"),
            new GUIContent("Keep Vertical", "Ignore spline roll/pitch — objects always stand straight up, only yaw follows the spline."));
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("scatterRandomYRotation"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scatterScaleRange"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scatterOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scatterSeed"));
        EditorGUI.indentLevel--;
    }
}