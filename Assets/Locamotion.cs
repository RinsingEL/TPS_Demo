using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Locamotion : MonoBehaviour
{
    Animator animator;
    Transform playertransform;
    Transform cameratransform;
    public enum LocomotionState
    {
        Idle,
        Walk,
        Run
    };
    public LocomotionState Locomotionstate=LocomotionState.Idle;

    float WalkSpeed = 2f;
    float RunSpeed = 4f;

    Vector2 MoveInput;//输入方向
    Vector3 PlayerMovement=new Vector3(0,0,0);//玩家移动方向
    void Start()
    {
        playertransform=GetComponent<Transform>();
        cameratransform=Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        CameraMovement();
        RunAndWalk();
    }
    void CameraMovement()
    {
        Vector3 CameraForward = new Vector3(cameratransform.forward.x, 0, cameratransform.forward.z).normalized;
        PlayerMovement = CameraForward * MoveInput.y + cameratransform.right * MoveInput.x;
        PlayerMovement = playertransform.InverseTransformPoint(PlayerMovement);
    }
    void RunAndWalk()
    {
        animator.SetFloat("Speed",PlayerMovement.magnitude*RunSpeed,0.1f,Time.deltaTime);
    }
}
