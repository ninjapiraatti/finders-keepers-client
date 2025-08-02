using UnityEngine;

public class PlayerAiming : MonoBehaviour
{
  [Header("Simple Aiming")]
  public Transform aimDirection; // The sprite inside the player that rotates
  public GameObject crosshairUI; // Simple UI crosshair element

  private Camera playerCamera;
  private bool isLocalPlayer = false;

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
  }

  void UpdateCrosshair()
  {
    if (crosshairUI != null)
    {
      // Get mouse world position
      Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
      mouseWorldPos.z = transform.position.z; // Keep on same Z plane as player

      // Convert the mouse world position back to screen position for UI
      Vector3 crosshairScreenPos = playerCamera.WorldToScreenPoint(mouseWorldPos);
      crosshairUI.transform.position = mouseWorldPos;

      Debug.Log("Player world: " + transform.position + " | Mouse world: " + mouseWorldPos + " | Crosshair screen: " + crosshairScreenPos);
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
}
