using UnityEngine;

public class Bullet : MonoBehaviour
{
  [Header("Bullet Settings")]
  public float speed = 10f;
  public float lifetime = 5f;
  public float damage = 10f;
  public LayerMask collisionLayers = -1; // What layers can the bullet hit

  private Vector3 direction;
  private string shooterId; // ID of the player who shot this bullet
  private Vector3 lastPosition;
  private Collider2D bulletCollider;

  void Start()
  {
    // Store initial position for raycast
    lastPosition = transform.position;
    // Get bullet's own collider to exclude it from raycasts
    bulletCollider = GetComponent<Collider2D>();
    // Destroy bullet after lifetime expires
    Destroy(gameObject, lifetime);
  }

  void Update()
  {
    // Store current position before moving
    Vector3 currentPosition = transform.position;

    // Calculate movement for this frame
    Vector3 movement = direction * speed * Time.deltaTime;
    Vector3 newPosition = currentPosition + movement;

    // Raycast from last position to new position to check for collisions
    float distance = Vector3.Distance(lastPosition, newPosition);
    if (distance > 0) // Only raycast if we're actually moving
    {
      // Temporarily disable bullet's own collider to avoid self-collision
      if (bulletCollider != null)
        bulletCollider.enabled = false;
        
      RaycastHit2D hit = Physics2D.Raycast(lastPosition, direction, distance, collisionLayers);
      
      // Re-enable bullet's collider
      if (bulletCollider != null)
        bulletCollider.enabled = true;

      if (hit.collider != null)
      {
        // Hit something - move to hit point and handle collision
        transform.position = hit.point;
        OnHit(hit);
        return;
      }
    }

    // No collision - move normally
    transform.position = newPosition;
    lastPosition = transform.position;
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

  private void OnHit(RaycastHit2D hit)
  {
    // Check if bullet hit something
    if (hit.collider.CompareTag("Player"))
    {
      // Don't hit the player who shot the bullet
      NetworkPlayer hitPlayer = hit.collider.GetComponent<NetworkPlayer>();
      if (hitPlayer != null && hitPlayer.GetPlayerId() == shooterId)
        return;

      // Handle damage or other effects here
      Debug.Log($"Bullet hit player: {hitPlayer.GetPlayerName()}");

      // Destroy bullet on hit
      Destroy(gameObject);
    }
    else if (hit.collider.CompareTag("Solid"))
    {
      // Destroy bullet when hitting walls or obstacles
      Debug.Log($"Bullet hit: {hit.collider.name}");
      Destroy(gameObject);
    }
    else
    {
      // Hit something else - log it and destroy bullet
      Debug.Log($"Bullet hit: {hit.collider.name}");
      Destroy(gameObject);
    }
  }
}
