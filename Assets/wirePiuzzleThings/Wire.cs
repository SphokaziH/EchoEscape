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
        // Save the original position of the draggable wire end
        startPosition = transform.position;

        // Use the existing wire_start object
        Transform wireStart = transform.parent.Find("wire_start");

        if (wireStart != null)
        {
            startPoint = wireStart.position;
        }
        else
        {
            // Fallback to original behaviour
            startPoint = transform.parent.position;

            Debug.LogWarning(
                "wire_start not found for " + gameObject.name
            );
        }

        if (wireLine != null)
        {
            wireLine.positionCount = 2;
            wireLine.useWorldSpace = true;

            wireLine.SetPosition(0, startPoint);
            wireLine.SetPosition(1, startPosition);
        }
    }

    private void OnMouseDrag()
    {
        if (Camera.main == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            Camera.main.ScreenPointToRay(mousePosition);

        // Keep the wire moving on the puzzle plane
        Plane wirePlane =
            new Plane(Vector3.forward, startPoint);

        float distance;

        if (wirePlane.Raycast(ray, out distance))
        {
            Vector3 newPosition =
                ray.GetPoint(distance);

            Collider[] colliders =
                Physics.OverlapSphere(
                    newPosition,
                    wireRadius
                );

            foreach (Collider collider in colliders)
            {
                if (collider.gameObject != gameObject)
                {
                    if (collider.transform.parent == null)
                        continue;

                    // Check for the matching wire
                    if (transform.parent.name ==
                        collider.transform.parent.name)
                    {
                        Vector3 connectionPosition =
                            collider.transform.position;

                        // Keep the completed wire flat
                        // instead of moving toward the camera
                        connectionPosition.z =
                            startPosition.z;

                        UpdateWire(connectionPosition);

                        if (Main.Instance != null)
                            Main.Instance.LightChange(1);

                        Wire otherWire =
                            collider.GetComponent<Wire>();

                        if (otherWire != null)
                            otherWire.Done();

                        Done();
                        return;
                    }
                }
            }

            // Follow the mouse while dragging
            UpdateWire(newPosition);
        }
    }

    private void OnMouseUp()
    {
        UpdateWire(startPosition);
    }

    void UpdateWire(Vector3 newPosition)
    {
        // Move the Moving object
        transform.position = newPosition;

        // Stretch the actual wire
        if (wireLine != null)
        {
            wireLine.SetPosition(0, startPoint);
            wireLine.SetPosition(1, newPosition);
        }
    }

    void Done()
    {
        if (lightOn != null)
            lightOn.SetActive(true);

        Destroy(this);
    }
}