using UnityEngine;

public class SlidePuzzleInteraction : Interactable
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
        return "[E] REPAIR CIRCUIT";
    }

    public override void Interact()
    {
        
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

        Debug.Log("Slide puzzle started.");
    }

    public void PuzzleComplete()
    {
        Debug.Log("Slide puzzle complete!");
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
