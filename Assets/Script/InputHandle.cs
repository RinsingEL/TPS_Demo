using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Claims;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandle : MonoBehaviour
{
    CharacterController characterController;
    CharacterControl characterControl;
    public Vector2 MoveInput;// ‰»Î∑ΩœÚ
    public bool IsWalk;
    public bool IsDodge;
    public bool IsDive;
    public bool IsGround;
    public bool IsAim;
    public bool IsFire;
    Vector2 AxisValue;
    public bool IsJumpPressed = false;
    public float LeftOrRight;
    // Start is called before the first frame update
    void Awake()
    {
        //Handle Callback
        characterControl = new CharacterControl();
        characterControl.Player.Enable();
        characterControl.Player.Jump.started += GetJumpInput;
        characterControl.Player.Jump.canceled += GetJumpInput;
        characterControl.Player.RunDive.started += GetDive;
        characterControl.Player.RunDive.canceled += GetDive;
        characterControl.Player.Move.started += GetMove;
        characterControl.Player.Move.performed += GetMove;
        characterControl.Player.Move.canceled += GetMove;
        characterControl.Player.Walk.started += GetWalk;
        characterControl.Player.Walk.canceled += GetWalk;
        characterControl.Player.Aim.started += GetAim;
        characterControl.Player.Aim.canceled += GetAim;
        characterControl.Player.Look.started += GetLook;
        characterControl.Player.Look.performed += GetLook;
        characterControl.Player.Look.canceled += GetLook;
        characterControl.Player.Fire.started += GetFire;
        characterControl.Player.Fire.canceled += GetFire;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void GetMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }

    public void GetJumpInput(InputAction.CallbackContext ctx)
    {
        IsJumpPressed = ctx.ReadValueAsButton();
        LeftOrRight = UnityEngine.Random.Range(-1f, 1f);
        if (LeftOrRight > 0)
        {
            LeftOrRight = 1;
        }
        else
        {
            LeftOrRight = -1;
        }
    }
    public void GetDive(InputAction.CallbackContext ctx)
    {
        IsDive = ctx.ReadValueAsButton();
    }
    void GetWalk(InputAction.CallbackContext ctx)
    {
        IsWalk = ctx.ReadValueAsButton();
    }
    public void GetAim(InputAction.CallbackContext ctx)
    {
        IsAim = ctx.ReadValueAsButton();
    }
    public void GetLook(InputAction.CallbackContext ctx)
    {
        AxisValue = ctx.ReadValue<Vector2>();
    }
    private void GetFire(InputAction.CallbackContext ctx)
    {
        IsFire = ctx.ReadValueAsButton();
    }
}
