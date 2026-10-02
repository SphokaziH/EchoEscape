using UnityEngine;

public class DoorController : MonoBehaviour
{
    public enum OpenAxis
    {
        X,
        Z
    }

    [Header("Door")]
    public Transform movingDoor;
    public OpenAxis openAxis = OpenAxis.Z;

    [Header("Movement")]
    public float openDistance = -5f;
    public float moveDuration = 2f;

    [Header("Interaction")]
    public Transform player;
    public float interactionDistance = 5f;

    [Header("Door Audio")]
    public AudioSource audioSource;
    public AudioClip openingSound;
    public AudioClip closingSound;

    [Header("Echo Noise")]
    public EchoAI echo;
    public float doorNoiseRadius = 12f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Vector3 targetPosition;

    private bool isOpen = false;
    private bool isMoving = false;

    void Start()
    {
        if (movingDoor == null)
        {
            Debug.LogError(
                "Moving Door has not been assigned on " + gameObject.name
            );

            return;
        }

        closedPosition = movingDoor.position;
        openPosition = closedPosition;

        if (openAxis == OpenAxis.X)
        {
            openPosition.x += openDistance;
        }
        else
        {
            openPosition.z += openDistance;
        }

        targetPosition = closedPosition;
    }

    void Update()
    {
        if (movingDoor == null || player == null)
            return;

        float distanceToPlayer = Vector3.Distance(
            player.position,
            movingDoor.position
        );

        if (distanceToPlayer <= interactionDistance &&
            Input.GetKeyDown(KeyCode.E))
        {
            ToggleDoor();
        }

        if (isMoving)
        {
            MoveDoor();
        }
    }

    void ToggleDoor()
    {
        if (isOpen)
        {
            targetPosition = closedPosition;
            isOpen = false;

            PlayDoorSound(closingSound);
        }
        else
        {
            targetPosition = openPosition;
            isOpen = true;

            PlayDoorSound(openingSound);
        }

        MakeDoorNoise();

        isMoving = true;
    }

    void MoveDoor()
    {
        float totalDistance = Vector3.Distance(
            closedPosition,
            openPosition
        );

        if (totalDistance <= 0f)
        {
            isMoving = false;
            return;
        }

        float speed = totalDistance / moveDuration;

        movingDoor.position = Vector3.MoveTowards(
            movingDoor.position,
            targetPosition,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(
            movingDoor.position,
            targetPosition
        ) < 0.001f)
        {
            movingDoor.position = targetPosition;
            isMoving = false;
        }
    }

    void PlayDoorSound(AudioClip sound)
    {
        if (audioSource == null || sound == null)
            return;

        audioSource.Stop();
        audioSource.clip = sound;
        audioSource.Play();
    }

    void MakeDoorNoise()
    {
        if (echo == null)
            return;

        echo.HearNoise(
            movingDoor.position,
            doorNoiseRadius
        );
    }
}