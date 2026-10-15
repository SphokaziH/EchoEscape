
using UnityEngine;
using System.Collections;

public class KeycardInteraction : Interactable
{
    public GameObject keycardPopup;
    public float displayTime = 5f;

    private Coroutine popupRoutine;

    void Start()
    {
        interactionPrompt = "[E] READ KEYCARD";

        if (keycardPopup != null)
            keycardPopup.SetActive(false);
    }

    public override void Interact()
    {
        if (keycardPopup == null)
            return;

        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupRoutine = StartCoroutine(ShowKeycard());
    }

    IEnumerator ShowKeycard()
    {
        keycardPopup.SetActive(true);

        yield return new WaitForSeconds(displayTime);

        keycardPopup.SetActive(false);
        popupRoutine = null;
    }
}
