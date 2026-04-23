// MissileProjectileEditor.cs
// Custom inspector + scene gizmos for MissileProjectile.
//
// Scene view shows a single filled disc representing the actual blast radius used at runtime.
// The radius is derived from CircleCollider2D.radius * lossyScale when a collider is present,
// or falls back to the serialized explosionRadius field — exactly matching GetBlastRadius().
//
// Inspector shows a colour-coded phase timeline for homing missiles.

using UnityEditor;
using UnityEngine;
using Weapons;

[CustomEditor(typeof(MissileProjectile))]
public class MissileProjectileEditor : Editor
{
    // ── Palette ──────────────────────────────────────────────────────────────
    static readonly Color ColDrift    = new Color(0.40f, 0.75f, 1.00f, 0.90f);
    static readonly Color ColReorient = new Color(1.00f, 0.85f, 0.20f, 0.90f);
    static readonly Color ColBoost    = new Color(1.00f, 0.40f, 0.15f, 0.90f);

    static readonly Color ColBlastFill = new Color(1.00f, 0.50f, 0.00f, 0.18f);
    static readonly Color ColBlastRing = new Color(1.00f, 0.50f, 0.00f, 0.85f);

    // ── Inspector ────────────────────────────────────────────────────────────
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        var mp = (MissileProjectile)target;
        if (!mp.isHoming) { serializedObject.ApplyModifiedProperties(); return; }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Missile Phase Timeline", EditorStyles.boldLabel);

        float total = mp.driftDuration + mp.reorientDuration + mp.boostDuration;
        if (total > 0f)
        {
            Rect bar = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            DrawPhaseBar(bar, mp, total);

            EditorGUILayout.BeginHorizontal();
            DrawSwatch(ColDrift);    GUILayout.Label($"Drift {mp.driftDuration:0.##}s",       GUILayout.ExpandWidth(false));
            GUILayout.Space(8);
            DrawSwatch(ColReorient); GUILayout.Label($"Reorient {mp.reorientDuration:0.##}s", GUILayout.ExpandWidth(false));
            GUILayout.Space(8);
            DrawSwatch(ColBoost);    GUILayout.Label($"Boost {mp.boostDuration:0.##}s",       GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
        }


        // Remind user that blast behaviour lives on the explosion prefab
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Blast radius, screen shake, and knockback are configured on the Explosion Prefab's ExplosionController.",
            MessageType.None);

        serializedObject.ApplyModifiedProperties();
    }

    // ── Scene Gizmos ─────────────────────────────────────────────────────────
    void OnSceneGUI()
    {
        // Blast radius lives on the explosion prefab — nothing to draw on the missile itself.
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static void DrawPhaseBar(Rect r, MissileProjectile mp, float total)
    {
        float x = r.x;

        void Segment(float dur, Color c, string label)
        {
            float w   = r.width * (dur / total);
            var   seg = new Rect(x, r.y, w, r.height);
            EditorGUI.DrawRect(seg, c);
            EditorGUI.DrawRect(new Rect(x + w - 1, r.y, 1, r.height), new Color(0, 0, 0, 0.4f));
            if (w > 30)
            {
                var prev = GUI.color;
                GUI.color = Color.black;
                GUI.Label(new Rect(seg.x + 3, seg.y + 3, seg.width - 6, seg.height), label, EditorStyles.miniLabel);
                GUI.color = prev;
            }
            x += w;
        }

        Segment(mp.driftDuration,    ColDrift,    "Drift");
        Segment(mp.reorientDuration, ColReorient, "Reorient");
        Segment(mp.boostDuration,    ColBoost,    "Boost");
        GUI.Label(new Rect(r.xMax - 48, r.y + 3, 46, r.height), $"{total:0.##}s", EditorStyles.miniLabel);
    }

    static void DrawSwatch(Color c)
    {
        var r = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14), GUILayout.Height(14));
        EditorGUI.DrawRect(r, c);
    }
}



