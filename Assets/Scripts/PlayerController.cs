using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// The lab's rolling-ball controller (Lecture 06: AddForce), with a jump, plus Lecture 07's BoostSpeed (Part C) and the Knockback
// from Assignment 03 page 3 (steering waits while a hit's push plays out).
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement (the lab's rolling ball: AddForce every physics step)")]
    [SerializeField] float moveForce = 27f;     // N. Top speed on the flat = moveForce / drag = 6.75 m/s
    [SerializeField] float drag = 4f;           // sideways damping, so the ball slows when you let go. Gravity is not damped.
    [SerializeField] float jumpImpulse = 6.5f;  // Impulse on a 1 kg ball: dv = J / m = 6.5 m/s up
    [SerializeField] float jumpBuffer = 0.12f;  // Lecture 05: a press just before landing still counts
    [SerializeField] float coyoteTime = 0.12f;  // Lecture 05: a press just after leaving a ledge still counts

    Rigidbody rb;
    Vector3 input;
    float jumpBufferedUntil;
    float groundedUntil;
    float noControlUntil;

    // Lecture 07 slide 21: the power-up's state lives on the player, so it outlives the pickup.
    float speedMultiplier = 1f;
    Coroutine boost;
    float boostEndsAt;

    public bool IsBoosted => boost != null;
    public float BoostSecondsLeft => boost != null ? Mathf.Max(0f, boostEndsAt - Time.time) : 0f;
    bool IsGrounded => Time.time <= groundedUntil;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0f;        // damping is applied sideways only, in FixedUpdate, so falling stays natural
        rb.angularDamping = 0.5f;     // the ball rolls, and the spin dies away
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        Vector2 move = ReadMove();
        input = new Vector3(move.x, 0f, move.y);
        if (JumpPressed()) jumpBufferedUntil = Time.time + jumpBuffer;
    }

    void FixedUpdate()
    {
        // A push, not a set speed: the ball speeds up, rolls, and coasts to a stop. Not steering during a knockback.
        Vector3 sideways = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 drive = Time.time >= noControlUntil
            ? Vector3.ClampMagnitude(input, 1f) * moveForce * speedMultiplier
            : Vector3.zero;
        rb.AddForce(drive - sideways * drag);   // ForceMode.Force, once per step

        if (Time.time <= jumpBufferedUntil && IsGrounded)
        {
            jumpBufferedUntil = 0f;
            groundedUntil = 0f;
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x, 0f, v.z);   // a clean take-off, whatever the fall speed
            rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
        }
    }

    // Standing on something: a contact whose normal points mostly up (Lecture 04).
    void OnCollisionStay(Collision c)
    {
        for (int i = 0; i < c.contactCount; i++)
        {
            if (c.GetContact(i).normal.y > 0.5f)
            {
                groundedUntil = Time.time + coyoteTime;
                return;
            }
        }
    }

    // Called by Hazard.OnTriggerStay when a hit lands (Assignment 03, page 3).
    public void Knockback(Vector3 push, float seconds)
    {
        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, v.y, 0f);
        rb.AddForce(push, ForceMode.Impulse);   // Impulse: one kick, divided by mass
        noControlUntil = Time.time + seconds;
    }

    // Called by GameManager after a life is lost.
    public void Respawn(Vector3 position)
    {
        if (boost != null) StopCoroutine(boost);
        boost = null;
        speedMultiplier = 1f;
        noControlUntil = 0f;
        jumpBufferedUntil = 0f;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = position;
        transform.position = position;
        input = Vector3.zero;
    }

    // ---- Timed power-up. The rule for a second pickup is REFRESH: stop the running
    // coroutine and start a new one, so the newest pickup wins and the clock restarts.
    // Two 5 s pickups 3 s apart therefore end at 8 s.
    public void BoostSpeed(float multiplier, float seconds)
    {
        if (boost != null) StopCoroutine(boost);
        boost = StartCoroutine(Boost(multiplier, seconds));
    }

    IEnumerator Boost(float multiplier, float seconds)
    {
        speedMultiplier = multiplier;
        boostEndsAt = Time.time + seconds;
        yield return new WaitForSeconds(seconds);
        speedMultiplier = 1f;
        boost = null;
    }

    // ---- Input: works with the old Input Manager, the new Input System, or both.
    static Vector2 ReadMove()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k != null)
        {
            float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f)
                    - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f)
                    - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
        return Vector2.zero;
#endif
    }

    static bool JumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k != null) return k.spaceKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }
}
