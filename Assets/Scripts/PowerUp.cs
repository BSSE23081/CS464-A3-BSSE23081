using UnityEngine;

public enum PowerUpType { Speed, Shield }

// Lecture 07 slide 21: the pickup reports and disappears. The thing that lasts (the boost and the clock
// that ends it) runs on the PLAYER, because Destroy(gameObject) would stop a coroutine started here.
public class PowerUp : MonoBehaviour
{
    public PowerUpType type = PowerUpType.Speed;
    public float multiplier = 1.6f;
    public float duration = 5f;
    [SerializeField] float spin = 90f;
    [SerializeField] float bob = 0.15f;

    Vector3 basePosition;

    void Start() { basePosition = transform.position; }

    void Update()
    {
        transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f) * bob);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (type == PowerUpType.Speed)
        {
            if (other.TryGetComponent(out PlayerController pc)) pc.BoostSpeed(multiplier, duration);
            if (GameManager.Instance != null) GameManager.Instance.ShowMessage($"Speed boost! {duration:0}s");
        }
        else
        {
            if (other.TryGetComponent(out Health health)) health.shielded = true;
            if (GameManager.Instance != null) GameManager.Instance.ShowMessage("Shield: absorbs the next hit");
        }
        Destroy(gameObject);
    }
}
