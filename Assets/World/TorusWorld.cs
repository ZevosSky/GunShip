//==============================================================================
// @Author:   Gary Yang
// @File:     TorusWorld.cs
// @brief:    General World Math for a 2D torus world, including position wraps
//            and shortest delta calculations To prevent camera freakouts
//      
// @copyright MIT 
//==============================================================================

// TODO: Need to figure out how to make this map correctly with a 3D spoof of a planet

namespace World
{
    
using UnityEngine;

[CreateAssetMenu(fileName = "TorusWorld", menuName = "Scriptable Objects/TorusWorld")]
public class TorusWorld : ScriptableObject
{
    public float width  = 200f;
    public float height = 120f;

    /// Wraps a position into torus bounds
    public Vector2 Wrap(Vector2 pos)
    {
        pos.x = Mod(pos.x, width);
        pos.y = Mod(pos.y, height);
        return pos;
    }

    /// Shortest displacement from 'from' to 'to' on the torus.
    /// stops the camera from freaking out at seams.
    public Vector2 ShortestDelta(Vector2 from, Vector2 to)
    {
        float dx = to.x - from.x;
        float dy = to.y - from.y;

        if (dx >  width  * 0.5f) dx -= width;
        else if (dx < -width  * 0.5f) dx += width;

        if (dy >  height * 0.5f) dy -= height;
        else if (dy < -height * 0.5f) dy += height;

        return new Vector2(dx, dy);
    }

    // Proper modulo that handles negatives
    private static float Mod(float a, float b) => ((a % b) + b) % b;
}


}
