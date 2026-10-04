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
        startPoint = transform.parent.position;
        startPosition = transform.position;

        if (wireLine != null)
        {
            wireLine.positionCount = 2;
            wireLine.SetPosition(0, startPoint);
            wireLine.SetPosition(1, startPosition);
        }
    }

    private void OnMouseDrag()
    {
        Debug.Log("Dragging wire: " + gameObject.name);

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        Plane wirePlane = new Plane(Vector3.forward, startPoint);

        float distance;

        if (wirePlane.Raycast(ray, out distance))
        {
            Vector3 newPosition = ray.GetPoint(distance);

            Collider[] colliders = Physics.OverlapSphere(
                newPosition,
                wireRadius
            );

            foreach (Collider collider in colliders)
            {
                if (collider.gameObject != gameObject)
                {
                    UpdateWire(collider.transform.position);

                    if (transform.parent.name == collider.transform.parent.name)
                    {
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

            UpdateWire(newPosition);
        }
    }

    private void OnMouseUp()
    {
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