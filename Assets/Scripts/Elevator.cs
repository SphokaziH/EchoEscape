using UnityEngine;

public class Elevator : Interactable
{
    private bool completed = false;

    public override void Interact()
    {
        if (completed)
        {
            Debug.Log("Elevator already used.");
            return;
        }

        if (GameManager.instance.generatorActivated)
        {
            completed = true;

            Debug.Log("Elevator activated. Escape successful!");

            // For prototype:
            EndGame();
        }
        else
        {
            Debug.Log("Generator power required.");
        }
    }

    void EndGame()
    {
        Debug.Log("Prototype Complete!");
    }
}
