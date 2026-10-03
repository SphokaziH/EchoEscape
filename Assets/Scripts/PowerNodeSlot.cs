using UnityEngine;

public class PowerNodeSlot : Interactable
{
    [Header("Slot")]
    public GameObject installedNodeVisual;

    private bool nodeInstalled = false;

    public override void Interact()
    {
        if (nodeInstalled)
        {
            Debug.Log("A Power Node is already installed in this slot.");
            return;
        }

        if (GameManager.instance == null)
            return;

        if (!GameManager.instance.HasPowerNode())
        {
            Debug.Log("You need a Power Node.");
            return;
        }

        GameManager.instance.InstallPowerNode();

        nodeInstalled = true;

        if (installedNodeVisual != null)
            installedNodeVisual.SetActive(true);

        Debug.Log("Power Node installed.");
    }
}