using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyKnight : Enemy
{
    [Header("Hands Manager")]
    private float timingOffset;

    private void FixedUpdate()
    {
        if (knockbacked || !canMove || stunned) return;

        if (Vector2.Distance(transform.position, target) > .1f)
        {
            direction = (target - transform.position).normalized;
            rb.velocity = direction * speed * Time.fixedDeltaTime;

            // Point weapon towards target
            if (weapon != null && Player.instance != null)
            {
                // Weapon follow player
                if (!weapon.isAttacking) weapon.targetAngle = AngleBetweenTwoPoints(weapon.pivotPoint.position, Player.instance.transform.position);
            }

            if (target.x > transform.position.x) FlipSprite(false);
            else FlipSprite(true);
        }
        else rb.velocity = Vector2.zero;

        if (Player.instance != null && Vector2.Distance(Player.instance.transform.position, transform.position) < aggroRange) // Player in range
        {
            // Set player as destination
            if (!armed) Armed();
            target = Player.instance.transform.position;
            giveUpTime = baseGiveUpTime;

            // Attack if in range
            if (Vector2.Distance(Player.instance.transform.position, transform.position) < attackRange)
            {
                if (timingOffset > 0) timingOffset -= Time.fixedDeltaTime;
                else weapon.Attack();
            }
        }
        else
        {
            if (giveUpTime > 0) // Wait
            {
                giveUpTime -= Time.deltaTime;
                target = transform.position;
            }
            else Wander();
        }
        
        animator.SetFloat("Speed", rb.velocity.magnitude);
    }

    public new void TakeDamage(DamageTaken damageTaken)
    {
        timingOffset = Random.Range(0, .75f);

        base.TakeDamage(damageTaken);
    }
}
