using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScriptPhysicsManager))]
public class ScriptPhysicsManagerEditor : Editor
{
    private SerializedProperty _startModeProp;
    private SerializedProperty _targetsProp;
    private SerializedProperty _menuKeyProp;
    private SerializedProperty _enableAllKeyProp;
    private SerializedProperty _disableAllKeyProp;
    private SerializedProperty _toggleSelectedKeyProp;
    private SerializedProperty _nextTargetKeyProp;
    private SerializedProperty _prevTargetKeyProp;

    private bool _keysFoldout = true;

    private void OnEnable()
    {
        _startModeProp = serializedObject.FindProperty("_startMode");
        _targetsProp = serializedObject.FindProperty("_targets");
        _menuKeyProp = serializedObject.FindProperty("menuKey");
        _enableAllKeyProp = serializedObject.FindProperty("enableAllKey");
        _disableAllKeyProp = serializedObject.FindProperty("disableAllKey");
        _toggleSelectedKeyProp = serializedObject.FindProperty("toggleSelectedKey");
        _nextTargetKeyProp = serializedObject.FindProperty("nextTargetKey");
        _prevTargetKeyProp = serializedObject.FindProperty("prevTargetKey");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ScriptPhysicsManager manager = (ScriptPhysicsManager)target;

        EditorGUILayout.LabelField("Physics Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // ── Start mode ──
        EditorGUILayout.PropertyField(_startModeProp, new GUIContent("Start Mode"));
        EditorGUILayout.Space(4);

        // ── Menu key config ──
        _keysFoldout = EditorGUILayout.Foldout(_keysFoldout, "Menú y Teclas", true, EditorStyles.foldoutHeader);
        if (_keysFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_menuKeyProp, new GUIContent("Tecla Menú"));
            EditorGUILayout.PropertyField(_enableAllKeyProp, new GUIContent("Activar Todos"));
            EditorGUILayout.PropertyField(_disableAllKeyProp, new GUIContent("Desactivar Todos"));
            EditorGUILayout.PropertyField(_toggleSelectedKeyProp, new GUIContent("Alternar Seleccionado"));
            EditorGUILayout.PropertyField(_nextTargetKeyProp, new GUIContent("Siguiente Target"));
            EditorGUILayout.PropertyField(_prevTargetKeyProp, new GUIContent("Target Anterior"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(4);

        // ── Global buttons ──
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable All", GUILayout.Height(28)))
        {
            SetAllTargets(true);
        }
        if (GUILayout.Button("Disable All", GUILayout.Height(28)))
        {
            SetAllTargets(false);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // ── Targets array ──
        EditorGUILayout.PropertyField(_targetsProp, new GUIContent("Targets"), false);

        if (_targetsProp.isExpanded)
        {
            EditorGUI.indentLevel++;
            int size = _targetsProp.arraySize;
            int newSize = EditorGUILayout.IntField("Size", size);
            if (newSize > size)
            {
                _targetsProp.arraySize = newSize;
                for (int n = size; n < newSize; n++)
                    _targetsProp.GetArrayElementAtIndex(n)
                        .FindPropertyRelative("collidersEnabled").boolValue = true;
            }
            else if (newSize < size)
            {
                _targetsProp.arraySize = newSize;
            }

            for (int i = 0; i < _targetsProp.arraySize; i++)
            {
                SerializedProperty element = _targetsProp.GetArrayElementAtIndex(i);
                SerializedProperty targetProp = element.FindPropertyRelative("target");
                SerializedProperty enabledProp = element.FindPropertyRelative("collidersEnabled");

                EditorGUILayout.Space(2);
                Rect rect = EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();

                string label = targetProp.objectReferenceValue != null
                    ? targetProp.objectReferenceValue.name
                    : $"Element {i}";

                EditorGUILayout.LabelField($"[{i}] {label}", EditorStyles.miniBoldLabel);

                EditorGUILayout.PropertyField(targetProp, new GUIContent("Target"));

                bool prev = enabledProp.boolValue;
                Color original = GUI.backgroundColor;
                GUI.backgroundColor = prev ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.9f, 0.4f, 0.4f);

                string buttonLabel = prev ? "Colliders: ON" : "Colliders: OFF";
                if (GUILayout.Button(buttonLabel, GUILayout.Height(22)))
                {
                    enabledProp.boolValue = !prev;
                }
                GUI.backgroundColor = original;

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                if (i < _targetsProp.arraySize - 1)
                {
                    EditorGUILayout.Space(2);
                    DrawSeparator();
                }
            }
            EditorGUI.indentLevel--;
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            if (!Application.isPlaying)
                ApplyInEditMode();
        }

        EditorGUILayout.Space(8);
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Refresh Cache (Runtime)", GUILayout.Height(24)))
            {
                manager.RefreshCache();
            }
        }
    }

    private void SetAllTargets(bool enabled)
    {
        serializedObject.Update();
        for (int i = 0; i < _targetsProp.arraySize; i++)
        {
            SerializedProperty element = _targetsProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("collidersEnabled").boolValue = enabled;
        }
        serializedObject.ApplyModifiedProperties();

        if (Application.isPlaying)
        {
            ((ScriptPhysicsManager)target).RefreshCache();
        }
        else
        {
            ApplyInEditMode();
        }
    }

    private void ApplyInEditMode()
    {
        for (int i = 0; i < _targetsProp.arraySize; i++)
        {
            SerializedProperty element = _targetsProp.GetArrayElementAtIndex(i);
            SerializedProperty targetProp = element.FindPropertyRelative("target");
            SerializedProperty enabledProp = element.FindPropertyRelative("collidersEnabled");

            GameObject go = targetProp.objectReferenceValue as GameObject;
            if (go == null) continue;

            Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
                col.enabled = enabledProp.boolValue;
        }
    }

    private static void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
    }
}
