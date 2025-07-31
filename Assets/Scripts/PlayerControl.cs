using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
  //[SerializeField] private GridManager gridManager;
  [SerializeField] private Rigidbody2D _rb;
  [SerializeField] private float _speed = 5;
  [Header("Movement Settings")]
  public float moveSpeed = 5f;
  public float smoothTime = 0.1f;

  private GameNetworkManager networkManager;
  private Vector2 lastSentPosition;
  private float positionSendThreshold = 0.1f; // Send position updates when moved this much
  private float lastSendTime;
  private float sendInterval = 0.05f; // Send at most 20 times per second

  private bool isLocalPlayer = false; // Track if this is the local player

  private Vector2 _input;

  void Start()
  {
    _rb = GetComponent<Rigidbody2D>();
    lastSentPosition = transform.position;

    // If networkManager is not set, try to find it
    if (networkManager == null)
    {
      networkManager = FindFirstObjectByType<GameNetworkManager>();
    }
  }

  public void SetNetworkManager(GameNetworkManager manager)
  {
    networkManager = manager;
    isLocalPlayer = true; // If network manager is set, this is the local player
  }

  private void FixedUpdate()
  {
    // Only process input and movement for local players
    if (!isLocalPlayer) return;

    GatherInput();
    Move();

    // Only send network updates if connected
    if (networkManager != null && networkManager.IsConnected())
    {
      CheckSendPosition();
    }
  }

  private void GatherInput()
  {
    _input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
  }

  private void Move()
  {
    if (_input == Vector2.zero) return;

    // Simple 2D movement using Rigidbody2D
    Vector2 inputDirection = _input.normalized;
    Vector2 newPosition = (Vector2)transform.position + inputDirection * _speed * Time.fixedDeltaTime;
    _rb.MovePosition(newPosition);
  }

  private void CheckSendPosition()
  {
    // Check if we should send position update
    float distanceMoved = Vector2.Distance(lastSentPosition, transform.position);
    float timeSinceLastSend = Time.time - lastSendTime;

    if (distanceMoved > positionSendThreshold && timeSinceLastSend > sendInterval)
    {
      if (networkManager != null)
      {
        networkManager.SendPositionUpdate(transform.position);
        lastSentPosition = transform.position;
        lastSendTime = Time.time;

        // Debug log for troubleshooting
        Debug.Log($"[LOCAL PLAYER] Sent position update: {transform.position} (moved: {distanceMoved:F3})");
      }
      else
      {
        Debug.LogWarning("NetworkManager is null - cannot send position update!");
      }
    }
  }
}

public static class Helpers
{
  private static Matrix4x4 _isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));
  public static Vector3 ToIso(this Vector3 input) => _isoMatrix.MultiplyPoint3x4(input);
}