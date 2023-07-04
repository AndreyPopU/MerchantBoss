using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    public GameObject[] lootTable;
    public bool open;
    public float spitForce;
    public Sprite openGFX;
    public Shader shaderWhite;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Shader shaderDefault;
    private AudioSource audioSource;
    private bool invincible = true;

    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        shaderDefault = spriteRenderer.material.shader;
        Invoke("WearOff", .5f);
    }

    public void DropLoot()
    {
        for (int i = 0; i < lootTable.Length; i++)
        {
            GameObject loot = Instantiate(lootTable[i].gameObject, transform.position, Quaternion.identity);
            loot.GetComponent<Collectable>().spitDown = true;
        }
    }

    public void Open()
    {
        if (open || invincible) return;

        Invoke("Drop", .15f);

        spriteRenderer.material.shader = shaderWhite;
        StartCoroutine(Shake(.075f, .15f));
        audioSource.Play();
        open = true;
    }

    public IEnumerator Shake(float force, float duration)
    {
        while (duration > 0)
        {
            duration -= Time.deltaTime;
            spriteRenderer.transform.localPosition = new Vector2(Random.Range(-1, 1), Random.Range(-1, 1)) * force;
            yield return null;
        }

        spriteRenderer.transform.localPosition = Vector2.zero;
    }

    void Drop()
    {
        spriteRenderer.material.shader = shaderDefault;
        animator.SetTrigger("open");
        spriteRenderer.sprite = openGFX;
        DropLoot();
    }

    private void WearOff()
    {
        invincible = false;
    }
}