
//==============================================================================
// @Author:   Gary Yang
// @File:     Envelope.cs
// @brief:    This is a generic envelope creator 
// @copyright MIT 
//==============================================================================


using Unity.Collections;
using System.Collections.Generic;

namespace Utils
{
    

using UnityEngine;




[CreateAssetMenu(fileName = "New Envelope", menuName = "Envelope")]
public class Envelope : ScriptableObject
{
    //===| Data packing |================================================||
    private enum PartType { Attack, Decline, Hold, Start, End}
    [CreateAssetMenu(menuName = "GunShip/Utils/Envelope Part")]
    private class EnvelopePart : ScriptableObject
    {
        [SerializeField] private PartType partType;
        [SerializeField] private EasingData easingData;
        [SerializeField] [Range(0,1)] private float unnormalizedTargetValue;
        [SerializeField] private float duration;
    }
 
    
    //===| UI Interface |================================================||
    [ReadOnly] private Gradient _gradient; 
    
    [SerializeField] private List<EnvelopePart> parts; // a list of all the parts
    
} // End of Envelope Class 


} // eof & namespace 
