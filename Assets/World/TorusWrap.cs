namespace World
{

using UnityEngine;

public class TorusWrap : MonoBehaviour
{
    [SerializeField] public TorusWorld world;

    private TrailRenderer[] _trails;
    private TrailSeamSuppressor[] _trailSuppressors;

    void Awake()
    {
        _trails = GetComponentsInChildren<TrailRenderer>(true);
        _trailSuppressors = GetComponentsInChildren<TrailSeamSuppressor>(true);
    }

    void LateUpdate()
    {
        Vector2 p = world.Wrap(transform.position);
        Vector2 current = transform.position;

        if ((p - current).sqrMagnitude > 0.001f)
        {
            foreach (var suppressor in _trailSuppressors)
            {
                if (suppressor != null)
                    suppressor.SuppressForSeamCrossing();
            }

            transform.position = new Vector3(p.x, p.y, transform.position.z);

            // Clear trails so they don't streak across the world on teleport
            foreach (var trail in _trails)
            {
                if (trail != null)
                    trail.Clear();
            }
        }
    }
}
}
