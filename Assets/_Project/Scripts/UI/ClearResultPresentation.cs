using Team5.Core;
using Team5.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // クリア専用。既存のゲームオーバーUI・イベントには触れない。
    public class ClearResultPresentation : MonoBehaviour
    {
        private static readonly Color Ink = new Color(0.25f, 0.16f, 0.10f);

        public void Show(Sprite backdrop, Sprite wood, GameObject playerPrefab,
            RoleDefinition role, GameResultSummary summary, GameBalance balance, Font font, Shader portraitShader)
        {
            foreach (Transform child in transform)
                if (child.name != "Background" && child.name != "RetryButton" && child.name != "TitleButton")
                    child.gameObject.SetActive(false);

            var background = transform.Find("Background").GetComponent<Image>();
            background.sprite = backdrop;
            background.color = Color.white;
            background.type = Image.Type.Simple;
            background.raycastTarget = false;

            string wanted = role != null && !string.IsNullOrWhiteSpace(role.displayName) ? role.displayName : "なりたい自分";
            string imposed = role != null && !string.IsNullOrWhiteSpace(role.imposedRoleName) ? role.imposedRoleName : "勇者";
            bool ramen = role != null && role.roleId == "ramen_chef";
            MakeText("ClearHeading", "これが、わたしらしさ。", font, 53, 850, 125, 900, 95);
            MakeText("ClearStory", imposed + "になるはずだった。\n" + (ramen
                ? "けれど、いま手にしているのは、自分で作った一杯だ。"
                : "けれど、いまここにいるのは、自分で選んだわたしだ。"), font, 28, 870, 245, 850, 105);
            MakeText("ClearRole", "なりたかった自分　" + wanted, font, 32, 870, 365, 850, 55);
            MakeText("ClearAchievement", $"なりたい自分への達成率　{AchievementPercent(summary.roleProgress, balance != null ? balance.roleProgressToWin : 100f)}%",
                font, 36, 870, 435, 850, 65);
            int seconds = Mathf.Max(0, Mathf.RoundToInt(summary.elapsedSeconds));
            MakeText("ClearRecords", $"個性 {Mathf.RoundToInt(summary.individuality)}　｜　平和度 {Mathf.RoundToInt(summary.peace)}\n時間 {seconds / 60}:{seconds % 60:00}　お題 {summary.tasksCompleted}回　発覚 {summary.timesCaught}回",
                font, 25, 870, 510, 850, 80);
            MakeText("ClearChief", ramen ? "村長「……まあ、一杯くらいは、\nいただこうか。」" : "村長「……それが、\nお前の選んだ道か。」",
                font, 30, 1130, 655, 600, 145);
            StyleButton("RetryButton", "もう一度あそぶ", wood, 850);
            StyleButton("TitleButton", "タイトルへ", wood, 1330);
            var portrait = new GameObject("ClearLiveCharacter", typeof(RectTransform), typeof(RawImage));
            portrait.transform.SetParent(transform, false);
            Place((RectTransform)portrait.transform, 65, 180, 690, 820);
            portrait.GetComponent<RawImage>().raycastTarget = false;
            portrait.AddComponent<ResultCharacterPreview>().Initialize(playerPrefab, role, portraitShader);
        }

        public static int AchievementPercent(float progress, float target)
        {
            if (float.IsNaN(progress) || float.IsInfinity(progress)) return 0;
            if (target <= 0f || float.IsNaN(target) || float.IsInfinity(target)) return 0;
            return Mathf.RoundToInt(Mathf.Clamp01(progress / target) * 100f);
        }

        private void MakeText(string name, string value, Font font, int size, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            Place((RectTransform)go.transform, x, y, width, height);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.color = Ink;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = size;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
        }

        private void StyleButton(string name, string caption, Sprite wood, float x)
        {
            var rect = transform.Find(name) as RectTransform;
            if (rect == null) return;
            Place(rect, x, 875, 440, 105);
            var button = rect.GetComponent<Button>();
            if (wood != null)
            {
                var image = rect.GetComponent<Image>();
                image.sprite = wood;
                image.type = Image.Type.Simple;
                image.color = new Color(0.70f, 0.49f, 0.30f);
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.9f, 0.7f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
                button.colors = colors;
            }
            var text = rect.GetComponentInChildren<Text>();
            text.text = caption;
            text.color = Color.white;
            text.fontSize = 32;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 22;
            text.resizeTextMaxSize = 32;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
