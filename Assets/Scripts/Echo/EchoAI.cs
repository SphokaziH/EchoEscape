using UnityEngine;
using UnityEngine.AI;

public class EchoAI : MonoBehaviour
{
    public enum EchoState
    {
        Patrol,
        Investigate,
        Hunt
    }

    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public Transform[] patrolPoints;
    public Material echoMaterial;
    public Animator animator;

    [Header("Detection")]
    public float closeRangeDetection = 2.5f;
    public float losePlayerRange = 15f;
    public float catchDistance = 2.5f;

    [Header("Hearing")]
    public float huntNoiseThreshold = 15f;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float huntSpeed = 5f;

    [Header("Investigation")]
    public float investigationTime = 4f;

    [Header("Shader")]
    public float patrolGlow = 0.7f;
    public float investigateGlow = 1.5f;
    public float huntGlow = 3f;

    public EchoState currentState = EchoState.Patrol;

    private int currentPatrolPoint = 0;
    private Vector3 investigationPosition;
    private float investigationTimer;

    void Start()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        GoToNextPatrolPoint();
    }

    void Update()
    {
        CheckCloseRange();

        switch (currentState)
        {
            case EchoState.Patrol:
                Patrol();
                break;

            case EchoState.Investigate:
                InvestigateState();
                break;

            case EchoState.Hunt:
                Hunt();
                break;
        }

        UpdateAnimation();
        UpdateShader();
    }

    void CheckCloseRange()
    {
        if (player == null)
            return;

        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (distanceToPlayer <= closeRangeDetection &&
            currentState != EchoState.Hunt)
        {
            Debug.Log("Player detected at close range -> HUNT");

            StartHunt();
        }
    }

    void Patrol()
    {
        agent.speed = patrolSpeed;

        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            GoToNextPatrolPoint();
        }
    }

    void InvestigateState()
    {
        agent.speed = patrolSpeed;

        if (agent.pathPending)
            return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            investigationTimer -= Time.deltaTime;

            if (investigationTimer <= 0f)
            {
                Debug.Log("Investigation finished -> PATROL");

                currentState = EchoState.Patrol;

                GoToNextPatrolPoint();
            }
        }
    }

    void Hunt()
    {
        if (player == null)
            return;

        agent.speed = huntSpeed;
        agent.SetDestination(player.position);

        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        // Temporary debug so we can see the real distance
        Debug.Log(
            "HUNT distance to player: " +
            distanceToPlayer
        );

        // Echo catches the player
        if (distanceToPlayer <= catchDistance)
        {
            Debug.Log(
                "Echo caught the player -> GAME OVER"
            );

            agent.isStopped = true;

            if (GameManager.instance != null)
            {
                GameManager.instance.GameOver();
            }
            else
            {
                Debug.LogError(
                    "GameManager instance was not found."
                );
            }

            return;
        }

        // Player escapes far enough away
        if (distanceToPlayer > losePlayerRange)
        {
            Debug.Log(
                "Echo lost player -> INVESTIGATE"
            );

            investigationPosition = player.position;
            investigationTimer = investigationTime;

            currentState = EchoState.Investigate;

            agent.speed = patrolSpeed;

            agent.SetDestination(
                investigationPosition
            );
        }
    }

    void StartHunt()
    {
        if (player == null)
            return;

        agent.isStopped = false;

        currentState = EchoState.Hunt;

        agent.speed = huntSpeed;

        agent.SetDestination(
            player.position
        );
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return;
        }

        agent.isStopped = false;
        agent.speed = patrolSpeed;

        agent.SetDestination(
            patrolPoints[currentPatrolPoint].position
        );

        currentPatrolPoint++;

        if (currentPatrolPoint >= patrolPoints.Length)
        {
            currentPatrolPoint = 0;
        }
    }

    public void HearNoise(
        Vector3 position,
        float noiseRadius
    )
    {
        float distanceToNoise =
            Vector3.Distance(
                transform.position,
                position
            );

        // Echo cannot hear noises outside their radius
        if (distanceToNoise > noiseRadius)
        {
            return;
        }

        // Do not interrupt an active hunt
        if (currentState == EchoState.Hunt)
        {
            return;
        }

        // Loud noises immediately cause Hunt
        if (noiseRadius >= huntNoiseThreshold)
        {
            Debug.Log(
                "LOUD NOISE -> HUNT | Distance: " +
                distanceToNoise +
                " | Noise Radius: " +
                noiseRadius
            );

            StartHunt();

            return;
        }

        // Normal noises cause Investigate
        Debug.Log(
            "NOISE -> INVESTIGATE | Distance: " +
            distanceToNoise +
            " | Noise Radius: " +
            noiseRadius
        );

        Investigate(position);
    }

    public void Investigate(Vector3 position)
    {
        if (currentState == EchoState.Hunt)
        {
            return;
        }

        // Remember where the sound happened
        investigationPosition = position;

        // Reset the investigation timer
        investigationTimer = investigationTime;

        currentState = EchoState.Investigate;

        agent.isStopped = false;
        agent.speed = patrolSpeed;

        // Move to the sound location
        agent.SetDestination(
            investigationPosition
        );
    }

    void UpdateAnimation()
    {
        if (animator == null ||
            agent == null)
        {
            return;
        }

        bool isWalking =
            agent.velocity.magnitude > 0.1f;

        animator.SetBool(
            "IsWalking",
            isWalking
        );
    }

    void UpdateShader()
    {
        if (echoMaterial == null)
        {
            return;
        }

        switch (currentState)
        {
            case EchoState.Patrol:

                echoMaterial.SetFloat(
                    "_GlowStrength",
                    patrolGlow
                );

                break;

            case EchoState.Investigate:

                echoMaterial.SetFloat(
                    "_GlowStrength",
                    investigateGlow
                );

                break;

            case EchoState.Hunt:

                echoMaterial.SetFloat(
                    "_GlowStrength",
                    huntGlow
                );

                break;
        }
    }
}