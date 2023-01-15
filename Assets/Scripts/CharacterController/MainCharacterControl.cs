using UnityEngine;

public class MainCharacterControl : MonoBehaviour
{
	public OrbitCameraData cameraData;
	public PlayerMovementData movementData;

	private CameraControls viewControls;
	private MovementControls movementControls;

	private void Awake()
	{
		movementControls = new MovementControls();
		movementControls.Player.Move.performed += ctx => movementData.movementInput = ctx.ReadValue<Vector2>();
		movementControls.Player.Sprint.performed += ctx => movementData.sprintCondition = !movementData.sprintCondition;

		viewControls = new CameraControls();
		viewControls.Player.View.performed += ctx => cameraData.rotationInput = ctx.ReadValue<Vector2>();
	}

	private void OnEnable()
	{
		movementControls.Enable();
		viewControls.Enable();
	}

	private void Update()
	{
		if (movementControls.Player.Jump.triggered) movementData.jump = true;
	}

	private void OnDisable()
	{
		movementControls.Disable();
		viewControls.Disable();
	}
}