using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Entity : MonoBehaviour
{
    public enum GameClass { Knight, Archer, Wizard }
    public GameClass gameClass;

    public Weapon weapon;

    [Header("Stats")]
    public int health;
    public int damage;
    public float speed;
    public float attackSpeed;
    public int criticalChance;
    public bool stunned;
    public bool knockbacked;
    public bool canMove;

    [Header("Health")]
    public MySlider smartSlider;
    public Text healthText;

    [Header("Hands Manager")]
    public Transform back;
    public Transform freeHand;
    public Transform offHand;
    public Transform weaponCenter;
    protected Transform handL, handR;
    public List<SpriteRenderer> gfxComponents;
    public bool flipPass;
    public bool armed;

    [Header("Other")]
    public GameObject GFX;
    public BoxCollider2D coreCollider;
    public BoxCollider2D triggerCollider;
    public GameObject stunEffect;
    public ParticleSystem xpEffect;
    public AudioClip hitEffect;
    public AudioClip critEffect;
    public Transform pivotPoint;

    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected Shader shaderDefault;
    protected Shader shaderWhite;
    protected AudioSource audioSource;
    public bool swordInvincible;
    protected bool blocking;
    protected float baseShieldCD;
    protected int maxHealth;
    protected bool flipped;

    public void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GFX.GetComponent<SpriteRenderer>();
        shaderWhite = Shader.Find("GUI/Text Shader");
        if (spriteRenderer != null) shaderDefault = spriteRenderer.material.shader;

        handL = gfxComponents[2].transform;
        handR = gfxComponents[3].transform;

        // Setup UI
        maxHealth = health;
        smartSlider.UpdateValues(health, maxHealth);
        if (healthText != null) healthText.text = health + "/" + maxHealth;
    }

    public void TakeDamage(DamageTaken damageTaken)
    {
        if (swordInvincible) return;

        StartCoroutine(Knockback(damageTaken.knockback, damageTaken.knockbackDirection));

        if (blocking) return;

        // Deal damage
        health -= damageTaken.damage;

        // Display damage
        Text damageText = Instantiate(GameManager.instance.hitNumberPrefab, transform.position + new Vector3(Random.Range(-1f, 1f), 
            Random.Range(-.35f, .5f), 0), Quaternion.identity).GetComponentInChildren<Text>();
        damageText.text = (damageTaken.damage).ToString();

        if (damageTaken.critMultiplier > 1) 
        {
            audioSource.clip = critEffect;
            damageText.color = Color.yellow;
        }
        else audioSource.clip = hitEffect;

        // Hit Effect
        foreach (SpriteRenderer sh in gfxComponents)
            sh.material.shader = shaderWhite;
        StartCoroutine(Shake(.075f, .3f));
        audioSource.Play();
        animator.SetFloat("Speed", 0);
        smartSlider.LoseHealth(health, .3f, .2f);
        if (healthText != null) healthText.text = health + "/" + maxHealth;

        if (health <= 0)
        {
            // Disable colliders
            Collider2D[] colliders = GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            Invoke("XP", .15f);
            Destroy(gameObject, .3f);
        }
        else
        {
            // Hit Effect
            StartCoroutine(Shake(.075f, .3f));
            if (spriteRenderer != null) spriteRenderer.material.shader = shaderWhite;
            CancelInvoke("Default");
            Invoke("Default", .3f);
        }
    }

    public void Armed()
    {
        if (weapon == null || DialogueManager.instance.chatBox.activeInHierarchy) return;

        flipPass = true;
        armed = !armed;
        weapon.Holster();

        // Free hand
        if (armed)
        {
            if (flipped)
            {
                handL.parent = weaponCenter;
                handL.localPosition = weapon.handPlacement; //new Vector2(0.45f, 0);
                handL.rotation = Quaternion.identity;
            }
            else
            {
                handR.parent = weaponCenter;
                handR.localPosition = new Vector2(0.45f, 0);
                handR.rotation = Quaternion.identity;
            }
        }
        else
        {
            if (flipped)
            {
                handL.parent = offHand;
                handL.localPosition = new Vector2(-.3f, -0.25f);
                handL.rotation = Quaternion.identity;
            }
            else
            {
                handR.parent = offHand;
                handR.localPosition = new Vector2(.3f, -0.25f);
                handR.rotation = Quaternion.identity;
            }
        }
    }

    public void FlipSprite(bool flip)
    {
        if (flip == flipped && !flipPass) return;

        flipPass = false;

        float multiplier = flip ? -1 : 1;

        if (armed)
        {
            if (flip) // Left
            {
                handL.parent = weaponCenter;
                handR.parent = offHand;
                handL.localPosition = weapon.handPlacement;
                handR.localPosition = new Vector2(.3f, -0.25f);

                if (weapon.type == Weapon.Type.Staff)
                {
                    weapon.transform.localPosition = -Vector3.right * .7f;
                    weapon.GFX.flipX = true;
                    handL.localPosition = new Vector2(-.5f, 0);
                    handL.transform.parent = weapon.GFX.transform;
                }
            }
            else // Right
            {
                handR.parent = weaponCenter;
                handL.parent = offHand;
                handR.localPosition = weapon.handPlacement;
                handL.localPosition = new Vector2(-.3f, -0.25f);

                if (weapon.type == Weapon.Type.Staff)
                {
                    weapon.transform.localPosition = Vector3.right * .7f;
                    weapon.GFX.flipX = false;
                    handR.localPosition = new Vector2(.5f, 0);
                    handR.transform.parent = weapon.GFX.transform;
                }
            }
        }
        else
        {
            if (flip) // Left
            {
                handL.parent = offHand;
                handL.localPosition = new Vector2(-.3f, -0.25f);
                handL.rotation = Quaternion.identity;
            }
            else // Right
            {
                handR.parent = offHand;
                handR.localPosition = new Vector2(.3f, -0.25f);
                handR.rotation = Quaternion.identity;
            }
        }

        handL.rotation = Quaternion.identity;
        handR.rotation = Quaternion.identity;

        for (int i = 0; i < 6; i++) // 6 because gfxComponents has 6 elements + the weapon which shouldn't be flipped
            gfxComponents[i].flipX = flip;

        back.localScale = new Vector3(multiplier, 1, 1);

        flipped = flip;
    }

    public void Stun(float time)
    {
        if (stunEffect != null) StartCoroutine(StunCo(time));
    }

    public IEnumerator StunCo(float time)
    {
        stunned = true;
        stunEffect.SetActive(true);
        yield return new WaitForSeconds(time);
        stunEffect.SetActive(false);
        stunned = false;

    }

    public IEnumerator Knockback(float force, Vector2 direction)
    {
        knockbacked = true;
        rb.velocity = Vector2.zero;
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        yield return new WaitForSecondsRealtime(.1f);
        rb.velocity = Vector2.zero;
        yield return new WaitForSecondsRealtime(.2f);
        knockbacked = false;
    }

    public IEnumerator Shake(float force, float duration)
    {
        while(duration > 0)
        {
            duration -= Time.deltaTime;
            GFX.transform.localPosition = new Vector2(Random.Range(-1, 1), Random.Range(-1, 1)) * force;
            yield return null;
        }

        GFX.transform.localPosition = Vector2.zero;
    }

    private void Default()
    {
        if (spriteRenderer != null) spriteRenderer.material.shader = shaderDefault;
        else
        {
            foreach (SpriteRenderer sh in gfxComponents)
                sh.material.shader = shaderDefault;
        }
        swordInvincible = false;
    }

    protected void XP()
    {
        xpEffect.transform.parent = null;
        xpEffect.Play();
    }

    protected float AngleBetweenTwoPoints(Vector2 from, Vector2 to)
    {
        Vector2 direction = to - from;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
}
