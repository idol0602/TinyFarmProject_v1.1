using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingSceneManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Image loadingBarFill;
    public TMP_Text loadingText;

    private void Start()
    {
        StartCoroutine(LoadFarmScene());
    }

    IEnumerator LoadFarmScene()
    {
        // Giả lập delay ban đầu
        yield return new WaitForSeconds(0.5f);

        AsyncOperation operation = SceneManager.LoadSceneAsync("mapSummer");
        operation.allowSceneActivation = false;

        float fakeProgress = 0f;

        while (!operation.isDone)
        {
            // Giả lập tăng dần để bar chạy chậm và mượt
            fakeProgress += Time.deltaTime * 0.4f; // 0.15f = tốc độ (giảm số này để chạy chậm hơn)
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            float progress = Mathf.Min(fakeProgress, targetProgress);

            // Cập nhật thanh bar
            if (loadingBarFill != null)
                loadingBarFill.fillAmount = progress;

            // Cập nhật text phần trăm
            if (loadingText != null)
                loadingText.text = "Loading... " + Mathf.RoundToInt(progress * 100f) + "%";

            // Khi đạt 100% thì chuyển cảnh
            if (progress >= 1f)
            {
                yield return new WaitForSeconds(1f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
