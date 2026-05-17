using UnityEngine;
using UnityEngine.InputSystem;

public class TrolleyHold : MonoBehaviour
{
    public Camera playerCamera;

    public float interactDistance = 3f;
    public Vector3 holdOffset = new Vector3(0f, 0f, 3f);

    public float rotationOffsetY = 0f;

    public float followStrength = 18f;
    public float maxSpeed = 8f;
    public float rotateSpeed = 240f;

    public float breakDistance = 8f;
    public float breakDelay = 0.5f;
    public float fastTurnReleaseAngle = 45f;

    public float sidePullStrength = 0.085f;
    public float maxSidePull = 0.12f;

    public Transform trolleyVisualRoot;
    public float handleLeanAngle = 8f;
    public float leanSmooth = 8f;

    private Rigidbody heldTrolley;
    private Rigidbody targetTrolley;
    private Rigidbody playerRigidbody;

    private Collider[] playerColliders;
    private Collider[] trolleyColliders;

    private Quaternion holdRotationOffset;
    private Quaternion originalVisualRotation;

    private float holdStartTime;
    private float previousYaw;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        playerRigidbody = GetComponent<Rigidbody>();
        playerColliders = GetComponentsInChildren<Collider>();

        if (playerCamera != null)
            previousYaw = playerCamera.transform.eulerAngles.y;
    }

    void Update()
    {
        if (playerCamera == null)
            return;

        targetTrolley = FindTargetTrolley();

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldTrolley == null)
            {
                if (targetTrolley != null)
                    HoldTrolley(targetTrolley);
            }
            else
            {
                ReleaseTrolley();
            }
        }
    }

    void FixedUpdate()
    {
        if (heldTrolley == null)
        {
            if (playerCamera != null)
                previousYaw = playerCamera.transform.eulerAngles.y;

            return;
        }

        Vector3 targetPosition = playerCamera.transform.TransformPoint(holdOffset);
        targetPosition.y = heldTrolley.position.y;

        Vector3 direction = targetPosition - heldTrolley.position;
        Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z);

        if (Time.time - holdStartTime > breakDelay && horizontalDirection.magnitude > breakDistance)
        {
            ReleaseTrolley();
            return;
        }

        Vector3 horizontalVelocity = horizontalDirection * followStrength;
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxSpeed);

        heldTrolley.linearVelocity = new Vector3(
            horizontalVelocity.x,
            heldTrolley.linearVelocity.y,
            horizontalVelocity.z
        );

        Quaternion playerYaw = Quaternion.Euler(
            0f,
            playerCamera.transform.eulerAngles.y + rotationOffsetY,
            0f
        );

        Quaternion targetRotation = playerYaw * holdRotationOffset;

        Quaternion newRotation = Quaternion.RotateTowards(
            heldTrolley.rotation,
            targetRotation,
            rotateSpeed * Time.fixedDeltaTime
        );

        heldTrolley.MoveRotation(newRotation);

        ApplySidePull();
        ApplyVisualLean();
    }

    Rigidbody FindTargetTrolley()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            Rigidbody rb = hit.collider.attachedRigidbody;

            if (rb != null)
                return rb;
        }

        return null;
    }

    void HoldTrolley(Rigidbody rb)
    {
        heldTrolley = rb;
        holdStartTime = Time.time;
        previousYaw = playerCamera.transform.eulerAngles.y;

        if (trolleyVisualRoot != null)
            originalVisualRotation = trolleyVisualRoot.localRotation;

        Quaternion playerYaw = Quaternion.Euler(
            0f,
            playerCamera.transform.eulerAngles.y + rotationOffsetY,
            0f
        );

        holdRotationOffset = Quaternion.Inverse(playerYaw) * heldTrolley.rotation;

        heldTrolley.useGravity = true;
        heldTrolley.isKinematic = false;
        heldTrolley.angularVelocity = Vector3.zero;

        trolleyColliders = heldTrolley.GetComponentsInChildren<Collider>();

        foreach (Collider playerCol in playerColliders)
        {
            foreach (Collider trolleyCol in trolleyColliders)
            {
                Physics.IgnoreCollision(playerCol, trolleyCol, true);
            }
        }
    }

    void ReleaseTrolley()
    {
        if (heldTrolley == null)
            return;

        if (playerColliders != null && trolleyColliders != null)
        {
            foreach (Collider playerCol in playerColliders)
            {
                foreach (Collider trolleyCol in trolleyColliders)
                {
                    Physics.IgnoreCollision(playerCol, trolleyCol, false);
                }
            }
        }

        if (trolleyVisualRoot != null)
            trolleyVisualRoot.localRotation = originalVisualRotation;

        heldTrolley = null;
        trolleyColliders = null;
    }

    void ApplySidePull()
    {
        float currentYaw = playerCamera.transform.eulerAngles.y;
        float yawDifference = Mathf.DeltaAngle(previousYaw, currentYaw);
        previousYaw = currentYaw;

        if (Time.time - holdStartTime > breakDelay && Mathf.Abs(yawDifference) > fastTurnReleaseAngle)
        {
            ReleaseTrolley();
            return;
        }

        if (Mathf.Abs(yawDifference) < 0.1f)
            return;

        Vector3 rightDirection = playerCamera.transform.right;
        rightDirection.y = 0f;
        rightDirection.Normalize();

        Vector3 sideMovement = -rightDirection * yawDifference * sidePullStrength;
        sideMovement = Vector3.ClampMagnitude(sideMovement, maxSidePull);

        if (playerRigidbody != null)
            playerRigidbody.MovePosition(playerRigidbody.position + sideMovement);
        else
            transform.position += sideMovement;
    }

    void ApplyVisualLean()
    {
        if (trolleyVisualRoot == null)
            return;

        Quaternion targetLean = originalVisualRotation * Quaternion.Euler(handleLeanAngle, 0f, 0f);

        trolleyVisualRoot.localRotation = Quaternion.Lerp(
            trolleyVisualRoot.localRotation,
            targetLean,
            leanSmooth * Time.fixedDeltaTime
        );
    }

    void OnGUI()
    {
        Rigidbody promptTrolley = heldTrolley != null ? heldTrolley : targetTrolley;

        if (promptTrolley == null)
            return;

        Vector3 worldPosition = promptTrolley.transform.position + Vector3.up * 2f;
        Vector3 screenPosition = playerCamera.WorldToScreenPoint(worldPosition);

        if (screenPosition.z < 0)
            return;

        string text = heldTrolley == null ? "E" : "E Release";

        GUIStyle style = new GUIStyle();
        style.fontSize = 32;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;

        Rect rect = new Rect(screenPosition.x - 100, Screen.height - screenPosition.y - 25, 200, 50);

        GUI.Label(rect, text, style);
    }
}