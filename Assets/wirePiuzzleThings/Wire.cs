using UnityEngine;
using UnityEngine.InputSystem;

public class Wire : MonoBehaviour
{
    Vector3 startPoint;
    Vector3 startPosition;

    public LineRenderer wireLine;
    public GameObject lightOn;

    public float wireRadius = 0.2f;

    private bool connected = false;

    void Start()
    {
        startPosition = transform.position;

        Transform wireStart = transform.parent.Find("Wire_start");

        if (wireStart != null)
        {
            startPoint = wireStart.position;
        }
        else
        {
            startPoint = transform.parent.position;

            Debug.LogWarning(
                "Wire_start not found for " + gameObject.name
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
        if (connected || Camera.main == null || Mouse.current == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            Camera.main.ScreenPointToRay(mousePosition);

        Plane wirePlane =
            new Plane(Vector3.forward, startPoint);

        float distance;

        if (wirePlane.Raycast(ray, out distance))
        {
            Vector3 newPosition = ray.GetPoint(distance);

            Collider[] colliders =
                Physics.OverlapSphere(newPosition, wireRadius);

            foreach (Collider collider in colliders)
            {
                if (collider.gameObject == gameObject)
                    continue;

                if (collider.transform.parent == null)
                    continue;

                if (transform.parent.name ==
                    collider.transform.parent.name)
                {
                    Wire otherWire =
                        collider.GetComponent<Wire>();

                    if (otherWire == null || otherWire == this || otherWire.connected)
                        continue;

                    Vector3 connectionPosition =
                        collider.transform.position;

                    connectionPosition.z = startPosition.z;

                    UpdateWire(connectionPosition);

                    connected = true;

                    otherWire.Done();
                    Done();

                    if (Main.Instance != null)
                        Main.Instance.LightChange(1);

                    return;
                }
            }

            UpdateWire(newPosition);
        }
    }

    private void OnMouseUp()
    {
        if (!connected)
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
        connected = true;

        if (lightOn != null)
            lightOn.SetActive(true);

        if (wireLine != null)
            wireLine.enabled = true;
    }
}