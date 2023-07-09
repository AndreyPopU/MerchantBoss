 using System.Collections;
using UnityEngine;

public class BossNecromancer : Enemy
{
    public GameObject satanicCirclePrefab;
    public float dashRange;

    [Header("Cooldowns")]
    public float attackCD = 3; // (overall)
    public float summonCD;
    public float slashCD;
    public float circleCD;

    [Header("Warp")]
    public float warpRange;
    public float teleportCD;
    public ParticleSystem warpEffect;
    private Vector3 warpPosition;
    public LayerMask warpMask;
    
    public Sprite icon;

    private float baseSummonCD, baseSlashCD, baseCircleCD;

    public AudioClip dashClip;
    public ParticleSystem spinEffect;

    private Vector3 dashDirection;
    private float distance;
    private float baseTeleportCD;

    private void Awake()
    {
        baseSummonCD = summonCD;
        baseSlashCD = slashCD;
        baseCircleCD = circleCD;
        baseTeleportCD = teleportCD;
    }

    private new void Start()
    {
        base.Start();

        summonCD = slashCD = circleCD = 0;
        GameManager.instance.bossIcon.sprite = icon;
        smartSlider = GameManager.instance.bossHealthSlider;
        healthText = GameManager.instance.bossHealthText;
        smartSlider.UpdateValues(health, maxHealth);
        GameManager.instance.ShowBossUI();
        armed = true;
    }

    protected new void Wander()
    {
        // Go to point
        if (Vector2.Distance(transform.position, wanderPoint) > .1)
        {
            target = wanderPoint;
            direction = (target - transform.position).normalized;
            rb.velocity = direction * speed * Time.fixedDeltaTime;
        }
        else // Reach point
        {
            rb.velocity = Vector3.zero;

            if (waitTime > 0) waitTime -= Time.deltaTime;
            else WanderPoint();
        }
    }

    void FixedUpdate()
    {
        if (!weapon.isAttacking || weapon.windingUp) Wander();

        if (Player.instance == null || attackCD > 0) return;

        distance = Vector2.Distance(Player.instance.transform.position, transform.position);

        // If player in attack range
        if (distance < attackRange)
        {
            // Slash
            if (slashCD <= 0)
            {
                rb.velocity = Vector3.zero;
                wanderPoint = transform.position;
                waitTime = Random.Range(4, 6);
                weapon.Attack();
                slashCD = baseSlashCD;
                attackCD = Random.Range(4, 8);
                return;
            }
        }
        else
        {
            // Summon
            if (summonCD <= 0) 
            {
                SummonEnemies();
                StartCoroutine(SpinScythe());
                summonCD = baseSummonCD;
                attackCD = Random.Range(2, 6);
                return;
            }
            else
            {
                // Circle
                if (circleCD <= 0)
                {
                    animator.SetFloat("Direction", flipped ? 1 : 0);
                    animator.SetTrigger("Slam");
                    circleCD = baseCircleCD;
                    attackCD = Random.Range(2, 6);
                    return;
                }
            }
        }
    }

    private void Update()
    {
        if (!weapon.windingUp)
        {
            if (rb.velocity.x > 0) FlipSprite(false);
            else if (rb.velocity.x < 0) FlipSprite(true);
        }

        // Deal With Cooldowns
        if (attackCD > 0) attackCD -= Time.deltaTime;
        if (teleportCD > 0) teleportCD -= Time.deltaTime;
        if (slashCD > 0) slashCD -= Time.deltaTime;
        if (summonCD > 0) summonCD -= Time.deltaTime;
        if (circleCD > 0) circleCD -= Time.deltaTime;
    }

    public new void TakeDamage(DamageTaken damageTaken)
    {
        if (health <= 0) DropLoot(18, 24);

        // Dash
        if (teleportCD <= 0)
        {
            // Effect
            warpEffect.transform.position = transform.position;
            warpEffect.Play();

            // Warp in radius
            warpPosition = transform.position + (Vector3)(Vector3.one * (Random.insideUnitCircle.normalized * warpRange));

            // Raycast
            RaycastHit2D hit = Physics2D.Raycast(transform.position, warpPosition - transform.position, warpRange, warpMask);

            // Warp
            if (hit)
            {
                if (hit.collider.gameObject.tag == "Border" || hit.collider.gameObject.layer == 13)
                {
                    Vector3 offset = ((Vector3)hit.point - transform.position).normalized;
                    transform.position = (Vector3)hit.point - offset;
                }
            }
            else transform.position += dashDirection;


            flipPass = true;
            teleportCD = baseTeleportCD;

            // Reset wander
            WanderPoint();
        }

        base.TakeDamage(damageTaken);
    }

    public void WanderPoint()
    {
        // Wander in radius
        wanderPoint = transform.position + (Vector3)(Vector3.one * (Random.insideUnitCircle.normalized * wanderRange));

        // Raycast
        RaycastHit2D hit = Physics2D.Raycast(transform.position, wanderPoint - transform.position, wanderRange, warpMask);

        // Warp
        if (hit)
        {
            if (hit.collider.gameObject.tag == "Border" || hit.collider.gameObject.layer == 13)
            {
                Vector3 offset = ((Vector3)hit.point - transform.position).normalized;
                wanderPoint = (Vector3)hit.point - offset;
            }
        }

        waitTime = Random.Range(4, 6);
    }

    public new void FlipSprite(bool flipped)
    {
        base.FlipSprite(flipped);

        weaponCenter.transform.localScale = new Vector3(flipped ? -1 : 1, 1, 1);

        weapon.GFX.flipX = false;

        if (flipped) // Left
        {
            handL.parent = weapon.GFX.transform;
            handL.localPosition = weapon.handPlacement;
            handR.parent = offHand;
            handR.localPosition = new Vector2(.45f, -0.35f);

            // Weapon
            weapon.transform.localPosition = -Vector3.right * .9f;
        }
        else // Right
        {
            handR.parent = weapon.GFX.transform;
            handR.localPosition = weapon.handPlacement;
            handL.parent = offHand;
            handL.localPosition = new Vector2(-.45f, -0.35f);

            // Weapon
            weapon.transform.localPosition = Vector3.right * .9f;
        }

        handR.rotation = Quaternion.identity;
        handL.rotation = Quaternion.identity;
    }

    public void SlamScythe()
    {
        GameObject circle = Instantiate(satanicCirclePrefab, Player.instance.transform.position, Quaternion.identity);
        Destroy(circle, 4);
        CameraManager.instance.StartCoroutine(CameraManager.instance.Shake(.2f, .1f));
    }

    void SummonEnemies()
    {
        Room.instance.Spawn(1);
    }

    IEnumerator SpinScythe()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        weapon.spinning = true;
        // Particle
        spinEffect.Play();

        float spinForce = 0;
        float spinTime = 1.5f;

        while (spinTime > 0)
        {
            spinTime -= Time.deltaTime;

            if (!flipped) // Right
            {
                if (spinForce > -25) spinForce -= 1;
                else if (spinForce > -60) spinForce -= 20;
            }
            else // Left
            {
                if (spinForce < 25) spinForce += 1;
                else if (spinForce < 60) spinForce += 20;
            }

            weaponCenter.Rotate(0, 0, spinForce);

            yield return waitForFixedUpdate;
        }

        weaponCenter.rotation = Quaternion.identity;
        spinEffect.Stop();
        weapon.spinning = false;
    }
}
