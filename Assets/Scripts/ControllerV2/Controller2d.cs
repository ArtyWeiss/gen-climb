using System;
using UnityEngine;

public class Controller2d : MonoBehaviour
{
    [Range(-1f, 1f)] public float horizontalInput;
    public float speed;
    public bool isGrounded;

    public Transform lifter;
    public float lifterRadius;
    public float groundCheckDistance;

    private void Update()
    {
        Move();
        StickToGround();
    }

    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(lifter.position, lifterRadius);
    }

    private void Move()
    {
        var deltaMoveX = horizontalInput * Time.deltaTime * speed;
        var currentPosition = transform.position;
        currentPosition.x += deltaMoveX;

        transform.position = currentPosition;
    }

    private void StickToGround()
    {
        var ray = new Ray {origin = lifter.position, direction = -lifter.up};
        Debug.DrawRay(ray.origin, ray.direction * (groundCheckDistance + lifterRadius), Color.magenta, 0.01f, false);

        var sphere_hits = Physics.SphereCastAll(lifter.position, lifterRadius, -lifter.up, groundCheckDistance);
        if (sphere_hits.Length > 0)
        {
            isGrounded = true;

            var hits = Physics.RaycastAll(ray, groundCheckDistance + lifterRadius);
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

                transform.position = new Vector3(transform.position.x, maxHeight + lifterRadius, transform.position.z);
            }
        }
        else
        {
            isGrounded = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(lifter.position, lifterRadius);
        var offset = lifter.up * groundCheckDistance;
        Gizmos.DrawWireSphere(lifter.position - offset, lifterRadius);
    }
}