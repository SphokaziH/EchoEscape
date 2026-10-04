using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Interaction")]
    public string interactionPrompt = "[E] INTERACT";

    public virtual void Interact()
    {
        Debug.Log("Interacted with " + gameObject.name);
    }

    public virtual string GetInteractionPrompt()
    {
        return interactionPrompt;
    }
}