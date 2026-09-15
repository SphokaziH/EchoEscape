using UnityEngine;
using UnityEngine.Events;

public class buttonScript : MonoBehaviour
{
    public int keypadNumber = 1;

    public UnityEvent KeypadClicked;

    private void OnMouseDown()
    {
        KeypadClicked.Invoke();
    }
}
