using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public float speed = 12f;
    public float gravity = -9.8f;
    public float jumpHeight = 3f;

    // Sprinting details
    public float walkSpeed = 7f;
    public float sprintSpeed = 10f;

    // Crouching details
    public float crouchSpeed = 5.6f;
    public float crouchHeight = 1.2f;
    private float standHeight = 2f;

    // Stamina details
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float currentStamina;
    [SerializeField] float staminaDrainRate = 20f;
    [SerializeField] float staminaRegenRate = 15f;

    // Keycodes
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    // Echo noise system
    public EchoAI echoAI;

    public float crouchNoiseRadius = 3f;
    public float walkNoiseRadius = 9f;
    public float sprintNoiseRadius = 20f;
    public float noiseInterval = 0.5f;

    private float noiseTimer;

    Vector3 velocity;
    bool isGrounded;

    public MovementState state;

    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }

    private void Start()
    {
        standHeight = transform.localScale.y;
        currentStamina = maxStamina;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool isMoving =
            Mathf.Abs(x) > 0.1f ||
            Mathf.Abs(z) > 0.1f;

        bool isCrouching =
            Input.GetKey(crouchKey);

        bool isSprinting =
            Input.GetKey(sprintKey) &&
            currentStamina > 0f &&
            isMoving &&
            !isCrouching;

        if (isCrouching)
        {
            state = MovementState.crouching;
            speed = crouchSpeed;
        }
        else if (isSprinting)
        {
            state = MovementState.sprinting;
            speed = sprintSpeed;
        }
        else
        {
            state = MovementState.walking;
            speed = walkSpeed;
        }

        GenerateMovementNoise(
            isMoving,
            isSprinting,
            isCrouching
        );

        Vector3 move =
            transform.right * x +
            transform.forward * z;

        controller.Move(
            move * speed * Time.deltaTime
        );

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y =
                Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        if (isCrouching)
        {
            controller.height = crouchHeight;
            controller.center =
                new Vector3(0, crouchHeight / 2f, 0);
        }
        else
        {
            controller.height = standHeight;
            controller.center =
                new Vector3(0, standHeight / 2f, 0);
        }

        if (isSprinting)
        {
            currentStamina -=
                staminaDrainRate * Time.deltaTime;
        }
        else if (currentStamina < maxStamina)
        {
            currentStamina +=
                staminaRegenRate * Time.deltaTime;
        }

        currentStamina =
            Mathf.Clamp(
                currentStamina,
                0f,
                maxStamina
            );

        velocity.y += gravity * Time.deltaTime;

        controller.Move(
            velocity * Time.deltaTime
        );
    }

    private void GenerateMovementNoise(
        bool isMoving,
        bool isSprinting,
        bool isCrouching)
    {
        if (!isMoving || echoAI == null)
        {
            noiseTimer = 0f;
            return;
        }

        noiseTimer -= Time.deltaTime;

        if (noiseTimer > 0f)
            return;

        float noiseRadius;

        if (isCrouching)
        {
            noiseRadius = crouchNoiseRadius;
        }
        else if (isSprinting)
        {
            noiseRadius = sprintNoiseRadius;
        }
        else
        {
            noiseRadius = walkNoiseRadius;
        }

        echoAI.HearNoise(
            transform.position,
            noiseRadius
        );

        noiseTimer = noiseInterval;
    }
}