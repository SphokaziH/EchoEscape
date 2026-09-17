using UnityEngine;

public class PlayerInteraction : MonoBehaviour{
    public float interactRange = 3f; //this is how far the raycast checks
    public KeyCode interactKey = KeyCode.E;
    void Update(){
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit; // stores info about what the ray has hit

        if (Physics.Raycast(ray, out hit, interactRange)){

            // check if what we hit has the Interactable tag
            if (hit.collider.CompareTag("Interactable")){
                Debug.Log("Looking at: " + hit.collider.name);

                if (Input.GetKey(interactKey)){
                    Debug.Log("Interacted with: " + hit.collider.name);
                }
            }
        }
    }
}