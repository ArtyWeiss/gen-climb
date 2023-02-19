using System;
using UnityEngine;

public class Controller2d : MonoBehaviour
{
    [Range(-1f, 1f)] public float horizontalInput;
    public float acceleration;
    public float maxVelocity;

    public bool isGrounded;

    public Rigidbody body;
    [Range(0f, 2f)] public float lifterOffset;
    [Range(0f, 2f)] public float lifterRadius;
    [Range(0f, 1f)] public float stickRayOffset;

    public LayerMask groundMask;

    private const float LIFTER_BIAS = 0.8f;
    private const float GRAVITY = -10f;

    private void Update()
    {
        Move();
        StickToGround();
    }

    private void Move()
    {
        var velocity = body.velocity;

        velocity.x += horizontalInput * acceleration * Time.deltaTime;
        if (Math.Abs(velocity.x) > maxVelocity)
        {
            velocity.x = maxVelocity * Mathf.Sign(velocity.x);
        }

        velocity.y = isGrounded ? 0f : velocity.y + GRAVITY * Time.deltaTime;

        body.velocity = velocity;
    }

    private void StickToGround()
    {
        var bodyTransform = body.transform;
        var lifterPos = bodyTransform.position - bodyTransform.up * lifterOffset;
        var ray = new Ray {origin = bodyTransform.position, direction = -bodyTransform.up};

        if (Physics.CheckSphere(lifterPos, lifterRadius, groundMask))
        {
            isGrounded = true;
            var hits = Physics.RaycastAll(ray, lifterOffset + lifterRadius + stickRayOffset, groundMask);
            if (hits.Length > 0)
            {
                var maxHeight = float.NegativeInfinity;
                foreach (var hit in hits)
                {
                    if (hit.point.y > maxHeight)
                    {
                        maxHeight = hit.point.y;
                    }
                }

                var newHeight = maxHeight + lifterRadius * LIFTER_BIAS + lifterOffset;

                bodyTransform.position = new Vector3(bodyTransform.position.x, newHeight, bodyTransform.position.z);
            }
        }
        else
        {
            isGrounded = false;
        }
    }

    private void OnDrawGizmos()
    {
        var bodyTransform = body.transform;
        var lifterPos = bodyTransform.position - bodyTransform.up * lifterOffset;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(lifterPos, lifterRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(bodyTransform.position, -bodyTransform.up * (lifterOffset + lifterRadius + stickRayOffset));
    }
}