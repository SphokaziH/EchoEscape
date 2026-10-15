using UnityEngine;
using UnityEngine.InputSystem;

public class NoiseTest : MonoBehaviour
{
    public EchoAI echo;

    public float crouchingNoise = 3f;
    public float walkingNoise = 6f;
    public float doorNoise = 10f;
    public float sprintingNoise = 15f;
    public float alarmNoise = 20f;

    void Update()
    {
        if (Keyboard.current == null || echo == null)
            return;

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            echo.HearNoise(transform.position, crouchingNoise);
            Debug.Log("Crouching noise");
        }

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            echo.HearNoise(transform.position, walkingNoise);
            Debug.Log("Walking noise");
        }

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            echo.HearNoise(transform.position, doorNoise);
            Debug.Log("Door noise");
        }

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            echo.HearNoise(transform.position, sprintingNoise);
            Debug.Log("Sprinting noise");
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            echo.HearNoise(transform.position, alarmNoise);
            Debug.Log("Alarm noise");
        }
    }
}