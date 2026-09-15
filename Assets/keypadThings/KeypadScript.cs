using UnityEngine;
using UnityEditor.Events;
using UnityEngine.Events;

public class KeypadScript : MonoBehaviour
{
    public string password = "1234";
    private string input = "";

    public AudioClip clickSound;
    public AudioClip wrongEntry;
    AudioSource audioSource;

    public UnityEvent OpenDoor;

    public void Start()
    {
        input="";
        audioSource = GetComponent<AudioSource>();
    }
    public void ButtonClicked(string num)
    {
        audioSource.PlayOneShot(clickSound);    
        input += num;
        if (input.Length == password.Length)
        {
            if (input == password)
            {
                Debug.Log("Correct Password");
                OpenDoor.Invoke();
            }
            else
            {
                Debug.Log("Incorrect Password");
                audioSource.PlayOneShot(wrongEntry);
                input = "";
            }
        }
    }
}
