using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClassCard : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int classIndex;
    public StatsUp classBaseStats;
    public Text[] statsTexts;

    [Tooltip("-1 0 1")]
    public int position;
    public bool flipped;
    public bool selected;
    public GameObject chosenEffect;

    [Header("Weapon on card")]
    public GameObject weaponPrefab;
    public GameObject shieldPrefab;

    public List<ClassCard> neighbourCards;
    public GameObject front, back, contents;
    public bool mouseOver;

    Vector3 clickScale = new Vector2(1.15f, 1.15f);
    Vector3 enterScale = new Vector2(1.075f, 1.075f);
    private Button chooseButton;
    private bool flipping;

    void Start()
    {
        neighbourCards.AddRange(FindObjectsOfType<ClassCard>());
        neighbourCards.Remove(this);
        chooseButton = GetComponentInChildren<Button>();

        // Change back stats to match
        for (int i = 0; i < statsTexts.Length; i++)
        {
            // If you can make this work then great, but otherwise I'll do it the dumb way
            float.TryParse(classBaseStats.GetType().GetProperties()[i].GetValue(classBaseStats, null).ToString(), out float result);
            statsTexts[i].text = classBaseStats.GetType().GetProperties()[i].GetValue(classBaseStats, null).ToString();
        }
    }

    public void Flip()
    {
        flipped = !flipped;

        front.SetActive(!flipped);
        back.SetActive(flipped);
    }

    public IEnumerator FlipCard()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        flipping = true;

        if (flipped)
        {
            while (contents.transform.rotation.eulerAngles.y > 89)
            {
                contents.transform.rotation = Quaternion.Euler(0, contents.transform.rotation.eulerAngles.y - 10, 0);
                yield return waitForFixedUpdate;
            }

            Flip();

            while (contents.transform.rotation.eulerAngles.y > 9)
            {
                contents.transform.rotation = Quaternion.Euler(0, contents.transform.rotation.eulerAngles.y - 10, 0);
                yield return waitForFixedUpdate;
            }
        }
        else
        {
            while (contents.transform.rotation.eulerAngles.y < 89)
            {
                contents.transform.rotation = Quaternion.Euler(0, contents.transform.rotation.eulerAngles.y + 10, 0);
                yield return waitForFixedUpdate;
            }

            Flip();

            while (contents.transform.rotation.eulerAngles.y < 179)
            {
                contents.transform.rotation = Quaternion.Euler(0, contents.transform.rotation.eulerAngles.y + 10, 0);
                yield return waitForFixedUpdate;
            }
        }

        flipping = false;
    }


    IEnumerator Click()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        while (transform.localScale.x < clickScale.x - .03f && mouseOver)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, clickScale, .5f);
            yield return waitForFixedUpdate;
        }
        transform.localScale = clickScale;

        while (transform.localScale.x > 1 + .02f && mouseOver)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, .5f);
            yield return waitForFixedUpdate;
        }
        transform.localScale = Vector3.one;
    }

    IEnumerator Enter()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();
        while (transform.localScale.x < enterScale.x - .02f && mouseOver)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, enterScale, .5f);
            yield return waitForFixedUpdate;
        }
        transform.localScale = enterScale;
    }

    public void Choose()
    {
        selected = true;
        chooseButton.interactable = false;
        chooseButton.GetComponent<MyButton>().enabled = false;
        StartCoroutine(SlideCenter());
        StartCoroutine(Effect());

        // Left card is selected - slide center card Right
        // Right card is selected - slide center card Left
        int direction = 1;
        if (position > 0) direction = -1;
        
        neighbourCards[0].StartCoroutine(neighbourCards[0].SlideOffScreen(direction));
        neighbourCards[1].StartCoroutine(neighbourCards[1].SlideOffScreen(neighbourCards[1].position));
        StartCoroutine(Big());
        StartCoroutine(HideText());

        GiveStats();

        Invoke("ContinueDialogue", 4);
    }

    public void GiveStats()
    {
        // Give player class stats
        Player.instance.SetStats(classBaseStats);

        // Give player his class
        switch (classIndex)
        {
            case 0: Player.instance.gameClass = Player.GameClass.Knight; break;
            case 1: Player.instance.gameClass = Player.GameClass.Archer; break;
            case 2: Player.instance.gameClass = Player.GameClass.Wizard; break;
        }

        // Give player his weapon
        Weapon weapon = Instantiate(weaponPrefab, transform.position, Quaternion.identity, Player.instance.weaponCenter).GetComponent<Weapon>();
        if (weapon.type == Weapon.Type.Sword) weapon.transform.rotation = Quaternion.Euler(Vector3.forward * -90);
        weapon.transform.localPosition = weapon.holdOffset;
        weapon.pivotPoint = Player.instance.pivotPoint;
        weapon.wielder = Player.instance;
        weapon.GFX = weapon.GetComponentInChildren<SpriteRenderer>();
        Player.instance.weapon = weapon;
        Player.instance.armed = true;
        Player.instance.flipPass = true;

        // Add to player's GFX components so weapon gets effected by whatever the player is effected (invincibility, damage, etc.)
        while (Player.instance.gfxComponents.Count > 6) Player.instance.gfxComponents.RemoveAt(Player.instance.gfxComponents.Count - 1);
        Player.instance.gfxComponents.Add(weapon.GFX);
        if (weapon.type == Weapon.Type.Bow) Player.instance.gfxComponents.Add(weapon.GetComponentsInChildren<SpriteRenderer>()[1]);

        // Handle shield
        if (weapon.type == Weapon.Type.Sword)
        {
            GameObject shield = null;
            shield = Instantiate(shieldPrefab, transform.position, Quaternion.identity, Player.instance.freeHand);
            Player.instance.shield = shield;
            int multiplier = -1;
            if (Camera.main.ScreenToWorldPoint(Input.mousePosition).x < Player.instance.transform.position.x) multiplier = 1;
            shield.transform.localPosition = new Vector3(.15f * multiplier, -.25f, 0);

            // Every part (but the hands) has it's own steel effect, set the sprite to the mask of the effect
            int actualIndex = -1;
            for (int i = 0; i < Player.instance.gfxComponents.Count; i++)
            {
                actualIndex++;
                if (i == 6) break; // Don't make weapons steel
                if (i == 2 || i == 3) { actualIndex--; continue; }// Skip hands

                Player.instance.steelEffects[actualIndex].sprite = Player.instance.gfxComponents[i].GetComponent<SpriteRenderer>().sprite;
                Player.instance.steelEffects[actualIndex].gameObject.SetActive(false);
            }
        }
        Invoke("Disarm", .5f);
    }

    private void ContinueDialogue()
    {
        DialogueManager.instance.SetDelegate(GameManager.instance.ShowPlayerUI);
        string[] phrases = new string[] { "1Congratulations you are officially a hero now.",
            "1You can holster your weapon by pressing Q.",
            "1Head into the dungeon whenever you feel like it." };
        DialogueManager.instance.UIactive = true;
        DialogueManager.instance.AddPhrases(phrases);
    }

    public IEnumerator Effect()
    {
        YieldInstruction waitForFixedUdate = new WaitForFixedUpdate();

        CanvasGroup group = chosenEffect.GetComponent<CanvasGroup>();
        chosenEffect.SetActive(true);
        float effectTime = 3;

        while (effectTime > 0)
        {
            if (group.alpha < 1) group.alpha += .05f;
            if (chosenEffect.transform.localScale.x < 1) chosenEffect.transform.localScale += Vector3.one * .05f;

            effectTime -= Time.fixedDeltaTime;
            chosenEffect.transform.Rotate(chosenEffect.transform.forward, 1);
            yield return waitForFixedUdate;
        }

        while (group.alpha > 0)
        {
            group.alpha -= .05f;
            chosenEffect.transform.Rotate(chosenEffect.transform.forward, 1);
            yield return waitForFixedUdate;
        }

        StartCoroutine(SlideOffScreen(1));
    }

    private IEnumerator HideText()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();
        Transform text = transform.parent.GetChild(0);

        while (text.localPosition.y < 1000)
        {
            text.localPosition += Vector3.up * 50;
            yield return waitForFixedUpdate;
        }

        text.gameObject.SetActive(false);
    }

    private IEnumerator Big()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        while (transform.localScale.x < 1.3f)
        {
            transform.localScale += Vector3.one * .05f;
            yield return waitForFixedUpdate;
        }
    }

    public IEnumerator SlideOffScreen(int direction)
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        if (direction > 0)
        {
            while (transform.localPosition.x < 3000)
            {
                transform.localPosition += Vector3.right * 200;
                yield return waitForFixedUpdate;
            }
        }
        else if (direction < 0)
        {
            while (transform.localPosition.x > -3000)
            {
                transform.localPosition -= Vector3.right * 200;
                yield return waitForFixedUpdate;
            }
        }

        gameObject.SetActive(false);
    }

    private IEnumerator SlideCenter()
    {
        if (position == 0) yield break;

        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        if (position < 0)
        {
            while (transform.localPosition.x < 0)
            {
                transform.position += Vector3.right * 100;

                yield return waitForFixedUpdate;
            }
        }
        else if (position > 0)
        {
            while (transform.localPosition.x > 0)
            {
                transform.position -= Vector3.right * 100;

                yield return waitForFixedUpdate;
            }
        }
    }

    IEnumerator Exit()
    {
        if (selected) yield break;

        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();
        while (transform.localScale.x > 1 + .02f && !mouseOver)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, .5f);
            yield return waitForFixedUpdate;
        }
        transform.localScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (selected || flipping) return;

        StartCoroutine(Click());
        StartCoroutine(FlipCard());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selected) return;

        mouseOver = true;
        transform.localScale = Vector3.one;
        StartCoroutine(Enter());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selected) return;

        mouseOver = false;
        transform.localScale = enterScale;
        StartCoroutine(Exit());
    }

    private void Disarm()
    {
        if (Player.instance.armed) Player.instance.Armed();
    }
}
