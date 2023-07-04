using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    public string npcName;
    public string[] phrases;
    public bool initiateDialogue;
    public float speed;
    public float interactionDistance;
    public bool flipped;
    public bool faceLeft;
    public List<Transform> waypoints;
    public int restWaypoint;

    [Header("GFX Manager")]
    public SpriteRenderer[] gfxComponents;
    public Transform offHand;
    public Transform handL, handR;
    public Vector2 offsetL = new Vector2(-.3f, -0.25f);
    public Vector2 offsetR = new Vector2(.3f, -0.25f);
    public Transform back;

    private Animator animator;
    private Rigidbody2D rb;
    private int currWaypoint;
    private Vector2 direction;
    private float waitTime = 1;
    private bool questNpc;
    private bool interacted;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        animator.speed = .75f;

        if (faceLeft) FlipSprite(true);

        if (waypoints.Count > 0)
        {
            // Calculate flip
            if (transform.position.x < waypoints[currWaypoint].position.x) FlipSprite(false);
            else FlipSprite(true);
        }
        else questNpc = true;
    }

    private void Update()
    {
        if (questNpc)
        {
            if (Vector2.Distance(Player.instance.transform.position, transform.position) < interactionDistance)
            {
                if (Input.GetButtonDown("Interact") && !interacted) Interact();
            }
        }
    }

    void FixedUpdate()
    {
        if (questNpc) return;

        if (waypoints.Count > 0)
        {
            // Wander around the village
            if (Vector2.Distance(transform.position, waypoints[currWaypoint].position) > .1f)
            {
                // Calculate direction
                direction = waypoints[currWaypoint].position - transform.position;
                rb.velocity = direction.normalized * speed * Time.fixedDeltaTime;

                // Calculate flip
                if (transform.position.x < waypoints[currWaypoint].position.x) FlipSprite(false);
                else FlipSprite(true);
            }
            else // Go to next waypoint on list
            {
                rb.velocity = Vector2.zero;

                if (waitTime > 0) waitTime -= Time.deltaTime;
                else
                {
                    if (currWaypoint == waypoints.Count - 1) { waypoints.Reverse(); currWaypoint = 0; }
                    else currWaypoint++;

                    waitTime = 1;
                    if (currWaypoint == restWaypoint) waitTime *= 3;
                }
            }

            animator.SetFloat("Speed", rb.velocity.normalized.magnitude);
        }
    }

    private void Interact()
    {
        interacted = true;

        // Player face towards NPC and vice versa
        if (transform.position.x > Player.instance.transform.position.x)
        {
            Player.instance.FlipSprite(false);
            FlipSprite(true);
        }
        else
        {
            Player.instance.FlipSprite(true);
            FlipSprite(false);
        }

        if (Player.instance.armed) Player.instance.Armed();
        Player.instance.Immobilize(true);

        if (GameManager.instance.playerName == string.Empty) DialogueManager.instance.SetDelegate(GameManager.instance.PromptName);
        DialogueManager.instance.AddPhrases(phrases);
        DialogueManager.instance.PromptChat(true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() && initiateDialogue && !interacted) Interact();
    }

    public void FlipSprite(bool flip)
    {
        if (flipped == flip) return;

        if (back != null)
        {
            float multiplier = flip ? -1 : 1;
            back.localScale = new Vector3(multiplier, 1, 1);
        }

        if (flip) offHand.transform.parent.parent.transform.localScale = new Vector3(-1, 1, 1);
        else offHand.transform.parent.parent.transform.localScale = new Vector3(1, 1, 1);

        foreach (SpriteRenderer rend in gfxComponents)
            rend.flipX = flip;

        flipped = flip;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * interactionDistance);
    }
}
