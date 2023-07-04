using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadePanel : MonoBehaviour
{
    public static FadePanel instance;

    public CanvasGroup group;

    private void Awake()
    {
        if (instance != null) Destroy(gameObject);
        else instance = this;
        group = GetComponent<CanvasGroup>();
    }

    public void Fade() => StartCoroutine(FadeIE());

    public void Fade(float delay, int sceneIndex) => StartCoroutine(FadeIE(delay, sceneIndex));

    private IEnumerator FadeIE()
    {
        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        if (group.alpha < 1)
        {
            while (group.alpha < 1)
            {
                group.alpha += Time.fixedDeltaTime;
                yield return waitForFixedUpdate;
            }
        }
        else if (group.alpha > 0)
        {
            while (group.alpha > 0)
            {
                group.alpha -= Time.fixedDeltaTime;
                yield return waitForFixedUpdate;
            }
        }
    }

    private IEnumerator FadeIE(float delay, int sceneIndex)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();

        if (group.alpha < 1)
        {
            if (sceneIndex >= 0)
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
                operation.allowSceneActivation = false;

                while (!operation.isDone && group.alpha < 1)
                {
                    group.alpha += Time.fixedDeltaTime;
                    yield return waitForFixedUpdate;
                }
                operation.allowSceneActivation = true;
            }
            else
            {
                while (group.alpha < 1)
                {
                    group.alpha += Time.fixedDeltaTime;
                    yield return waitForFixedUpdate;
                }
            }
        }
        else if (group.alpha > 0)
        {
            while (group.alpha > 0)
            {
                group.alpha -= .2f * Time.fixedDeltaTime;
                yield return waitForFixedUpdate;
            }
        }

        if (sceneIndex >= 0) // In case a scene was loaded
            Fade();

    }
}
