using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public int powerNodesCollected = 0;
    public int totalPowerNodes = 3;

    public bool generatorActivated = false;
    public bool elevatorUnlocked = false;

    void Awake()
    {
        instance = this;
    }

    public void CollectPowerNode()
    {
        powerNodesCollected++;

        Debug.Log("Power Nodes: " + powerNodesCollected + "/" + totalPowerNodes);

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
}
