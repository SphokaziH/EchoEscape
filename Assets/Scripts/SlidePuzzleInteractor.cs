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


    void Update()
    {
        // Emergency exit from puzzle
        if (puzzleActive && Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Exited slide puzzle using H key.");
            ExitPuzzle();
        }
    }


    public override string GetInteractionPrompt()
    {
        return "[E] SOLVE CIRCUIT";
    }


    public override void Interact()
    {
        EnterPuzzle();
    }


    private void EnterPuzzle()
    {
        if (puzzleActive)
            return;


        puzzleActive = true;

        Debug.Log("Slide puzzle started.");


        // Disable player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        else
        {
            Debug.LogError("PlayerMovement is not assigned!");
        }


        // Disable camera movement
        if (mouseLook != null)
        {
            mouseLook.enabled = false;
        }
        else
        {
            Debug.LogError("MouseLook is not assigned!");
        }


        // Disable main puzzle collider
        if (puzzleCollider != null)
        {
            puzzleCollider.enabled = false;
        }


        // Unlock cursor for puzzle interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }



    public void PuzzleComplete()
    {
        if (!puzzleActive)
            return;

        Debug.Log("Slide puzzle complete!");

        ExitPuzzle();
    }



    private void ExitPuzzle()
    {
        puzzleActive = false;


        // Restore player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }


        // Restore camera movement
        if (mouseLook != null)
        {
            mouseLook.enabled = true;
        }


        // Restore puzzle collider
        if (puzzleCollider != null)
        {
            puzzleCollider.enabled = true;
        }


        // Lock cursor back to game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        Debug.Log("Returned to player control.");
    }
}