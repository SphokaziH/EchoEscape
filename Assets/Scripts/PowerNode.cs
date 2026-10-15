using UnityEngine;

public class PowerNode : Interactable
{
    private bool collected = false;

    public override void Interact()
    {
        if (collected)
            return;

        collected = true;

        GameManager.instance.CollectPowerNode();

        gameObject.SetActive(false);
    }
}