using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Wire : MonoBehaviour
{
    Vector3 startPoint;
    Vector3 startPosition;

    public LineRenderer wireLine;
    public GameObject lightOn;

    public float wireRadius = 0.2f;

    void Start()
    {
        // Starting position of the wire
        startPoint = transform.parent.position;
        startPosition = transform.position;

        // Set up LineRenderer
        if (wireLine != null)
        {
            wireLine.positionCount = 2;
            wireLine.SetPosition(0, startPoint);
            wireLine.SetPosition(1, startPosition);
        }
    }

    private void OnMouseDrag()
    {
        // Get mouse position
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Create a ray from the camera through the mouse position
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        // Plane where the wires are located
        Plane wirePlane = new Plane(Vector3.forward, startPoint);

        float distance;

        if (wirePlane.Raycast(ray, out distance))
        {
            // Get the mouse position on the wire plane
            Vector3 newPosition = ray.GetPoint(distance);

            // Check for nearby wire endpoints
            Collider[] colliders = Physics.OverlapSphere(
                newPosition,
                wireRadius
            );

            foreach (Collider collider in colliders)
            {
                if (collider.gameObject != gameObject)
                {
                    UpdateWire(collider.transform.position);

                    // Check if this is the matching wire
                    if (transform.parent.name == collider.transform.parent.name)
                    {
                        Main.Instance.LightChange(1);

                        Wire otherWire =
                            collider.GetComponent<Wire>();

                        if (otherWire != null)
                        {
                            otherWire.Done();
                        }

                        Done();

                        return;
                    }
                }
            }

            // No matching endpoint found
            UpdateWire(startPosition);
        }
    }

    private void OnMouseUp()
    {
        // Return wire to original position
        UpdateWire(startPosition);
    }

    void UpdateWire(Vector3 newPosition)
    {
        transform.position = newPosition;

        if (wireLine != null)
        {
            wireLine.SetPosition(0, startPoint);
            wireLine.SetPosition(1, newPosition);
        }
    }

    void Done()
    {
        lightOn.SetActive(true);

        Destroy(this);
    }
}