using System.Collections;
using Team5.Core;
using Team5.Data;
using Team5.NPC;
using Team5.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // ゲームの中の演出：拾った・作った・お題をこなした・見つかったときに、粒と浮かぶ文字を出す
    public class WorldFx : MonoBehaviour
    {
        public static WorldFx Instance { get; private set; }

        [SerializeField] private Material particleMaterial; // URP の Particles/Unlit
        [SerializeField] private Font font;

        private static readonly Color Accent = new(1f, 0.784f, 0.239f);
        private static readonly Color Warning = new(0.839f, 0.235f, 0.259f);
        private static readonly Color Moss = new(0.557f, 0.608f, 0.498f);

        private GameSceneContext context;
        private Color roleColor = new(0.949f, 0.639f, 0.227f);
        private Camera mainCamera;

        public Material ParticleMaterial => particleMaterial;

        private void Awake() => Instance = this;

        // ほかの演出（PlayerVfx）から浮かぶ文字を出す
        public void ShowText(Vector3 position, string text, Color color, float seconds = 1.6f, int size = 56) => FloatText(position, text, color, seconds, size);

        private void Start()
        {
            context = GameSceneContext.Instance;
            mainCamera = Camera.main;
            if (context == null) return;
            var manager = GameManager.Instance;
            var role = context.Role != null ? context.Role : manager != null ? manager.CurrentRole : null;
            if (role != null) roleColor = role.roleColor;

            if (context.Crafting != null) context.Crafting.OnCrafted += HandleCrafted;
            context.Tasks.OnTaskCompleted += HandleTaskCompleted;
            context.Tasks.OnTaskFailed += HandleTaskFailed;
            context.Surveillance.OnCaught += HandleCaught;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (context == null) return;
            if (context.Crafting != null) context.Crafting.OnCrafted -= HandleCrafted;
            context.Tasks.OnTaskCompleted -= HandleTaskCompleted;
            context.Tasks.OnTaskFailed -= HandleTaskFailed;
            context.Surveillance.OnCaught -= HandleCaught;
        }

        private Vector3 PlayerHead => context != null && context.Player != null ? context.Player.position + Vector3.up * 2f : Vector3.zero;

        // ItemPickup から呼ぶ
        public void Pickup(Vector3 position, ItemDefinition item)
        {
            Burst(position + Vector3.up * 0.4f, Color.white, 14, 2.2f);
            FloatText(PlayerHead, $"+ {item.displayName}", Color.white);
        }

        private void HandleCrafted(RecipeDefinition recipe)
        {
            Burst(PlayerHead - Vector3.up * 0.8f, roleColor, 40, 3.5f);
            FloatText(PlayerHead, $"{recipe.displayName} ができた！  進み具合 +{recipe.roleProgressReward:0}", roleColor, 1.4f);
        }

        private void HandleTaskCompleted(TaskInstance task)
        {
            Burst(PlayerHead - Vector3.up * 0.8f, Accent, 24, 2.6f);
            var definition = task.Definition;
            string reward = definition.rewardItem != null ? $"\n+ {definition.rewardItem.displayName}（ごほうび）" : "";
            FloatText(PlayerHead, $"平和度 +{definition.peaceReward:0}　個性 −{definition.individualityCost:0}{reward}", Accent, 1.6f);
        }

        private void HandleTaskFailed(TaskInstance task)
        {
            FloatText(PlayerHead, $"お題失敗…  平和度 −{task.Definition.failPeacePenalty:0}", Warning);
        }

        private void HandleCaught(VisionSensor watcher)
        {
            FloatText(watcher.transform.position + Vector3.up * 2.4f, "！", Warning, 1f, 90);
            StartCoroutine(Startle(watcher)); // 見つけた NPC が驚いて跳ねる（AN-02）
            Burst(PlayerHead - Vector3.up * 0.8f, Warning, 18, 2.4f);
        }

        // NPC のモデルだけを跳ねさせる（本体は NavMeshAgent が動かすので触らない）
        private IEnumerator Startle(VisionSensor watcher)
        {
            var npc = watcher != null ? watcher.GetComponentInParent<NpcModelAnimator>() : null;
            var animator = npc != null ? npc.GetComponentInChildren<Animator>() : null;
            if (animator == null) yield break;
            var model = animator.transform;
            Vector3 home = model.localPosition;
            Vector3 homeScale = model.localScale;
            const float seconds = 0.45f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (model == null) yield break;
                float k = t / seconds;
                float arc = Mathf.Sin(k * Mathf.PI);
                model.localPosition = home + Vector3.up * (0.3f * arc);
                model.localScale = new Vector3(homeScale.x * (1f - 0.06f * arc), homeScale.y * (1f + 0.12f * arc), homeScale.z * (1f - 0.06f * arc));
                yield return null;
            }
            if (model != null) { model.localPosition = home; model.localScale = homeScale; }
        }

        // ---------- 粒 ----------

        private void Burst(Vector3 position, Color color, int count, float speed)
        {
            var go = new GameObject("FxBurst");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            ps.Play();
        }

        // ---------- 浮かぶ文字 ----------

        private void FloatText(Vector3 position, string text, Color color, float seconds = 1.1f, int size = 44)
        {
            var go = new GameObject("FxText", typeof(Canvas));
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.01f;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            ((RectTransform)go.transform).sizeDelta = new Vector2(900, 120);
            var label = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            label.transform.SetParent(go.transform, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var t = label.GetComponent<Text>();
            t.text = text; t.font = font; t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.GetComponent<Outline>().effectColor = new Color(0.18f, 0.169f, 0.2f, 0.9f);
            label.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            StartCoroutine(FloatRoutine(go, t, seconds));
        }

        private IEnumerator FloatRoutine(GameObject go, Text text, float seconds)
        {
            Vector3 start = go.transform.position;
            Color color = text.color;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = t / seconds;
                go.transform.position = start + Vector3.up * (1.2f * (1f - Mathf.Pow(1f - k, 2f)));
                if (mainCamera != null) go.transform.rotation = mainCamera.transform.rotation;
                float pop = k < 0.15f ? Mathf.Lerp(0.6f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Min(1f, (k - 0.15f) / 0.2f));
                go.transform.localScale = Vector3.one * 0.01f * pop;
                text.color = new Color(color.r, color.g, color.b, k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
                yield return null;
            }
            Destroy(go);
        }
    }
}
