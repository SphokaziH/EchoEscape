using UnityEngine;
using TMPro;
using System.Collections;

public class PowerNodeHUD : MonoBehaviour
{
    [Header("Node Indicators")]
    public TMP_Text node1;
    public TMP_Text node2;
    public TMP_Text node3;

    [Header("Installed Counter")]
    public TMP_Text installedText;

    [Header("Notification")]
    public TMP_Text notificationText;
    public float notificationDuration = 2f;

    [Header("Colours")]
    public Color emptyColour = new Color(0.39f, 0.45f, 0.45f);
    public Color collectedColour = new Color(0.18f, 0.85f, 0.78f);

    private int previousCollected = -1;
    private int previousInstalled = -1;

    private Coroutine notificationCoroutine;

    void Start()
    {
        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (GameManager.instance == null)
            return;

        int collected = GameManager.instance.powerNodesCollected;
        int installed = GameManager.instance.powerNodesInstalled;
        int total = GameManager.instance.totalPowerNodes;

        // Update carried Power Node indicators
        UpdateNode(node1, collected >= 1);
        UpdateNode(node2, collected >= 2);
        UpdateNode(node3, collected >= 3);

        // Update installed counter
        if (installedText != null)
        {
            installedText.text =
                "INSTALLED  " + installed + " / " + total;
        }

        // Prevent popups when the scene first loads
        if (previousCollected == -1)
        {
            previousCollected = collected;
            previousInstalled = installed;
            return;
        }

        // A Power Node was collected
        if (collected > previousCollected)
        {
            ShowNotification(
                "POWER NODE ACQUIRED\n" +
                collected + " / " + total
            );
        }

        // A Power Node was installed
        if (installed > previousInstalled)
        {
            if (installed >= total)
            {
                ShowNotification(
                    "POWER NODE INSTALLED\n" +
                    installed + " / " + total +
                    "\n\nPOWER CORE ONLINE\n" +
                    "REPAIR THE GENERATOR CIRCUIT",
                    4f
                );
            }
            else
            {
                ShowNotification(
                    "POWER NODE INSTALLED\n" +
                    installed + " / " + total
                );
            }
        }

        previousCollected = collected;
        previousInstalled = installed;
    }

    void UpdateNode(TMP_Text node, bool collected)
    {
        if (node == null)
            return;

        if (collected)
        {
            node.text = "[■]";
            node.color = collectedColour;
        }
        else
        {
            node.text = "[ ]";
            node.color = emptyColour;
        }
    }

    void ShowNotification(string message)
    {
        ShowNotification(message, notificationDuration);
    }

    void ShowNotification(string message, float duration)
    {
        if (notificationText == null)
            return;

        if (notificationCoroutine != null)
            StopCoroutine(notificationCoroutine);

        notificationCoroutine =
            StartCoroutine(NotificationRoutine(message, duration));
    }

    IEnumerator NotificationRoutine(string message, float duration)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        notificationText.gameObject.SetActive(false);

        notificationCoroutine = null;
    }
}