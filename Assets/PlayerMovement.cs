using UnityEngine;

public class PlayerMovement : MonoBehaviour{
    public CharacterController controller;
    public float speed = 12f; //controlls the speed of the character's movement
    public float gravity = -9.8f;
    public float jumpHeight = 3f;

    // Sprinting details
    public float walkSpeed = 7f;
    public float sprintSpeed = 10f;

    // crouching details
    public float crouchSpeed = 5.6f;
    public float crouchHeight = 1.2f;
    private float standHeight = 2f;

    // stamina details
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float currentStamina;
    [SerializeField] float staminaDrainRate = 20f; // per second
    [SerializeField] float staminaRegenRate = 15f; //per second

    // keycodes
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    public Transform groundCheck;
    public float groundDistance = 0.4f; // radius of the sphere that is used to check
    public LayerMask groundMask;

    Vector3 velocity;   //stores the player's current velocity
    bool isGrounded; // returns true if the player's on the ground and false if otherwise

    public MovementState state; // stores the current state the player is in
    /* Player movement states **/
    public enum MovementState { 
        walking,
        sprinting,
        crouching,
        air
    }

    private void StateHandler(){

        // crouching mode
        if (Input.GetKey(crouchKey)){
            state = MovementState.crouching;
            speed = crouchSpeed;
        }
        // sprinting mode
        if (isGrounded && Input.GetKey(sprintKey)){
            state = MovementState.sprinting;
            speed = sprintSpeed;
        }

        // walking mode
        else if (isGrounded){
            state = MovementState.walking;
            speed = walkSpeed;
        }

        // air mode
        else{
            state = MovementState.air;
        }
    }

    private void Start(){
        standHeight = transform.localScale.y;
    }

    // Update is called once per frame
    void Update(){
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        StateHandler();

        /*
            resetting the velocity when the player's on the ground
         */
        if(isGrounded && velocity.y < 0){
            velocity.y = -2f;
        }
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
        bool isSprinting = Input.GetKey(sprintKey) && currentStamina > 0 && isMoving;

        // restricts the player to only move in the direction it is pointing at
        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded) {
            velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
        }

        // start to crouch
        if (Input.GetKey(crouchKey)){
            controller.height = crouchHeight;
            controller.center = new Vector3(0, crouchHeight / 2f, 0);
        }
        else{ // stop couching
            controller.height = standHeight;
            controller.center = new Vector3(0, standHeight / 2f, 0);
        }

        // stamina
        if(isSprinting){
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else if(currentStamina < maxStamina){
                currentStamina += staminaRegenRate * Time.deltaTime;
        }
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        speed = isSprinting ? sprintSpeed : walkSpeed;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

    }

}
