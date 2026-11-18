using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Team5.UI
{
    // シーンの切り替えを暗転でつなぐ。GameManager が最初に使ったときに作られ、ずっと残る
    public class SceneFader : MonoBehaviour
    {
        private const float FadeOutSeconds = 0.35f;
        private const float FadeInSeconds = 0.45f;
        private static readonly Color Dark = new(0.18f, 0.169f, 0.2f, 1f); // UI 背景（暗）#2E2B33

        private static SceneFader instance;
        private CanvasGroup group;
        private bool busy;

        public static SceneFader Instance
        {
            get
            {
                if (instance == null) instance = Create();
                return instance;
            }
        }

        private static SceneFader Create()
        {
            var go = new GameObject("SceneFader", typeof(Canvas), typeof(CanvasGroup));
            DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var image = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(go.transform, false);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            image.GetComponent<Image>().color = Dark;
            var fader = go.AddComponent<SceneFader>();
            fader.group = go.GetComponent<CanvasGroup>();
            fader.group.alpha = 0f;
            fader.group.blocksRaycasts = false;
            return fader;
        }

        // 暗転 → シーンを読み込む → 明転。読み込みが終わったら onLoaded を呼ぶ
        public void LoadScene(string sceneName, Action onLoaded = null)
        {
            if (busy) return;
            StartCoroutine(Transition(sceneName, onLoaded));
        }

        private IEnumerator Transition(string sceneName, Action onLoaded)
        {
            busy = true;
            group.blocksRaycasts = true;
            yield return Fade(1f, FadeOutSeconds);
            var load = SceneManager.LoadSceneAsync(sceneName);
            while (!load.isDone) yield return null;
            onLoaded?.Invoke();
            yield return null; // 新しいシーンの Start を待つ
            yield return Fade(0f, FadeInSeconds);
            group.blocksRaycasts = false;
            busy = false;
        }

        // 一時停止中（timeScale = 0）でも動くように unscaled で進める
        private IEnumerator Fade(float target, float seconds)
        {
            float start = group.alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            group.alpha = target;
        }
    }
}
