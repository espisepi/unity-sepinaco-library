using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ScriptSkyDome))]
public class ScriptSkyDomeEditor : Editor
{
    private SerializedProperty _renderFace;
    private SerializedProperty _isVisible;
    private SerializedProperty _domePosition;
    private SerializedProperty _domeScale;
    private SerializedProperty _domeRotation;
    private SerializedProperty _enableContinuousRotation;
    private SerializedProperty _rotationSpeed;
    private SerializedProperty _rotationAxis;
    private SerializedProperty _selectedMeshIndex;
    private SerializedProperty _meshEntries;
    private SerializedProperty _customMaterial;

    private bool _meshListFoldout = true;

    private void OnEnable()
    {
        _renderFace               = serializedObject.FindProperty("_renderFace");
        _isVisible                = serializedObject.FindProperty("_isVisible");
        _domePosition             = serializedObject.FindProperty("_domePosition");
        _domeScale                = serializedObject.FindProperty("_domeScale");
        _domeRotation             = serializedObject.FindProperty("_domeRotation");
        _enableContinuousRotation = serializedObject.FindProperty("_enableContinuousRotation");
        _rotationSpeed            = serializedObject.FindProperty("_rotationSpeed");
        _rotationAxis             = serializedObject.FindProperty("_rotationAxis");
        _selectedMeshIndex        = serializedObject.FindProperty("_selectedMeshIndex");
        _meshEntries              = serializedObject.FindProperty("_meshEntries");
        _customMaterial           = serializedObject.FindProperty("_customMaterial");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawRenderSection();
        DrawVisibilitySection();
        DrawTransformSection();
        DrawRotationSection();
        DrawMaterialSection();
        DrawMeshSection();

        serializedObject.ApplyModifiedProperties();
    }

    // ───────────────────────── Sections ─────────────────────────

    private void DrawRenderSection()
    {
        EditorGUILayout.LabelField("Renderizado", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_renderFace, new GUIContent("Cara de Renderizado",
            "Inside = ver desde dentro (cielo), Outside = ver desde fuera, Both = ambas caras."));
        EditorGUILayout.Space(6);
    }

    private void DrawVisibilitySection()
    {
        EditorGUILayout.LabelField("Visibilidad", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_isVisible, new GUIContent("Visible en Escena"));
        EditorGUILayout.Space(6);
    }

    private void DrawTransformSection()
    {
        EditorGUILayout.LabelField("Transform del Domo", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_domePosition, new GUIContent("Posición"));
        EditorGUILayout.PropertyField(_domeScale, new GUIContent("Escala"));
        EditorGUILayout.PropertyField(_domeRotation, new GUIContent("Rotación"));
        EditorGUILayout.Space(6);
    }

    private void DrawRotationSection()
    {
        EditorGUILayout.LabelField("Rotación Continua", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_enableContinuousRotation,
            new GUIContent("Activar Rotación"));

        if (_enableContinuousRotation.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_rotationSpeed,
                new GUIContent("Velocidad (°/s)"));
            EditorGUILayout.PropertyField(_rotationAxis,
                new GUIContent("Eje de Rotación"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(6);
    }

    private void DrawMaterialSection()
    {
        EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_customMaterial, new GUIContent("Material Personalizado",
            "Dejar vacío para usar el material SkyDome por defecto. " +
            "Si el shader del material personalizado tiene la propiedad _Cull, " +
            "la cara de renderizado se aplicará automáticamente."));
        EditorGUILayout.Space(6);
    }

    private void DrawMeshSection()
    {
        _meshListFoldout = EditorGUILayout.Foldout(
            _meshListFoldout,
            "Forma (Meshes Disponibles)",
            true,
            EditorStyles.foldoutHeader);

        if (!_meshListFoldout) return;

        EditorGUI.indentLevel++;

        int count = _meshEntries.arraySize;

        if (count > 0)
        {
            string[] names = BuildMeshNames(count);

            int current  = Mathf.Clamp(_selectedMeshIndex.intValue, 0, count - 1);
            int selected = EditorGUILayout.Popup("Mesh Activo", current, names);

            if (selected != _selectedMeshIndex.intValue)
                _selectedMeshIndex.intValue = selected;

            EditorGUILayout.Space(4);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "No hay meshes en la lista. Pulsa 'Restaurar Primitivas' " +
                "para añadir las formas por defecto.",
                MessageType.Warning);
        }

        DrawMeshEntries();

        EditorGUILayout.Space(6);
        DrawMeshButtons();

        EditorGUI.indentLevel--;
    }

    // ───────────────────────── Mesh Helpers ─────────────────────────

    private string[] BuildMeshNames(int count)
    {
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
        {
            SerializedProperty entry = _meshEntries.GetArrayElementAtIndex(i);
            string n = entry.FindPropertyRelative("name").stringValue;
            names[i] = string.IsNullOrEmpty(n) ? $"(sin nombre {i})" : n;
        }
        return names;
    }

    private void DrawMeshEntries()
    {
        for (int i = 0; i < _meshEntries.arraySize; i++)
        {
            SerializedProperty entry    = _meshEntries.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = entry.FindPropertyRelative("name");
            SerializedProperty meshProp = entry.FindPropertyRelative("mesh");
            SerializedProperty builtIn  = entry.FindPropertyRelative("isBuiltIn");

            bool isSelected = (i == _selectedMeshIndex.intValue);

            EditorGUILayout.BeginHorizontal();

            GUIStyle indicator = new GUIStyle(EditorStyles.label)
            {
                fontStyle  = isSelected ? FontStyle.Bold : FontStyle.Normal,
                fixedWidth = 18
            };
            EditorGUILayout.LabelField(isSelected ? "►" : " ", indicator, GUILayout.Width(18));

            if (builtIn.boolValue)
            {
                GUIStyle label = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUILayout.LabelField(nameProp.stringValue, label);
            }
            else
            {
                nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue);

                meshProp.objectReferenceValue = EditorGUILayout.ObjectField(
                    meshProp.objectReferenceValue, typeof(Mesh), false,
                    GUILayout.Width(120));

                if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
                {
                    _meshEntries.DeleteArrayElementAtIndex(i);

                    if (_selectedMeshIndex.intValue >= _meshEntries.arraySize)
                        _selectedMeshIndex.intValue = Mathf.Max(0, _meshEntries.arraySize - 1);

                    break;
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawMeshButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ Añadir Mesh Personalizado"))
        {
            int idx = _meshEntries.arraySize;
            _meshEntries.InsertArrayElementAtIndex(idx);

            SerializedProperty newEntry = _meshEntries.GetArrayElementAtIndex(idx);
            newEntry.FindPropertyRelative("name").stringValue            = "Personalizado " + (idx + 1);
            newEntry.FindPropertyRelative("mesh").objectReferenceValue   = null;
            newEntry.FindPropertyRelative("isBuiltIn").boolValue         = false;
        }

        if (GUILayout.Button("Restaurar Primitivas"))
        {
            serializedObject.ApplyModifiedProperties();

            ScriptSkyDome dome = (ScriptSkyDome)target;
            Undo.RecordObject(dome, "Restaurar Primitivas SkyDome");
            dome.PopulateBuiltInMeshes();
            EditorUtility.SetDirty(dome);

            serializedObject.Update();
        }

        EditorGUILayout.EndHorizontal();
    }
}
