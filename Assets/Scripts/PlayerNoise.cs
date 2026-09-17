using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    [Header("References")]
    public EchoAI echo;
    public PlayerMovement playerMovement;

    [Header("Noise Levels")]
    public float walkingNoise = 10f;
    public float sprintingNoise = 18f;
    public float crouchingNoise = 4f;

    [Header("Noise Timing")]
    public float noiseInterval = 0.5f;

    private Vector3 lastPosition;
    private float noiseTimer;

    void Start()
    {
        lastPosition = transform.position;

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        float movement =
            Vector3.Distance(
                transform.position,
                lastPosition
            );

        noiseTimer -= Time.deltaTime;

        if (movement > 0.01f &&
            noiseTimer <= 0f)
        {
            MakeNoise();
            noiseTimer = noiseInterval;
        }

        lastPosition = transform.position;
    }

    void MakeNoise()
    {
        if (echo == null ||
            playerMovement == null)
        {
            return;
        }

        float noise;

        switch (playerMovement.state)
        {
            case PlayerMovement.MovementState.crouching:
                noise = crouchingNoise;
                Debug.Log("Player CROUCH noise: " + noise);
                break;

            case PlayerMovement.MovementState.sprinting:
                noise = sprintingNoise;
                Debug.Log("Player SPRINT noise: " + noise);
                break;

            case PlayerMovement.MovementState.walking:
                noise = walkingNoise;
                Debug.Log("Player WALK noise: " + noise);
                break;

            default:
                return;
        }

        echo.HearNoise(
            transform.position,
            noise
        );
    }
}