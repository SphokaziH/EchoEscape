using UnityEngine;

public class objPickup : Interactable
{
    [Header("Object References")]
    public Transform objTransform;
    public Transform cameraTrans;
    public Rigidbody objRigidbody;

    [Header("Interaction Settings")]
    public float interactionDistance = 3f;
    public float holdDistance = 0.5f;
    public float throwAmount = 2f;

    [Header("Throw Sound")]
    [Tooltip("Attach an AudioSource to the object and assign a short throw/impact sound clip.")]
    public AudioSource throwNoise;

    [Header("Echo Noise")]
    public EchoAI echo;
    public float objectNoiseRadius = 20f;

    [Header("Object State")]
    public bool interactable = true;
    public bool pickedup;

    private Transform originalParent;

    private void Start()
    {
        if (objTransform == null)
            objTransform = transform;

        if (objRigidbody == null)
            objRigidbody = objTransform.GetComponent<Rigidbody>();

        if (cameraTrans == null && Camera.main != null)
            cameraTrans = Camera.main.transform;

        if (objRigidbody == null || cameraTrans == null)
        {
            Debug.LogError("objPickup requires a Rigidbody and a player camera.", this);
            enabled = false;
            return;
        }

        originalParent = objTransform.parent;

        if (throwNoise == null)
            throwNoise = objTransform.GetComponent<AudioSource>();

        if (throwNoise != null)
        {
            throwNoise.playOnAwake = false;
            throwNoise.loop = false;
            throwNoise.spatialBlend = 1f; // 3D sound follows the object
        }
        else
        {
            Debug.LogWarning("No AudioSource is assigned to objPickup. Throw sound will not play.", this);
        }
    }

    private void Update()
    {
        if (cameraTrans == null || objTransform == null || objRigidbody == null)
            return;

        float distanceToPlayer = Vector3.Distance(
            cameraTrans.position,
            objTransform.position
        );

        interactable = distanceToPlayer <= interactionDistance;

        // E: Pick up or drop.
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (pickedup)
                DropObject();
            else if (interactable)
                PickUpObject();
        }

        // T: Throw while holding the object.
        if (pickedup && Input.GetKeyDown(KeyCode.T))
            ThrowObject();

        // Keep the held object in front of the camera.
        if (pickedup)
        {
            objTransform.position =
                cameraTrans.position + cameraTrans.forward * holdDistance;
        }
    }
    private void PickUpObject()
    {
        pickedup = true;

        objRigidbody.linearVelocity = Vector3.zero;
        objRigidbody.angularVelocity = Vector3.zero;
        objRigidbody.isKinematic = true;

        objTransform.SetParent(cameraTrans);
        objTransform.localPosition = new Vector3(-0.15f, -0.05f, holdDistance);
        objTransform.localRotation = Quaternion.identity;
    }

    private void DropObject()
    {
        pickedup = false;

        objTransform.SetParent(originalParent);
        objRigidbody.isKinematic = false;
        objRigidbody.useGravity = true;
    }
    
    private void ThrowObject()
    {
        pickedup = false;

        objTransform.SetParent(originalParent);
        objRigidbody.isKinematic = false;
        objRigidbody.useGravity = true;

        objRigidbody.linearVelocity = Vector3.zero;
        objRigidbody.angularVelocity = Vector3.zero;

        objRigidbody.AddForce(
            cameraTrans.forward * throwAmount,
            ForceMode.Impulse
        );

        // Play the assigned clip once. It ends naturally; no Stop() is needed.
        if (throwNoise != null && throwNoise.clip != null)
            throwNoise.PlayOneShot(throwNoise.clip);
        else if (throwNoise != null)
            Debug.LogWarning("The throw AudioSource has no AudioClip assigned.", this);

        // Notify Echo once per throw rather than calling it every frame.
        if (echo != null)
            echo.HearNoise(objTransform.position, objectNoiseRadius);
    }

    public override string GetInteractionPrompt()
    {
        if (pickedup)
            return "[T] THROW ";

        return "[E] PICK UP" ;
    }
}

