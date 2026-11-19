using System.Collections.Generic;
using Team5.Core;
using Team5.Data;
using Team5.NPC;
using Team5.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // 村人の頭の上に名前と肩書きを出す。お題で話しかける相手は金色で「！」をつける。
    // 近くに行くと、その人らしいひとことを吹き出しで言う（だれが誰か覚えやすいように）
    public class NpcNameplateView : MonoBehaviour
    {
        private struct Persona
        {
            public string name;
            public string title;
            public string[] lines;
        }

        private static readonly Dictionary<string, Persona> Personas = new Dictionary<string, Persona>
        {
            { "chief", new Persona { name = "村長", title = "村の「らしさ」を守る人", lines = new[] { "らしく生きるのが、いちばんの幸せじゃ", "わしの若いころは、みんな言われたとおりに生きたものじゃ", "今日も村は平和じゃのう" } } },
            { "guard", new Persona { name = "見張り", title = "ぐるぐる村を見回る", lines = new[] { "異常なし！……たぶん", "あやしい動きは見逃さないぞ", "見張りらしく、目は閉じないのだ" } } },
            { "cook", new Persona { name = "食堂のおかみ", title = "献立は百年同じ", lines = new[] { "決まった物を、決まったとおりに食べな！", "うちの献立は、ひいばあちゃんの代から変わらないよ", "残さず食べるのが、村の子らしさだよ" } } },
            { "villager1", new Persona { name = "うわさ好きの村人", title = "聞いた話はすぐ広める", lines = new[] { "ねえ聞いた？ 最近、らしくない子がいるんだって", "あたしの目はごまかせないわよ", "今日のうわさ、もう知ってる？" } } },
            { "villager2", new Persona { name = "まじめな村人", title = "毎日同じ道を歩く", lines = new[] { "決められた道を歩くのが、いちばん安心だ", "道から外れると、落ち着かないんだ", "今日も、いつもの道を通るよ" } } },
        };

        private class Plate
        {
            public NpcTalkable npc;
            public Transform anchor;
            public Persona persona;
            public RectTransform root;
            public Text nameText;
            public Text titleText;
            public RectTransform bubble;
            public Text bubbleText;
            public float nextLine;
            public float lineUntil;
            public int lineIndex;
        }

        private static readonly Color Gold = new Color(1f, 0.82f, 0.24f);
        private Font font;
        private Sprite panelSprite;
        private GameSceneContext context;
        private RectTransform canvasRect;
        private readonly List<Plate> plates = new List<Plate>();

        public void Init(Font font, Sprite panelSprite) { this.font = font; this.panelSprite = panelSprite; }

        // お題などの文で使う：npcId から呼び名
        public static string NameOf(string npcId)
        {
            return npcId != null && Personas.TryGetValue(npcId, out var p) ? p.name : npcId;
        }

        private void Start()
        {
            context = GameSceneContext.Instance;
            var canvasGo = new GameObject("NameplateCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect = (RectTransform)canvasGo.transform;

            foreach (var npc in FindObjectsByType<NpcTalkable>(FindObjectsSortMode.None))
            {
                if (!Personas.TryGetValue(npc.NpcId ?? "", out var persona)) continue;
                var anchor = npc.transform.parent != null ? npc.transform.parent : npc.transform;
                plates.Add(Build(npc, anchor, persona));
            }
        }

        private Plate Build(NpcTalkable npc, Transform anchor, Persona persona)
        {
            var root = new GameObject("Plate_" + npc.NpcId, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvasRect, false);
            var nameText = NewText(root, persona.name, 26, Color.white, new Vector2(0f, 0f));
            var titleText = NewText(root, persona.title, 18, new Color(0.9f, 0.88f, 0.82f, 0.9f), new Vector2(0f, -24f));

            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            bubble.SetParent(root, false);
            bubble.anchoredPosition = new Vector2(0f, 52f);
            bubble.sizeDelta = new Vector2(520f, 52f);
            var bubbleImage = bubble.GetComponent<Image>();
            bubbleImage.sprite = panelSprite;
            bubbleImage.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bubbleImage.color = panelSprite != null ? Color.white : new Color(0.1f, 0.08f, 0.1f, 0.85f);
            bubbleImage.raycastTarget = false;
            var bubbleText = NewText(bubble, "", 22, Color.white, Vector2.zero);
            bubbleText.rectTransform.sizeDelta = new Vector2(500f, 48f);
            bubble.gameObject.SetActive(false);

            return new Plate
            {
                npc = npc, anchor = anchor, persona = persona, root = root, nameText = nameText, titleText = titleText,
                bubble = bubble, bubbleText = bubbleText, nextLine = Time.time + Random.Range(1f, 5f), lineIndex = Random.Range(0, persona.lines.Length),
            };
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || context == null || context.Player == null) return;
            bool ended = GameManager.Instance != null && GameManager.Instance.State == GameState.Result;
            var task = context.Tasks != null ? context.Tasks.Current : null;
            string talkTarget = task != null && task.State == TaskState.Active && task.Definition != null && task.Definition.type == TaskType.TalkToNpc ? task.Definition.targetSpotId : null;

            Vector2 size = canvasRect.rect.size;
            float scale = size.x / Mathf.Max(Screen.width, 1);
            foreach (var p in plates)
            {
                if (p.anchor == null) { p.root.gameObject.SetActive(false); continue; }
                Vector3 world = p.anchor.position + Vector3.up * 2.25f;
                Vector3 screen = cam.WorldToScreenPoint(world);
                float distance = Vector3.Distance(context.Player.position, p.anchor.position);
                bool target = talkTarget == p.npc.NpcId;
                bool show = !ended && screen.z > 0f && (distance < 16f || target);
                if (p.root.gameObject.activeSelf != show) p.root.gameObject.SetActive(show);
                if (!show) continue;

                p.root.anchoredPosition = new Vector2(screen.x, screen.y) * scale - size * 0.5f;
                float s = Mathf.Lerp(1.05f, 0.75f, Mathf.InverseLerp(4f, 16f, distance));
                p.root.localScale = Vector3.one * s;
                p.nameText.text = target ? $"！ {p.persona.name}（お題の相手）" : p.persona.name;
                p.nameText.color = target ? Gold : Color.white;

                // 近くにいるときだけ、ときどきひとこと
                if (distance < 6f && Time.time >= p.nextLine)
                {
                    p.lineIndex = (p.lineIndex + 1) % p.persona.lines.Length;
                    p.bubbleText.text = p.persona.lines[p.lineIndex];
                    p.lineUntil = Time.time + 3.2f;
                    p.nextLine = Time.time + Random.Range(7f, 11f);
                }
                bool talking = Time.time < p.lineUntil;
                if (p.bubble.gameObject.activeSelf != talking) p.bubble.gameObject.SetActive(talking);
            }
        }

        private Text NewText(Transform parent, string value, int size, Color color, Vector2 pos)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            text.rectTransform.sizeDelta = new Vector2(500f, size * 1.4f);
            text.rectTransform.anchoredPosition = pos;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            return text;
        }
    }
}
