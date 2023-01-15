using UnityEngine;

public class PlayerMovementData : MonoBehaviour {
	public bool onGround;
	public bool onSteep;

	[Header("Walk settings")]
	[Range(0f, 100f)] public float maxAcceleration = 50f;
	[Range(0f, 100f)] public float maxSpeed = 8f;
	[Range(0f, 100f)] public float maxSprintSpeed = 12f;
	[Range(0f, 90f)] public float maxGroundAngle = 40f;
	[Range(0f, 90f)] public float maxStairsAngle = 50f;
	public LayerMask stairsMask = -1;

	[Header("Snapping settings")]
	[Range(0f, 100f)] public float maxSnapSpeed = 100f;
	[Min(0f)] public float probeDistance = 1f;
	public LayerMask probeMask = -1;

	[Header("Jump settings")]
	public bool jump;
	public float jumpHeight = 2f;
	public int maxAirJumps = 1;
	[Range(0f, 1f)] public float fallingStopFactor = 0.5f;

	[Header("Fly settings")]
	[Range(0f, 100f)] public float maxAirAcceleration = 5f;

	[Header("References")]
	public Rigidbody body;
	public SphereCollider playerCollider;
	public Transform playerInputSpace = default;

	[HideInInspector] public Vector2 movementInput;
	[HideInInspector] public bool sprintCondition;
}