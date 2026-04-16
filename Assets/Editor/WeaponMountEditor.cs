// WeaponMountEditor.cs
// Custom inspector for WeaponMount.
// Changes header colour and shows role-specific guidance based on the Role enum.

using UnityEditor;
using UnityEngine;
using Weapons;

[CustomEditor(typeof(WeaponMount))]
public class WeaponMountEditor : Editor
{
    // Which weapon component type is "expected" for each role
    private static readonly System.Type[] ExpectedTypes =
    {
        typeof(GunWeapon),      // Primary
        typeof(MissileWeapon),  // Secondary
        null,                   // Tertiary — anything goes
    };

    private static readonly string[] RoleDescriptions =
    {
        "Hold trigger  (SPACE) — e.g. Chaingun",
        "Single press  (ENTER) — e.g. Missiles",
        "Tertiary key  (future) — custom weapon",
    };

    private static readonly Color[] RoleColors =
    {
        new Color(0.30f, 0.55f, 1.00f),   // Primary   — blue
        new Color(1.00f, 0.55f, 0.20f),   // Secondary — orange
        new Color(0.65f, 0.40f, 1.00f),   // Tertiary  — purple
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var roleProp   = serializedObject.FindProperty("role");
        var muzzleProp = serializedObject.FindProperty("muzzle");
        var weaponProp = serializedObject.FindProperty("weapon");

        // ── Role selector ────────────────────────────────────────────────
        EditorGUILayout.PropertyField(roleProp);
        int roleIdx = roleProp.enumValueIndex;

        // ── Coloured role banner ─────────────────────────────────────────
        Color bannerColor = roleIdx >= 0 && roleIdx < RoleColors.Length
            ? RoleColors[roleIdx] : Color.grey;

        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = bannerColor;
        EditorGUILayout.HelpBox(
            $"{(WeaponRole)roleIdx}  ·  {RoleDescriptions[roleIdx]}",
            MessageType.None);
        GUI.backgroundColor = prevBg;

        EditorGUILayout.Space(4);

        // ── Muzzle & Weapon fields ───────────────────────────────────────
        EditorGUILayout.PropertyField(muzzleProp);
        EditorGUILayout.PropertyField(weaponProp);

        // ── Validation warnings ─────────────────────────────────────────
        var weaponObj = weaponProp.objectReferenceValue as WeaponBase;
        System.Type expected = roleIdx >= 0 && roleIdx < ExpectedTypes.Length
            ? ExpectedTypes[roleIdx] : null;

        if (weaponObj == null)
        {
            EditorGUILayout.HelpBox("No weapon assigned — mount will do nothing.", MessageType.Warning);
        }
        else if (expected != null && !expected.IsAssignableFrom(weaponObj.GetType()))
        {
            EditorGUILayout.HelpBox(
                $"Role '{(WeaponRole)roleIdx}' usually uses {expected.Name}, " +
                $"but {weaponObj.GetType().Name} is attached.\n" +
                "This is allowed — just double-check it's intentional.",
                MessageType.Info);
        }
        else if (expected != null)
        {
            // All good — show a small green tick message
            var prevColor = GUI.color;
            GUI.color = new Color(0.4f, 1f, 0.5f);
            EditorGUILayout.HelpBox($"✓ {weaponObj.GetType().Name} matches role.", MessageType.None);
            GUI.color = prevColor;
        }

        if (muzzleProp.objectReferenceValue == null)
            EditorGUILayout.HelpBox("No muzzle assigned — will fire from this transform's position.", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}

