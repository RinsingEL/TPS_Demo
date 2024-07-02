using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class RootMotion : MonoBehaviour
{
    //Input
    Vector2 MoveInput;
    //Move
    private float CurrentSpeed;
    public float RunSpeed = 4f;
    public float WalkSpeed = 1.5f;
    public float AirPose = 1f;
    public float LocoPose = 2f;
    bool IsRunning = false;
    //CameraRotation
    Transform CameraTransform;
    Transform playertransform;
    float _rotationVelocity;
    float RotationSmoothTime = 0.12f;
    //Jump
    bool IsJumpPressed = false;
    public float VerticalSpeed;
    //Jump variables
    float MaxJumpTime = 1f;
    float gravity;
    float MaxJumpHeight = 3f;
    float InitialJumpVelocity;
    //CheckGround
    bool IsGround;
    float groundoffset = 0.5f;
    bool IsJumping = false;
    Vector3 currentMovement = Vector3.zero;
    //Aim Variables
    bool IsAim = false;
    [SerializeField]private CinemachineVirtualCamera Aimcamera;
    Vector2 AxisValue;
    [Range(1f, 10f)]public float Sensitivity;
    float TurnSpeed=0f;

    Animator animator;
    CharacterController characterController;
    CharacterControl inputActions;
    //denoising
    int CacheIndex = 0;
    static int Cache_Max = 3;//define constant variable 
    Vector3[] Vel_Cache = new Vector3[Cache_Max];
    Vector3 GroundVelocity;

    private void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    void Start()
    {
        CameraTransform = Camera.main.transform;
        playertransform = GetComponent<Transform>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        //Handle Input
        inputActions = new CharacterControl();
        inputActions.Player.Enable();
        inputActions.Player.Move.started += OnMove;
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Jump.started += OnJump;
        inputActions.Player.Jump.canceled += OnJump;
        inputActions.Player.Aim.started += OnAim;
        inputActions.Player.Aim.canceled += OnAim;
        inputActions.Player.Look.started += OnLook;
        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.canceled += OnLook;
        SetJumpVariables();
    }
    void SetJumpVariables()
    {
        //refer physical formula
        float timeToApex = MaxJumpTime / 2;
        gravity = (-2 * MaxJumpHeight) / Mathf.Pow(timeToApex, 2);
        InitialJumpVelocity = (2 * MaxJumpHeight) / timeToApex;
    }
    void Update()
    {
        CheckGround();
        MoveCameraRotation();
        CharactorJumpMove();
        HandleMove();
        Gravity();
        HandleJump();
        HandleAim();
    }
    void CharactorJumpMove()
    {

        if (characterController.isGrounded)
        {
            characterController.Move(currentMovement * Time.deltaTime);
            GroundVelocity = Average(animator.velocity);
        }
        else
        {
            GroundVelocity.y = currentMovement.y;
            if (MoveInput != Vector2.zero)
                characterController.Move(GroundVelocity * Time.deltaTime);
            else
                characterController.Move(currentMovement * Time.deltaTime);
        }

    }
    void CheckGround()
    {
        if (Physics.SphereCast(playertransform.position + (Vector3.up * groundoffset), characterController.radius, Vector3.down, out RaycastHit hit, groundoffset - characterController.radius + 2 * characterController.skinWidth))
            IsGround = true;
        else
            IsGround = false;
    }
    Vector3 Average(Vector3 Vel)
    {
        Vector3 result = Vector3.zero;
        Vector2 GroundSpeed = new(Vel.x, Vel.z);
        if (GroundSpeed.magnitude > 1)
            Vel_Cache[CacheIndex] = Vel;
        CacheIndex++;
        CacheIndex %= Cache_Max;
        foreach (Vector3 item in Vel_Cache)
        {
            result += item;
        };
        return result / (Cache_Max + 1);
    }
    void Gravity()
    {
        bool IsFalling = currentMovement.y <= 0;
        float multiplier;
        if (IsFalling || !IsJumpPressed)
        {
            multiplier = 3.0f;
        }
        else
        {
            animator.SetFloat("Pose", AirPose, 0.1f, Time.deltaTime);
            multiplier = 1.0f;
        }
        if (characterController.isGrounded)
        {
            //Character controller property will be false if there is no downward speed,so we set a downward speed
            animator.SetFloat("Pose", LocoPose, 0.1f, Time.deltaTime);
            currentMovement.y = -0.5f;//Ground down speed
        }
        else
        {
            float previewVelocity = currentMovement.y;
            float newVelocity = currentMovement.y + gravity * multiplier * Time.deltaTime;
            currentMovement.y = Mathf.Max((previewVelocity + newVelocity) * 0.5f, -20f);
        }
    }
    void HandleMove()
    {

        if (MoveInput == Vector2.zero)
        {
            if (IsRunning)
            {
                animator.SetBool("Stop", true);
                IsRunning = false;
            }
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0f, 0.5f);
            if (CurrentSpeed < 0.01f)
                CurrentSpeed = 0f;
            TurnSpeed = 0f;
            animator.SetFloat("TurnSpeed", TurnSpeed, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveSpeed", CurrentSpeed);

        }
        else if (MoveInput != Vector2.zero)
        {
            IsRunning = true;
            CurrentSpeed = RunSpeed;
            if (IsAim)
            {
                IsRunning=false;
                CurrentSpeed = WalkSpeed*MoveInput.magnitude;
                TurnSpeed = MoveInput.x*1.5f;
            }
            animator.SetFloat("TurnSpeed", TurnSpeed, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveSpeed", CurrentSpeed, 0.1f, Time.deltaTime);
        }

    }
    void HandleJump()
    {

        if (IsJumpPressed && characterController.isGrounded && !IsJumping)
        {
            IsJumping = true;

            animator.SetFloat("VerticalHeight", 1f);
            currentMovement.y = InitialJumpVelocity;
        }
        else if (IsJumping && !IsJumpPressed && IsGround)
        {
            IsJumping = false;
        }
    }
    void HandleAim()
    {
        if(IsAim)
            Aimcamera.gameObject.SetActive(true);
        else
            Aimcamera.gameObject.SetActive(false);
            
    }
    void MoveCameraRotation()
    {
        if (MoveInput != Vector2.zero&&!IsAim)
        {
            //Camera Initial Angle
            float InitialAngle = CameraTransform.transform.eulerAngles.y;
            float targetRotation = Mathf.Atan2(MoveInput.x, MoveInput.y) * Mathf.Rad2Deg + InitialAngle;
            float rotation = Mathf.SmoothDampAngle(playertransform.eulerAngles.y, targetRotation, ref _rotationVelocity, RotationSmoothTime);
            playertransform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
        else if (IsAim)
        {
            float XInitialAngle = CameraTransform.transform.eulerAngles.y;
            float XtargetRotation = AxisValue.x * Sensitivity + XInitialAngle;
            float rotation = Mathf.SmoothDampAngle(playertransform.eulerAngles.y, XtargetRotation, ref _rotationVelocity, RotationSmoothTime);
            playertransform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
    }
    void SetStopToFalse()
    {
        animator.SetBool("Stop", false);
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext ctx)
    {
        IsJumpPressed = ctx.ReadValueAsButton();
    }
    public void OnAim(InputAction.CallbackContext ctx)
    {
        IsAim = ctx.ReadValueAsButton();
    }
    public void OnLook(InputAction.CallbackContext ctx)
    {
        AxisValue = ctx.ReadValue<Vector2>();
    }
}
