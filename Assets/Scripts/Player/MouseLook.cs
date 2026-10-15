using UnityEngine;

public class MouseLook : MonoBehaviour{
    // controls mouse movement/ mouse speed
    public float mouseSensitivity = 100f;
    public Transform playerBody; // makes mouseX and mouseY rotate the entire body of the player
    float xRotation = 0f;

    void Start(){
        // hides and locks the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update(){
        /*
        pre-programmed axis on unity that will change based on our
        mouse movement
        **/
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // ensures to never look behind the player

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        /*
        Making the mouse movement form side to side rotate the entire body of the the player
        **/
        playerBody.Rotate(Vector3.up * mouseX);
    }

}
