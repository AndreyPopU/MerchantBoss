using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : Entity
{
    public static Player instance;

    [Header("Sound")]
    public AudioClip shieldSlamClip;
    public AudioClip[] metalClangs;
    public AudioClip[] dashClips;

    [Header("Shield")]
    public GameObject shield;
    public float blockCD;
    public Material steelMat;
    public SpriteMask[] steelEffects;
    public Sprite steelHands;
    public Sprite regularHands;

    [Header("Dash")]
    public float dashRange;
    public float dashCD;
    public bool dashing;

    [Header("Mage Exclusive")]
    public LayerMask warpMask;
    public ParticleSystem warpEffect;
    public ParticleSystem smokeDashEffect;

    private Vector2 movement;
    private Camera cam;
    
    private float chargeTime = 1;
    private Vector2 dashDirection;
    private float baseDashCD;
    private bool invincible;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(this);

        cam = Camera.main;
    }

    new void Start()
    {
        base.Start();
        shaderDefault = gfxComponents[0].material.shader;

        if (cam.ScreenToWorldPoint(Input.mousePosition).x > transform.position.x) FlipSprite(false);
        else FlipSprite(true);

        warpEffect.transform.SetParent(null);

        baseShieldCD = blockCD;
        baseDashCD = dashCD;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) Armed();
        Shield();

        if (!canMove) return;

        if (weapon != null && !weapon.holstered) { if (Input.GetButtonDown("Attack") || weapon.Qattack) weapon.Attack(); }

        if (armed)
        {
            if (dashCD > 0) dashCD -= Time.deltaTime;
            else
            {
                if (Input.GetButtonDown("Jump"))
                {
                    dashing = true;
                    StartCoroutine(Dash());
                    dashCD = baseDashCD;
                }
            }
        }

        if (dashing) return;

        if (armed)
        {
            if (cam.ScreenToWorldPoint(Input.mousePosition).x > transform.position.x) FlipSprite(false);
            else FlipSprite(true);
        }
        else
        {
            if (movement.x > 0) FlipSprite(false);
            else if (movement.x < 0) FlipSprite(true);
        }
    }

    void FixedUpdate()
    {
        if (dashing)
        {
            if (chargeTime > 0) chargeTime -= Time.deltaTime;
            else BreakCharge();
        }

        if (dashing || knockbacked || !canMove) return;

        if (weapon != null)
        {
            // Weapon follow cursor
            Vector2 positionOnScreen = weapon.pivotPoint.position;
            Vector2 mouseOnScreen = cam.ScreenToWorldPoint(Input.mousePosition);
            if (!weapon.isAttacking) weapon.targetAngle = GameManager.instance.AngleBetweenTwoPoints(positionOnScreen, mouseOnScreen);
        }

        movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        rb.velocity = movement * speed * Time.fixedDeltaTime;
        animator.SetFloat("Speed", movement.magnitude);
    }

    public new void FlipSprite(bool flip)
    {
        base.FlipSprite(flip);

        float multiplier = flip ? -1 : 1;
        int direction = flip ? 1 : 0;

        animator.SetFloat("direction", direction);

        if (armed)
        {
            if (flip) // Left
            {
                if (shield != null)
                {
                    shield.transform.parent = handR;
                    shield.transform.localPosition = Vector3.zero;
                    shield.transform.localRotation = Quaternion.identity;
                }
            }
            else // Right
            {
                if (shield != null)
                {
                    shield.transform.parent = handL;
                    shield.transform.localPosition = Vector3.zero;
                    shield.transform.localRotation = Quaternion.identity;
                }
            }

            if (shield != null) shield.GetComponent<SpriteRenderer>().flipX = flip;
        }
        

        if (blocking)
        {
            for (int i = 0; i < steelEffects.Length; i++)
            {
                steelEffects[i].transform.localScale = new Vector3(multiplier, 1, 1);
                steelEffects[i].GetComponent<SpriteRenderer>().flipX = !flipped;
            }
        }
    }

    public new void Armed()
    {
        base.Armed();

        if (shield != null && !dashing)
        {
            if (armed)
            {
                shield.GetComponent<SpriteRenderer>().sortingOrder = 8;
                shield.transform.parent = freeHand;
                shield.transform.localPosition = new Vector3(.15f, -0.25f, 0);
            }
            else
            {
                shield.GetComponent<SpriteRenderer>().sortingOrder = -2;
                shield.transform.parent = back;
                shield.transform.localPosition = Vector3.right * -0.225f;
            }
        }
    }

    public new void TakeDamage(DamageTaken damageTaken)
    {
        if (blocking)
        {
            audioSource.clip = metalClangs[Random.Range(0, metalClangs.Length)];
            audioSource.Play();
            StartCoroutine(Knockback(damageTaken.knockback, damageTaken.knockbackDirection));
        }

        if (invincible) return;

        if (smokeDashEffect.isPlaying) smokeDashEffect.Stop();

        base.TakeDamage(damageTaken);

        if (dashing && gameClass == GameClass.Knight) BreakCharge();

        if (health <= 0)
        {
            invincible = true;
            swordInvincible = true;
            FadePanel.instance.Fade(2, 0);
        }
        else StartCoroutine(Invincibility());
    }

    public void Shield()
    {
        if (shield == null) return;

        if (blockCD > 0) blockCD -= Time.deltaTime; 
        else
        {
            if (!armed || invincible) return;

            if (Input.GetKeyDown(KeyCode.X))
            {
                // Shield slam
                animator.SetTrigger("ShieldSlam");
                blockCD = baseShieldCD;
            }
        }
    }

    public void ShieldSlamEvent()
    {
        // Activate barrier
        audioSource.clip = shieldSlamClip;
        audioSource.Play();
        blocking = true;
        invincible = true;
        CameraManager.instance.StartCoroutine(CameraManager.instance.Shake(.1f, .05f));

        // Steel effect
        float multiplier = flipped ? -1: 1;
        int actualIndex = -1;
        for (int i = 0; i < gfxComponents.Count; i++)
        {
            actualIndex++;
            if (i == 6) break; // Don't make weapons steel
            if (i == 2 || i == 3) { actualIndex--; continue; }// Skip hands
            gfxComponents[i].material = steelMat;
            steelEffects[actualIndex].transform.localScale = new Vector3(multiplier, 1, 1);
            steelEffects[actualIndex].GetComponent<SpriteRenderer>().flipX = !flipped;
            steelEffects[actualIndex].gameObject.SetActive(true);
            flipPass = true;
        }

        handL.GetComponent<SpriteRenderer>().sprite = steelHands;
        handR.GetComponent<SpriteRenderer>().sprite = steelHands;

        Invoke("StopBlocking", 3);
    }

    public void ShieldChargeEvent()
    {
        AudioSource.PlayClipAtPoint(shieldSlamClip, transform.position);
        audioSource.clip = dashClips[2];
        audioSource.Play();
        invincible = true;
        smokeDashEffect.Play();
        rb.AddForce(dashDirection, ForceMode2D.Impulse);
        chargeTime = 1;
    }

    public IEnumerator Dash()
    {
        // If input is neutral return
        if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0) yield break;

        Vector2 rawDashDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        dashDirection = rawDashDirection.normalized * dashRange;

        if (gameClass == GameClass.Knight)
        {
            // Flip sprite in direction
            if (rawDashDirection.x > 0) FlipSprite(false);
            else if (rawDashDirection.x < 0) FlipSprite(true);

            // Hide weapon and deal with hand placement
            weapon.Holster();
            if (!flipped)
            {
                handR.localPosition = new Vector3(0, -.2f, 0);
                handR.GetComponent<SpriteRenderer>().sortingOrder = -8;
                handR.rotation = Quaternion.identity;
            }
            else
            {
                handL.localPosition = new Vector3(0, -.2f, 0);
                handL.GetComponent<SpriteRenderer>().sortingOrder = -8;
                handL.rotation = Quaternion.identity;
            }

            // Charge build up Animation
            animator.SetBool("dash", dashing);
            smokeDashEffect.Play();
            rb.AddForce(dashDirection, ForceMode2D.Impulse);
            chargeTime = 1;
            rb.velocity = Vector2.zero;

        }
        else if (gameClass == GameClass.Archer)
        {
            audioSource.clip = dashClips[1];
            audioSource.Play();
            smokeDashEffect.Play();
            rb.velocity = Vector2.zero;
            rb.AddForce(dashDirection, ForceMode2D.Impulse);
            yield return new WaitForSecondsRealtime(.1f);
            smokeDashEffect.Stop();
            dashing = false;
            flipPass = true;
        }
        else if (gameClass == GameClass.Wizard)
        {
            // Effect
            warpEffect.transform.position = transform.position;
            audioSource.clip = dashClips[2];
            audioSource.Play();
            warpEffect.Play();

            // Raycast
            RaycastHit2D hit = Physics2D.Raycast(transform.position, rawDashDirection, 3, warpMask);

            // Warp
            if (hit)
            {
                if (hit.collider.gameObject.tag == "Border")
                {
                    Vector3 offset = ((Vector3)hit.point - transform.position).normalized;
                    transform.position = (Vector3)hit.point - offset * .35f;
                }
                else transform.position += (Vector3)dashDirection;
            }
            else transform.position += (Vector3)dashDirection;

            dashing = false;
            flipPass = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Collectable collectable))
        {
            GameManager.instance.coins += collectable.value;
            AudioSource.PlayClipAtPoint(collectable.pickUp, collectable.transform.position);
            Destroy(collectable.gameObject);
        }

        if (gameClass == GameClass.Knight && dashing)
        {
            if (collision.TryGetComponent(out Entity entity))
            {
                // Calculate Critical chance
                int critMultiplier = 1;
                if (Random.Range(0, 101) <= criticalChance) critMultiplier = 2;

                DamageTaken damageTaken = new DamageTaken(damage * critMultiplier, 10, entity.transform.position - transform.position, critMultiplier);
                if (dashing) damageTaken.knockback *= 1.5f;

                if (entity is EnemyKnight) entity.GetComponent<EnemyKnight>().TakeDamage(damageTaken);
                else if (entity is TargetDummy) entity.GetComponent<TargetDummy>().TakeDamage(damageTaken);
                else entity.TakeDamage(damageTaken);

                if (dashing)
                {
                    BreakCharge();
                    entity.Stun(1.5f);
                }
            }
            else if (collision.TryGetComponent(out TreasureChest chest))
            {
                chest.Open();
                BreakCharge();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (gameClass == GameClass.Knight && dashing) BreakCharge();
    }

    private void BreakCharge()
    {
        audioSource.Stop();
        weapon.Holster();
        if (chargeTime > 0) CameraManager.instance.StartCoroutine(CameraManager.instance.Shake(.15f, .05f));
        chargeTime = 0;
        smokeDashEffect.Stop();
        dashing = false;
        animator.SetBool("dash", dashing);
        flipPass = true;
        handL.GetComponent<SpriteRenderer>().sortingOrder = 6;
        handR.GetComponent<SpriteRenderer>().sortingOrder = 8;
        invincible = false;
    }

    private IEnumerator Invincibility()
    {
        invincible = true;
        swordInvincible = true;

        yield return new WaitForSeconds(.3f);
        int ticks = 10;
        int alpha;
        float time = .2f;

        while(ticks > 0)
        {
            if (blocking) break;

            if (ticks % 2 == 0) 
            {
                alpha = 0; 
                if (time > .06f) time -= .03f;
            }
            else alpha = 1;

            foreach (SpriteRenderer sprite in gfxComponents) sprite.color = new Color(1, 1, 1, alpha);
            if (weapon != null)
            {
                weapon.GFX.color = new Color(1, 1, 1, alpha);

                if (weapon.type == Weapon.Type.Bow)
                    weapon.transform.GetChild(2).GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, alpha);
            }

            if (shield != null) shield.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, alpha);

            yield return new WaitForSeconds(time);
            ticks--;
        }

        if (blocking)
        {
            for (int i = 0; i < gfxComponents.Count; i++)
            {
                if (i < 6) gfxComponents[i].material = steelMat;
                gfxComponents[i].color = Color.white;
            }
            print("Interupted");
        }

        invincible = false;
        swordInvincible = false;
    }

    private void StopBlocking()
    {
        blocking = false;
        invincible = false;
        swordInvincible = false;

        int actualIndex = -1;
        for (int i = 0; i < gfxComponents.Count; i++)
        {
            actualIndex++;
            if (i == 6) break; // Don't make weapons steel
            if (i == 2 || i == 3) { actualIndex--; continue; } // Skip hands
            gfxComponents[i].material.shader = shaderDefault;
            steelEffects[actualIndex].gameObject.SetActive(false);
            flipPass = true;
        }
        handL.GetComponent<SpriteRenderer>().sprite = regularHands;
        handR.GetComponent<SpriteRenderer>().sprite = regularHands;
    }

    public void SetStats(StatsUp classStats)
    {
        health = classStats.health;
        damage = classStats.damage;
        attackSpeed = classStats.attackSpeed;
        speed = classStats.speed;
        criticalChance = classStats.criticalChance;
        dashRange = classStats.dashRange;
        baseDashCD = classStats.dashCD;

        // Setup UI
        maxHealth = health;
        smartSlider.UpdateValues(health, maxHealth);
        if (healthText != null) healthText.text = health + "/" + maxHealth;
    }

    public void AddStats(StatsUp classStats)
    {
        health += classStats.health;
        damage += classStats.damage;
        attackSpeed += classStats.attackSpeed;
        speed += classStats.speed;
        criticalChance += classStats.criticalChance;
        dashRange += classStats.dashRange;
        baseDashCD += classStats.dashCD;

        // Setup UI
        maxHealth = health;
        smartSlider.UpdateValues(health, maxHealth);
        if (healthText != null) healthText.text = health + "/" + maxHealth;
    }

    public void Descend() => StartCoroutine(DescentCO());

    public void Ascend() => StartCoroutine(AscentCO());

    private IEnumerator DescentCO()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        float descentFactor = .035f;
        float alpha = 1;

        while(transform.localScale.x > .001f)
        {
            transform.localScale -= Vector3.one * descentFactor;
            alpha -= descentFactor;
            foreach (SpriteRenderer sprite in gfxComponents) sprite.color = new Color(1, 1, 1, alpha);
            yield return waitForFixedUpdate;
        }

        transform.localScale = Vector3.zero;
    }

    private IEnumerator AscentCO()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        float descentFactor = .035f;
        float alpha = 0;

        while (transform.localScale.x < 1)
        {
            transform.localScale += Vector3.one * descentFactor;
            alpha += descentFactor;
            foreach (SpriteRenderer sprite in gfxComponents) sprite.color = new Color(1, 1, 1, alpha);
            yield return waitForFixedUpdate;
        }

        transform.localScale = Vector3.one;
    }

    public void Immobilize(bool move)
    {
        canMove = !move;
        if (!canMove)
        { 
            rb.velocity = Vector2.zero;
            animator.SetFloat("Speed", 0);
        }
    }

    private void Default()
    {
        foreach (SpriteRenderer sh in gfxComponents)
            sh.material.shader = shaderDefault;
    }

    public void Invisible(int alpha)
    {
        foreach (SpriteRenderer sprite in gfxComponents) sprite.color = new Color(1, 1, 1, alpha);
    }
}
