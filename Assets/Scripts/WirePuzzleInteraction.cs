using UnityEngine;

public class WirePuzzleInteraction : Interactable
{
    [Header("Player")]
    public PlayerMovement playerMovement;
    public MouseLook mouseLook;

    private bool puzzleActive = false;
    private Collider puzzleCollider;

    void Start()
    {
        puzzleCollider = GetComponent<Collider>();
    }

    public override string GetInteractionPrompt()
    {
        if (GameManager.instance == null)
            return "";

        if (GameManager.instance.generatorActivated)
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

        if (GameManager.instance.generatorActivated)
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

        // Disable the large puzzle collider so it does not
        // block the individual wire colliders.
        if (puzzleCollider != null)
            puzzleCollider.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Wire puzzle started.");
    }

    public void PuzzleComplete()
    {
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

        // Turn the main puzzle collider back on.
        if (puzzleCollider != null)
            puzzleCollider.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}