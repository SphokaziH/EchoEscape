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

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public PlayerMovement playerMovement;

    private bool gameOver = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Hide Game Over screen when the game starts
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
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