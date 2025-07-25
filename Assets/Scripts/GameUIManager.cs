using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("Connection UI")]
    public GameObject connectionPanel;
    public TMP_InputField playerNameInput;
    public TMP_InputField serverUrlInput;
    public Button connectButton;
    public Button disconnectButton;
    public TextMeshProUGUI statusText;

    [Header("Game UI")]
    public GameObject gamePanel;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI playerListText;
    public TextMeshProUGUI instructionsText;

    [Header("Debug UI")]
    public GameObject debugPanel;
    public TextMeshProUGUI debugText;
    public ScrollRect debugScrollRect;

    private GameNetworkManager networkManager;
    private System.Collections.Generic.List<string> debugMessages = new System.Collections.Generic.List<string>();
    private int maxDebugMessages = 50;

    void Start()
    {
        networkManager = FindFirstObjectByType<GameNetworkManager>();

        if (networkManager != null)
        {
            // Subscribe to network events
            networkManager.OnConnected += OnConnected;
            networkManager.OnDisconnected += OnDisconnected;
            networkManager.OnPlayerJoined += OnPlayerJoined;
            networkManager.OnPlayerLeft += OnPlayerLeft;
        }

        SetupUI();
    }

    private void SetupUI()
    {
        // Initialize UI state
        ShowConnectionPanel();

        // Set default values
        if (playerNameInput != null)
            playerNameInput.text = "Unity Player " + Random.Range(1000, 9999);

        if (serverUrlInput != null)
            serverUrlInput.text = "ws://37.27.225.36:8087";

        // Setup button events
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);

        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(OnDisconnectClicked);

        // Set instructions
        if (instructionsText != null)
        {
            instructionsText.text = "Use WASD keys to move around\nPress F1 to toggle debug panel";
        }

        UpdateStatus("Disconnected", Color.red);
    }

    void Update()
    {
        // Toggle debug panel with F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleDebugPanel();
        }

        // Update player count and list
        UpdateGameUI();
    }

    public async void OnConnectClicked()
    {
        if (networkManager == null) return;

        // Update network manager settings
        if (playerNameInput != null)
            networkManager.playerName = playerNameInput.text;

        if (serverUrlInput != null)
            networkManager.serverUrl = serverUrlInput.text;

        // Start connection
        await networkManager.ConnectToServer();

        UpdateStatus("Connecting...", Color.yellow);
        connectButton.interactable = false;
    }

    public void OnDisconnectClicked()
    {
        if (networkManager == null) return;

        // Disconnect from server
        Application.Quit(); // For now, just quit - you might want to handle this differently
    }

    private void OnConnected(string serverUrl)
    {
        ShowGamePanel();
        UpdateStatus($"Connected to {serverUrl}", Color.green);
        AddDebugMessage($"Successfully connected to {serverUrl}");

        if (disconnectButton != null)
            disconnectButton.interactable = true;
    }

    private void OnDisconnected()
    {
        ShowConnectionPanel();
        UpdateStatus("Disconnected", Color.red);
        AddDebugMessage("Disconnected from server");

        if (connectButton != null)
            connectButton.interactable = true;

        if (disconnectButton != null)
            disconnectButton.interactable = false;
    }

    private void OnPlayerJoined(Player player)
    {
        AddDebugMessage($"Player joined: {player.name} (ID: {player.id})");
    }

    private void OnPlayerLeft(string playerId)
    {
        AddDebugMessage($"Player left: {playerId}");
    }

    private void ShowConnectionPanel()
    {
        if (connectionPanel != null) connectionPanel.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(false);
    }

    private void ShowGamePanel()
    {
        if (connectionPanel != null) connectionPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);
    }

    private void UpdateStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }

    private void UpdateGameUI()
    {
        if (networkManager == null) return;

        // Update player count (you'd need to track this in NetworkManager)
        // For now, just show connection status
        if (playerCountText != null)
        {
            playerCountText.text = networkManager.IsConnected() ? "Connected" : "Disconnected";
        }
    }

    private void ToggleDebugPanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(!debugPanel.activeSelf);
        }
    }

    public void AddDebugMessage(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string formattedMessage = $"[{timestamp}] {message}";

        debugMessages.Add(formattedMessage);

        // Keep only the latest messages
        if (debugMessages.Count > maxDebugMessages)
        {
            debugMessages.RemoveAt(0);
        }

        // Update debug text
        if (debugText != null)
        {
            debugText.text = string.Join("\n", debugMessages);

            // Scroll to bottom
            if (debugScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                debugScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (networkManager != null)
        {
            networkManager.OnConnected -= OnConnected;
            networkManager.OnDisconnected -= OnDisconnected;
            networkManager.OnPlayerJoined -= OnPlayerJoined;
            networkManager.OnPlayerLeft -= OnPlayerLeft;
        }
    }
}
