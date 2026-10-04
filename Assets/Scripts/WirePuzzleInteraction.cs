using UnityEngine;

public class WirePuzzleInteraction : Interactable
{
    [Header("Player")]
    public PlayerMovement playerMovement;
    public MouseLook mouseLook;

    private bool puzzleActive = false;

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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Wire puzzle started.");
    }
}