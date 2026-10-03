using UnityEngine;

public class Main : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int lightCount;
    private int onCount = 0;
    static public Main Instance;
    [SerializeField] private DoorController elevatorDoorController;
    void Awake()
    {
        Instance = this;
    }
    public void LightChange(int points)
    {
        onCount= onCount+ points;
        if (onCount == lightCount)
        {
            Debug.Log($"Power restored: ");
            // Call method to change lights
            //???
            // open door elevator door
            if (elevatorDoorController != null)
            {
                elevatorDoorController.OpenDoor();
            }
            else
            {
                Debug.LogError("Door Controller has not been assigned!");
            }

        }
    }
}
