using UnityEngine;
using TMPro;

public class NetworkPlayer : MonoBehaviour
{
    [Header("Player Display")]
    public TextMeshPro nameText;
    public Renderer playerRenderer;
    public Color localPlayerColor = Color.red;
    public Color remotePlayerColor = Color.blue;

    [Header("Smooth Movement")]
    public bool enableSmoothMovement = true;
    public float smoothSpeed = 10f;

    private string playerId;
    private string playerName;
    private bool isLocalPlayer;
    private Vector3 targetPosition;
    private float lastUpdateTime;

    public void Initialize(string id, string name, bool isLocal)
    {
        playerId = id;
        playerName = name;
        isLocalPlayer = isLocal;
        targetPosition = transform.position;
        SetupPlayerVisuals();
    }

    private void SetupPlayerVisuals()
    {
        // Set player name
        if (nameText != null)
        {
            nameText.text = playerName;
        }

        // Set player color
        if (playerRenderer != null)
        {
            Material material = playerRenderer.material;
            material.color = isLocalPlayer ? localPlayerColor : remotePlayerColor;
        }
    }

    public void UpdateNetworkPosition(Vector3 position)
    {
        if (isLocalPlayer)
        {
            Debug.LogWarning($"[NETWORK PLAYER] Attempted to update local player position from network - ignoring!");
            return; // Don't update local player from network
        }
        lastUpdateTime = Time.time;

        Debug.Log($"[NETWORK PLAYER] Remote player {playerName} position updated to {position}");

        if (!enableSmoothMovement)
        {
            transform.position = position;
        }
        else
        {
            targetPosition = position;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer && enableSmoothMovement)
        {
            // Smooth movement for remote players
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }

        // Optional: Add extrapolation for better prediction
        if (!isLocalPlayer && enableSmoothMovement)
        {
            float timeSinceUpdate = Time.time - lastUpdateTime;
            if (timeSinceUpdate > 0.1f) // If we haven't received an update in 100ms
            {
                // You could add prediction/extrapolation here
            }
        }
    }

    public string GetPlayerId()
    {
        return playerId;
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public bool IsLocalPlayer()
    {
        return isLocalPlayer;
    }
}
