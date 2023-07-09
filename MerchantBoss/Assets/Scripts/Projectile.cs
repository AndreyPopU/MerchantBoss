using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Weapon parentWeapon;
    public float speed;
    public float homingTime;
    public bool pooled;
    public bool arrow;
    public bool homing;
    public Transform target;

    [HideInInspector]
    public Collider2D coreCollider;
    [HideInInspector]
    public Vector2 direction;
    private Rigidbody2D rb;
    private bool dealsDamage = true;
    public ParticleSystem trailEffect;
    public ParticleSystem splashEffect;
    private float targetAngle;
    private Vector3 desiredRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coreCollider = GetComponent<Collider2D>();
        trailEffect = GetComponentInChildren<ParticleSystem>();
        splashEffect = transform.GetChild(1).GetComponent<ParticleSystem>();

        arrow = name.Contains("Arrow") ? true : false;
    }

    void FixedUpdate()
    {
        if (dealsDamage && !pooled)
        {
            if (homingTime > 0 && target != null) 
            {
                // Rotate towards target
                targetAngle = GameManager.instance.AngleBetweenTwoPoints(transform.position, target.position);
                desiredRotation = Vector3.forward * targetAngle;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(desiredRotation), .125f);
                homingTime -= Time.fixedDeltaTime;
            }
            rb.velocity = transform.right * speed;
        }
    }

    public void Pool()
    {
        if (pooled)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            trailEffect.Play();
            dealsDamage = true;
        }
        else
        {
            if (parentWeapon != null && parentWeapon.wielder != null)
            {
                Physics2D.IgnoreCollision(coreCollider, parentWeapon.wielder.coreCollider);
                Physics2D.IgnoreCollision(coreCollider, parentWeapon.wielder.triggerCollider);
            }
            Splash();
            trailEffect.Stop();
            parentWeapon = null;

            if (arrow) GameManager.instance.arrowPool.Add(this);
            else GameManager.instance.magicPool.Add(this);
            transform.position = Vector3.one * 5000;
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            dealsDamage = false;
        }

        pooled = !pooled;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!dealsDamage || pooled) return;

        if (collision.TryGetComponent(out Entity entity))
        {
            if (parentWeapon != null && parentWeapon.wielder != null && parentWeapon.wielder != entity)
            {
                // Calculate Critical chance
                int critMultiplier = 1;
                if (Random.Range(0, 101) <= parentWeapon.wielder.criticalChance + parentWeapon.critChance) critMultiplier = 2;

                DamageTaken damageTaken = new DamageTaken((parentWeapon.damage + parentWeapon.wielder.damage) * critMultiplier, 5, entity.transform.position - parentWeapon.wielder.transform.position, critMultiplier);
                if (entity is Player) entity.GetComponent<Player>().TakeDamage(damageTaken);
                else if (entity is BossNecromancer && parentWeapon.wielder == Player.instance) entity.GetComponent<BossNecromancer>().TakeDamage(damageTaken);
                else if (entity is EnemyKnight) entity.GetComponent<EnemyKnight>().TakeDamage(damageTaken);
                else if (entity is EnemyArcher) entity.GetComponent<EnemyArcher>().TakeDamage(damageTaken);
                else if (entity is EnemyMage) entity.GetComponent<EnemyMage>().TakeDamage(damageTaken);
                else if (entity is TargetDummy) entity.GetComponent<TargetDummy>().TakeDamage(damageTaken);
                //else entity.TakeDamage(damageTaken);

                if (entity.TryGetComponent(out Enemy enemy)) enemy.aggroRange = 15;

                if (!pooled) Pool();
            }
        }
        else if (collision.TryGetComponent(out TreasureChest chest))
        {
            chest.Open();
            if (!pooled) Pool();
        }

        if (collision.TryGetComponent(out Weapon weapon) && weapon.isAttacking) // Deflection
        {
            // Arrow
            if (arrow)
            {
                if (parentWeapon.wielder != null)
                {
                    Physics2D.IgnoreCollision(coreCollider, parentWeapon.wielder.coreCollider);
                    Physics2D.IgnoreCollision(coreCollider, parentWeapon.wielder.triggerCollider);
                }
                target = parentWeapon.wielder.transform;
                transform.rotation = Quaternion.Euler(new Vector3(0, 0, GameManager.instance.AngleBetweenTwoPoints(transform.position, target.position)));
                parentWeapon = weapon;
            }
            else // Magic
            {
                if (!pooled) Pool(); 
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<TargetDummy>()) return;

        if (arrow)
        {
            dealsDamage = false;
            coreCollider.enabled = false;
            rb.velocity = transform.right * speed;
            Invoke("Stick", .02f);
        }
        else
        {
            if (!pooled) Pool();
        }
    }

    private void Stick()
    {
        Splash();
        rb.velocity = Vector3.zero;
        rb.isKinematic = false;
        trailEffect.Stop();
        Invoke("Pool", 3);
    }

    private void Splash()
    {
        if (splashEffect != null)
        {
            splashEffect.transform.SetParent(null);
            splashEffect.Play();
            Invoke("ReturnSplash", 1);
        }
    }

    private void ReturnSplash()
    {
        splashEffect.transform.parent = transform;
        splashEffect.transform.localPosition = Vector3.zero;
    }
}

public class DamageTaken
{
    public int damage, critMultiplier;
    public float knockback;
    public Vector2 knockbackDirection;

    public DamageTaken(int _damage, float _knockback, Vector2 _knockbackDirection, int _critMultiplier)
    {
        damage = _damage;
        knockback = _knockback;
        knockbackDirection = _knockbackDirection;
        critMultiplier = _critMultiplier;
    }
}

