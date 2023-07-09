using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    public GameObject chatBox;
    public Image icon;
    public Text nameText;
    public Text chatText;
    public int currentPhrase;
    public List<string> phrases;
    public Sprite[] icons;
    public string[] names;
    public delegate void DialogueEnd();
    public DialogueEnd dialogueEnd;

    private AudioSource audioSource;
    [HideInInspector] public bool UIactive;
    private bool writingText;

    private void Start()
    {
        instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (chatBox.activeInHierarchy && chatBox.transform.position.y > 120)
        {
            if (Input.GetButtonUp("Jump") || Input.GetMouseButtonUp(0))
            {
                if (writingText) 
                { 
                    chatText.text = phrases[currentPhrase]; 
                    StopAllCoroutines();
                    audioSource.Stop();
                    writingText = false; 
                }
                else DisplayText();
            }
        }
    }

    public void SetDelegate(DialogueEnd end) => dialogueEnd = end;

    public void DisplayText()
    {
        if (currentPhrase == -1) PromptChat(true);
        currentPhrase++;

        if (currentPhrase == phrases.Count) 
        {
            if (dialogueEnd == null) Player.instance.Immobilize(false);
            else
            {
                dialogueEnd();
                dialogueEnd = null;
            }
            PromptChat(false);

            return; 
        }

        StartCoroutine(ChatboxTextType(phrases[currentPhrase]));
    }

    public IEnumerator ChatboxTextType(string phrase)
    {
        audioSource.Play();
        chatText.text = "";
        List<char> phraseChars = new List<char>(phrase.ToCharArray()); 
        writingText = true;

        // Select icon
        int index = int.Parse(phraseChars[0].ToString());
        icon.sprite = icons[index];
        nameText.text = names[index];
        phraseChars.RemoveAt(0);
        phrases[currentPhrase] = new string(phraseChars.ToArray());

        for (int i = 0; i < phraseChars.Count; i++)
        {
            if (!writingText)
            {
                chatText.text = phrase;
                break;
            }
            chatText.text += phraseChars[i];

            yield return new WaitForSeconds(.035f);
        }

        audioSource.Stop();
        writingText = false;
    }

    public void AddPhrase(string phrase) 
    {
        phrases.Clear();
        chatText.text = string.Empty;
        currentPhrase = -1;
        phrases.Add(phrase);
        PromptChat(true);
    }

    public void AddPhrases(string[] phrase) 
    {
        phrases.Clear();
        chatText.text = string.Empty;
        currentPhrase = -1;
        phrases.AddRange(phrase);
        PromptChat(true);
    }

    public void PromptChat(bool prompt)
    {
        if (prompt)
        {
            int index = int.Parse(phrases[0].ToCharArray()[0].ToString());
            icon.sprite = icons[index];
            nameText.text = names[index];
            if (GameManager.instance.playerUI.gameObject.activeInHierarchy) GameManager.instance.PlayerUIPrompt(false);
        }
        else
        {
            if (UIactive) GameManager.instance.PlayerUIPrompt(true);
        }

        StartCoroutine(PromptChatCO(prompt));
    }

    private IEnumerator PromptChatCO(bool prompt)
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        if (prompt)
        {
            // If chat is already active don't bother
            if (chatBox.activeInHierarchy) yield break;

            chatBox.SetActive(true);

            while (chatBox.transform.position.y < 125)
            {
                chatBox.transform.position += Vector3.up * 25;
                yield return waitForFixedUpdate;
            }

            DisplayText();
            yield break;
        }

        if (!chatBox.activeInHierarchy) yield break;

        while (chatBox.transform.position.y > -615)
        {
            chatBox.transform.position -= Vector3.up * 25;
            yield return waitForFixedUpdate;
        }
        chatBox.SetActive(false);
    }
}
