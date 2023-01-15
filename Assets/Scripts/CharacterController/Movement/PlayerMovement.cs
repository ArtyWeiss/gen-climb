using UnityEngine;

public class PlayerMovement : MonoBehaviour {
	public PlayerMovementData movementData;

	private float acceleration;
	private float movementSpeed;
	private Vector3 desiredSpeed, previousDesiredSpeed;
	private int jumpPhase;
	private float minGroundDotProduct;
	private float minStairsDotProduct;
	private Vector3 contactNormal;
	private Vector3 steepNormal;
	private Vector3 velocity;
	private int stepsSinceLastGrounded;
	private int stepsSinceLastJump;

	private void Awake() {
		minGroundDotProduct = Mathf.Cos(movementData.maxGroundAngle * Mathf.Deg2Rad);
		minStairsDotProduct = Mathf.Cos(movementData.maxStairsAngle * Mathf.Deg2Rad);
	}

	private void OnCollisionEnter(Collision other) {
		EvaluateCollision(other);
	}

	private void OnCollisionStay(Collision other) {
		EvaluateCollision(other);
	}

	private void FixedUpdate() {
		UpdateState();
		Move();
		Jump();
		movementData.body.velocity = velocity;
		ClearState();
	}

	private void UpdateState() {
		stepsSinceLastGrounded += 1;
		stepsSinceLastJump += 1;
		velocity = movementData.body.velocity;
		if (movementData.onGround || SnapToGround() || CheckSteepContacts()) {
			stepsSinceLastGrounded = 0;
			jumpPhase = 0;
			contactNormal.Normalize();
		}
		else {
			contactNormal = Vector3.up;
		}
	}

	private void Move() {
		SetAcceleration();
		GetMovementDirection();
		AdjustVelocity();
		UpdateSprintCondition();
	}

	private void Jump() {
		if (!movementData.jump) return;
		if (movementData.onGround || jumpPhase < movementData.maxAirJumps) {
			stepsSinceLastJump = 0;
			jumpPhase += 1;
			var body = movementData.body;
			var jumpDirection = (contactNormal + Vector3.up).normalized;
			var jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * movementData.jumpHeight); // Рассчет скорости исходя из гравитации и необходимой высоты прыжка
			var alignedSpeed = Vector3.Dot(body.velocity, jumpDirection);
			if (alignedSpeed > 0f) {
				jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f); // Если скорость направлена вверх, то урезаем скорость прыжка
			}
			else if (alignedSpeed < 0f && movementData.fallingStopFactor > 0f) {
				jumpSpeed = jumpSpeed - alignedSpeed * movementData.fallingStopFactor; // Если прыгаем в падении, то добавляем к скорости прыжка часть скорости падения
			}
			velocity += jumpDirection * jumpSpeed;
		}
		movementData.jump = false;
	}

	private void ClearState() {
		movementData.onGround = false;
		movementData.onSteep = false;
	}

	private void SetAcceleration() {
		acceleration = movementData.onGround ? movementData.maxAcceleration : movementData.maxAirAcceleration;
		movementSpeed = movementData.sprintCondition ? movementData.maxSprintSpeed : movementData.maxSpeed;
	}

	private void GetMovementDirection() {
		previousDesiredSpeed = desiredSpeed;
		if (movementData.playerInputSpace) {
			var inputSpace = movementData.playerInputSpace;
			var forward = inputSpace.forward;
			forward.y = 0f;
			forward.Normalize();
			var right = inputSpace.right;
			right.y = 0f;
			right.Normalize();
			desiredSpeed = (forward * movementData.movementInput.y + right * movementData.movementInput.x) * movementSpeed;
		}
		else {
			desiredSpeed = new Vector3(movementData.movementInput.x, 0f, movementData.movementInput.y) * movementSpeed;
		}
	}

	private void AdjustVelocity() {
		// Получаем X и Z в плоскости контакта 
		var xAxis = ProjectOnContactPlane(movementData.body.transform.right).normalized;
		var zAxis = ProjectOnContactPlane(movementData.body.transform.forward).normalized;
		// Проецируем скорость на полученные вектора
		var currentX = Vector3.Dot(velocity, xAxis);
		var currentZ = Vector3.Dot(velocity, zAxis);
		// Считаем новые координаты, используя выровненные вектора
		var maxSpeedChange = acceleration * Time.deltaTime;
		var newX = Mathf.MoveTowards(currentX, desiredSpeed.x, maxSpeedChange);
		var newZ = Mathf.MoveTowards(currentZ, desiredSpeed.z, maxSpeedChange);

		velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
	}

	private void UpdateSprintCondition() {
		// Выключаем спринт, если поменялся угол движения более чем на 90 градусов, либо если была остановка
		var dot = Vector3.Dot(previousDesiredSpeed, desiredSpeed);
		if (dot < 0f || desiredSpeed.magnitude <= 0f) movementData.sprintCondition = false;
	}

	private void EvaluateCollision(Collision collision) {
		var minDotProduct = GetMinDot(collision.gameObject.layer);
		for (int i = 0; i < collision.contactCount; i++) {
			var normal = collision.GetContact(i).normal;
			if (normal.y >= minDotProduct) {
				movementData.onGround = true;
				// Усредняем нормаль, чтобы при более чем одном контакте вектор получался каждый раз одинаковый
				contactNormal += normal;
			}
			else if (normal.y > -0.01f) {
				movementData.onSteep = true;
				steepNormal += normal;
			}
		}
	}

	private Vector3 ProjectOnContactPlane(Vector3 vector) {
		return vector - contactNormal * Vector3.Dot(vector, contactNormal);
	}

	private bool SnapToGround() {
		// Делаем снапинг, только если был потерян контакт на один шаг симуляции. Первые шаги во время прыжка снапинг не делаем.
		if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 4) return false;
		// Если текущая скорость выше максимальной возможной для снаппинга, даже не пытаемся делать рейкаст
		var speed = velocity.magnitude;
		if (speed > movementData.maxSnapSpeed) return false;

		if (!Physics.Raycast(movementData.body.position, Vector3.down, out RaycastHit hit, movementData.probeDistance, movementData.probeMask)) return false;
		// Проверяем, угол нормали хита
		if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer)) return false;

		contactNormal = hit.normal;
		var dot = Vector3.Dot(velocity, hit.normal);
		// Если угол между текущей скоростью и нормалью хита рекаста больше 90, т.е. движимся вверх относительно плоскости пола
		if (dot > 0f) {
			velocity = (velocity - hit.normal * dot).normalized * speed;
		}
		return true;
	}

	private bool CheckSteepContacts() {
		if (movementData.onSteep) {
			steepNormal.Normalize();
			if (steepNormal.y >= minGroundDotProduct) {
				movementData.onGround = true;
				contactNormal = steepNormal;
				return true;
			}
		}
		return false;
	}

	float GetMinDot(int layer) {
		// Проверяем, является ли слой лестницой или чем-то другим
		return (movementData.stairsMask & (1 << layer)) == 0 ? minGroundDotProduct : minStairsDotProduct;
	}

	private void OnDrawGizmos() {
		Gizmos.color = movementData.onGround ? Color.green : Color.red;
		Gizmos.DrawWireSphere(transform.position, movementData.playerCollider.radius);
		Gizmos.DrawRay(transform.position, Vector3.down * movementData.probeDistance);
		Gizmos.color = Color.cyan;
		Gizmos.DrawRay(transform.position, velocity);
	}
}