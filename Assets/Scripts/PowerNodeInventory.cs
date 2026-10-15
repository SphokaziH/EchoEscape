using UnityEngine;

public class PowerNodeInventory : MonoBehaviour
{
    public int powerNodes = 0;
    public int maxPowerNodes = 3;

    public void AddPowerNode()
    {
        if (powerNodes >= maxPowerNodes)
            return;

        powerNodes++;

        Debug.Log("Power Node collected: " + powerNodes + " / " + maxPowerNodes);
    }

    public bool HasPowerNode()
    {
        return powerNodes > 0;
    }

    public void UsePowerNode()
    {
        if (powerNodes > 0)
        {
            powerNodes--;
        }
    }
}