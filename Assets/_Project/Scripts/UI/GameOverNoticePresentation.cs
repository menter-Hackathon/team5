using Team5.Core;
using Team5.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // ゲームオーバー専用。村役場から届く書類（除籍通知書・適合証明書・現状維持通知書）の形で結果を出す。
    // 机の上に傾けて置いた書類（二重線の枠・番号・題名・宛名・本文・記・記録の表・備考・以上・村長印）と、
    // 結果の判子、テープで貼った写真、ボタンで組む。デザインは Claude Design の「結果画面の案（村役場の通達）」
    public class GameOverNoticePresentation : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(0x3B, 0x35, 0x30, 0xFF);
        private static readonly Color SubInk = new Color32(0x6F, 0x6A, 0x63, 0xFF);
        private static readonly Color Line = new Color32(0xD9, 0xD3, 0xC7, 0xFF);
        private static readonly Color Red = new Color32(0xC8, 0x32, 0x3A, 0xFF);

        private Font font;

        private struct Notice
        {
            public Color desk, paper, ink, stamp;
            public string title, body, sub, stampText, note, caption;
            public bool fadedPhoto;
        }

        public void Show(GameResult result, RoleDefinition role, GameResultSummary summary, GameBalance balance, Font textFont, Sprite photo)
        {
            font = textFont;
            foreach (Transform child in transform)
                if (child.name != "Background" && child.name != "RetryButton" && child.name != "TitleButton")
                    child.gameObject.SetActive(false);

            string imposed = role != null && !string.IsNullOrWhiteSpace(role.imposedRoleName) ? role.imposedRoleName : "勇者";
            string wanted = role != null && !string.IsNullOrWhiteSpace(role.displayName) ? role.displayName : "なりたい自分";
            var n = NoticeOf(result, imposed);

            var background = transform.Find("Background").GetComponent<Image>();
            background.sprite = null;
            background.color = n.desk;

            // 書類（少し傾ける）
            var paper = Box("NoticePaper", transform, n.paper, 150, 70, 1000, 930);
            paper.localRotation = Quaternion.Euler(0, 0, 1.2f);
            Shadow(paper);
            // 二重線の枠
            var frame = Stretch(Img("FrameOuter", paper, n.ink), 24);
            var frameIn = Stretch(Img("FramePaper", frame, n.paper), 3);
            var frameLine = Stretch(Img("FrameInner", frameIn, n.ink), 3);
            var content = Stretch(Img("Content", frameLine, n.paper), 2);

            var column = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            column.SetParent(content, false);
            column.anchorMin = Vector2.zero; column.anchorMax = Vector2.one;
            column.offsetMin = new Vector2(52, 30); column.offsetMax = new Vector2(-52, -30);
            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14; layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;

            Text(column, "Number", "村役場　第 0042 号", 20, SubInk, TextAnchor.MiddleLeft);
            Text(column, "Title", Spaced(n.title), 40, n.ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text(column, "To", imposed + "　殿", 28, n.ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text(column, "Body", n.body, 24, n.ink, TextAnchor.UpperLeft).lineSpacing = 1.2f;
            if (!string.IsNullOrEmpty(n.sub)) Text(column, "Sub", n.sub, 24, SubInk, TextAnchor.MiddleLeft);
            Text(column, "Ki", "記", 24, n.ink, TextAnchor.MiddleCenter);

            // 記録の表（上下に太い線）
            Rule(column, n.ink, 2);
            var grid = new GameObject("Records", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
            grid.SetParent(column, false);
            var g = grid.GetComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(412, 56); g.spacing = new Vector2(8, 4); g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 2;
            grid.GetComponent<LayoutElement>().preferredHeight = 56 * 3 + 8;
            float indMax = balance != null ? balance.individualityMax : 100f;
            float peaceMax = balance != null ? balance.peaceMax : 100f;
            Record(grid, "個性の濃さ", $"{Mathf.RoundToInt(summary.individuality)}", $" / {Mathf.RoundToInt(indMax)}", n.ink, false);
            Record(grid, "村の平和度", $"{Mathf.RoundToInt(summary.peace)}", $" / {Mathf.RoundToInt(peaceMax)}", n.ink, result == GameResult.GameOverPeace);
            Record(grid, "こなしたお題", $"{summary.tasksCompleted}", " 回", n.ink, false);
            Record(grid, "見つかった回数", $"{summary.timesCaught}", " 回", n.ink, false);
            Record(grid, $"{wanted}への進み具合", $"{Mathf.RoundToInt(summary.roleProgress)}", " / 100", n.ink, false);
            Rule(column, n.ink, 2);

            Text(column, "Note", "備考　" + n.note, 22, n.ink, TextAnchor.UpperLeft).lineSpacing = 1.15f;
            Text(column, "End", "以上", 22, n.ink, TextAnchor.MiddleRight);

            // 発行者と村長印（書類の右下）
            var issuer = Text(paper, "Issuer", "村役場　村長", 28, n.ink, TextAnchor.MiddleRight, FontStyle.Bold);
            Place(issuer.rectTransform, 520, 840, 340, 50);
            var seal = Box("Seal", paper, n.stamp, 875, 832, 64, 64);
            seal.localRotation = Quaternion.Euler(0, 0, -3f);
            var sealIn = Stretch(Img("SealPaper", seal, n.paper), 3);
            var sealText = Text(sealIn, "SealText", "村長\n之印", 18, n.stamp, TextAnchor.MiddleCenter);
            Stretch(sealText.rectTransform, 0);

            // 結果の判子（四角・朱肉の色）
            var stamp = Box("Stamp", paper, n.stamp, 790, 28, 170, 170); // 題名の右上（本文と表にかからない位置）
            stamp.localRotation = Quaternion.Euler(0, 0, -10f);
            stamp.GetComponent<Image>().color = new Color(n.stamp.r, n.stamp.g, n.stamp.b, 0.88f);
            var stampIn = Stretch(Img("StampPaper", stamp, n.paper), 8);
            var stampText = Text(stampIn, "StampText", n.stampText, n.stampText.Length > 2 ? 44 : 64, n.stamp, TextAnchor.MiddleCenter, FontStyle.Bold);
            stampText.lineSpacing = 0.85f;
            Stretch(stampText.rectTransform, 0);

            // テープで貼った写真
            var frameImg = Box("Photo", transform, Color.white, 1250, 120, 540, 590);
            frameImg.localRotation = Quaternion.Euler(0, 0, -2.4f);
            Shadow(frameImg);
            var pic = Img("Picture", frameImg, n.fadedPhoto ? new Color(0.75f, 0.75f, 0.75f, 0.8f) : Color.white);
            pic.GetComponent<Image>().sprite = photo;
            pic.GetComponent<Image>().preserveAspect = true;
            Place(pic, 24, 24, 492, 492);
            var tape = Box("Tape", frameImg, new Color(0.85f, 0.83f, 0.78f, 0.85f), 200, -18, 140, 38);
            tape.localRotation = Quaternion.Euler(0, 0, 4f);
            if (!string.IsNullOrEmpty(n.caption))
            {
                var cap = Text(frameImg, "Caption", n.caption, 28, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                Place(cap.rectTransform, 0, 524, 540, 56);
            }

            // ボタン（写真の下）
            MoveButton("RetryButton", 1250, 890, 330, 76);
            MoveButton("TitleButton", 1600, 890, 260, 76);
        }

        private static Notice NoticeOf(GameResult result, string imposed)
        {
            switch (result)
            {
                case GameResult.GameOverPeace:
                    return new Notice
                    {
                        desk = new Color32(0x2E, 0x2B, 0x33, 0xFF), paper = new Color32(0xFB, 0xF7, 0xEF, 0xFF), ink = Ink, stamp = Red,
                        title = "除籍通知書",
                        body = $"あなたは、{imposed}にふさわしくない行いを村人に繰り返し目撃されました。村の決まりにより、本日をもって村の名簿から除籍します。",
                        stampText = "追放",
                        note = "らしくない行動は、こっそり行いましょう。お題をこなすと平和度が戻ります。",
                        caption = "",
                    };
                case GameResult.GameOverFaded:
                    return new Notice
                    {
                        desk = new Color32(0x57, 0x54, 0x4F, 0xFF), paper = new Color32(0xE9, 0xE6, 0xE0, 0xFF), ink = new Color32(0x4A, 0x47, 0x44, 0xFF),
                        stamp = new Color32(0x5E, 0x5A, 0x56, 0xFF),
                        title = "適合証明書",
                        body = $"あなたが、村の定める『{imposed}らしさ』のすべての項目に適合していることを、ここに証明します。",
                        sub = "もう、迷うことは何もありません。",
                        stampText = "適合",
                        note = "言われたことばかりでは、色が消えてしまいます。こっそりクラフトして、自分の色を取り戻しましょう。",
                        caption = "証明写真",
                        fadedPhoto = true,
                    };
                default:
                    return new Notice
                    {
                        desk = new Color32(0x34, 0x32, 0x4A, 0xFF), paper = new Color32(0xF4, 0xEF, 0xE4, 0xFF), ink = Ink,
                        stamp = new Color32(0x4F, 0x4A, 0x80, 0xFF),
                        title = "現状維持通知書",
                        body = $"職業変更の申し出は、本日の日没をもって期限を過ぎました。引き続き、{imposed}として登録します。",
                        sub = "これまでどおりに。",
                        stampText = "現状\n維持",
                        note = "素材を先に集めておくと、クラフトが早く終わります。",
                        caption = "作りかけのまま",
                    };
            }
        }

        // 題名は字の間をあける（書類らしく）
        private static string Spaced(string s) => string.Join(" ", s.ToCharArray());

        private void Record(RectTransform grid, string label, string value, string unit, Color ink, bool warn)
        {
            var cell = new GameObject(label, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            cell.SetParent(grid, false);
            cell.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var l = Text(cell, "Label", label, 21, SubInk, TextAnchor.MiddleLeft);
            Stretch(l.rectTransform, 0); l.rectTransform.offsetMin = new Vector2(4, 0);
            var v = Text(cell, "Value", $"{value}<size=18><color=#6F6A63>{unit}</color></size>", 30, warn ? Red : ink, TextAnchor.MiddleRight, FontStyle.Bold);
            Stretch(v.rectTransform, 0); v.rectTransform.offsetMax = new Vector2(-8, 0);
            var line = Img("Line", cell, Line);
            line.anchorMin = new Vector2(0, 0); line.anchorMax = new Vector2(1, 0); line.pivot = new Vector2(0.5f, 0);
            line.anchoredPosition = Vector2.zero; line.sizeDelta = new Vector2(0, 1);
        }

        private void MoveButton(string name, float x, float y, float w, float h)
        {
            if (transform.Find(name) is RectTransform rect) Place(rect, x, y, w, h);
        }

        private Text Text(Transform parent, string name, string value, int size, Color color, TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font; t.text = value; t.fontSize = size; t.color = color; t.alignment = anchor; t.fontStyle = style;
            t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void Rule(Transform column, Color color, float height)
        {
            var go = new GameObject("Rule", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(column, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<LayoutElement>().preferredHeight = height;
        }

        private static RectTransform Img(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        private static RectTransform Box(string name, Transform parent, Color color, float x, float y, float w, float h)
        {
            var rect = Img(name, parent, color);
            Place(rect, x, y, w, h);
            return rect;
        }

        private static RectTransform Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        private static void Shadow(RectTransform rect)
        {
            var shadow = rect.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.35f);
            shadow.effectDistance = new Vector2(0, -16);
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
