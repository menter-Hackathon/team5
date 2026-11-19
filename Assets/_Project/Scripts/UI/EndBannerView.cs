using System.Collections;
using Team5.Core;
using Team5.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // 勝敗が決まった瞬間に、画面の真ん中に「クリア！」「ゲームオーバー」を大きく出す（結果画面に移るまでの数秒）。Game シーンに置く
    public class EndBannerView : MonoBehaviour
    {
        [SerializeField] private Font font;

        private static readonly Color Gold = new(1f, 0.784f, 0.239f);
        private static readonly Color Warning = new(0.92f, 0.3f, 0.32f);
        private static readonly Color Fog = new(0.85f, 0.83f, 0.8f);

        private GameEndJudge judge;

        private void Start()
        {
            judge = FindAnyObjectByType<GameEndJudge>();
            if (judge != null) judge.OnEnding += Show;
        }

        private void OnDestroy()
        {
            if (judge != null) judge.OnEnding -= Show;
        }

        private void Show(GameResult result)
        {
            var context = GameSceneContext.Instance;
            RoleDefinition role = context != null ? context.Role : null;
            string wanted = role != null && !string.IsNullOrEmpty(role.displayName) ? role.displayName : "なりたい自分";

            string title, sub;
            Color color;
            switch (result)
            {
                case GameResult.Clear: title = "クリア！"; sub = $"{wanted}になれた！"; color = Gold; break;
                case GameResult.GameOverPeace: title = "ゲームオーバー"; sub = "村にばれてしまった…"; color = Warning; break;
                case GameResult.GameOverFaded: title = "ゲームオーバー"; sub = "色が消えてしまった…"; color = Fog; break;
                default: title = "タイムアップ"; sub = $"{wanted}には、まだなれなかった…"; color = Warning; break;
            }
            StartCoroutine(Play(title, sub, color, result == GameResult.Clear));
        }

        private IEnumerator Play(string title, string sub, Color color, bool clear)
        {
            var canvasGo = new GameObject("EndBanner", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var group = canvasGo.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            // 画面を少し暗くする帯
            var band = NewRect("Band", canvasGo.transform);
            band.anchorMin = new Vector2(0f, 0.5f);
            band.anchorMax = new Vector2(1f, 0.5f);
            band.sizeDelta = new Vector2(0f, 300f);
            var bandImage = band.gameObject.AddComponent<Image>();
            bandImage.color = clear ? new Color(0.12f, 0.08f, 0.02f, 0.55f) : new Color(0.05f, 0.04f, 0.08f, 0.7f);
            bandImage.raycastTarget = false;

            var head = NewText(title, band, 130, color);
            head.rectTransform.anchoredPosition = new Vector2(0f, 38f);
            var subText = NewText(sub, band, 44, Color.white);
            subText.rectTransform.anchoredPosition = new Vector2(0f, -84f);

            // ふわっと出て、見出しが弾む
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / 0.35f;
                band.localScale = new Vector3(1f, Mathf.Lerp(0.2f, 1f, Mathf.SmoothStep(0f, 1f, t / 0.35f)), 1f);
                yield return null;
            }
            group.alpha = 1f;
            band.localScale = Vector3.one;
            UiFx.Punch(this, head.transform, clear ? 0.25f : 0.15f, 0.4f);
            if (!clear) UiFx.Shake(this, head.rectTransform, 14f, 0.4f);
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private Text NewText(string value, Transform parent, int size, Color color)
        {
            var rect = NewRect("Text", parent);
            rect.sizeDelta = new Vector2(1800f, size * 1.4f);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            return text;
        }
    }
}
