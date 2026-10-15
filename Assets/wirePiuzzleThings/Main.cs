using UnityEngine;

public class Main : MonoBehaviour
{
    public int lightCount = 4;

    public WirePuzzleInteraction wirePuzzleInteraction;

    private int onCount = 0;

    public static Main Instance;

    private bool puzzleCompleted = false;

    void Awake()
    {
        Instance = this;
    }

    public void LightChange(int points)
    {
        if (puzzleCompleted)
            return;

        onCount += points;

        Debug.Log(
            "Generator circuit progress: " +
            onCount +
            "/" +
            lightCount
        );

        if (onCount >= lightCount)
        {
            CompletePuzzle();
        }
    }

    void CompletePuzzle()
    {
        if (puzzleCompleted)
            return;

        if (GameManager.instance == null)
        {
            Debug.LogError("GameManager could not be found.");
            return;
        }

        if (GameManager.instance.powerNodesInstalled <
            GameManager.instance.totalPowerNodes)
        {
            Debug.Log(
                "Generator cannot activate. Install all Power Nodes first."
            );

            return;
        }

        puzzleCompleted = true;

        if (wirePuzzleInteraction != null)
        {
            wirePuzzleInteraction.PuzzleComplete();
        }
        else
        {
            Debug.LogError("WirePuzzleInteraction is not assigned.");
        }

        Debug.Log(
            "Generator circuit repaired. Facility power restored."
        );
    }
}