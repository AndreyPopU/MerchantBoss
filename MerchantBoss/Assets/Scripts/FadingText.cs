using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadingText : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private bool triggered;
    private int multiplier = 1;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void FixedUpdate()
    {
        if (triggered) canvasGroup.alpha += Time.fixedDeltaTime * multiplier;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>())
        {
            triggered = true;
            Invoke("Fade", 10);
        }
    }

    private void Fade() => multiplier = -1;
}
