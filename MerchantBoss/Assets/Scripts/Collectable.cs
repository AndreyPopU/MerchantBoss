using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    public Vector2 spitMinMax;
    public int value;
    public float afkTime = 1;
    public AudioClip pickUp;

    public float frequency, amplitude;
    public float offset;

    private Transform target;
    private Rigidbody2D rb;
    private Vector2 posOffset;
    private Vector2 tempPos;
    private Transform GFX;
    [HideInInspector]
    public bool spitDown;

    void Start()
    {
        target = Player.instance.transform;
        rb = GetComponent<Rigidbody2D>();
        GFX = transform.GetChild(0);
        posOffset = GFX.localPosition;
        offset = Random.Range(-.2f, .2f);

        Spit(spitDown);
        Invoke("Activate", afkTime);
    }

    private void Update()
    {
        // Float
        tempPos = posOffset;
        tempPos.y += Mathf.Sin((Time.timeSinceLevelLoad + offset) * Mathf.PI * frequency) * amplitude;
        GFX.localPosition = tempPos;
    }


    void FixedUpdate()
    {
        if (target == null) return;

        if (afkTime > 0) afkTime -= Time.fixedDeltaTime;
        else
        {
            if (Vector2.Distance(transform.position, target.position) < 1.5f)
                transform.position = Vector3.Lerp(transform.position, target.position, 10 * Time.fixedDeltaTime);
        }
    }

    void Spit(bool down)
    {
        Vector2 spitDirection;

        if (down) spitDirection = new Vector2(Random.Range(-1f, 1f), -1);
        else spitDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));

        float spitForce = Random.Range(spitMinMax.x, spitMinMax.y);
        rb.AddForce(spitDirection * spitForce, ForceMode2D.Impulse);
    }

    private void Activate()
    {
        GetComponent<CircleCollider2D>().enabled = true;

        switch (value)
        {
            case 1: GetComponent<Animator>().Play("CoinSilverIdle", -1, Random.Range(0f, 1f)); break;
            case 2: GetComponent<Animator>().Play("CoinGoldenIdle", -1, Random.Range(0f, 1f)); break;
            case 5: GetComponent<Animator>().Play("DiamondIdle", -1, Random.Range(0f, 1f)); break;
        }
    }
}
