using System.Collections;
using UnityEngine;

// Lecture 07 slides 15 to 17: one script for every danger. A trigger that asks whatever it touches
// to take damage, and throws it clear if the hit lands. With restTime > 0 it is a crusher:
// off for restTime, warning for warnTime, then the strike (collider and slab on together).
[RequireComponent(typeof(Collider))]
public class Hazard : MonoBehaviour
{
    public int damage = 1;
    public float knockback = 8f;     // ForceMode.Impulse: 8 m/s on the 1 kg ball
    public float knockTime = 0.25f;  // how long the player stops steering

    [Header("Crusher (leave restTime at 0 for spikes, guards and sweepers)")]
    public float restTime = 0f;
    public float warnTime = 0.7f;    // 0.5 s or more: reaction time is about 0.25 s
    public float strikeTime = 0.4f;
    public float startDelay = 0f;
    public GameObject warning;
    public GameObject slab;

    Collider zone;

    void Awake() { zone = GetComponent<Collider>(); }

    IEnumerator Start()
    {
        if (restTime <= 0f) yield break;     // not a crusher: always on
        zone.enabled = false;
        SetActive(warning, false);
        SetActive(slab, false);
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        while (true)
        {
            zone.enabled = false;
            SetActive(slab, false);
            SetActive(warning, false);
            yield return new WaitForSeconds(restTime);   // safe: the collider is off

            SetActive(warning, true);
            yield return new WaitForSeconds(warnTime);   // the player can see it coming

            SetActive(warning, false);
            zone.enabled = true;
            SetActive(slab, true);
            yield return new WaitForSeconds(strikeTime); // the strike
        }
    }

    static void SetActive(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }

    // Stay, not Enter: standing on spikes keeps hurting. Health decides how often a hit lands.
    void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent(out Health health)) return;
        if (!health.TakeDamage(damage)) return;          // only a hit that lands knocks back

        Rigidbody body = other.attachedRigidbody;
        if (body == null) return;

        // Away from the hazard, flattened so a hit never launches the ball upward.
        Vector3 away = body.position - zone.ClosestPoint(body.position);
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = body.position - transform.position;   // inside the box: use its centre
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = transform.forward;
        Vector3 push = away.normalized * knockback;

        if (other.TryGetComponent(out PlayerController p)) p.Knockback(push, knockTime);
        else body.AddForce(push, ForceMode.Impulse);
    }
}
