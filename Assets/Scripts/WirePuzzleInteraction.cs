using UnityEngine;

public class WirePuzzleInteraction : Interactable
{
    [Header("Player")]
    public PlayerMovement playerMovement;
    public MouseLook mouseLook;

    private bool puzzleActive = false;
    private bool puzzleCompleted = false;

    private Collider puzzleCollider;

    void Start()
    {
        puzzleCollider = GetComponent<Collider>();
    }

    public override string GetInteractionPrompt()
    {
        if (GameManager.instance == null)
            return "";

        if (puzzleCompleted || GameManager.instance.generatorActivated)
            return "";

        if (GameManager.instance.powerNodesInstalled <
            GameManager.instance.totalPowerNodes)
        {
            return "INSTALL ALL POWER NODES FIRST";
        }

        return "[E] REPAIR CIRCUIT";
    }

    public override void Interact()
    {
        if (GameManager.instance == null)
            return;

        if (puzzleCompleted || GameManager.instance.generatorActivated)
            return;

        if (GameManager.instance.powerNodesInstalled <
            GameManager.instance.totalPowerNodes)
            return;

        EnterPuzzle();
    }

    void EnterPuzzle()
    {
        if (puzzleActive)
            return;

        puzzleActive = true;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (mouseLook != null)
            mouseLook.enabled = false;

        if (puzzleCollider != null)
            puzzleCollider.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Wire puzzle started.");
    }

    public void PuzzleComplete()
    {
        if (puzzleCompleted)
            return;

        puzzleCompleted = true;

        Debug.Log("Wire puzzle complete!");

        if (GameManager.instance != null)
            GameManager.instance.ActivateGenerator();

        FacilityPower.Restore();

        ExitPuzzle();
    }

    void ExitPuzzle()
    {
        puzzleActive = false;

        if (playerMovement != null)
            playerMovement.enabled = true;

        if (mouseLook != null)
            mouseLook.enabled = true;

        if (puzzleCollider != null)
            puzzleCollider.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Returned to normal gameplay.");
    }
}