using System;
using UnityEngine;

public class OrbitCamera : MonoBehaviour {
	public OrbitCameraData cameraData;
	public PlayerMovementData movementData;

	private Vector3 focusPosition, previousFocusPosition;
	private Vector2 smoothedVelocity;
	private float lastManualRotationTime;

	private Vector3 CameraHalfExtends {
		get {
			Vector3 halfExtends;
			halfExtends.y = cameraData.camera.nearClipPlane * Mathf.Tan(0.5f * Mathf.Deg2Rad * cameraData.camera.fieldOfView);
			halfExtends.x = halfExtends.y * cameraData.camera.aspect;
			halfExtends.z = 0f;
			return halfExtends;
		}
	}

	private void Awake() {
		focusPosition = cameraData.focusTransform.position;
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
		transform.localRotation = Quaternion.Euler(cameraData.orbitAngles);
	}

	private void LateUpdate() {
		UpdateFocusPosition();
		SetCameraFov();

		Quaternion lookRotation;
		if (ManualRotation() || AutomaticRotation()) {
			ConstrainAngles();
			lookRotation = Quaternion.Euler(cameraData.orbitAngles);
		}
		else {
			lookRotation = transform.localRotation;
		}
		var lookDirection = lookRotation * Vector3.forward;
		var lookPosition = focusPosition - lookDirection * cameraData.distance;

		CheckCameraObstructions(lookDirection, lookRotation, ref lookPosition);
		transform.SetPositionAndRotation(lookPosition, lookRotation);
	}

	private void UpdateFocusPosition() {
		previousFocusPosition = focusPosition;
		var currentFocusPosition = cameraData.focusTransform.position;
		if (cameraData.focusRadius > 0f) {
			var distance = Vector3.Distance(currentFocusPosition, focusPosition);
			var blendFactor = 1f;
			// Если дистанция между предыдущим рассчитанным положением и текущим положением таргета превышает радиус фокуса, то начинаем двигать камеру
			if (distance > 0.01f && cameraData.focusCentering > 0f) {
				blendFactor = Mathf.Pow(1f - cameraData.focusCentering, Time.unscaledDeltaTime);
			}
			if (distance > cameraData.focusRadius) {
				blendFactor = Mathf.Min(blendFactor, cameraData.focusRadius / distance);
			}
			focusPosition = Vector3.Lerp(currentFocusPosition, focusPosition, blendFactor);
		}
		else {
			focusPosition = currentFocusPosition;
		}
	}

	private bool ManualRotation() {
		var input = cameraData.rotationInput;
		// Если инпут около нуля, то нчего не делаем
		if (input.x >= -Mathf.Epsilon && input.x <= Mathf.Epsilon && input.y >= -Mathf.Epsilon && input.y <= Mathf.Epsilon) return false;

		lastManualRotationTime = Time.unscaledTime;
		// Рассчитывем сглаженную скорость поворота
		var inputValues = cameraData.rotationSpeed * Time.unscaledDeltaTime * cameraData.smoothing * input;
		smoothedVelocity.x = Mathf.Lerp(smoothedVelocity.x, inputValues.y, 1f / cameraData.smoothing);
		smoothedVelocity.y = Mathf.Lerp(smoothedVelocity.y, inputValues.x, 1f / cameraData.smoothing);
		cameraData.orbitAngles += smoothedVelocity;
		return true;
	}

	private bool AutomaticRotation() {
		if (Time.unscaledTime - lastManualRotationTime < cameraData.alignDelay) return false;
		// Если время задержки прошло, то проверяем, есть ли движение на протяжении последнего кадра
		var movement = new Vector2(focusPosition.x - previousFocusPosition.x, focusPosition.z - previousFocusPosition.z);
		var movementDeltaSqr = movement.sqrMagnitude;
		if (movementDeltaSqr < Mathf.Epsilon) return false;
		// Считаем угол поворота по направлению движения и скорость поворота
		var headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
		var deltaAbs = Mathf.Abs(Mathf.DeltaAngle(cameraData.orbitAngles.y, headingAngle));
		var rotationChange = cameraData.rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
		if (deltaAbs < cameraData.alignSmoothRange) {
			rotationChange *= deltaAbs / cameraData.alignSmoothRange;
		}
		else if (180f - deltaAbs < cameraData.alignSmoothRange) {
			rotationChange *= (180f - deltaAbs) / cameraData.alignSmoothRange;
		}
		cameraData.orbitAngles.y = Mathf.MoveTowardsAngle(cameraData.orbitAngles.y, headingAngle, rotationChange);
		return true;
	}

	private void ConstrainAngles() {
		cameraData.orbitAngles.x = Mathf.Clamp(cameraData.orbitAngles.x, cameraData.minVerticalAngle, cameraData.maxVerticalAngle);

		if (cameraData.orbitAngles.y < 0f) {
			cameraData.orbitAngles.y += 360f;
		}
		else if (cameraData.orbitAngles.y >= 360f) {
			cameraData.orbitAngles.y -= 360f;
		}
	}

	private void SetCameraFov() {
		var blendFactor = Mathf.Pow(1f - cameraData.viewAngleSmoothing, Time.unscaledDeltaTime);
		var relativeSpeed = Mathf.Clamp01(movementData.body.velocity.magnitude / Mathf.Max(movementData.maxSprintSpeed, movementData.maxSpeed));
		var targetViewAngle = Mathf.Lerp(cameraData.defaultCameraViewAngle, cameraData.maxCameraViewAngle, relativeSpeed);
		cameraData.camera.fieldOfView = Mathf.Lerp(targetViewAngle, cameraData.camera.fieldOfView, blendFactor);
	}

	private void CheckCameraObstructions(Vector3 lookDirection, Quaternion lookRotation, ref Vector3 lookPosition) {
		var rectOffset = lookDirection * cameraData.camera.nearClipPlane;
		var rectPosition = lookPosition + rectOffset;
		var castSource = cameraData.focusTransform.position;
		var castLine = rectPosition - castSource;
		var castDistance = castLine.magnitude;
		var castDirection = castLine / castDistance;

		if (!Physics.BoxCast(castSource, CameraHalfExtends, castDirection, out RaycastHit hit, lookRotation, castDistance, cameraData.obstructionMask)) return;
		rectPosition = castSource + castDirection * hit.distance;
		lookPosition = rectPosition - rectOffset;
	}

	private static float GetAngle(Vector2 direction) {
		var angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
		return direction.x < 0f ? 360f - angle : angle;
	}
}