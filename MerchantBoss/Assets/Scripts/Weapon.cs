using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public enum Type { Sword, Bow, Staff, Scythe }

    public Type type;

    public Entity wielder;
    public bool isAttacking;
    public bool Qattack;
    private float checkAngle, pivotAngle, mouseAngle;

    [Header("Stats")]
    public int damage;
    public float attackSpeed;
    public float critChance;
    public float shootCD;

    [Header("Sword")]
    public bool alternateTop;
    public float recoverAngle;
    public float smoothness;
    public AudioClip swing1, swing2;

    [Header("Bow")]
    public GameObject arrowPrefab;
    public Transform shootPoint;
    public GameObject indicationArrow;

    [Header("Staff")]
    public float burstInterval;
    public float homingTime;

    [Header("Scythe")]
    public bool windingUp;
    public bool spinning;

    [Header("Weapon positions")]
    public Vector2 handPlacement;
    public Vector2 holdOffset;
    public Vector2 backOffset;
    public float holsterRotation;

    public SpriteRenderer GFX;
    [HideInInspector]
    public float targetAngle;
    public Transform pivotPoint;
    private Vector3 desiredRotation;
    private ParticleSystem slashEffect;
    private Animator animator;
    private AudioSource audioSource;
    private BoxCollider2D coreCollider;
    public bool holstered;
    private Camera cam;
    private float reloadSpeed;

    void Start()
    {
        cam = Camera.main;
        coreCollider = GetComponent<BoxCollider2D>();
        slashEffect = transform.GetComponentInChildren<ParticleSystem>();
        animator = GetComponent<Animator>();
        shootCD = wielder.attackSpeed;
        audioSource = GetComponent<AudioSource>();

        if (type != Type.Sword)
        {
            reloadSpeed = 1 / shootCD;
            if (animator) animator.SetFloat("reloadSpeed", reloadSpeed);
        }
    }

    private void Update()
    {
        if (holstered || !wielder.canMove) return;

        // Angle bullshit
        pivotAngle = pivotPoint.rotation.eulerAngles.z -(recoverAngle * Alternate());

        mouseAngle = GameManager.instance.AngleBetweenTwoPoints(pivotPoint.position, Camera.main.ScreenToWorldPoint(Input.mousePosition));
        checkAngle = pivotAngle - mouseAngle;
    }

    void FixedUpdate()
    {
        if (holstered || !wielder.canMove) return;

        if (type == Type.Sword)
        {
            if (recoverAngle > 30) recoverAngle -= 2.5f * (wielder.attackSpeed * attackSpeed);

            // Handle rotation
            desiredRotation = new Vector3(0f, 0f, targetAngle + recoverAngle * Alternate());
            pivotPoint.rotation = Quaternion.Lerp(pivotPoint.rotation, Quaternion.Euler(desiredRotation), (smoothness + (wielder.attackSpeed * attackSpeed)) * Time.fixedDeltaTime);
        }
        else if (type == Type.Bow) 
            pivotPoint.rotation = Quaternion.Lerp(pivotPoint.rotation, Quaternion.Euler(new Vector3(0f, 0f, targetAngle)), (smoothness + (wielder.attackSpeed * attackSpeed)) * Time.fixedDeltaTime);
        else if (type == Type.Scythe && !windingUp && !spinning)
        {
            if (recoverAngle > 30) recoverAngle -= 2.5f * (wielder.attackSpeed * attackSpeed);

            // Handle rotation
            if (wielder.flipped)
            {
                desiredRotation = new Vector3(0f, 0f, targetAngle - recoverAngle);
            }
            else
            {
                desiredRotation = new Vector3(0f, 0f, targetAngle + recoverAngle);
            }

            pivotPoint.rotation = Quaternion.Lerp(pivotPoint.rotation, Quaternion.Euler(desiredRotation), (smoothness + (wielder.attackSpeed * attackSpeed)) * Time.fixedDeltaTime);
        }

        if (type != Type.Sword && shootCD > 0) shootCD -= Time.fixedDeltaTime;
    }

    IEnumerator WindUp()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        windingUp = true;
        wielder.rb.velocity = Vector2.zero;

        if (wielder.flipped)
        {
            recoverAngle = -30;
            // Fast wind up
            while (recoverAngle > -55)
            {
                recoverAngle -= 2.5f;
                pivotPoint.rotation = Quaternion.Lerp(pivotPoint.rotation, Quaternion.Euler(0, 0, recoverAngle), (smoothness + (wielder.attackSpeed * attackSpeed)) * Time.fixedDeltaTime);
                yield return waitForFixedUpdate;
            }

            // Slow and dramatic wind up
            while (recoverAngle > -65)
            {
                recoverAngle -= .4f;
                pivotPoint.rotation = Quaternion.Lerp(pivotPoint.rotation, Quaternion.Euler(0, 0, recoverAngle), (smoothness + (wielder.attackSpeed * attackSpeed)) * Time.fixedDeltaTime);
                yield return waitForFixedUpdate;
            }
        }
        else
        {
            // Fast wind up
            while (recoverAngle < 55)
            {
                recoverAngle += 2.5f;
                pivotPoint.rotation = Quaternion.Lerp(pivotPoint.rotation, Quaternion.Euler(0, 0, recoverAngle), (smoothness + (wielder.attackSpeed * attackSpeed)) * Time.fixedDeltaTime);
                yield return waitForFixedUpdate;
            }

            // Slow and dramatic wind up
            while (recoverAngle < 65)
            {
                recoverAngle += .4f;
                pivotPoint.rotation = Quaternion.Lerp(pivotPoint.rotation, Quaternion.Euler(0, 0, recoverAngle), (smoothness + (wielder.attackSpeed * attackSpeed)) * Time.fixedDeltaTime);
                yield return waitForFixedUpdate;
            }
        }

        windingUp = false;

        Slash();
    }

    public void Attack()
    {
        if (type == Type.Sword) Slash();
        else if (type == Type.Bow)
        {
            if (shootCD < .25f) Qattack = true;

            if (shootCD > 0) return;
            animator.SetTrigger("fire");
            shootCD = wielder.attackSpeed;
        }
        else if (type == Type.Staff) StartCoroutine(ShootMagic());
        else if (type == Type.Scythe) StartCoroutine(WindUp());

        Qattack = false;
    }

    public void Slash()
    {
        if (recoverAngle < 45) Qattack = true;
        if (wielder == Player.instance && (Mathf.Abs(checkAngle) > 5 && Mathf.Abs(checkAngle) < 355)) return;

        // If sword hasn't returned in start position and therefore isn't ready for a swing
        if (recoverAngle > 31 && type == Type.Sword) return;

        slashEffect.Play();
        alternateTop = !alternateTop;
        audioSource.clip = alternateTop ? swing1 : swing2;
        audioSource.Play();
        if (type == Type.Sword)
        {
            recoverAngle = 135;
            Invoke("DisableTrail", .2f);
        }
        else if (type == Type.Scythe)
        {
            recoverAngle = 330;
            Invoke("DisableTrail", .6f);
        }
        isAttacking = true;
    }

    public void ShootArrow() // Animator event!!!!!!
    {
        audioSource.Play();
        Projectile arrow = GameManager.instance.arrowPool[0];
        GameManager.instance.arrowPool.RemoveAt(0);
        arrow.parentWeapon = this;
        Physics2D.IgnoreCollision(arrow.coreCollider, arrow.parentWeapon.wielder.coreCollider);
        Physics2D.IgnoreCollision(arrow.coreCollider, arrow.parentWeapon.wielder.triggerCollider);
        arrow.transform.position = shootPoint.position;
        arrow.transform.rotation = Quaternion.Euler(new Vector3(0, 0, transform.rotation.eulerAngles.z));
        arrow.Pool();
    }

    public IEnumerator ShootMagic()
    {
        if (shootCD < .25f) Qattack = true;

        if (shootCD > 0) yield break;

        animator.SetTrigger("shoot");
        shootCD = wielder.attackSpeed;

        Transform target = null;

        if (wielder.GetComponent<Player>())
        {
            // Get everything in radius of cursor
            List<Collider2D> enemies = new List<Collider2D>();
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            enemies.AddRange(Physics2D.OverlapCircleAll(mousePos, 15));

            // Filter list of objects
            for (int i = 0; i < enemies.Count; i++)
            {
                if (!enemies[i].GetComponent<Enemy>() && !enemies[i].GetComponent<TargetDummy>())
                {
                    enemies.RemoveAt(i);
                    i--; // Might not work
                }
            }

            // Find closest enemy to cursor
            if (enemies.Count > 1) target = ClosestTo(mousePos, enemies);
            else if (enemies.Count == 1) target = enemies[0].transform;
        }
        else target = Player.instance.transform;

        int projectilesShot = 0;
        int projectilesToShoot = target == Player.instance.transform ? 2 : 3;
        while(projectilesShot < projectilesToShoot)
        {
            yield return new WaitForSeconds(burstInterval);

            // Grab projectile from pool
            Projectile magic = GameManager.instance.magicPool[0];
            GameManager.instance.magicPool.RemoveAt(0);
            magic.parentWeapon = this;

            // Set position and rotation
            Vector2 positionOnScreen = pivotPoint.position;
            Vector2 mouseOnScreen = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float angle = GameManager.instance.AngleBetweenTwoPoints(positionOnScreen, mouseOnScreen);

            Vector3 offset = new Vector3(Random.Range(-.1f, .1f), Random.Range(-.1f, .1f), 0);
            magic.transform.position = shootPoint.position + offset;
            Physics2D.IgnoreCollision(magic.coreCollider, magic.parentWeapon.wielder.coreCollider);
            Physics2D.IgnoreCollision(magic.coreCollider, magic.parentWeapon.wielder.triggerCollider);
            magic.transform.rotation = Quaternion.Euler(0, 0, angle);
            magic.homing = true;
            magic.homingTime = homingTime;
            magic.target = target;
            magic.Pool();

            projectilesShot++;
        }
    }

    public void Holster() // Always opposite to armed
    {
        holstered = !holstered;
        if (slashEffect != null) slashEffect.Stop();
        if (animator != null) animator.enabled = !holstered;

        if (coreCollider != null) coreCollider.enabled = !holstered;

        if (holstered)
        {
            transform.parent = wielder.back;
            transform.localPosition = backOffset;
            transform.rotation = Quaternion.Euler(Vector3.forward * holsterRotation);
        }
        else
        {
            transform.parent = wielder.weaponCenter;
            transform.localPosition = holdOffset;
            if (type == Type.Sword) transform.localRotation = Quaternion.Euler(Vector3.forward * -90);
            else transform.localRotation = Quaternion.identity;
        }

        if (type == Type.Bow)
        {
            indicationArrow.SetActive(!holstered);
            int multiplier = holstered ? -1 : 1;
            transform.localScale = new Vector3(multiplier, 1, 1);
        }
    }

    private int Alternate() // Sword
    {
        if (alternateTop) return 1;
        return -1;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isAttacking) return;

        if (collision.TryGetComponent(out Entity entity) || collision.GetComponent<TargetDummy>())
        {
            if (wielder != entity)
            {
                // Calculate Critical chance
                int critMultiplier = 1;
                if (Random.Range(0, 101) <= wielder.criticalChance + critChance) critMultiplier = 2;

                DamageTaken damageTaken = new DamageTaken((damage + wielder.damage) * critMultiplier, 5, entity.transform.position - wielder.transform.position, critMultiplier);

                if (entity is Player) entity.GetComponent<Player>().TakeDamage(damageTaken);
                else if (entity is BossNecromancer && wielder == Player.instance) entity.GetComponent<BossNecromancer>().TakeDamage(damageTaken);
                else if (entity is EnemyKnight) entity.GetComponent<EnemyKnight>().TakeDamage(damageTaken);
                else if (entity is EnemyArcher) entity.GetComponent<EnemyArcher>().TakeDamage(damageTaken);
                else if (entity is EnemyMage) entity.GetComponent<EnemyMage>().TakeDamage(damageTaken);
                else if (entity is TargetDummy) entity.GetComponent<TargetDummy>().TakeDamage(damageTaken);
                //else entity.TakeDamage(damageTaken);

                entity.swordInvincible = true;
                //entity.TakeDamage(damageTaken);
            }
        }
        else if (collision.TryGetComponent(out TreasureChest chest)) chest.Open();
    }

    private Transform ClosestTo(Vector3 radiusPoint, List<Collider2D> entities)
    {
        Transform closestEntity = entities[0].transform;

        for (int i = 1; i < entities.Count; i++)
        {
            if (Vector2.Distance(radiusPoint, entities[i].transform.position) < (closestEntity.position - radiusPoint).magnitude)
                closestEntity = entities[i].transform;
        }

        return closestEntity;
    }

    private void DisableTrail() // Sword trail
    {
        slashEffect.Stop();
        isAttacking = false;
    }
}
