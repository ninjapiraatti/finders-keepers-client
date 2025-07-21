using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
  [SerializeField] private float movementSpeed = 2f;
  //[SerializeField] private GridManager gridManager;
  [SerializeField] private Rigidbody2D _rb;
  [SerializeField] private float _speed = 5;
  //private Vector2 movementDirection;
  //private Tile lastHighlightedTile;

  private Vector2 _input;
  private void Update()
  {
    GatherInput();
  }

  private void FixedUpdate()
  {
    Move();
  }

  private void GatherInput()
  {
    _input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
  }

  private void Move()
  {
    if (_input == Vector2.zero) return;

    // Simple 2D movement
    _rb.MovePosition((Vector2)transform.position + _input.normalized * _speed * Time.deltaTime);
  }
  void Start()
  {
    _rb = GetComponent<Rigidbody2D>();
  }
  /*
  void Update()
  {
    movementDirection = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    //UpdateNearestTile();

    // Check for attack input
    /*
    if (Input.GetButtonDown("Fire1")) // You can change "Fire1" to any other input if needed
    {
      Attack();
    }
    */
}


/*

void FixedUpdate()
{
  rb.velocity = movementDirection * movementSpeed;
}

void UpdateNearestTile()
{
  Tile nearestTile = gridManager.GetNearestTile(transform.position);
  //Debug.Log(nearestTile);

  if (nearestTile != lastHighlightedTile)
  {
    if (lastHighlightedTile != null)
    {
      lastHighlightedTile.UpdateAppearance(false);
    }
    nearestTile.UpdateAppearance(true);
    lastHighlightedTile = nearestTile;
  }
}

void Attack()
{
  if (lastHighlightedTile != null)
  {
    lastHighlightedTile.OnAttack(); // Call the attack method on the nearest tile
  }
}
*/

public static class Helpers
{
  private static Matrix4x4 _isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));
  public static Vector3 ToIso(this Vector3 input) => _isoMatrix.MultiplyPoint3x4(input);
}