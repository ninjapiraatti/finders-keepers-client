using UnityEngine;

public class PlayerAiming : MonoBehaviour
{
  [Header("Simple Aiming")]
  public Transform aimDirection; // The sprite inside the player that rotates
  public GameObject crosshairUI; // Simple UI crosshair element

  [Header("Shooting")]
  public GameObject bulletPrefab; // Bullet prefab to spawn
  public Transform bulletSpawnPoint; // Where bullets spawn from (optional, uses player position if null)
  public float fireRate = 0.5f; // Time between shots in seconds

  private Camera playerCamera;
  private bool isLocalPlayer = false;
  private float lastShotTime = 0f;

  void Start()
  {
    playerCamera = Camera.main;

    // Hide mouse cursor
    Cursor.visible = true;
  }

  public void SetAsLocalPlayer(bool isLocal)
  {
    isLocalPlayer = isLocal;

    if (crosshairUI != null)
    {
      crosshairUI.SetActive(isLocal);
    }
  }

  void Update()
  {
    if (!isLocalPlayer) return;

    UpdateCrosshair();
    RotateAimSprite();
    HandleShooting();
  }

  void UpdateCrosshair()
  {
    if (crosshairUI != null)
    {
      Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
      mouseWorldPos.z = transform.position.z; // Keep on same Z plane as player
      crosshairUI.transform.position = mouseWorldPos;
    }
  }

  void RotateAimSprite()
  {
    if (aimDirection == null || playerCamera == null) return;

    // Get mouse world position - same calculation as crosshair
    Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
    mouseWorldPos.z = transform.position.z;

    // Calculate direction from player to mouse world position
    Vector3 direction = (mouseWorldPos - transform.position).normalized;

    // Calculate rotation angle
    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

    // Apply rotation to aim direction sprite
    aimDirection.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
  }

  void HandleShooting()
  {
    // Check for left mouse button click
    if (Input.GetMouseButtonDown(0))
    {
      // Check fire rate cooldown
      if (Time.time >= lastShotTime + fireRate)
      {
        Shoot();
        lastShotTime = Time.time;
      }
    }
  }

  void Shoot()
  {
    if (bulletPrefab == null)
    {
      Debug.LogWarning("Bullet prefab not assigned!");
      return;
    }

    if (playerCamera == null) return;

    // Calculate shoot direction - same as crosshair/aim direction
    Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
    mouseWorldPos.z = transform.position.z;
    Vector3 shootDirection = (mouseWorldPos - transform.position).normalized;

    // Determine spawn position
    Vector3 spawnPosition = bulletSpawnPoint != null ? bulletSpawnPoint.position : transform.position;

    // Spawn bullet
    GameObject bulletObject = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

    // Initialize bullet with direction and shooter info
    // Get player ID from NetworkPlayer component
    NetworkPlayer networkPlayer = GetComponent<NetworkPlayer>();
    string playerId = networkPlayer != null ? networkPlayer.GetPlayerId() : "unknown";

    // Initialize bullet using a simple approach
    var bulletScript = bulletObject.GetComponent("Bullet") as MonoBehaviour;
    if (bulletScript != null)
    {
      // Set the direction and shooter ID using reflection or direct assignment
      bulletScript.SendMessage("SetDirection", shootDirection, SendMessageOptions.DontRequireReceiver);
      bulletScript.SendMessage("SetShooterId", playerId, SendMessageOptions.DontRequireReceiver);
    }

    // Optional: Rotate bullet to face direction
    float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
    bulletObject.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
  }
}
