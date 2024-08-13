using Cinemachine;
using System;
using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
public class Move : NetworkBehaviour
{
    private NetworkVariable<bool> RigActive = new NetworkVariable<bool>(true, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<Vector3> netAimPos = new(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Destroy(FreeLook);
        }
        //锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        //获取玩家的动画以及相机，玩家位置
        characterController = GetComponent<CharacterController>();
        if(IsOwner)
        {
            Aimcamera = GameObject.Find("Aim Camera").GetComponent<CinemachineVirtualCamera>();
            Aimcamera.Follow = CameraLookAt.transform;
        }
        animator = GetComponent<Animator>();
        playertransform = GetComponent<Transform>();
        cameratransform = Camera.main.transform;
        rigBuilder = GetComponent<RigBuilder>();
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

        //Handle Jump Variables
        SetJumpVariables();

    }
    Animator animator;
    Transform playertransform;
    Transform cameratransform;

    CharacterController characterController;
    CharacterControl characterControl;


    public float RotationSmoothTime = 0.12f;
    private float _rotationVelocity;

    public bool IsWalk;
    bool IsDodge;
    bool IsDive;
    bool IsGround;
    float groundoffset = 0.5f;
    //gravity variables
    float gravity = -9.8f;
    float GroundGravity = -0.5f;

    //Jump Variables
    bool IsJumping = false;
    bool IsJumpPressed = false;
    float LeftOrRight;
    float InitialJumpVelocity;
    float MaxJumpHeight = 1.5f;
    float MaxJumpTime = 0.4f;

    //Movement Variables
    Vector3 CurrentMovement;
    Vector3 CurrentRunMovement;
    Vector2 MoveInput;//输入方向

    //Handle Aim
    bool IsAim;
    Vector2 AxisValue;
    float cinemachineTargetX = 0f;
    float cinemachineTargetY = 0f;
    [Range(1f, 10f)] public float Sensitivity;
    public CinemachineFreeLook FreeLook;
    public CinemachineVirtualCamera Aimcamera;
    [SerializeField] public LayerMask aimColliderLayer = new LayerMask();
    public GameObject CameraLookAt;
    public GameObject AimTagert;
    //Rig
    [SerializeField] private RigBuilder rigBuilder;
    //character face
    Vector3 MousePosition = Vector3.zero;
    //Fire
    bool IsFire;
    bool IsFiring = false;

    float WalkSpeed = 2f;
    float RunSpeed = 4f;
    float LandSpeed = 4f;

    void SetJumpVariables()
    {
        //refer physical formula
        float timeToApex = MaxJumpTime / 2;
        gravity = (-2 * MaxJumpHeight) / Mathf.Pow(timeToApex, 2);
        InitialJumpVelocity = (2 * MaxJumpHeight) / timeToApex;
    }
    void Update()
    {
        if (IsOwner)
            LocalInput();
        else
            SyncInput();
    }

    private void SyncInput()
    {
        rigBuilder.enabled = RigActive.Value;
        AimTagert.transform.position = netAimPos.Value;
    }

