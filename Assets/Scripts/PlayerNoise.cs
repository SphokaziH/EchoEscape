using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    public EchoAI echo;

    public float walkingNoise = 6f;
    public float sprintingNoise = 15f;
    public float crouchingNoise = 3f;

    private CharacterController controller;
    private Vector3 lastPosition;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    void Update()
    {
        float movement =
            Vector3.Distance(transform.position, lastPosition);

        if (movement > 0.01f)
        {
            MakeNoise();
        }

        lastPosition = transform.position;
    }


    void MakeNoise()
    {
        float noise = walkingNoise;

        // later we can connect this to your sprint/crouch state

        echo.HearNoise(transform.position, noise);
    }
}
