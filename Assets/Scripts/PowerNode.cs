using UnityEngine;

public class PowerNode : MonoBehaviour
{
    private bool collected = false;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            Collect();
        }
    }

    void Collect()
    {
        if (collected)
            return;

        collected = true;

        GameManager.instance.CollectPowerNode();

        gameObject.SetActive(false);
    }
}
