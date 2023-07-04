using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonEntrance : MonoBehaviour
{
    public bool open;
    public int facingDirection;

    public Transform enterPoint, exitPoint;
    public Sprite closedSprite;
    private Sprite openedSprite;
    private Rigidbody2D playerRb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool entered = false;
    private BoxCollider2D coreCollider;
    private AudioSource audioSource;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        openedSprite = spriteRenderer.sprite;
        animator = GetComponent<Animator>();
        playerRb = Player.instance.GetComponent<Rigidbody2D>();
        coreCollider = GetComponent<BoxCollider2D>();
        coreCollider.enabled = false;
        animator.SetInteger("direction", facingDirection);
        audioSource = GetComponent<AudioSource>();
    }

    public IEnumerator Enter()
    {
        entered = true;
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);
        operation.allowSceneActivation = false;

        if (Player.instance.armed) Player.instance.Armed();

        // Deal with player as he enters
        Player.instance.canMove = false;

        if (enterPoint.position.x > Player.instance.transform.position.x) Player.instance.FlipSprite(false);
        else Player.instance.FlipSprite(true);

        Vector2 enterDirection = (enterPoint.position - Player.instance.transform.position).normalized;

        while (Vector2.Distance(enterPoint.position, Player.instance.transform.position) > .5f)
        {
            // Player enter
            playerRb.velocity = enterDirection * Player.instance.speed * Time.fixedDeltaTime;

            yield return waitForFixedUpdate;
        }

        Player.instance.Invisible(0);
        playerRb.velocity = Vector2.zero;
        Player.instance.transform.position = transform.position;

        yield return new WaitForSeconds(.5f);

        Open(); // Close door

        yield return new WaitForSeconds(.5f);

        while (FadePanel.instance.group.alpha < 1)
        {
            FadePanel.instance.group.alpha += .1f;
            yield return waitForFixedUpdate;
        }

        // Change scene
        operation.allowSceneActivation = true;
    }

    public void Open()
    {
        open = !open;

        if (facingDirection == 2)
        {
            Invoke("CloseShake", .3f); // Adjust
            return;
        }

        if (open) spriteRenderer.sprite = openedSprite;
        else
        {
            audioSource.Play();
            spriteRenderer.sprite = closedSprite;
        }

        // Play animation
        animator.SetBool("open", open);
        animator.SetTrigger("interact");
    }

    public void Exit() => StartCoroutine(ExitCO());

    private IEnumerator ExitCO()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();
        entered = true;

        // Deal with direction
        Player.instance.transform.position = Room.instance.exit.transform.position;
        if (exitPoint.position.x > Player.instance.transform.position.x) Player.instance.FlipSprite(false);
        else Player.instance.FlipSprite(true);

        // Start fading in
        while (FadePanel.instance.group.alpha > 0)
        {
            FadePanel.instance.group.alpha -= .1f;
            yield return waitForFixedUpdate;
        }

        Open();

        yield return new WaitForSeconds(1);

        Player.instance.transform.localScale = Vector3.one;
        Player.instance.Invisible(1);
        Vector2 exitDirection = (exitPoint.position - Player.instance.transform.position).normalized;

        while (Vector2.Distance(exitPoint.transform.position, Player.instance.transform.position) > .5f)
        {
            // Player exit
            playerRb.velocity = exitDirection * Player.instance.speed * Time.fixedDeltaTime;

            yield return waitForFixedUpdate;
        }

        // Arm player
        Open();
        coreCollider.enabled = true;
        Player.instance.coreCollider.enabled = true;
        if (!Player.instance.armed) Player.instance.Armed();
        playerRb.velocity = Vector2.zero;
        Player.instance.canMove = true;
    }

    public void CloseShake() // Animation Keyframe
    {
        audioSource.Play();
        CameraManager camManager = FindObjectOfType<CameraManager>();
        camManager.StartCoroutine(camManager.Shake(.1f, .05f));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (entered || !open) return;

        if (collision.GetComponent<Player>()) StartCoroutine(Enter());
    }
}
