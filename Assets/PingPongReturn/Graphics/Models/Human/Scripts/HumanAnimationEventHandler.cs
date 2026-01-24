using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphicAssets
{
    public class HumanAnimationEventHandler : MonoBehaviour
    {
        public const string OnTossedFunctionName = nameof(Tossed);
        public const string OnHitFunctionName = nameof(Hit);

        public event Action onTossed;
        public event Action onHit;

        void Tossed()
        {
            Debug.Log("Tossed");
            onTossed?.Invoke(); 
        } 

        void Hit()
        {
            Debug.Log("Hit");
            onHit?.Invoke();
        }
    }
}
