// BossControllerEditor.cs
// Custom editor for BossController that draws interactive Handles so you can
// drag and rotate the gun / missile muzzle Transforms directly in the Scene view
// whenever the BossController GameObject is selected.

using UnityEditor;
using UnityEngine;
using Enemies;

[CustomEditor(typeof(BossController))]
public class BossControllerEditor : Editor
{
    // Serialized property references – cached on enable
    private SerializedProperty _bodyGunMuzzles;
    private SerializedProperty _bodyMissileMuzzles;

    private void OnEnable()
    {
        // target can be null briefly during domain reloads / prefab editing
        if (target == null) return;
        _bodyGunMuzzles     = serializedObject.FindProperty("bodyGunMuzzles");
        _bodyMissileMuzzles = serializedObject.FindProperty("bodyMissileMuzzles");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        if (target == null) return;

        // Refresh properties if they were skipped during a reload
        if (_bodyGunMuzzles == null || _bodyMissileMuzzles == null)
        {
            _bodyGunMuzzles     = serializedObject.FindProperty("bodyGunMuzzles");
            _bodyMissileMuzzles = serializedObject.FindProperty("bodyMissileMuzzles");
        }

        // Gun muzzles – yellow/gold
        DrawMuzzleHandles(_bodyGunMuzzles,     new Color(1f, 0.85f, 0f, 1f));
        // Missile muzzles – orange/red
        DrawMuzzleHandles(_bodyMissileMuzzles, new Color(1f, 0.35f, 0.1f, 1f));
    }

    // ── Draw move + rotate handles for every non-null Transform in the array ──
    private void DrawMuzzleHandles(SerializedProperty arrayProp, Color color)
    {
        if (arrayProp == null) return;

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            var elemProp  = arrayProp.GetArrayElementAtIndex(i);
            var muzzle    = elemProp.objectReferenceValue as Transform;
            if (muzzle == null) continue;

            EditorGUI.BeginChangeCheck();

            Handles.color = color;

            // ── Position handle ──────────────────────────────────────────
            Vector3 newPos = Handles.PositionHandle(muzzle.position, muzzle.rotation);

            // ── Rotation handle (disc around Z-axis for 2-D workflow) ────
            float   handleSize = HandleUtility.GetHandleSize(muzzle.position) * 0.6f;
            Quaternion newRot  = Handles.Disc(
                muzzle.rotation,
                muzzle.position,
                Vector3.forward,     // 2-D: rotate around Z
                handleSize,
                false,
                1f);

            // Draw a direction arrow so it's obvious which way the muzzle points
            Handles.color = color * 0.85f;
            Handles.DrawLine(muzzle.position,
                             muzzle.position + muzzle.up * handleSize * 1.6f, 2f);
            Handles.DrawSolidDisc(muzzle.position + muzzle.up * handleSize * 1.6f,
                                  Vector3.forward, handleSize * 0.08f);

            // Label
            Handles.color = Color.white;
            Handles.Label(muzzle.position + Vector3.up * (handleSize * 0.3f),
                          $"{arrayProp.name}[{i}]\n{muzzle.name}",
                          EditorStyles.miniLabel);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(muzzle, $"Move/Rotate Muzzle {muzzle.name}");

                muzzle.position = newPos;
                muzzle.rotation = newRot;

                // Mark the scene dirty so changes persist
                EditorUtility.SetDirty(muzzle);
            }
        }
    }
}

