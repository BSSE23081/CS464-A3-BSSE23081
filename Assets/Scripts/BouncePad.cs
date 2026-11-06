using UnityEngine;

// Part A's force, on purpose: ForceMode.VelocityChange. A bounce pad should launch everything at the
// same speed whatever its mass, so it uses the mode that ignores mass and is applied once on entry.
public class BouncePad : MonoBehaviour
{
    [SerializeField] float launchSpeed = 9f;   // m/s straight up: rises v^2 / 2g = 4.1 m

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Rigidbody body = other.attachedRigidbody;
        if (body == null) return;

        Vector3 v = body.linearVelocity;
        body.linearVelocity = new Vector3(v.x, 0f, v.z);   // a fall would otherwise eat into the launch
        body.AddForce(Vector3.up * launchSpeed, ForceMode.VelocityChange);   // push the player UP: steering rewrites sideways speed
    }
}
