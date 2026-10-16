using UnityEngine;
using UnityEngine.Events;

public class KeypadScript : MonoBehaviour
{
    public string password = "1234";
    private string input = "";

    public AudioClip clickSound;
    public AudioClip wrongEntry;
    public AudioClip doorOpen;
    AudioSource audioSource;
    public Transform SafeDoor;
    public KeypadInteraction keypadInteraction; 
    [Header("Echo Noise")]
    public EchoAI echo;
    public float objectNoiseRadius = 20f;

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
                if (keypadInteraction != null)
                {
                    keypadInteraction.PuzzleComplete();
                }
                else
                {
                    Debug.LogError("KeypadInteraction is not assigned.");
                }
                Debug.Log("Correct Password");
                SafeDoor.Rotate(Vector3.up * 120f);
                audioSource.PlayOneShot(doorOpen);
            }
            else
            {
                Debug.Log("Incorrect Password");
                audioSource.PlayOneShot(wrongEntry);
                input = "";
                // Notify Echo once per throw rather than calling it every frame.
                if (echo != null)
                    echo.HearNoise(this.transform.position, objectNoiseRadius);
            }
        }
    }
}
