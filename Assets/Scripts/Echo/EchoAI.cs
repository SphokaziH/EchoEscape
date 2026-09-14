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
    public float detectionRange = 10f;
    public float losePlayerRange = 15f;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float huntSpeed = 5f;

    [Header("Investigation")]
    public float investigationTime = 3f;

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
            agent = GetComponent<NavMeshAgent>();

        GoToNextPatrolPoint();
    }

    void Update()
    {
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

    void Patrol()
    {
        agent.speed = patrolSpeed;

        if (player != null)
        {
            float distanceToPlayer =
                Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange)
            {
                currentState = EchoState.Hunt;
                return;
            }
        }

        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            GoToNextPatrolPoint();
        }
    }

    void Hunt()
    {
        if (player == null)
            return;

        agent.speed = huntSpeed;
        agent.SetDestination(player.position);

        float distanceToPlayer =
            Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > losePlayerRange)
        {
            investigationPosition = player.position;
            investigationTimer = investigationTime;

            currentState = EchoState.Investigate;
            agent.SetDestination(investigationPosition);
        }
    }

    void InvestigateState()
    {
        agent.speed = patrolSpeed;

        if (player != null)
        {
            float distanceToPlayer =
                Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange)
            {
                currentState = EchoState.Hunt;
                return;
            }
        }

        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            investigationTimer -= Time.deltaTime;

            if (investigationTimer <= 0f)
            {
                currentState = EchoState.Patrol;
                GoToNextPatrolPoint();
            }
        }
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        agent.SetDestination(
            patrolPoints[currentPatrolPoint].position);

        currentPatrolPoint++;

        if (currentPatrolPoint >= patrolPoints.Length)
        {
            currentPatrolPoint = 0;
        }
    }

    public void Investigate(Vector3 position)
    {
        investigationPosition = position;
        investigationTimer = investigationTime;

        currentState = EchoState.Investigate;
        agent.SetDestination(investigationPosition);
    }

    void UpdateAnimation()
    {
        if (animator == null || agent == null)
            return;

        bool isWalking = agent.velocity.magnitude > 0.1f;

        animator.SetBool("IsWalking", isWalking);
    }

    void UpdateShader()
    {
        if (echoMaterial == null)
            return;

        switch (currentState)
        {
            case EchoState.Patrol:
                echoMaterial.SetFloat("_GlowStrength", patrolGlow);
                break;

            case EchoState.Investigate:
                echoMaterial.SetFloat("_GlowStrength", investigateGlow);
                break;

            case EchoState.Hunt:
                echoMaterial.SetFloat("_GlowStrength", huntGlow);
                break;
        }
    }
}