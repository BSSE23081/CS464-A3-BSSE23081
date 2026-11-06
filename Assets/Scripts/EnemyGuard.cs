using UnityEngine;

public enum PatrolMode { Loop, PingPong }
public enum GuardState { Patrol, Chase, Return }

// Lecture 07's EnemyPatrol (slides 7 to 12) grown into slide 13's three states:
// Patrol on waypoints, Chase inside chaseRange, Return once you are beyond giveUpRange.
// Put this on a guard with a kinematic Rigidbody, a trigger collider and Hazard.cs.
[RequireComponent(typeof(Rigidbody))]
public class EnemyGuard : MonoBehaviour
{
    [Header("Route")]
    public Transform[] points;
    public PatrolMode mode = PatrolMode.Loop;
    public float waitTime = 0f;                // pause at each waypoint

    [Header("Speeds (always slower than the player's 6.5 m/s)")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float turnSpeed = 360f;

    [Header("Chasing")]
    public bool canChase = true;
    public float chaseRange = 6f;              // starts a chase inside this
    public float giveUpRange = 9f;             // gives up only outside this, so it can't flicker at one edge

    [Header("Arrival")]
    public float arriveDistance = 0.1f;        // bigger than one step (speed x Time.deltaTime), so it can't be stepped over

    GuardState state = GuardState.Patrol;
    Transform player;
    int index;
    int step = 1;
    float waitUntil;

    public GuardState State => state;

    void Start()
    {
        PlayerController pc = FindAnyObjectByType<PlayerController>();   // found in Start, so one prefab fits every level
        if (pc != null) player = pc.transform;

        if (points == null || points.Length < 2)
        {
            Debug.LogWarning($"{name}: a guard needs two or more waypoints.");
            enabled = false;
            return;
        }
        transform.position = points[0].position;
        index = 1;
    }

    void Update()
    {
        switch (state)
        {
            case GuardState.Patrol:
                Patrol();
                if (canChase && InRange(chaseRange)) state = GuardState.Chase;
                break;

            case GuardState.Chase:
                if (player != null) MoveToward(player.position, chaseSpeed);
                if (player == null || !InRange(giveUpRange))
                {
                    state = GuardState.Return;
                    index = NearestPoint();
                }
                break;

            case GuardState.Return:
                MoveToward(points[index].position, patrolSpeed);
                if (Arrived(points[index].position))
                {
                    state = GuardState.Patrol;
                    index = NextIndex();
                }
                else if (canChase && InRange(chaseRange)) state = GuardState.Chase;
                break;
        }
    }

    void Patrol()
    {
        if (Time.time < waitUntil) return;
        Vector3 target = points[index].position;
        MoveToward(target, patrolSpeed);
        if (Arrived(target))
        {
            index = NextIndex();
            waitUntil = Time.time + waitTime;
        }
    }

    // MoveTowards: at most speed x deltaTime, never overshoots. The guard keeps its own height.
    void MoveToward(Vector3 target, float speed)
    {
        target.y = transform.position.y;
        Vector3 toTarget = target - transform.position;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (toTarget.sqrMagnitude > 0.0001f)   // zero at the waypoint: LookRotation(zero) logs a warning
        {
            Quaternion look = Quaternion.LookRotation(toTarget);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }
    }

    // Arrival by distance, not ==  (== only works right after MoveTowards).
    bool Arrived(Vector3 target)
    {
        target.y = transform.position.y;
        return Vector3.Distance(transform.position, target) < arriveDistance;
    }

    bool InRange(float range)
    {
        if (player == null) return false;
        float dx = player.position.x - transform.position.x;
        float dz = player.position.z - transform.position.z;
        return dx * dx + dz * dz < range * range;   // flat distance, so height differences don't count
    }

    int NextIndex()
    {
        if (mode == PatrolMode.Loop) return (index + 1) % points.Length;
        int ahead = index + step;
        if (ahead < 0 || ahead >= points.Length) step = -step;   // turn round at either end
        return index + step;
    }

    int NearestPoint()
    {
        int best = 0;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            float sqr = (points[i].position - transform.position).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = i; }
        }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = new Color(1f, 0.2f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, giveUpRange);
    }
}
