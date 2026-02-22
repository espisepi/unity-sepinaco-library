using Sepinaco.SceneTools;
using UnityEditor;
using UnityEngine;

namespace Sepinaco.SceneTools.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="PhysicsManager"/>.
    /// Provides colorised collider-state toggles, add / remove buttons per target,
    /// and global Enable / Disable All actions.
    /// </summary>
    [CustomEditor(typeof(PhysicsManager))]
    public class PhysicsManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty _startModeProp;
        private SerializedProperty _targetsProp;
        private SerializedProperty _menuKeyProp;
        private SerializedProperty _menuAnchorProp;
        private SerializedProperty _activateAllKeyProp;
        private SerializedProperty _deactivateAllKeyProp;
        private SerializedProperty _toggleSelectedKeyProp;
        private SerializedProperty _nextTargetKeyProp;
        private SerializedProperty _prevTargetKeyProp;

        private bool _keysFoldout = true;

        private void OnEnable()
        {
            _startModeProp         = serializedObject.FindProperty("_startMode");
            _targetsProp           = serializedObject.FindProperty("_targets");
            _menuKeyProp           = serializedObject.FindProperty("_menuKey");
            _menuAnchorProp        = serializedObject.FindProperty("_menuAnchor");
            _activateAllKeyProp    = serializedObject.FindProperty("_activateAllKey");
            _deactivateAllKeyProp  = serializedObject.FindProperty("_deactivateAllKey");
            _toggleSelectedKeyProp = serializedObject.FindProperty("_toggleSelectedKey");
            _nextTargetKeyProp     = serializedObject.FindProperty("_nextTargetKey");
            _prevTargetKeyProp     = serializedObject.FindProperty("_prevTargetKey");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            PhysicsManager manager = (PhysicsManager)target;

            EditorGUILayout.LabelField("Physics Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(_startModeProp, new GUIContent("Start Mode"));
            EditorGUILayout.Space(4);

            _keysFoldout = EditorGUILayout.Foldout(
                _keysFoldout, "Menu & Key Bindings", true, EditorStyles.foldoutHeader);

            if (_keysFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_menuKeyProp,           new GUIContent("Menu Key"));
                EditorGUILayout.PropertyField(_menuAnchorProp,        new GUIContent("Menu Anchor"));
                EditorGUILayout.PropertyField(_activateAllKeyProp,    new GUIContent("Enable All"));
                EditorGUILayout.PropertyField(_deactivateAllKeyProp,  new GUIContent("Disable All"));
                EditorGUILayout.PropertyField(_toggleSelectedKeyProp, new GUIContent("Toggle Selected"));
                EditorGUILayout.PropertyField(_nextTargetKeyProp,     new GUIContent("Next Target"));
                EditorGUILayout.PropertyField(_prevTargetKeyProp,     new GUIContent("Previous Target"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable All", GUILayout.Height(28)))
                SetAllTargets(true);
            if (GUILayout.Button("Disable All", GUILayout.Height(28)))
                SetAllTargets(false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            EditorGUILayout.PropertyField(_targetsProp, new GUIContent("Targets"), false);

            if (_targetsProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                int size = _targetsProp.arraySize;
                int newSize = Mathf.Max(0, EditorGUILayout.IntField("Size", size));

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

                int indexToDelete = -1;
                int indexToInsert = -1;

                for (int i = 0; i < _targetsProp.arraySize; i++)
                {
                    SerializedProperty element    = _targetsProp.GetArrayElementAtIndex(i);
                    SerializedProperty targetProp = element.FindPropertyRelative("target");
                    SerializedProperty enabledProp = element.FindPropertyRelative("collidersEnabled");

                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();

                    string label = targetProp.objectReferenceValue != null
                        ? targetProp.objectReferenceValue.name
                        : $"Element {i}";

                    EditorGUILayout.LabelField($"[{i}] {label}", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(targetProp, new GUIContent("Target"));

                    bool prev = enabledProp.boolValue;
                    Color original = GUI.backgroundColor;
                    GUI.backgroundColor = prev
                        ? new Color(0.4f, 0.9f, 0.4f)
                        : new Color(0.9f, 0.4f, 0.4f);

                    string buttonLabel = prev ? "Colliders: ON" : "Colliders: OFF";
                    if (GUILayout.Button(buttonLabel, GUILayout.Height(22)))
                        enabledProp.boolValue = !prev;

                    GUI.backgroundColor = original;
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical(GUILayout.Width(24));
                    if (GUILayout.Button("+", GUILayout.Width(22), GUILayout.Height(22)))
                        indexToInsert = i + 1;
                    if (GUILayout.Button("\u2212", GUILayout.Width(22), GUILayout.Height(22)))
                        indexToDelete = i;
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();

                    if (i < _targetsProp.arraySize - 1)
                    {
                        EditorGUILayout.Space(2);
                        DrawSeparator();
                    }
                }

                if (indexToDelete >= 0)
                {
                    _targetsProp.DeleteArrayElementAtIndex(indexToDelete);
                }
                else if (indexToInsert >= 0)
                {
                    _targetsProp.InsertArrayElementAtIndex(indexToInsert);
                    _targetsProp.GetArrayElementAtIndex(indexToInsert)
                        .FindPropertyRelative("target").objectReferenceValue = null;
                    _targetsProp.GetArrayElementAtIndex(indexToInsert)
                        .FindPropertyRelative("collidersEnabled").boolValue = true;
                }

                EditorGUILayout.Space(4);
                if (GUILayout.Button("+ Add Target", GUILayout.Height(24)))
                {
                    int idx = _targetsProp.arraySize;
                    _targetsProp.arraySize++;
                    _targetsProp.GetArrayElementAtIndex(idx)
                        .FindPropertyRelative("target").objectReferenceValue = null;
                    _targetsProp.GetArrayElementAtIndex(idx)
                        .FindPropertyRelative("collidersEnabled").boolValue = true;
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
                    manager.RefreshCache();
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
                ((PhysicsManager)target).RefreshCache();
            else
                ApplyInEditMode();
        }

        private void ApplyInEditMode()
        {
            for (int i = 0; i < _targetsProp.arraySize; i++)
            {
                SerializedProperty element     = _targetsProp.GetArrayElementAtIndex(i);
                SerializedProperty targetProp  = element.FindPropertyRelative("target");
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
}
