using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorStateEventHandler : StateMachineBehaviour
{
    public event Action<Animator, AnimatorStateInfo, int> onStateEnter;
    public event Action<Animator, AnimatorStateInfo, int> onStateUpdate;
    public event Action<Animator, AnimatorStateInfo, int> onStateExit;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        onStateEnter?.Invoke(animator, stateInfo, layerIndex);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        onStateUpdate?.Invoke(animator, stateInfo, layerIndex);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        onStateExit?.Invoke(animator, stateInfo, layerIndex);
    }
}
