using UnityEngine;

// A sweeper: a kinematic Rigidbody slid back and forth with MovePosition in FixedUpdate
// (Lecture 07 slide 28). Put Hazard.cs and a trigger collider on the same object.
[RequireComponent(typeof(Rigidbody))]
public class MovingHazard : MonoBehaviour
{
    public Vector3 axis = Vector3.right;
    public float distance = 5f;
    public float speed = 2.5f;   // metres a second along the axis: keep the rhythm steady so it can be learnt

    Rigidbody rb;
    Vector3 start;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        start = transform.position - axis.normalized * (distance * 0.5f);   // the placed position is the middle
    }

    void FixedUpdate()
    {
        float d = Mathf.PingPong(Time.time * speed, distance);
        rb.MovePosition(start + axis.normalized * d);
    }
}
