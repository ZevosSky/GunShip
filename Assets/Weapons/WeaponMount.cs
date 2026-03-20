using UnityEngine;

namespace Weapons
{
    public class WeaponMount : MonoBehaviour
    {
        [SerializeField] Transform muzzle;
        [SerializeField] WeaponBase weapon;

        // If no explicit muzzle child is assigned, fire from this transform itself
        private Transform FirePoint => muzzle != null ? muzzle : transform;

        public void TriggerDown() => weapon?.TriggerDown(FirePoint);
        public void TriggerUp()   => weapon?.TriggerUp();

        private void Update()
        {
            weapon?.Tick(Time.deltaTime);
        }

        //====| Gizmos |=============================================================
        // Colour coding:
        //   Red    = mount exists but no weapon assigned
        //   Yellow = weapon assigned, muzzle missing (using mount position)
        //   Green  = fully configured
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            bool hasWeapon = weapon != null;
            bool hasMuzzle = muzzle != null;

            Gizmos.color = (!hasWeapon)   ? new Color(1f, 0.2f, 0.2f, 0.8f)   // red
                         : (!hasMuzzle)   ? new Color(1f, 0.85f, 0f,  0.8f)   // yellow
                                          : new Color(0.2f, 1f, 0.4f, 0.8f);  // green

            // Circle at mount position
            Gizmos.DrawWireSphere(transform.position, 0.12f);

            // Arrow showing fire direction (transform.up = ship forward)
            Transform tip = FirePoint;
            Vector3   dir = tip.up;
            Vector3   tipPos = tip.position;

            Gizmos.DrawLine(tipPos, tipPos + dir * 0.45f);
            // Arrowhead
            Gizmos.DrawLine(tipPos + dir * 0.45f, tipPos + dir * 0.30f + tip.right * 0.10f);
            Gizmos.DrawLine(tipPos + dir * 0.45f, tipPos + dir * 0.30f - tip.right * 0.10f);
        }

        private void OnDrawGizmosSelected()
        {
            // When selected also draw a line from mount to muzzle so you can see the offset
            if (muzzle != null && muzzle != transform)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, muzzle.position);
                Gizmos.DrawWireSphere(muzzle.position, 0.06f);
            }

            // Label (requires Handles)
            UnityEditor.Handles.color  = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.right * 0.18f,
                weapon != null ? $"[{weapon.GetType().Name}]" : "[empty mount]");
        }
#endif
    }
}