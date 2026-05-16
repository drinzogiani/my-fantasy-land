using UnityEngine;

public class PlayerPush : MonoBehaviour
{
    public float pushPower = 4f;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb == null)
            return;

        if (rb.isKinematic)
            return;

        Vector3 pushDirection = new Vector3(
            hit.moveDirection.x,
            0,
            hit.moveDirection.z
        );

        rb.AddForce(pushDirection * pushPower, ForceMode.Impulse);
    }
}