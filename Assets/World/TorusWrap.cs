
namespace World
{
    

using UnityEngine;

public class TorusWrap : MonoBehaviour
{
    [SerializeField] public TorusWorld world;

    void LateUpdate()
    {
        Vector2 p = world.Wrap(transform.position);
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }
}
}
