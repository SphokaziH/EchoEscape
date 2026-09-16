using UnityEngine;

public class Generator : MonoBehaviour
{
    void Update()
    {
        if (GameManager.instance.generatorActivated)
        {
            Debug.Log("Generator ON");
        }
    }
}