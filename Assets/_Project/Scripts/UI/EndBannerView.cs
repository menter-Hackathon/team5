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
            string imposed = role != null && !string.IsNullOrEmpty(role.imposedRoleName) ? role.imposedRoleName : "勇者";

            // 村役場からの「通達」の紙。ゲームオーバーは淡々と、少し皮肉に
            string body, stamp;
            Color stampColor;
            switch (result)
            {
                case GameResult.Clear:
                    body = $"届け出を受理しました。\n本日より、あなたの「らしさ」を\n<color=#B8860B>{wanted}</color> に変更します。";
                    stamp = "受理"; stampColor = new Color(0.85f, 0.6f, 0.1f); break;
                case GameResult.GameOverPeace:
                    body = $"あなたは村の「らしさ」を乱しました。\n本日をもって、{imposed}の家の名簿から\nあなたの名前を消します。";
                    stamp = "追放"; stampColor = new Color(0.78f, 0.12f, 0.12f); break;
                case GameResult.GameOverFaded:
                    body = $"おめでとうございます。\nあなたは立派な{imposed}になりました。\nもう、何も迷うことはありません。";
                    stamp = "適合"; stampColor = new Color(0.35f, 0.35f, 0.4f); break;
                default:
                    body = $"日が沈みました。\n明日も、あなたは{imposed}として生きます。\nこれまでどおりに。";
                    stamp = "現状維持"; stampColor = new Color(0.3f, 0.3f, 0.55f); break;
            }
            StartCoroutine(Play(body, stamp, stampColor, result == GameResult.Clear));
        }

        private IEnumerator Play(string body, string stamp, Color stampColor, bool clear)
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

            // 画面を暗くする
            var dim = NewRect("Dim", canvasGo.transform);
            dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one;
            dim.offsetMin = dim.offsetMax = Vector2.zero;
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = clear ? new Color(0.1f, 0.07f, 0.02f, 0.45f) : new Color(0.02f, 0.02f, 0.04f, 0.65f);
            dimImage.raycastTarget = false;

            // 紙
            var paper = NewRect("Paper", canvasGo.transform);
            paper.anchorMin = paper.anchorMax = new Vector2(0.5f, 0.5f);
            paper.sizeDelta = new Vector2(860f, 560f);
            var paperImage = paper.gameObject.AddComponent<Image>();
            paperImage.color = new Color(0.93f, 0.89f, 0.78f);
            paperImage.raycastTarget = false;
            var shadow = paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(10f, -12f);
            var border = NewRect("Border", paper);
            border.anchorMin = Vector2.zero; border.anchorMax = Vector2.one;
            border.offsetMin = new Vector2(18f, 18f); border.offsetMax = new Vector2(-18f, -18f);
            var borderImage = border.gameObject.AddComponent<Image>();
            borderImage.color = new Color(0f, 0f, 0f, 0f);
            var borderLine = border.gameObject.AddComponent<Outline>();
            borderLine.effectColor = new Color(0.35f, 0.28f, 0.2f, 0.8f);
            borderLine.effectDistance = new Vector2(2f, 2f);

            var ink = new Color(0.17f, 0.13f, 0.1f);
            var head = NewText("らしさの村 村役場より　通達", paper, 34, ink);
            head.rectTransform.anchoredPosition = new Vector2(0f, 200f);
            var rule = NewRect("Rule", paper);
            rule.sizeDelta = new Vector2(700f, 3f);
            rule.anchoredPosition = new Vector2(0f, 165f);
            rule.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.28f, 0.2f, 0.7f);
            var bodyText = NewText(body, paper, 38, ink);
            bodyText.rectTransform.sizeDelta = new Vector2(760f, 260f);
            bodyText.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            bodyText.lineSpacing = 1.25f;
            var sign = NewText("らしさの村 村長", paper, 30, ink);
            sign.alignment = TextAnchor.MiddleLeft;
            sign.rectTransform.sizeDelta = new Vector2(740f, 50f);
            sign.rectTransform.anchoredPosition = new Vector2(0f, -200f);

            // はんこ
            var stampRect = NewRect("Stamp", paper);
            stampRect.anchoredPosition = new Vector2(240f, -170f);
            stampRect.sizeDelta = new Vector2(stamp.Length > 2 ? 300f : 220f, 120f);
            stampRect.localRotation = Quaternion.Euler(0f, 0f, 14f);
            var stampFrame = stampRect.gameObject.AddComponent<Image>();
            stampFrame.color = new Color(stampColor.r, stampColor.g, stampColor.b, 0.12f);
            stampFrame.raycastTarget = false;
            var frameLine = stampRect.gameObject.AddComponent<Outline>();
            frameLine.effectColor = stampColor;
            frameLine.effectDistance = new Vector2(4f, 4f);
            var stampText = NewText(stamp, stampRect, 76, stampColor);
            stampText.rectTransform.sizeDelta = stampRect.sizeDelta;
            stampRect.gameObject.SetActive(false);

            // 紙が下からすべり込み、少しおいて、はんこが押される
            for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.45f);
                group.alpha = k;
                paper.anchoredPosition = new Vector2(0f, Mathf.Lerp(-160f, 0f, k));
                paper.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-4f, -1.2f, k));
                yield return null;
            }
            group.alpha = 1f;
            paper.anchoredPosition = Vector2.zero;
            for (float t = 0f; t < 0.7f; t += Time.unscaledDeltaTime) yield return null;
            stampRect.gameObject.SetActive(true);
            for (float t = 0f; t < 0.14f; t += Time.unscaledDeltaTime)
            {
                stampRect.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, t / 0.14f);
                yield return null;
            }
            stampRect.localScale = Vector3.one;
            UiFx.Shake(this, paper, clear ? 6f : 12f, 0.25f);
            if (clear) UiFx.Punch(this, stampRect, 0.15f, 0.3f);
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
            return text;
        }
    }
}
