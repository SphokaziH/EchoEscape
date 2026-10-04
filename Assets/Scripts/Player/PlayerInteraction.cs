using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Interaction UI")]
    public TMP_Text interactionPrompt;

    void Start()
    {
        if (interactionPrompt != null)
            interactionPrompt.gameObject.SetActive(false);
    }

    void Update()
    {
        bool lookingAtInteractable = false;

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            Interactable interactable =
                hit.collider.GetComponent<Interactable>();

            if (interactable != null)
            {
                lookingAtInteractable = true;

                if (interactionPrompt != null)
                {
                    interactionPrompt.text =
                        interactable.GetInteractionPrompt();

                    interactionPrompt.gameObject.SetActive(true);
                }

                if (Input.GetKeyDown(interactKey))
                {
                    interactable.Interact();
                }
            }
        }

        if (!lookingAtInteractable && interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }
}