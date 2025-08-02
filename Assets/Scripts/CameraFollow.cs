using UnityEngine;

public class CameraFollow : MonoBehaviour
{
  [Header("Follow Settings")]
  public Transform target; // The player to follow
  public float smoothSpeed = 0.125f; // How smooth the camera follows
  public Vector3 offset = new Vector3(0, 0, -10); // Offset from target

  [Header("Auto Setup")]
  public bool autoFindLocalPlayer = true;

  private void Start()
  {
    // Auto-find the local player if no target is set
    if (target == null && autoFindLocalPlayer)
    {
      FindLocalPlayer();
    }
  }

  private void LateUpdate()
  {
    if (target == null)
    {
      // Try to find the local player again if we don't have a target
      if (autoFindLocalPlayer)
      {
        FindLocalPlayer();
      }
      return;
    }

    // Calculate the desired position
    Vector3 desiredPosition = target.position + offset;

    // Smoothly move the camera towards the desired position
    Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    transform.position = smoothedPosition;
  }

  private void FindLocalPlayer()
  {
    // Try multiple methods to reliably identify the local player
    // Method 1: Find a NetworkPlayer that is marked as local (most reliable)
    NetworkPlayer[] networkPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
    foreach (NetworkPlayer netPlayer in networkPlayers)
    {
      if (netPlayer.IsLocalPlayer())
      {
        target = netPlayer.transform;
        return;
      }
    }

    // Second, try to find a PlayerController that is the local player
    PlayerController[] playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
    foreach (PlayerController pc in playerControllers)
    {
      // Check if this PlayerController has a NetworkManager set (indicating it's the local player)
      // Local players have their SetNetworkManager method called by GameNetworkManager
      if (pc.enabled) // Local players have their controller enabled, remote players have it disabled
      {
        target = pc.transform;
        return;
      }
    }

    // Alternative method: Find the player object that belongs to the local player via GameNetworkManager
    GameNetworkManager networkManager = FindFirstObjectByType<GameNetworkManager>();
    if (networkManager != null)
    {
      string myPlayerId = networkManager.GetMyPlayerId();
      if (!string.IsNullOrEmpty(myPlayerId))
      {
        // Look for NetworkPlayer with matching ID
        foreach (NetworkPlayer netPlayer in networkPlayers)
        {
          if (netPlayer.GetPlayerId() == myPlayerId)
          {
            target = netPlayer.transform;
            return;
          }
        }
      }
    }

    // As a fallback, find any GameObject tagged as "Player"
    GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
    if (foundPlayer != null)
    {
      target = foundPlayer.transform;
    }
  }

  // Public method to manually set the target
  public void SetTarget(Transform newTarget)
  {
    target = newTarget;
  }
}
