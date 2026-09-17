using UnityEngine;

public class Generator : Interactable
{
    private bool activated = false;

    public override void Interact()
    {
        if (activated)
        {
            Debug.Log("Generator is already running.");
            return;
        }

        if (GameManager.instance.powerNodesCollected >= 3)
        {
            activated = true;

            Debug.Log("Generator Activated!");

            GameManager.instance.ActivateGenerator();
        }
        else
        {
            Debug.Log("You need 3 Power Nodes.");
        }
    }
}