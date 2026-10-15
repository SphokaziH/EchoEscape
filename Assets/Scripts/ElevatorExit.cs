using UnityEngine;

public class ElevatorExit : Interactable
{
    public GameObject winScreen;

    public PlayerMovement playerMovement;
    public MouseLook mouseLook;

    private bool gameWon = false;

    public override string GetInteractionPrompt()
    {
        if (gameWon)
            return "";

        if (GameManager.instance == null)
            return "";

        if (!GameManager.instance.generatorActivated)
            return "RESTORE POWER TO USE ELEVATOR";

        return "[E] ESCAPE";
    }

    public override void Interact()
    {
        if (gameWon)
            return;

        if (GameManager.instance == null)
            return;

        if (!GameManager.instance.generatorActivated)
            return;

        WinGame();
    }

    void WinGame()
    {
        gameWon = true;

        Debug.Log("YOU ESCAPED!");

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (mouseLook != null)
            mouseLook.enabled = false;

        if (winScreen != null)
            winScreen.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}