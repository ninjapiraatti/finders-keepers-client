using UnityEngine;

public class PlayerAimingSetup : MonoBehaviour
{
  void Start()
  {
    // Find the NetworkPlayer component
    NetworkPlayer networkPlayer = GetComponent<NetworkPlayer>();

    // Get or add the PlayerAiming component
    PlayerAiming playerAiming = GetComponent<PlayerAiming>();
    if (playerAiming == null)
    {
      playerAiming = gameObject.AddComponent<PlayerAiming>();
    }

    // Set up aiming for local player when NetworkPlayer is initialized
    if (networkPlayer != null && networkPlayer.IsLocalPlayer())
    {
      playerAiming.SetAsLocalPlayer(true);
    }
  }
}
