using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Temporary")]
    public ClassCard[] classCards;

    [Header("Player name UI")]
    public GameObject playerUI;
    public string playerName;
    public GameObject verifyText;
    public Text nameText;
    public InputField nameField;

    [Header("Pools and damage")]
    public GameObject hitNumberPrefab;
    public List<Projectile> arrowPool = new List<Projectile>();
    public List<Projectile> magicPool = new List<Projectile>();

    [Header("Other")]
    public GameObject classScreen;
    public Transform nameScreen;
    public Texture2D cursorSprite;
    public int coins;


    private void Awake()
    {
        if (instance != null) Destroy(gameObject);
        else instance = this;

        DontDestroyOnLoad(this);
    }

    void Start()
    {
        FadePanel.instance.group.alpha = 1;
        FadePanel.instance.Fade();
        Vector2 hotSpot = new Vector2(cursorSprite.width / 8, cursorSprite.height / 10);
        Cursor.SetCursor(cursorSprite, hotSpot, CursorMode.Auto);

        nameText.text = playerName;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U)) classCards[0].GiveStats();
        if (Input.GetKeyDown(KeyCode.I)) classCards[1].GiveStats();
        if (Input.GetKeyDown(KeyCode.O)) classCards[2].GiveStats();
    }

    public float AngleBetweenTwoPoints(Vector2 from, Vector2 to)
    {
        Vector2 direction = to - from;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    public void SetName()
    {
        // Verify name
        if (nameField.text.Length < 3)
        {
            verifyText.SetActive(true);
            CancelInvoke("HideVerification");
            Invoke("HideVerification", 5);
        }
        else
        {
            DialogueManager.instance.names[0] = playerName;
            playerName = nameField.text;
            nameText.text = playerName;
            nameScreen.GetComponentInChildren<Button>().interactable = false;
            StartCoroutine(HideNamePrompt());
        }
    }

    IEnumerator HideNamePrompt()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        while(nameScreen.transform.localPosition.y > -400)
        {
            nameScreen.transform.position -= Vector3.up * 20;
            yield return waitForFixedUpdate;
        }

        nameScreen.gameObject.SetActive(false);
        DialogueManager.instance.SetDelegate(PromptClass);
        DialogueManager.instance.AddPhrase("1Nice to meet you " + GameManager.instance.playerName + "! What kind of hero do you want to be?");
    }

    IEnumerator ShowNamePrompt()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        while (nameScreen.transform.localPosition.y < 0)
        {
            nameScreen.transform.position += Vector3.up * 20;
            yield return waitForFixedUpdate;
        }

        nameScreen.transform.localPosition = Vector3.zero;
    }

    IEnumerator ShowClassPrompt()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        while (classScreen.transform.localPosition.x < 0)
        {
            classScreen.transform.position += Vector3.right * 100;
            yield return waitForFixedUpdate;
        }

        nameScreen.transform.localPosition = Vector3.zero;
    }

    public void PlayerUIPrompt(bool show) => StartCoroutine(PlayerUIPromptCO(show));

    IEnumerator PlayerUIPromptCO(bool show)
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        if (show)
        {
            while (playerUI.transform.localPosition.y < 0)
            {
                playerUI.transform.position += Vector3.up * 20;
                yield return waitForFixedUpdate;
            }

            nameScreen.transform.localPosition = Vector3.zero;
        }
        else
        {
            while (playerUI.transform.localPosition.y > -400)
            {
                playerUI.transform.position -= Vector3.up * 20;
                yield return waitForFixedUpdate;
            }
        }
    }

    public void PromptName()
    {
        nameScreen.gameObject.SetActive(true);
        StartCoroutine(ShowNamePrompt());
    }

    public void PromptClass()
    {
        classScreen.SetActive(true);
        StartCoroutine(ShowClassPrompt());
    }

    public void ShowPlayerUI()
    {
        playerUI.SetActive(true);
        StartCoroutine(PlayerUIPromptCO(true));
        Player.instance.canMove = true;
    }

    private void HideVerification() => verifyText.SetActive(false); // Invoked
}
