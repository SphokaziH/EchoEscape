using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Wire : MonoBehaviour
{
    Vector3 startPoint;
    Vector3 startPosition;
    public SpriteRenderer wireEnd;
    public GameObject lightOn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPoint = transform.parent.position;
        startPosition= transform.position;
    }

    // Update is called once per frame
    private void OnMouseDrag()
    {
        Vector2 mousePosition2D =Mouse.current.position.ReadValue();
        Vector3 mousePosition3D = new Vector3(mousePosition2D.x, mousePosition2D.y, 0);
        Vector3 newPosition = Camera.main.ScreenToWorldPoint(mousePosition3D);
        newPosition.z = 0;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(newPosition, .2f);
        foreach (Collider2D collider in colliders)
        {
            if(collider.gameObject != gameObject)
            {
                UpdateWire(collider.transform.position);

                if(transform.transform.name.Equals(collider.transform.parent.name))
                {

                    Main.Instance.LightChange(1);
                    collider.GetComponent<Wire>()?.Done();

                    Done();   
                }
                return;
            }
        }
        UpdateWire(startPosition);

    }

    private void OnMouseUp()
    {
        UpdateWire(startPosition);
    }
    void UpdateWire(Vector3 newPosition)
    {
        transform.position = newPosition;

        Vector3 direction = newPosition - startPoint;
        transform.right = direction * transform.lossyScale.x;

        float dist = Vector2.Distance(startPoint, newPosition);
        wireEnd.size = new Vector2(dist, wireEnd.size.y);
    }

    void Done()
    {
        lightOn.SetActive(true);
        Destroy(this);
        
    }
}
