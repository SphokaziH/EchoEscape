using UnityEngine;

public class CollisionTest : MonoBehaviour
{
    void OnControllerColliderHit(ControllerColliderHit hit){
        Debug.Log("Character Controller hit: " + hit.gameObject.name);
    }
}