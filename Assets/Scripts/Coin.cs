using UnityEngine;

// Lecture 06's Coin: a trigger that reports to the GameManager and destroys itself.
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    public int value = 1;
    [SerializeField] float spin = 140f;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void Update()
    {
        transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;   // the built-in Player tag; tags are case-sensitive
        GameManager.Instance.AddScore(value);
        Destroy(gameObject);                        // the GameObject, not just this script
    }
}
