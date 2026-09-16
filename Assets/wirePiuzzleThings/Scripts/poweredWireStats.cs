using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum Color { blue, green, red , yellow};
public class PoweredWireStats : MonoBehaviour {

    public bool movable = false; 
    public bool moving = false;
    public Color wireColor;
    public Vector3 startPos;
    public bool connected = false;
    public Vector3 connectedPosition;

    void Start()
    {
        startPos = transform.position;
    }
}
