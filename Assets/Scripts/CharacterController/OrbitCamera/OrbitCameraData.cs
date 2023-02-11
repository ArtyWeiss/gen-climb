using UnityEngine;

public class OrbitCameraData : MonoBehaviour {
	[Header("Follow settings")]
	public Transform focusTransform;
	[Range (1f, 20f)] public float distance;
	[Min(0f)] public float focusRadius = 1f;
	[Range (0f, 1f)] public float focusCentering = 0.5f;

	[Header("Rotation settings")] public bool fixedRotation;
	public Vector2 orbitAngles;
	[Range(1f, 360f)] public float rotationSpeed = 90f;
	public float smoothing = 2f;

	[Header("Align settings")]
	[Min(0f)] public float alignDelay = 5f;
	[Range(0f, 90f)] public float alignSmoothRange = 45f;

	[Header("Dynamic view angle settings")] 
	[Range (0f, 1f)] public float viewAngleSmoothing = 0.5f;
	public float defaultCameraViewAngle = 60f;
	public float maxCameraViewAngle = 90f;

	[Header("Camera constraints")] 
	[Range(-89f, 89f)] public float minVerticalAngle = -70f;
	[Range(-89f, 89f)] public float maxVerticalAngle = 89f;
	public LayerMask obstructionMask = -1;

	[Header("References")]
	public new Camera camera;

	[HideInInspector] public Vector2 rotationInput;
}