// WeaponDataEditor.cs
// Shows only the fields relevant to the selected WeaponType.

using UnityEditor;
using UnityEngine;
using Weapons;

[CustomEditor(typeof(WeaponData))]
public class WeaponDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── Type selector (always visible) ───────────────────────────────
        var typeProp = serializedObject.FindProperty("Type");
        EditorGUILayout.PropertyField(typeProp);
        var weaponType = (WeaponType)typeProp.enumValueIndex;

        // ── Coloured banner ──────────────────────────────────────────────
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = weaponType switch
        {
            WeaponType.Gun    => new Color(0.30f, 0.60f, 1.00f),   // blue
            WeaponType.Missile => new Color(1.00f, 0.55f, 0.20f),  // orange
            WeaponType.Laser  => new Color(1.00f, 0.25f, 0.40f),   // red-pink
            _                 => Color.grey,
        };
        EditorGUILayout.HelpBox($"Editing:  {weaponType}  data", MessageType.None);
        GUI.backgroundColor = prevBg;

        EditorGUILayout.Space(6);

        // ── Core (always) ────────────────────────────────────────────────
        SectionHeader("Core");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("damage"));

        // ── Gun fields ───────────────────────────────────────────────────
        if (weaponType == WeaponType.Gun)
        {
            EditorGUILayout.Space(4);
            SectionHeader("Rate of Fire");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxShotsPerSecond"));

            EditorGUILayout.Space(4);
            SectionHeader("Machine Gun Spin-up");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasSpinUp"));
            if (serializedObject.FindProperty("hasSpinUp").boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spinUpTime"));

            EditorGUILayout.Space(4);
            SectionHeader("Projectile Physics");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileLifetime"));

            EditorGUILayout.Space(4);
            SectionHeader("Kick / Spread");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSpreadDegrees"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("knockbackForce"));
        }

        // ── Missile fields ───────────────────────────────────────────────
        if (weaponType == WeaponType.Missile)
        {
            EditorGUILayout.Space(4);
            SectionHeader("Missile");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSalvoSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("missileSpreadAngle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("missileSalvoResetTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("missileTurnSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("missileJerkForce"));

            EditorGUILayout.Space(4);
            SectionHeader("Projectile Physics");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileLifetime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("knockbackForce"));
        }

        // ── Laser fields ─────────────────────────────────────────────────
        if (weaponType == WeaponType.Laser)
        {
            EditorGUILayout.Space(4);
            SectionHeader("Laser Charge");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasLaserCharge"));
            if (serializedObject.FindProperty("hasLaserCharge").boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chargeTime"));

            EditorGUILayout.Space(4);
            SectionHeader("Projectile Physics");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileLifetime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("knockbackForce"));
        }

        // ── VFX / SFX (always) ───────────────────────────────────────────
        EditorGUILayout.Space(4);
        SectionHeader("VFX / SFX");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("projectilePrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("muzzleVFXPrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("explosionPrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fireSfx"));

        serializedObject.ApplyModifiedProperties();
    }

    private static void SectionHeader(string label)
    {
        var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
        EditorGUILayout.LabelField(label, style);
    }
}

