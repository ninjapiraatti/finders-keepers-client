using UnityEngine;
using NativeWebSocket;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// Network message classes
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

// Main game network manager
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
        await ConnectToServer();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    public async System.Threading.Tasks.Task ConnectToServer()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }

        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            if (enableDebugLogs) Debug.Log("Connected to Finders Keepers Server!");
            OnConnected?.Invoke(serverUrl);
            JoinGame();
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError($"WebSocket Error: {e}");
        };

        websocket.OnClose += (e) =>
        {
            if (enableDebugLogs) Debug.Log($"Connection closed: {e}");
            OnDisconnected?.Invoke();
            CleanupPlayers();
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
            await websocket.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to server: {e.Message}");
        }
    }

    private async void JoinGame()
    {
        myPlayerId = System.Guid.NewGuid().ToString();

        var joinMessage = new ClientMessage
        {
            type = "Join",
            player_name = playerName,
            player_id = myPlayerId
        };

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
        CleanupPlayers();

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

        SpawnPlayer(player);
        OnPlayerJoined?.Invoke(player);
    }

    private void HandlePlayerMoved(PlayerMovedMessage message)
    {
        Vector3 newPosition = new Vector3(message.x, message.y, message.z);

        // Only update remote players, never update our own player from network messages
        if (message.player_id != myPlayerId && otherPlayers.ContainsKey(message.player_id))
        {
            var playerObject = otherPlayers[message.player_id];
            if (playerObject != null)
            {
                Debug.Log($"[PLAYER MOVEMENT] : {transform.position}");
                var networkPlayer = playerObject.GetComponent<NetworkPlayer>();
                if (networkPlayer != null)
                {
                    networkPlayer.UpdateNetworkPosition(newPosition);
                }
                else
                {
                    playerObject.transform.position = newPosition;
                }
            }
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

        var playerController = playerObject.GetComponent<NetworkPlayer>();
        if (playerController != null)
        {
            playerController.Initialize(player.id, player.name, isMyPlayer);
        }

        if (isMyPlayer)
        {
            myPlayerId = player.id;
            myPlayerObject = playerObject;

            var localController = playerObject.GetComponent<PlayerController>();
            if (localController != null)
            {
                localController.enabled = true;
                localController.SetNetworkManager(this);
            }

            if (enableDebugLogs) Debug.Log($"Spawned local player: {player.name}");
        }
        else
        {
            otherPlayers[player.id] = playerObject;

            var localController = playerObject.GetComponent<PlayerController>();
            if (localController != null)
            {
                localController.enabled = false;
            }

            if (enableDebugLogs) Debug.Log($"Spawned remote player: {player.name}");
        }
    }

    private void CleanupPlayers()
    {
        foreach (var kvp in otherPlayers)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        otherPlayers.Clear();

        if (myPlayerObject != null)
        {
            Destroy(myPlayerObject);
            myPlayerObject = null;
        }
    }

    public bool IsConnected()
    {
        return websocket?.State == WebSocketState.Open;
    }

    public string GetMyPlayerId()
    {
        return myPlayerId;
    }

    async void OnApplicationQuit()
    {
        await SafeCloseWebSocket();
    }

    async void OnDestroy()
    {
        await SafeCloseWebSocket();
        myPlayerId = null;
    }

    private async System.Threading.Tasks.Task SafeCloseWebSocket()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            try
            {
                await websocket.Close();
            }
            catch (Exception e)
            {
                if (enableDebugLogs) Debug.Log($"WebSocket close exception (this is usually harmless): {e.Message}");
            }
        }
    }
}
