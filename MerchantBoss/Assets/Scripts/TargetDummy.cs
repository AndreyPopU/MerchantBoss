using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetDummy : Entity
{
    private new void Start()
    {
        audioSource = GetComponent<AudioSource>();
        GFX = transform.GetChild(0).gameObject;
        spriteRenderer = GFX.GetComponent<SpriteRenderer>();
        shaderWhite = Shader.Find("GUI/Text Shader");
        shaderDefault = spriteRenderer.material.shader;
        rb = GetComponent<Rigidbody2D>();
    }

    public new void TakeDamage(DamageTaken damageTaken)
    {
        if (swordInvincible) return;

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
        StartCoroutine(Shake(.075f, .3f));
        if (spriteRenderer != null) spriteRenderer.material.shader = shaderWhite;
        Invoke("Default", .3f);
    }

    private void Default()
    {
        spriteRenderer.material.shader = shaderDefault;
        swordInvincible = false;
    }
}
