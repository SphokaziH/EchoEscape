using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoweredWireBehaviour : MonoBehaviour
{
    bool mouseDown = false;
    public PoweredWireStats wireStats;
    public LineRenderer line;

    void Start()
    {
        wireStats = gameObject.GetComponent<PoweredWireStats>();
        line = gameObject.GetComponentInParent<LineRenderer>();
    }

    void Update()
    {
        MoveWire();
        line.SetPosition(3, new Vector3( gameObject.transform.localPosition.x -.1f, gameObject.transform.localPosition.y - .1f,0));
        line.SetPosition(2, new Vector3(gameObject.transform.localPosition.x - .4f, gameObject.transform.localPosition.y - .1f,0));

    }
    void OnMouseDown()
    {
        mouseDown = true;
    }

    void OnMouseOver()
    {
        wireStats.movable = true;
    }

    void OnMouseUp()
    {
        mouseDown = false;
        if (!wireStats.connected)
        {
            gameObject.transform.position = wireStats.startPos;
        }
        if (wireStats.connected)
        {
            gameObject.transform.position = wireStats.connectedPosition;
        }
    }
    void OnMouseExit()
    {
        if (!wireStats.moving)
        {
            wireStats.movable = true;
        }
    }

    void MoveWire()
    {
        if (mouseDown && wireStats.movable) {
            wireStats.moving = true;
            float mouseX = Input.mousePosition.x;
            float mouseY = Input.mousePosition.y;
            gameObject.transform.position = Camera.main.ScreenToWorldPoint(
                   new Vector3(mouseX, mouseY, transform.position.z));
            gameObject.transform.position = new Vector3(gameObject.transform.position.x,
                gameObject.transform.position.y, transform.parent.transform.position.z);
        }
        else
        {
            wireStats.moving = false;  
        }
    }


}
