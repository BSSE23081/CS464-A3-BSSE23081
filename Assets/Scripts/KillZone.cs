using UnityEngine;

// Lecture 07 slide 16: falling is a mistake, not damage. One wide trigger under the level
// that skips the hearts and costs a life.
public class KillZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) GameManager.Instance.PlayerDied();
    }
}
