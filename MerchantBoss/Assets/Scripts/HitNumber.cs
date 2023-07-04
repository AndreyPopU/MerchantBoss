using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitNumber : MonoBehaviour
{
    private CanvasGroup group;

    void Start()
    {
        transform.parent.GetComponent<Canvas>().sortingOrder = 5;
        group = GetComponent<CanvasGroup>();
        float scale = Random.Range(.85f, 1.15f);
        transform.localScale = new Vector2(transform.localScale.x * scale, transform.localScale.y * scale);

        StartCoroutine(DoTheThing());
    }

    IEnumerator DoTheThing()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        while (group.alpha < 1)
        {
            transform.position += Vector3.up * .1f;
            group.alpha += .1f;
            yield return waitForFixedUpdate;
        }

        Vector3 originalPos = transform.position;
        Vector3 shakePos;
        float duration = .25f;
        float force = .05f;

        while (duration > 0)
        {
            duration -= Time.deltaTime;
            float randomX = Random.Range(originalPos.x - 1 * force, originalPos.x + 1 * force);
            float randomY = Random.Range(originalPos.y - 1 * force, originalPos.y + 1 * force);
            shakePos = new Vector3(randomX, randomY, transform.position.z);
            transform.position = shakePos;

            yield return waitForFixedUpdate;
        }
        transform.position = originalPos;

        yield return new WaitForSeconds(1f);

        while (group.alpha > 0)
        {
            group.alpha -= .1f;
            yield return waitForFixedUpdate;
        }

        Destroy(transform.parent.gameObject);
    }
}
