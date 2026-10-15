using UnityEngine;

public class KeypadInteraction : Interactable
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
        return "[E] ENTER PASSWORD";
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

        if (puzzleCollider != null)
            puzzleCollider.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Keypad puzzle started.");
    }

    public void PuzzleComplete()
    {
        if (puzzleCompleted)
            return;

        puzzleCompleted = true;

        Debug.Log("Safe Unlocked");

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