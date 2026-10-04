using UnityEngine;

public class PlayerMovement : MonoBehaviour{
    public CharacterController controller;
    private Animator animator;  // creates a slot to hold the player's animator

    [Header("Movement")]
    public float speed = 7f;
    public float gravity = -9.8f;
    public float jumpHeight = 3f;

    [Header("Sprinting")]
    public float walkSpeed = 7f;
    public float sprintSpeed = 10f;

    [Header("Crouching")]
    public float crouchSpeed = 5.6f;
    public float crouchHeight = 1.2f;
    private float standHeight = 2f;

    [Header("Stamina")]
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float currentStamina;
    [SerializeField] float staminaDrainRate = 20f;
    [SerializeField] float staminaRegenRate = 15f;

    [Header("Keys")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private Vector3 velocity;
    private bool isGrounded;

    public MovementState state;

    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }

    void Start(){
        if (controller == null)
            controller = GetComponent<CharacterController>();

        standHeight = controller.height;
        currentStamina = maxStamina;

        animator = GetComponentInChildren<Animator>(); //fills the slot only once, when the game starts
    }

    void Update(){
        //Debug.Log(controller.name);
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        animator.SetBool("isGrounded", isGrounded);

        if(isGrounded && velocity.y < 0){
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool isMoving = Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f;

        StateHandler(isMoving);

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);

        //keeps track of how fast the player is moving along the ground
        Vector3 flatVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        animator.SetFloat("Speed", flatVelocity.magnitude, 0.1f, Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded){
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        bool isCrouching = Input.GetKey(crouchKey);
        animator.SetBool("isCrouching", isCrouching); //switches the animator between standing and crouching animations

        if (isCrouching){
            controller.height = crouchHeight;
            controller.center = new Vector3(0, crouchHeight / 2f, 0);
        }
        else{
            controller.height = standHeight;
            controller.center = new Vector3(0, standHeight / 2f, 0);
        }

        if (state == MovementState.sprinting){
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else if (currentStamina < maxStamina){
            currentStamina += staminaRegenRate * Time.deltaTime;
        }

        currentStamina =Mathf.Clamp(currentStamina, 0f, maxStamina);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void StateHandler(bool isMoving){
        if (Input.GetKey(crouchKey)){
            controller.height = crouchHeight;
            state = MovementState.crouching;
            speed = crouchSpeed;
        }
        else if (isGrounded && Input.GetKey(sprintKey) && currentStamina > 0f && isMoving){
            state = MovementState.sprinting;
            speed = sprintSpeed;
        }
        else if (isGrounded){
            state = MovementState.walking;
            speed = walkSpeed;
        }
        else{
            state = MovementState.air;
            speed = walkSpeed;
        }
    }
}