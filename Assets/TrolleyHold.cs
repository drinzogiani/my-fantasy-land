using UnityEngine;
using UnityEngine.InputSystem;

public class TrolleyHold : MonoBehaviour
{
    public Camera playerCamera;
    public float interactDistance = 3f;
    public float holdDistance = 2.5f;
    public float holdHeight = -0.8f;

    private Rigidbody heldTrolley;
    private Collider[] heldColliders;

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        if (playerCamera == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldTrolley == null)
                TryHoldTrolley();
            else
                ReleaseTrolley();
        }
    }

    void LateUpdate()
    {
        if (heldTrolley == null)
            return;

        Vector3 targetPosition =
            playerCamera.transform.position +
            playerCamera.transform.forward * holdDistance +
            Vector3.up * holdHeight;

        heldTrolley.transform.position = targetPosition;

        heldTrolley.transform.rotation = Quaternion.Euler(
            0f,
            playerCamera.transform.eulerAngles.y,
            0f
        );
    }

    void TryHoldTrolley()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            Rigidbody rb = hit.collider.attachedRigidbody;

            if (rb == null)
                return;

            heldTrolley = rb;

            heldTrolley.linearVelocity = Vector3.zero;
            heldTrolley.angularVelocity = Vector3.zero;

            heldTrolley.useGravity = false;
            heldTrolley.isKinematic = true;

            heldColliders = heldTrolley.GetComponentsInChildren<Collider>();

            foreach (Collider col in heldColliders)
            {
                col.enabled = false;
            }
        }
    }

    void ReleaseTrolley()
    {
        foreach (Collider col in heldColliders)
        {
            col.enabled = true;
        }

        heldTrolley.isKinematic = false;
        heldTrolley.useGravity = true;

        heldTrolley = null;
        heldColliders = null;
    }
}