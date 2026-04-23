// OrbitalSpawner.cs
// Attach to a melee boss part (or any GameObject).
// Spawns <count> copies of a prefab evenly distributed around this transform
// and keeps them orbiting at a fixed radius.
// The component manages its own spawned children; destroying this component
// (or the host) also destroys the orbiters.
// Keep DISABLED while parented to the boss — BossController enables it on Detach().

using UnityEngine;

namespace Enemies
{
    public class OrbitalSpawner : MonoBehaviour
    {
        [Header("Orbiters")]
        [Tooltip("Prefab to spawn and orbit around this part")]
        [SerializeField] private GameObject orbiterPrefab;

        [Tooltip("Number of orbiters evenly distributed around the ring")]
        [SerializeField] [Min(1)] private int count = 3;

        [Tooltip("Radius of the orbit ring")]
        [SerializeField] private float orbitRadius = 3f;

        [Tooltip("Degrees per second (positive = counter-clockwise)")]
        [SerializeField] private float orbitSpeed = 90f;

        // Current angle offset shared by all orbiters
        private float _angle;

        // The spawned instances
        private Transform[] _orbiters;

        // ── Lifecycle ────────────────────────────────────────────────────────

        void OnEnable()
        {
            SpawnOrbiters();
        }

        void OnDisable()
        {
            DestroyOrbiters();
        }

        void OnDestroy()
        {
            DestroyOrbiters();
        }

        // ── Update ───────────────────────────────────────────────────────────

        void Update()
        {
            if (_orbiters == null) return;

            _angle += orbitSpeed * Time.deltaTime;
            if (_angle >= 360f) _angle -= 360f;
            if (_angle < 0f)   _angle += 360f;

            PlaceOrbiters();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        void SpawnOrbiters()
        {
            DestroyOrbiters(); // clean up any leftovers

            if (orbiterPrefab == null) return;

            _orbiters = new Transform[count];
            float step = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float   deg = _angle + step * i;
                Vector3 pos = OrbiterPosition(deg);
                var     go  = Instantiate(orbiterPrefab, pos, Quaternion.identity);
                _orbiters[i] = go.transform;
            }
        }

        void DestroyOrbiters()
        {
            if (_orbiters == null) return;
            foreach (var t in _orbiters)
            {
                if (t != null) Destroy(t.gameObject);
            }
            _orbiters = null;
        }

        void PlaceOrbiters()
        {
            float step = 360f / count;
            for (int i = 0; i < count; i++)
            {
                if (_orbiters[i] == null) continue;
                float   deg = _angle + step * i;
                _orbiters[i].position = OrbiterPosition(deg);
            }
        }

        Vector3 OrbiterPosition(float deg)
        {
            float rad = deg * Mathf.Deg2Rad;
            return transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // Draw the orbit band as a wire disc
            const int segments = 64;
            float     inner    = orbitRadius - 0.15f;
            float     outer    = orbitRadius + 0.15f;

            DrawCircle(transform.position, inner, new Color(0.2f, 0.8f, 1f, 0.6f), segments);
            DrawCircle(transform.position, outer, new Color(0.2f, 0.8f, 1f, 0.6f), segments);

            // Draw radial lines between inner and outer to close the band
            for (int s = 0; s < segments; s += segments / 8)
            {
                float   rad = s * Mathf.PI * 2f / segments;
                Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
                Gizmos.DrawLine(transform.position + dir * inner, transform.position + dir * outer);
            }

            // Draw dots where orbiters will sit
            if (count < 1) return;
            float step = 360f / count;
            Gizmos.color = new Color(1f, 0.85f, 0f);
            for (int i = 0; i < count; i++)
            {
                float   previewAngle = Application.isPlaying ? _angle : 0f;
                Vector3 pos          = OrbiterPosition(previewAngle + step * i);
                Gizmos.DrawSphere(pos, 0.2f);
            }
        }

        static void DrawCircle(Vector3 centre, float radius, Color col, int segments)
        {
            Gizmos.color = col;
            float step = Mathf.PI * 2f / segments;
            for (int i = 0; i < segments; i++)
            {
                float   a0 = step * i;
                float   a1 = step * (i + 1);
                Vector3 p0 = centre + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * radius;
                Vector3 p1 = centre + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * radius;
                Gizmos.DrawLine(p0, p1);
            }
        }
#endif
    }
}

