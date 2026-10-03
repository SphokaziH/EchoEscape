using UnityEngine;
using TMPro;
using System.Collections;

public class PowerNodeHUD : MonoBehaviour
{
    [Header("Node Indicators")]
    public TMP_Text node1;
    public TMP_Text node2;
    public TMP_Text node3;

    [Header("Pickup Notification")]
    public TMP_Text notificationText;
    public float notificationDuration = 2f;

    [Header("Colours")]
    public Color emptyColour = new Color(0.39f, 0.45f, 0.45f);
    public Color collectedColour = new Color(0.18f, 0.85f, 0.78f);

    private int previousNodeCount = -1;
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

        int nodes = GameManager.instance.powerNodesCollected;

        UpdateNode(node1, nodes >= 1);
        UpdateNode(node2, nodes >= 2);
        UpdateNode(node3, nodes >= 3);

        if (previousNodeCount == -1)
        {
            previousNodeCount = nodes;
            return;
        }

        if (nodes > previousNodeCount)
        {
            ShowNotification(nodes);
        }

        previousNodeCount = nodes;
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

    void ShowNotification(int nodes)
    {
        if (notificationText == null)
            return;

        if (notificationCoroutine != null)
            StopCoroutine(notificationCoroutine);

        notificationCoroutine = StartCoroutine(
            NotificationRoutine(nodes)
        );
    }

    IEnumerator NotificationRoutine(int nodes)
    {
        notificationText.text =
            "POWER NODE ACQUIRED\n" +
            nodes + " / " + GameManager.instance.totalPowerNodes;

        notificationText.gameObject.SetActive(true);

        yield return new WaitForSeconds(notificationDuration);

        notificationText.gameObject.SetActive(false);

        notificationCoroutine = null;
    }
}