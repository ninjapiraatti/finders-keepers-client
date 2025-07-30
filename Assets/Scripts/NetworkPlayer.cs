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

        // Add a simple distinguishing feature for local player
        /*
        if (isLocalPlayer)
        {
            // You might want to add a crown, outline, or other visual indicator
            var outline = gameObject.AddComponent<Outline>();
            if (outline != null)
            {
                outline.OutlineColor = Color.white;
                outline.OutlineWidth = 2f;
            }
        }
        */
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

// Simple outline script for highlighting local player
/*
public class Outline : MonoBehaviour
{
    public Color OutlineColor = Color.white;
    public float OutlineWidth = 2f;

    void Start()
    {
        // This is a simple implementation
        // For better outlines, consider using a proper outline shader
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create outline effect by duplicating material
            Material[] materials = renderer.materials;
            System.Array.Resize(ref materials, materials.Length + 1);
            materials[materials.Length - 1] = new Material(Shader.Find("Sprites/Outline"));
            materials[materials.Length - 1].color = OutlineColor;
            renderer.materials = materials;
        }
    }
}
*/
