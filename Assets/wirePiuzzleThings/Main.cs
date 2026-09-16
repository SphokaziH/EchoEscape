using UnityEngine;

public class Main : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int lightCount;
    private int onCount = 0;
    static public Main Instance;

    void Awake()
    {
        Instance = this;
    }
    public void LightChange(int points)
    {
        onCount= onCount+ points;
        if (onCount == lightCount)
        {
            ///open door
        }
    }
}
