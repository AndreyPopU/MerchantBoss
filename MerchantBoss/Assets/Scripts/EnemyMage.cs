using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMage : Enemy
{
    public float teleportCD;
    public float teleportRange;
    public ParticleSystem warpEffect;
    public LayerMask warpMask;
    
    protected Vector3 warpPosition;
    private Transform attackTarget;
    protected float baseTeleportCD;

    private new void Start()
    {
        base.Start();
        baseTeleportCD = teleportCD;
    }

    private void FixedUpdate()
    {
        if (knockbacked || !canMove || stunned) return;

        // Face target if there's one
        if (attackTarget != null)
        {
            if (attackTarget.position.x > transform.position.x) FlipSprite(false);
            else FlipSprite(true);
        }

        // Walk
        if (Vector2.Distance(transform.position, target) > .1f)
        {
            direction = (target - transform.position).normalized;
            rb.velocity = direction * speed * Time.fixedDeltaTime;
        }
        else rb.velocity = Vector2.zero;

        if (Player.instance != null && Vector2.Distance(Player.instance.transform.position, transform.position) < aggroRange) // Player in range
        {
            // Set player as destination
            if (!armed) Armed();
            attackTarget = Player.instance.transform;
            giveUpTime = baseGiveUpTime;

            // Attack if in range
            if (Vector2.Distance(Player.instance.transform.position, transform.position) < attackRange) weapon.Attack();
        }
        else attackTarget = null;

        Wander();


        animator.SetFloat("Speed", rb.velocity.magnitude);
    }

    public new void TakeDamage(DamageTaken damageTaken)
    {
        base.TakeDamage(damageTaken);

        if (!canMove || stunned) return;

        // Chance to teleport
        if (teleportCD <= 0) Warp();
    }

    private void Warp()
    {
        // Effect
        warpEffect.transform.position = transform.position;
        warpEffect.Play();

        // Warp in radius
        warpPosition = transform.position + (Vector3)(Vector3.one * (Random.insideUnitCircle.normalized * teleportRange));

        // Raycast
        RaycastHit2D hit = Physics2D.Raycast(transform.position, warpPosition - transform.position, teleportRange, warpMask);

        // Warp
        if (hit)
        {
            if (hit.collider.gameObject.tag == "Border" || hit.collider.gameObject.layer == 13)
            {
                Vector3 offset = ((Vector3)hit.point - transform.position).normalized;
                transform.position = (Vector3)hit.point;
            }
            else transform.position = warpPosition;
        }
        else transform.position = warpPosition;

        flipPass = true;
        teleportCD = baseTeleportCD;
    }

    private new void Wander()
    {
        if (waitTime > 0) waitTime -= Time.deltaTime;
        else
        {
            // Go to point
            if (Vector2.Distance(transform.position, wanderPoint) > .1) target = wanderPoint;
            else // Reach point
            {
                if (waitTime > 0) waitTime -= Time.deltaTime;
                else
                {
                    wanderPoint = spawnPoint + (Vector3)Random.insideUnitCircle * wanderRange;
                    waitTime = Random.Range(1, 4);
                }
            }
        }
    }
}