    private void LocalInput()
    {
        RayCheck();
        CheckGround();
        HandleAim();
        CameraMovement();
        //Handle Character Move
        CharactorMove();
        HandleMove();
        //isGround要停下来才会检测
        HandleGravity();
        //Handle Jump
        HandleJump();
        HandleFire();
    }
    void CheckGround()
    {
        if (Physics.SphereCast(playertransform.position + (Vector3.up * groundoffset), characterController.radius,
            Vector3.down, out RaycastHit hit, groundoffset - characterController.radius + 2 * characterController.skinWidth))
            IsGround = true;
        else
            IsGround = false;
    }
    void CharactorMove()
    {
        characterController.Move(CurrentMovement * Time.deltaTime);
    }
    void HandleGravity()
    {
        bool IsFalling = CurrentMovement.y <= 0;
        float multiplier;
        if (IsFalling || !IsJumpPressed)
            multiplier = 2.0f;
        else
            multiplier = 1.0f;
        if (characterController.isGrounded)
        {
            //Character controller property will be false if there is no downward speed,so we set a downward speed

            LandSpeed = Mathf.Lerp(0, LandSpeed, 0.9f);
            animator.SetFloat("YSpeed", LandSpeed);
            if (LandSpeed <= 0.5f)
            {
                LandSpeed = 0;
                animator.SetBool("IsJump", false);
            }
            CurrentMovement.y = GroundGravity;
            CurrentRunMovement.y = GroundGravity;
        }
        else
        {
            float previewVelocity = CurrentMovement.y;
            float newVelocity = CurrentMovement.y + gravity * multiplier * Time.deltaTime;
            CurrentMovement.y = Mathf.Max((previewVelocity + newVelocity) * 0.5f, -20f);
            CurrentRunMovement.y = (previewVelocity + newVelocity) * 0.5f;
        }
    }
    void HandleJump()
    {
        /*The requirement of Jump:
            1.Ground
            2.PressJump
            3.Not In Air
        */
        if (!IsJumping && IsGround && IsJumpPressed)
        {
            
            IsJumping = true;
            animator.SetBool("IsJump", IsJumpPressed);
            animator.SetFloat("LeftOrRight", LeftOrRight);
            animator.SetFloat("YSpeed", LandSpeed);
            LandSpeed = 4f;
            CurrentMovement.y = InitialJumpVelocity;
            CurrentRunMovement.y = InitialJumpVelocity;
        }
        else if (!IsJumpPressed && IsJumping && IsGround)
        {
            IsJumping = false;
        }
    }
    public bool IsMove = false;
    void HandleMove()
    {

        if (MoveInput != Vector2.zero)
        {
            IsMove = true;
            //If input isn't zero,move
            animator.SetBool("IsMove", IsMove);
            if (IsAim)
            //Switch walk pose while aiming
            {
                animator.SetBool("IsWalk", true);
                animator.SetFloat("HorizontalSpeed", MoveInput.x * 2);
                animator.SetFloat("VerticalSpeed", MoveInput.y * 2);
                //Get forward
                Vector3 forward = cameratransform.forward;
                forward.y = 0; // y轴设为0，保持在地面上
                forward = forward.normalized; // 向量归一化
                //Get Right
                Vector3 right = new Vector3(forward.z, 0, -forward.x);
                Vector3 Direction = MoveInput.x * right + MoveInput.y * forward;
                CurrentMovement = Direction * WalkSpeed;
            }
            //Switch run pose
            else
            {
                animator.SetBool("IsWalk", false);
                animator.SetFloat("VerticalSpeed", 4f, 0.1f, Time.deltaTime);
                CurrentMovement.x = Mathf.Lerp(CurrentMovement.x, RunSpeed * playertransform.forward.x, 0.5f);
                CurrentMovement.z = Mathf.Lerp(CurrentMovement.z, RunSpeed * playertransform.forward.z, 0.5f);
            }
        }
        else
        {
            IsMove = false;
            animator.SetFloat("VerticalSpeed", 0, 0.1f, Time.deltaTime);
            animator.SetFloat("HorizontalSpeed", 0, 0.1f, Time.deltaTime);
            animator.SetBool("IsMove", IsMove);
            CurrentMovement.x = 0;
            CurrentMovement.z = 0;
        }
    }

    void HandleAim()
    {
        if (IsAim)
        {
            //处理动画
            Aimcamera.gameObject.SetActive(true);
            animator.SetBool("IsAim", true);
            //确定鼠标位置
            Vector3 WorldAimTarget = MousePosition;
            WorldAimTarget.y = playertransform.position.y;
            AimTagert.transform.position = MousePosition;
            netAimPos.Value = AimTagert.transform.position;
            Vector3 CharacterFace = (WorldAimTarget - playertransform.position).normalized;
            if (CharacterFace.magnitude > 0.01f)
            {
                playertransform.forward = Vector3.Lerp(playertransform.forward, CharacterFace, Time.deltaTime * 20f);
            }
            rigBuilder.enabled = true;
            RigActive.Value = true;
        }
        else
        {
            animator.SetBool("IsAim", false);
            Aimcamera.gameObject.SetActive(false);
            rigBuilder.enabled = false;
            RigActive.Value = false;
        }
    }
    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
    void CameraMovement()
    {
        CameraLookAt.transform.forward = Camera.main.transform.forward;
        if (MoveInput != Vector2.zero && !IsAim)
        {
            float InitialAngle = cameratransform.transform.eulerAngles.y;
            float targetRotation = Mathf.Atan2(MoveInput.x, MoveInput.y) * Mathf.Rad2Deg + InitialAngle;
            float rotation = Mathf.SmoothDampAngle(playertransform.eulerAngles.y, targetRotation, ref _rotationVelocity,
                RotationSmoothTime);
            playertransform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
        else if (IsAim && AxisValue.sqrMagnitude > 0.01f)
        {
            cinemachineTargetX = CameraLookAt.transform.eulerAngles.y;
            cinemachineTargetX += AxisValue.x * Sensitivity * Time.deltaTime;
            cinemachineTargetY = CameraLookAt.transform.eulerAngles.x;
            cinemachineTargetY += -1f * AxisValue.y * Sensitivity * Time.deltaTime;
            CameraLookAt.transform.rotation = Quaternion.Euler(cinemachineTargetY, cinemachineTargetX, 0.0f);
        }
    }
    void RayCheck()
    {
        Vector2 ScreenCentre = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(ScreenCentre);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimColliderLayer))
        {
            MousePosition = raycastHit.point;
        }
    }
    void HandleFire()
    {
        if (IsFire && !IsFiring)
        {
            animator.SetBool("IsShoot", true); // 设置 IsShoot 为真
            Shoot.Instance.ShootBullet(AimTagert.transform.position);
            IsFiring = true;

        }
        else if (!IsFire && IsFiring) // 如果 IsFire 为假
        {
            animator.SetBool("IsShoot", false); // 设置 IsShoot 为假
            IsFiring = false;
        }
    }
    //Set Callback
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
    public void SetStopToFalse()
    {

    }
    public void OnLand()
    {

    }
}
