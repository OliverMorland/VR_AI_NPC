using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingController : MonoBehaviour
{
    public Animator animator;
    const string targetParameter = "MoveSpeed";

    public void SetWalkingSpeed(float walkingSpeed)
    {
        animator.SetFloat(targetParameter, walkingSpeed);
    }
}
