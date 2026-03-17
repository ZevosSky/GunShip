using UnityEngine;

namespace Weapons
{
    public class WeaponMount : MonoBehaviour
    {
        [SerializeField] Transform muzzle;
        [SerializeField] WeaponBase weapon;

        public void TriggerDown() => weapon?.TriggerDown(muzzle);
        public void TriggerUp()   => weapon?.TriggerUp();

        private void Update()
        {
            weapon?.Tick(Time.deltaTime);
        }
    }
}