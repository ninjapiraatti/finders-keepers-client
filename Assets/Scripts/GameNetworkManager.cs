using UnityEngine;
using NativeWebSocket;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class GameNetworkManager : MonoBehaviour
{
    [Header("Connection Settings")]
    public string serverUrl = "ws://37.27.225.36:8087";
    public string playerName = "AAA";

    [Header("Player Settings")]
    public GameObject playerPrefab;
    public Transform spawnPoint;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private WebSocket websocket;
    private string myPlayerId;
    private Dictionary<string, GameObject> otherPlayers = new Dictionary<string, GameObject>();
    private GameObject myPlayerObject;

    // Events for other scripts to listen to
    public event Action<string> OnConnected;
    public event Action OnDisconnected;
    public event Action<Player> OnPlayerJoined;
    public event Action<string> OnPlayerLeft;
    public event Action<string, Vector3> OnPlayerMoved;

    async void Start()
    {
        Debug.Log($"GameNetworkManager Start() - Attempting to connect to: {serverUrl}");
        await ConnectToServer();
    }

    private float lastConnectionCheck = 0f;
    private float connectionCheckInterval = 5f; // Check every 5 seconds

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif

        // Periodically log connection state for debugging
        if (enableDebugLogs && Time.time - lastConnectionCheck > connectionCheckInterval)
        {
            lastConnectionCheck = Time.time;
            if (websocket != null)
            {
                Debug.Log($"WebSocket State: {websocket.State}");
            }
            else
            {
                Debug.Log("WebSocket is null");
            }
        }
    }

    public async System.Threading.Tasks.Task ConnectToServer()
    {
        Debug.Log($"ConnectToServer() called - Server URL: {serverUrl}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Is Editor: {Application.isEditor}");

        if (websocket != null)
        {
            Debug.Log("Closing existing websocket connection");
            await websocket.Close();
        }

        Debug.Log($"Creating new WebSocket connection to: {serverUrl}");
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log($"WebSocket OnOpen event triggered! Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            if (enableDebugLogs) Debug.Log("Connected to Finders Keepers Server!");
            OnConnected?.Invoke(serverUrl);
            JoinGame();
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError($"WebSocket Error: {e} Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log($"WebSocket OnClose event triggered: {e} Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            if (enableDebugLogs) Debug.Log($"Connection closed: {e}");
            OnDisconnected?.Invoke();
            CleanupPlayers();
            // Clear player ID when connection is lost
            myPlayerId = null;
        };

        websocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            if (enableDebugLogs) Debug.Log($"Received: {message}");
            HandleServerMessage(message);
        };

        try
        {
            Debug.Log("Attempting to connect to WebSocket...");
            await websocket.Connect();
            Debug.Log("WebSocket.Connect() completed without exception");

            // Wait a bit and check the state
            await System.Threading.Tasks.Task.Delay(1000);
            Debug.Log($"Connection state after 1 second: {websocket.State}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to server: {e.Message}");
            Debug.LogError($"Exception Type: {e.GetType().Name}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
        }
    }

    private async void JoinGame()
    {
        // Generate a unique player ID for this client
        myPlayerId = System.Guid.NewGuid().ToString();

        Debug.Log($"JoinGame() called - Player name: {playerName}, Player ID: {myPlayerId}");
        var joinMessage = new ClientMessage
        {
            type = "Join",
            player_name = playerName,
            player_id = myPlayerId
        };

        Debug.Log("Sending join message to server. Player ID: " + myPlayerId);
        await SendMessage(joinMessage);
    }

    public async void SendPositionUpdate(Vector3 position)
    {
        if (websocket?.State != WebSocketState.Open) return;

        var moveMessage = new ClientMessage
        {
            type = "UpdatePosition",
            x = position.x,
            y = position.y,
            z = position.z
        };

        if (enableDebugLogs) Debug.Log($"[LOCAL] Sending position update: ({position.x:F2}, {position.y:F2}, {position.z:F2})");
        await SendMessage(moveMessage);
    }

    private async System.Threading.Tasks.Task SendMessage(ClientMessage message)
    {
        if (websocket?.State != WebSocketState.Open) return;

        try
        {
            string json = JsonConvert.SerializeObject(message);
            await websocket.SendText(json);
            if (enableDebugLogs) Debug.Log($"Sent: {json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send message: {e.Message}");
        }
    }

    private void HandleServerMessage(string messageJson)
    {
        try
        {
            var baseMessage = JsonConvert.DeserializeObject<ServerMessage>(messageJson);

            switch (baseMessage.type)
            {
                case "GameState":
                    var gameState = JsonConvert.DeserializeObject<GameStateMessage>(messageJson);
                    HandleGameState(gameState);
                    break;

                case "PlayerJoined":
                    var playerJoined = JsonConvert.DeserializeObject<PlayerJoinedMessage>(messageJson);
                    HandlePlayerJoined(playerJoined);
                    break;

                case "PlayerMoved":
                    var playerMoved = JsonConvert.DeserializeObject<PlayerMovedMessage>(messageJson);
                    HandlePlayerMoved(playerMoved);
                    break;

                case "PlayerLeft":
                    var playerLeft = JsonConvert.DeserializeObject<PlayerLeftMessage>(messageJson);
                    HandlePlayerLeft(playerLeft);
                    break;

                case "Error":
                    var error = JsonConvert.DeserializeObject<ErrorMessage>(messageJson);
                    Debug.LogError($"Server Error: {error.message}");
                    break;

                default:
                    Debug.LogWarning($"Unknown message type: {baseMessage.type}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse server message: {e.Message}\nMessage: {messageJson}");
        }
    }

    private void HandleGameState(GameStateMessage gameState)
    {
        // Clear existing players
        CleanupPlayers();

        // Spawn all players from game state
        foreach (var player in gameState.players)
        {
            SpawnPlayer(player);
        }
    }

    private void HandlePlayerJoined(PlayerJoinedMessage message)
    {
        var player = new Player
        {
            id = message.player_id,
            name = message.player_name,
            x = message.x,
            y = message.y,
            z = message.z
        };

        Debug.Log($"[SPAWN] Message player ID: {message.player_id}");
        SpawnPlayer(player);
        OnPlayerJoined?.Invoke(player);
    }

    private void HandlePlayerMoved(PlayerMovedMessage message)
    {
        Vector3 newPosition = new Vector3(message.x, message.y, message.z);

        if (enableDebugLogs)
        {
            Debug.Log($"[NETWORK] Player {message.player_id} moved to ({newPosition.x:F2}, {newPosition.y:F2}, {newPosition.z:F2})");
            Debug.Log($"[NETWORK] My player ID: {myPlayerId}, Message player ID: {message.player_id}");
        }

        // Only update remote players, never update our own player from network messages
        if (message.player_id != myPlayerId && otherPlayers.ContainsKey(message.player_id))
        {
            var playerObject = otherPlayers[message.player_id];
            if (playerObject != null)
            {
                // Use NetworkPlayer component for smooth movement
                var networkPlayer = playerObject.GetComponent<NetworkPlayer>();
                if (networkPlayer != null)
                {
                    if (enableDebugLogs) Debug.Log($"[NETWORK] Updating remote player via NetworkPlayer component");
                    networkPlayer.UpdateNetworkPosition(newPosition);
                }
                else
                {
                    // Fallback to direct position update
                    if (enableDebugLogs) Debug.Log($"[NETWORK] Updating remote player directly (no NetworkPlayer component)");
                    playerObject.transform.position = newPosition;
                }
            }
        }
        else if (message.player_id == myPlayerId)
        {
            if (enableDebugLogs) Debug.Log($"[NETWORK] Ignoring position update for local player");
        }
        else
        {
            if (enableDebugLogs) Debug.Log($"[NETWORK] Player {message.player_id} not found in otherPlayers dictionary");
        }

        OnPlayerMoved?.Invoke(message.player_id, newPosition);
    }

    private void HandlePlayerLeft(PlayerLeftMessage message)
    {
        if (otherPlayers.ContainsKey(message.player_id))
        {
            var playerObject = otherPlayers[message.player_id];
            if (playerObject != null)
            {
                Destroy(playerObject);
            }
            otherPlayers.Remove(message.player_id);
        }

        OnPlayerLeft?.Invoke(message.player_id);
    }

    private void SpawnPlayer(Player player)
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("Player prefab not assigned!");
            return;
        }

        // Check if this player is already spawned to avoid duplicates
        Debug.LogWarning("Player ID: " + player.id);
        Debug.LogWarning("myPlayerId:" + myPlayerId);
        bool isMyPlayer = player.id == myPlayerId;

        if (isMyPlayer && myPlayerObject != null)
        {
            if (enableDebugLogs) Debug.Log($"Local player {player.name} already exists, skipping spawn");
            return;
        }

        if (!isMyPlayer && otherPlayers.ContainsKey(player.id))
        {
            if (enableDebugLogs) Debug.Log($"Remote player {player.name} already exists, skipping spawn");
            return;
        }

        Vector3 spawnPosition = new Vector3(player.x, player.y, player.z);
        GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

        // Set up player object
        var playerController = playerObject.GetComponent<NetworkPlayer>();
        if (playerController != null)
        {
            playerController.Initialize(player.id, player.name, isMyPlayer);
        }

        // Check if this is our player
        if (isMyPlayer)
        {
            myPlayerId = player.id;
            myPlayerObject = playerObject;

            // Enable PlayerController for local player (if it exists)
            var localController = playerObject.GetComponent<PlayerController>();
            if (localController != null)
            {
                localController.enabled = true;
                localController.SetNetworkManager(this); // Set the network manager reference
            }

            if (enableDebugLogs) Debug.Log($"[SPAWN] Spawned local player: {player.name} (ID: {player.id}) at {spawnPosition}");
        }
        else
        {
            // This is another player
            otherPlayers[player.id] = playerObject;

            // Disable PlayerController for remote players (if it exists)
            var localController = playerObject.GetComponent<PlayerController>();
            if (localController != null)
            {
                localController.enabled = false;
            }

            if (enableDebugLogs) Debug.Log($"[SPAWN] Spawned remote player: {player.name} (ID: {player.id}) at {spawnPosition}");
        }
    }

    private void CleanupPlayers()
    {
        // Destroy all other players
        foreach (var kvp in otherPlayers)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        otherPlayers.Clear();

        // Destroy our player object
        if (myPlayerObject != null)
        {
            Destroy(myPlayerObject);
            myPlayerObject = null;
        }

        // Don't clear myPlayerId here - we still need it to identify ourselves in GameState messages
        // myPlayerId = null;
    }

    public bool IsConnected()
    {
        return websocket?.State == WebSocketState.Open;
    }

    public string GetMyPlayerId()
    {
        return myPlayerId;
    }

    // Add this method to test connectivity manually
    [ContextMenu("Test Connection")]
    public async void TestConnection()
    {
        Debug.Log("=== MANUAL CONNECTION TEST ===");
        await ConnectToServer();
    }

    // Add this method to test basic network connectivity
    [ContextMenu("Test Network Connectivity")]
    public void TestNetworkConnectivity()
    {
        Debug.Log("=== TESTING NETWORK CONNECTIVITY ===");
        try
        {
            // Test basic connectivity using UnityWebRequest
            StartCoroutine(TestNetworkCoroutine());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Network connectivity test failed: {e.Message}");
            Debug.LogError($"Exception type: {e.GetType().Name}");
        }
    }

    private System.Collections.IEnumerator TestNetworkCoroutine()
    {
        string testUrl = "http://37.27.225.36:8087";
        Debug.Log($"Testing connectivity to: {testUrl}");

        using (var request = UnityEngine.Networking.UnityWebRequest.Get(testUrl))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"HTTP connectivity test successful: {request.responseCode}");
            }
            else
            {
                Debug.LogError($"HTTP connectivity test failed: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
            }
        }
    }

    // Add this method to check current connection state
    [ContextMenu("Check Connection State")]
    public void CheckConnectionState()
    {
        if (websocket == null)
        {
            Debug.Log("WebSocket is null");
        }
        else
        {
            Debug.Log($"WebSocket State: {websocket.State}");
        }
    }

    async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }

    async void OnDestroy()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
        // Clear player ID when destroying the component
        myPlayerId = null;
    }
}

// Message classes matching your Rust server
[Serializable]
public class ClientMessage
{
    public string type;
    public string player_name;
    public string player_id;
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class ServerMessage
{
    public string type;
}

[Serializable]
public class GameStateMessage : ServerMessage
{
    public Player[] players;
}

[Serializable]
public class PlayerJoinedMessage : ServerMessage
{
    public string player_id;
    public string player_name;
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class PlayerMovedMessage : ServerMessage
{
    public string player_id;
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class PlayerLeftMessage : ServerMessage
{
    public string player_id;
}

[Serializable]
public class ErrorMessage : ServerMessage
{
    public string message;
}

[Serializable]
public class Player
{
    public string id;
    public string name;
    public float x;
    public float y;
    public float z;
}
