using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Power System")]
    public int powerNodesCollected = 0;
    public int totalPowerNodes = 3;
    public int powerNodesInstalled = 0;

    public bool generatorActivated = false;
    public bool elevatorUnlocked = false;

    [Header("Containment Ending")]
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
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void HandleEchoTrapped()
    {
        if (gameOver)
            return;

        echoTrapped = true;

        Debug.Log("Echo has been contained. Containment ending triggered.");
    }

    public void CollectPowerNode()
    {
        if (powerNodesCollected + powerNodesInstalled >= totalPowerNodes)
            return;

        powerNodesCollected++;

        Debug.Log(
            "Power Nodes in Inventory: " +
            powerNodesCollected +
            "/" +
            totalPowerNodes
        );

        if (powerNodesCollected + powerNodesInstalled >= totalPowerNodes)
        {
            Debug.Log("All Power Nodes found. Take them to the generator room.");
        }
    }

    public bool HasPowerNode()
    {
        return powerNodesCollected > 0;
    }

    public void InstallPowerNode()
    {
        if (powerNodesCollected <= 0)
            return;

        if (powerNodesInstalled >= totalPowerNodes)
            return;

        powerNodesCollected--;
        powerNodesInstalled++;

        Debug.Log(
            "Power Nodes Installed: " +
            powerNodesInstalled +
            "/" +
            totalPowerNodes
        );

        if (powerNodesInstalled >= totalPowerNodes)
        {
            Debug.Log("All Power Nodes installed. Generator ready.");
        }
    }

    public void ActivateGenerator()
    {
        if (generatorActivated)
            return;

        if (powerNodesInstalled < totalPowerNodes)
        {
            Debug.Log("Generator cannot start. Power Nodes are missing.");
            return;
        }

        generatorActivated = true;

        FacilityPower.Restore();

        elevatorUnlocked = true;

        Debug.Log(
            "Facility power restored. Emergency elevator and containment systems online."
        );
    }

    public void GameOver()
    {
        if (gameOver)
            return;

        gameOver = true;

        Debug.Log("GAME OVER - Echo caught the player.");

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (playerMovement != null)
            playerMovement.enabled = false;

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