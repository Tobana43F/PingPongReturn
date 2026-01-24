using System;
using UnityEngine;

public class Racket : MonoBehaviour
{
    public event Action<BallController> onBallEnter;

    [SerializeField] float spinEffectRate = 0.05f;

    public float SpinEffectRate { get { return spinEffectRate; } }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<BallController>(out var ballController))
        {
            onBallEnter?.Invoke(ballController);
        }
    }
}
