using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public CanvasGroup popupGroup;
    public TMP_Text popupText;

    public float fadeTime = 0.2f;
    public float showTime = 1.4f;

    private void Awake()
    {
        Instance = this;
        popupGroup.alpha = 0f;
        popupGroup.gameObject.SetActive(false);
        DontDestroyOnLoad(gameObject);
    }

    public static void ShowMessage(string msg)
    {
        if (Instance == null) return;
        Instance.Display(msg);
    }

    private void Display(string msg)
    {
        popupText.text = msg;

        // Ẩn SleepDialog nếu có
        var sleepDialog = GameObject.Find("SleepDialog");
        if (sleepDialog) sleepDialog.SetActive(false);

        if (!popupGroup.gameObject.activeSelf)
            popupGroup.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(PopupEffect());
    }

    private IEnumerator PopupEffect()
    {
        // Fade In
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            popupGroup.alpha = t / fadeTime;
            yield return null;
        }

        popupGroup.alpha = 1;
        yield return new WaitForSeconds(showTime);

        // Fade Out
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            popupGroup.alpha = 1 - (t / fadeTime);
            yield return null;
        }

        popupGroup.alpha = 0f;
        popupGroup.gameObject.SetActive(false);
    }
}
