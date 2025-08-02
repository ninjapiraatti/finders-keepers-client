using UnityEngine;

public class Bullet : MonoBehaviour
{
  [Header("Bullet Settings")]
  public float speed = 10f;
  public float lifetime = 5f;
  public float damage = 10f;

  private Vector3 direction;
  private string shooterId; // ID of the player who shot this bullet

  void Start()
  {
    // Destroy bullet after lifetime expires
    Destroy(gameObject, lifetime);
  }

  void Update()
  {
    // Move bullet in the direction it was fired
    transform.position += direction * speed * Time.deltaTime;
  }

  public void Initialize(Vector3 shootDirection, string playerId)
  {
    direction = shootDirection.normalized;
    shooterId = playerId;
  }

  public void SetDirection(Vector3 shootDirection)
  {
    direction = shootDirection.normalized;
  }

  public void SetShooterId(string playerId)
  {
    shooterId = playerId;
  }

  void OnTriggerEnter2D(Collider2D other)
  {
    // Check if bullet hit something
    if (other.CompareTag("Player"))
    {
      // Don't hit the player who shot the bullet
      NetworkPlayer hitPlayer = other.GetComponent<NetworkPlayer>();
      if (hitPlayer != null && hitPlayer.GetPlayerId() == shooterId)
        return;

      // Handle damage or other effects here
      Debug.Log($"Bullet hit player: {hitPlayer.GetPlayerName()}");

      // Destroy bullet on hit
      Destroy(gameObject);
    }
    else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
    {
      // Destroy bullet when hitting walls or obstacles
      Destroy(gameObject);
    }
  }
}
