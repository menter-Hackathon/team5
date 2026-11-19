using System.Collections.Generic;
using Team5.Core;
using Team5.Data;
using Team5.Items;
using Team5.NPC;
using Team5.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Team5.UI
{
    // 地図。左下にプレイヤーのまわりの小さな地図を出し、M（ゲームパッド Select）で村全体の地図を開く。Game シーンに置く
    // 村は真上のカメラでそのまま映す（村人の動きも見える）。プレイヤー・村人・お題の場所・素材・作業台は印で重ねる。地図をクリックしても開く
    public class MapView : MonoBehaviour
    {
        [SerializeField] private Font font;
        [SerializeField] private Sprite panelSprite;                         // UI_Panel_Dark
        [SerializeField] private Vector2 worldMin = new Vector2(-21f, -21f); // 村の端（x, z）
        [SerializeField] private Vector2 worldMax = new Vector2(21f, 21f);
        [SerializeField] private float miniRadius = 13f;                     // 小さな地図に映す半径（m）
        [SerializeField] private float miniSize = 230f;
        [SerializeField] private float fullSize = 760f;

        private static readonly Dictionary<string, string> SpotNames = new Dictionary<string, string>
        {
            { "chief_house", "村長の家" }, { "kitchen", "台所" }, { "library", "図書館" }, { "forge", "鍛冶場" },
            { "canteen", "食堂" }, { "barn", "納屋" }, { "home", "わが家" }, { "attic", "屋根裏" },
            { "well", "井戸" }, { "training_ground", "訓練場" }, { "plaza", "広場" }, { "field", "畑" }, { "forest", "森" },
        };

        private enum Kind { Player, Npc, Target, Pickup, Station }

        private class Marker
        {
            public Kind kind;
            public Transform target;
            public RectTransform mini;
            public RectTransform full;
            public Image miniImage;
            public Image fullImage;
            public VisionSensor sensor;
        }

        private GameSceneContext context;
        private RenderTexture snapshot;
        private RectTransform miniRoot;   // 小さな地図（中身を動かす）
        private RectTransform miniContent;
        private RectTransform fullPanel;
        private RectTransform fullContent;
        private readonly List<Marker> markers = new List<Marker>();
        private Marker playerMarker;
        private Marker targetMarker;
        private Marker goalMarker; // 目標（ObjectiveView）の次に行く場所
        private Sprite circle;
        private Sprite arrow;
        private TaskInstance shownTask;

        private Vector2 WorldSize => worldMax - worldMin;

        private System.Collections.IEnumerator Start()
        {
            context = GameSceneContext.Instance;
            circle = MakeCircle();
            arrow = MakeArrow();
            snapshot = new RenderTexture(1024, 1024, 24) { name = "MapView" };
            StartMapCamera();
            BuildUi();
            CollectMarkers();
            yield break;
        }

        private void OnDestroy()
        {
            if (snapshot != null) snapshot.Release();
        }

        // ---------- 村を真上から撮る ----------

        private Camera mapCamera;

        // 村を真上から映すカメラ。村人の動きもそのまま映る（数フレームに 1 回だけ描く）
        private void StartMapCamera()
        {
            var camGo = new GameObject("MapCamera");
            camGo.transform.SetParent(transform, false);
            mapCamera = camGo.AddComponent<Camera>();
            Vector2 center = (worldMin + worldMax) * 0.5f;
            mapCamera.transform.SetPositionAndRotation(new Vector3(center.x, 60f, center.y), Quaternion.Euler(90f, 0f, 0f));
            mapCamera.orthographic = true;
            mapCamera.orthographicSize = Mathf.Max(WorldSize.x, WorldSize.y) * 0.5f;
            mapCamera.nearClipPlane = 1f;
            mapCamera.farClipPlane = 120f;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(0.36f, 0.55f, 0.32f);
            mapCamera.targetTexture = snapshot;
            mapCamera.depth = -50f;
        }

        // ---------- 画面 ----------

        private void BuildUi()
        {
            var canvasGo = new GameObject("MapCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30; // チュートリアル（40）より下
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // 小さな地図（左下）
            var frame = NewRect("MiniMap", canvasGo.transform);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0f, 0f);
            frame.anchoredPosition = new Vector2(28f, 28f);
            frame.sizeDelta = new Vector2(miniSize + 12f, miniSize + 12f);
            AddPanel(frame).raycastTarget = true;
            AddClick(frame); // クリックでも全体の地図を開く

            miniRoot = NewRect("Mask", frame);
            miniRoot.sizeDelta = new Vector2(miniSize, miniSize);
            miniRoot.gameObject.AddComponent<RectMask2D>();

            float scale = miniSize / (miniRadius * 2f); // 1m あたりの px
            miniContent = NewRect("Content", miniRoot);
            miniContent.sizeDelta = WorldSize * scale;
            var miniImage = miniContent.gameObject.AddComponent<RawImage>();
            miniImage.texture = snapshot;
            miniImage.raycastTarget = false;

            var keyHint = NewText("M・クリック：地図を広げる", frame, 20, TextAnchor.UpperCenter);
            keyHint.rectTransform.anchorMin = keyHint.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            keyHint.rectTransform.pivot = new Vector2(0.5f, 0f);
            keyHint.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            var hintOutline = keyHint.gameObject.AddComponent<Outline>();
            hintOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            keyHint.rectTransform.sizeDelta = new Vector2(200f, 28f);

            // 村全体の地図（M で開く）
            fullPanel = NewRect("FullMap", canvasGo.transform);
            fullPanel.sizeDelta = new Vector2(fullSize + 40f, fullSize + 90f);
            AddPanel(fullPanel).raycastTarget = true;
            AddClick(fullPanel); // クリックで閉じる
            var title = NewText("村の地図", fullPanel, 34, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -32f);
            title.rectTransform.sizeDelta = new Vector2(fullSize, 50f);

            fullContent = NewRect("Content", fullPanel);
            fullContent.anchoredPosition = new Vector2(0f, -18f);
            fullContent.sizeDelta = new Vector2(fullSize, fullSize);
            var fullImage = fullContent.gameObject.AddComponent<RawImage>();
            fullImage.texture = snapshot;
            fullImage.raycastTarget = false;

            var legend = NewText("<color=#FFE15A>▲</color> 自分　<color=#FF5A4A>●</color> 村人（赤：見られている）　<color=#FFD23C>！</color> お題　<color=#7CF08A>◆</color> 素材　<color=#FFA040>■</color> 作業台　　M・クリックで閉じる",
                fullPanel, 22, TextAnchor.MiddleCenter);
            legend.supportRichText = true;
            legend.rectTransform.anchorMin = legend.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            legend.rectTransform.anchoredPosition = new Vector2(0f, 26f);
            legend.rectTransform.sizeDelta = new Vector2(fullSize + 20f, 32f);

            // 場所の名前（全体の地図だけ）
            foreach (var spot in FindObjectsByType<TaskSpot>(FindObjectsSortMode.None))
            {
                if (!SpotNames.TryGetValue(spot.SpotId, out string name)) continue;
                var label = NewText(name, fullContent, 20, TextAnchor.MiddleCenter);
                label.rectTransform.sizeDelta = new Vector2(160f, 26f);
                label.rectTransform.anchoredPosition = ToFull(spot.transform.position) + new Vector2(0f, -22f);
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            fullPanel.gameObject.SetActive(false);
        }

        private void CollectMarkers()
        {
            foreach (var pickup in FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
                AddMarker(Kind.Pickup, pickup.transform, circle, new Color(0.49f, 0.94f, 0.54f), 11f);
            foreach (var station in FindObjectsByType<CraftStation>(FindObjectsSortMode.None))
                AddMarker(Kind.Station, station.transform, null, new Color(1f, 0.63f, 0.25f), 16f);
            foreach (var sensor in FindObjectsByType<VisionSensor>(FindObjectsSortMode.None))
            {
                var m = AddMarker(Kind.Npc, sensor.transform, circle, new Color(1f, 0.55f, 0.3f), 16f);
                m.sensor = sensor;
            }
            goalMarker = AddMarker(Kind.Target, null, circle, new Color(0.98f, 0.5f, 0.78f), 24f, "★");
            targetMarker = AddMarker(Kind.Target, null, circle, new Color(1f, 0.82f, 0.24f), 26f);
            if (context != null && context.Player != null)
                playerMarker = AddMarker(Kind.Player, context.Player, arrow, new Color(1f, 0.88f, 0.35f), 26f);
        }

        private Marker AddMarker(Kind kind, Transform target, Sprite sprite, Color color, float size, string label = "！")
        {
            var m = new Marker { kind = kind, target = target };
            m.miniImage = NewIcon(miniRoot, sprite, color, size);
            m.fullImage = NewIcon(fullContent, sprite, color, size * 1.15f);
            m.mini = m.miniImage.rectTransform;
            m.full = m.fullImage.rectTransform;
            if (kind == Kind.Target)
            {
                foreach (var image in new[] { m.miniImage, m.fullImage })
                {
                    var mark = NewText(label, image.transform, 22, TextAnchor.MiddleCenter);
                    mark.color = new Color(0.25f, 0.15f, 0f);
                    mark.rectTransform.sizeDelta = image.rectTransform.sizeDelta;
                }
            }
            markers.Add(m);
            return m;
        }

        // ---------- 毎フレーム ----------

        private void Update()
        {
            // 地図のカメラは 3 フレームに 1 回だけ描く（重くしない）
            if (mapCamera != null) mapCamera.enabled = Time.frameCount % 3 == 0;

            var keyboard = Keyboard.current;
            bool toggle = (keyboard != null && keyboard.mKey.wasPressedThisFrame)
                          || (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);
            bool ended = GameManager.Instance != null && GameManager.Instance.State == GameState.Result;
            if (toggle && !ended) ToggleFull();
            if (ended && fullPanel != null && fullPanel.gameObject.activeSelf) fullPanel.gameObject.SetActive(false);
        }

        private void AddClick(RectTransform rect)
        {
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(ToggleFull);
        }

        private void ToggleFull()
        {
            // ボタンが選ばれたままだと Space / Enter でも開閉してしまうので外す
            if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            if (fullPanel == null) return;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Result) return;
            bool open = !fullPanel.gameObject.activeSelf;
            fullPanel.gameObject.SetActive(open);
            if (open) UiFx.Punch(this, fullPanel, 0.04f, 0.2f);
        }

        private void LateUpdate()
        {
            if (miniContent == null) return;
            Vector3 center = context != null && context.Player != null ? context.Player.position : Vector3.zero;
            float scale = miniSize / (miniRadius * 2f);
            // 小さな地図はプレイヤーが真ん中に来るように動かす
            Vector2 offset = new Vector2(center.x, center.z) - (worldMin + worldMax) * 0.5f;
            miniContent.anchoredPosition = -offset * scale;

            UpdateTarget();
            float pulse = 1f + 0.18f * Mathf.Sin(Time.unscaledTime * 6f);

            foreach (var m in markers)
            {
                bool alive = m.target != null && m.target.gameObject.activeInHierarchy;
                if (m.kind == Kind.Pickup && alive && m.target.TryGetComponent(out ItemPickup p)) alive = p.enabled;
                m.mini.gameObject.SetActive(alive);
                m.full.gameObject.SetActive(alive);
                if (!alive) continue;

                Vector3 pos = m.target.position;
                Vector2 local = (new Vector2(pos.x, pos.z) - new Vector2(center.x, center.z)) * scale;
                if (m.kind == Kind.Target)
                {
                    // お題の場所は、小さな地図の外なら縁に出す
                    float limit = miniSize * 0.5f - 14f;
                    if (Mathf.Abs(local.x) > limit || Mathf.Abs(local.y) > limit)
                        local *= limit / Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.y));
                    m.mini.localScale = m.full.localScale = Vector3.one * pulse;
                }
                m.mini.anchoredPosition = local;
                m.full.anchoredPosition = ToFull(pos);

                if (m.kind == Kind.Player)
                {
                    var rot = Quaternion.Euler(0f, 0f, -m.target.eulerAngles.y);
                    m.mini.localRotation = m.full.localRotation = rot;
                    m.mini.SetAsLastSibling();
                    m.full.SetAsLastSibling();
                }
                else if (m.kind == Kind.Npc)
                {
                    Color c = m.sensor != null && m.sensor.CanSeePlayer ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.6f, 0.35f);
                    m.miniImage.color = m.fullImage.color = c;
                    float s = m.sensor != null && m.sensor.CanSeePlayer ? pulse : 1f;
                    m.mini.localScale = m.full.localScale = Vector3.one * s;
                }
            }
        }

        // 今のお題の場所に印を動かす
        private void UpdateTarget()
        {
            if (goalMarker != null) goalMarker.target = ObjectiveView.NextTarget;
            if (targetMarker == null) return;
            var task = context != null && context.Tasks != null ? context.Tasks.Current : null;
            if (task == shownTask && (task == null || targetMarker.target != null)) return;
            shownTask = task;
            targetMarker.target = task != null ? FindTarget(task.Definition) : null;
        }

        private static Transform FindTarget(TaskDefinition def)
        {
            if (def == null) return null;
            switch (def.type)
            {
                case TaskType.ActionAtSpot:
                    foreach (var spot in FindObjectsByType<TaskSpot>(FindObjectsSortMode.None))
                        if (spot.SpotId == def.targetSpotId) return spot.transform;
                    break;
                case TaskType.TalkToNpc:
                    foreach (var npc in FindObjectsByType<NpcTalkable>(FindObjectsSortMode.None))
                        if (npc.NpcId == def.targetSpotId) return npc.transform;
                    break;
                case TaskType.BringItem:
                    var point = FindAnyObjectByType<DeliveryPoint>();
                    if (point != null) return point.transform;
                    break;
            }
            return null;
        }

        private Vector2 ToFull(Vector3 world)
        {
            Vector2 center = (worldMin + worldMax) * 0.5f;
            float scale = fullSize / Mathf.Max(WorldSize.x, WorldSize.y);
            return (new Vector2(world.x, world.z) - center) * scale;
        }

        // ---------- 部品 ----------

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private Image AddPanel(RectTransform rect)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = panelSprite;
            image.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = panelSprite != null ? new Color(1f, 1f, 1f, 0.92f) : new Color(0.1f, 0.08f, 0.06f, 0.85f);
            image.raycastTarget = false;
            return image;
        }

        private Text NewText(string value, Transform parent, int size, TextAnchor anchor)
        {
            var rect = NewRect("Text", parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        private static Image NewIcon(Transform parent, Sprite sprite, Color color, float size)
        {
            var rect = NewRect("Icon", parent);
            rect.sizeDelta = new Vector2(size, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return image;
        }

        private static Sprite MakeCircle()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "MapCircle" };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f));
                float a = Mathf.Clamp01(n * 0.5f - 1f - d);
                float edge = d > n * 0.5f - 7f ? 0.55f : 1f; // 縁を少し暗く
                tex.SetPixel(x, y, new Color(edge, edge, edge, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        // 上（北）向きの矢印
        private static Sprite MakeArrow()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "MapArrow" };
            Vector2 a = new Vector2(n * 0.5f, n - 2f), b = new Vector2(6f, 4f), c = new Vector2(n * 0.5f, 18f), d = new Vector2(n - 6f, 4f);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                bool inside = InTri(p, a, b, c) || InTri(p, a, c, d);
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        private static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s1 = Cross(b - a, p - a), s2 = Cross(c - b, p - b), s3 = Cross(a - c, p - c);
            bool neg = s1 < 0 || s2 < 0 || s3 < 0, pos = s1 > 0 || s2 > 0 || s3 > 0;
            return !(neg && pos);
        }

        private static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
    }
}
