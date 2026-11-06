using UnityEngine;

// A trigger you pass through: respawn here from now on (Assignment 03 page 4).
public class Checkpoint : MonoBehaviour
{
    [SerializeField] Renderer flag;
    [SerializeField] Color reachedColor = new Color(0.2f, 0.9f, 0.4f);
    bool reached;

    void OnTriggerEnter(Collider other)
    {
        if (reached || !other.CompareTag("Player")) return;
        reached = true;
        GameManager.Instance.SetCheckpoint(transform);
        if (flag != null) flag.material.color = reachedColor;
    }
}
