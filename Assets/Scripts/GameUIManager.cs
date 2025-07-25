using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("Game UI")]
    public GameObject gamePanel;
    public TextMeshProUGUI playerListText;
    public Button quitButton;

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
        ShowGamePanel();

        // Setup quit button
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
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

    public void OnQuitClicked()
    {
        AddDebugMessage("Quitting application...");

        // If connected, disconnect first
        if (networkManager != null && networkManager.IsConnected())
        {
            // You might want to call a disconnect method here if available
            // networkManager.Disconnect();
        }

        Application.Quit();

        // For editor testing
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnConnected(string serverUrl)
    {
        ShowGamePanel();
        AddDebugMessage($"Successfully connected to {serverUrl}");
    }

    private void OnDisconnected()
    {
        AddDebugMessage("Disconnected from server");
    }

    private void OnPlayerJoined(Player player)
    {
        AddDebugMessage($"Player joined: {player.name} (ID: {player.id})");
    }

    private void OnPlayerLeft(string playerId)
    {
        AddDebugMessage($"Player left: {playerId}");
    }

    private void ShowGamePanel()
    {
        if (gamePanel != null) gamePanel.SetActive(true);
    }

    private void UpdateGameUI()
    {
        if (networkManager == null) return;

        // Update player list here if needed
        // For now, this method can be used for other UI updates
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
