using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Power System")]
    public int powerNodesCollected = 0;
    public int totalPowerNodes = 3;

    public bool generatorActivated = false;
    public bool elevatorUnlocked = false;

    [Header("Containment (win condition)")]
    public bool echoTrapped = false;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public PlayerMovement playerMovement;

    private bool gameOver = false;

    void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        ContainmentUnit.OnEchoTrapped += HandleEchoTrapped;
    }

    void OnDisable()
    {
        ContainmentUnit.OnEchoTrapped -= HandleEchoTrapped;
    }

    void Start()
    {
        // Hide Game Over screen when the game starts
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    // Called when the containment doors have closed with Echo inside.
    void HandleEchoTrapped()
    {
        if (gameOver)
            return;

        echoTrapped = true;
        elevatorUnlocked = true;   // the way out opens once Echo is locked away

        Debug.Log("Echo is trapped in the containment cage. Elevator unlocked - escape the facility!");
    }

    public void CollectPowerNode()
    {
        powerNodesCollected++;

        Debug.Log(
            "Power Nodes: " +
            powerNodesCollected +
            "/" +
            totalPowerNodes
        );

        if (powerNodesCollected >= totalPowerNodes)
        {
            ActivateGenerator();
        }
    }

    public void ActivateGenerator()
    {
        generatorActivated = true;

        Debug.Log("Facility power restored.");
    }

    public void GameOver()
    {
        if (gameOver)
            return;

        gameOver = true;

        Debug.Log("GAME OVER - Echo caught the player.");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}