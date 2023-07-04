using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyArcher : Enemy
{
    public LayerMask raycastPass;

    public float backOffRange = 4;
    private float distance;
    private bool playerTarget;

    // Safe zone
    private int safeMultiplier = 1;
    private Vector2 safeZoneDirection;

    // Radius
    private Transform circleTransform;
    private float circleAngle;
    private Vector2 circleDirection;
    private int radiusMultiplier = 1;
    private float desiredRangeDistance;
    private float changeMovementCD;

    private new void Start()
    {
        base.Start();

        circleTransform = transform.GetChild(0);
        ChangeMovement();
    }

    private void FixedUpdate()
    {
        if (knockbacked || !canMove || stunned) return;

        if (changeMovementCD > 0) changeMovementCD -= Time.deltaTime;

        distance = Vector2.Distance(transform.position, target);

        if (distance > .1f)
        {
            // Direction movement
            direction = (target - transform.position).normalized;

            // Circular movement
            circleAngle = AngleBetweenTwoPoints(transform.position, target);
            circleTransform.rotation = Quaternion.Euler(0, 0, circleAngle);
            circleDirection = circleTransform.up;

            // Desired zone movement
            if (distance > desiredRangeDistance + .5f || distance > attackRange) safeMultiplier = 1;
            else if (distance < desiredRangeDistance - .5f || distance < backOffRange) safeMultiplier = -1;
            else ChangeMovement();
            safeZoneDirection = (target - transform.position).normalized;

            // Overall movement
            if (playerTarget) rb.velocity = (circleDirection * radiusMultiplier + safeZoneDirection * safeMultiplier) * speed * Time.fixedDeltaTime;
            else rb.velocity = direction * speed * Time.fixedDeltaTime;

            // Point weapon towards target
            if (weapon != null) weapon.targetAngle = AngleBetweenTwoPoints(weapon.pivotPoint.position, target);

            // Face target
            if (target.x > transform.position.x) FlipSprite(false);
            else FlipSprite(true);
        }
        else rb.velocity = Vector2.zero;

        if (Player.instance != null && Vector2.Distance(Player.instance.transform.position, transform.position) < aggroRange) // Player in range
        {
            // Set player as destination
            if (!armed) Armed();
            target = Player.instance.transform.position;
            playerTarget = true;
            giveUpTime = baseGiveUpTime;

            // Attack if in range
            if (distance < attackRange)
            {
                // Check if obstacles or enemies are in the way
                RaycastHit2D hit = Physics2D.Raycast(weapon.shootPoint.position, weapon.shootPoint.right, attackRange, raycastPass);
                Debug.DrawRay(weapon.shootPoint.position, weapon.shootPoint.right * attackRange);

                if (hit)
                {
                    if (hit.collider.gameObject.layer == 11) weapon.Attack();
                }
            }
        }
        else
        {
            if (giveUpTime > 0) // Wait
            {
                giveUpTime -= Time.deltaTime;
                target = transform.position;
                playerTarget = false;
            }
            else Wander();
        }

        animator.SetFloat("Speed", rb.velocity.magnitude);
    }

    private void ChangeMovement()
    {
        if (changeMovementCD > 0) return;

        radiusMultiplier = Random.Range(0, 5) > 2 ? 1 : -1;
        desiredRangeDistance = desiredRangeDistance > backOffRange ? backOffRange : attackRange;
        changeMovementCD = 1.5f;
    }

    public new void TakeDamage(DamageTaken damageTaken)
    {
        weapon.shootCD = weapon.attackSpeed;

        base.TakeDamage(damageTaken);
    }

    private new void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, backOffRange);
    }
}
