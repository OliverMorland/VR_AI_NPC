using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserFacer : MonoBehaviour
{
    [Range(0f, 1f)] public float rotationFactor = 0.02f;
    public bool isFacingUser;
    Transform userView;

    private void Start()
    {
        userView = Camera.main.transform;
    }

    private void Update()
    {
        if (isFacingUser)
        {
            TurnToFaceUser();
        }
    }

    private void TurnToFaceUser()
    {
        Vector3 directionToUser = GetDirectionToUser();
        Quaternion lookAtRotation = Quaternion.LookRotation(directionToUser);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookAtRotation, rotationFactor);
    }

    Vector3 GetDirectionToUser()
    {
        Vector3 directionToUser = (userView.transform.position - transform.position);
        directionToUser.y = 0;
        directionToUser.Normalize();
        return directionToUser;
    }

    public void SetIsFacingUser(bool shouldFaceUser)
    {
        isFacingUser = shouldFaceUser;
    }
}
