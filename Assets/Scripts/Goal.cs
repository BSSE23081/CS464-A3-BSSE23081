using UnityEngine;

// The goal of a ReachGoal level: the GameManager decides whether you have enough coins.
public class Goal : MonoBehaviour
{
    float nextCheck;

    void Update() { transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World); }

    void OnTriggerEnter(Collider other) { Check(other); }

    void OnTriggerStay(Collider other)
    {
        if (Time.time < nextCheck) return;   // standing on the goal re-checks once a second
        Check(other);
    }

    void Check(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        nextCheck = Time.time + 1f;
        GameManager.Instance.ReachGoal();
    }
}
