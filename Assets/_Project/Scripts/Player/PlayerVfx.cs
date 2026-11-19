using System.Collections;
using Team5.Core;
using Team5.Data;
using Team5.Items;
using Team5.UI;
using UnityEngine;

namespace Team5.Player
{
    // 個性の変化とロール変化の演出（AN-03）。プレイヤーに付ける
    // ・個性が上がる：ロールの色の粒が体から立ちのぼる　・個性が下がる：灰色の霧が体から抜けていく
    // ・着替え：ポンと煙が出て、ロールの色がきらめく
    // ・クリア：光の柱に包まれ、まだ着ていない服を着て「〇〇になれた！」
    // ・ゲームオーバー（個性 0）：体から灰色の霧が流れ出して消えていく　・ほかのゲームオーバー：がっくりする
    public class PlayerVfx : MonoBehaviour
    {
        [SerializeField] private float changeThreshold = 5f; // これ以上動いたときだけ出す（Issue #43）
        [SerializeField] private Material particleMaterial;  // 空なら WorldFx のものを使う

        private static readonly Color Gray = new(0.62f, 0.62f, 0.66f);

        private GameSceneContext context;
        private GameEndJudge judge;
        private PlayerAnimator playerAnimator;
        private OutfitController outfits;
        private RoleDefinition role;
        private Color roleColor = new(0.949f, 0.639f, 0.227f);

        private void Start()
        {
            context = GameSceneContext.Instance;
            playerAnimator = GetComponent<PlayerAnimator>();
            outfits = GetComponent<OutfitController>();
            var manager = GameManager.Instance;
            role = context != null && context.Role != null ? context.Role : manager != null ? manager.CurrentRole : null;
            if (role != null) roleColor = role.roleColor;

            if (context != null && context.Parameters != null) context.Parameters.OnChanged += HandleParameterChanged;
            judge = FindFirstObjectByType<GameEndJudge>();
            if (judge != null) judge.OnEnding += HandleEnding;
            if (outfits != null) outfits.OnEquipped += HandleEquipped;
        }

        private void OnDestroy()
        {
            if (context != null && context.Parameters != null) context.Parameters.OnChanged -= HandleParameterChanged;
            if (judge != null) judge.OnEnding -= HandleEnding;
            if (outfits != null) outfits.OnEquipped -= HandleEquipped;
        }

        private Material Material => particleMaterial != null ? particleMaterial : WorldFx.Instance != null ? WorldFx.Instance.ParticleMaterial : null;
        private Vector3 Chest => transform.position + Vector3.up * 1.0f;
        private Vector3 Head => transform.position + Vector3.up * 2.1f;

        private void HandleParameterChanged(ParameterType type, float before, float after)
        {
            if (type != ParameterType.Individuality) return;
            float delta = after - before;
            if (delta >= changeThreshold) IndividualityUp();
            else if (delta <= -changeThreshold) IndividualityDown();
        }

        private void IndividualityUp()
        {
            // ロールの色の粒がらせん状に立ちのぼる
            var ps = Make("VFX_IndividualityUp", transform.position + Vector3.up * 0.2f, true);
            var main = ps.main;
            main.duration = 0.9f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(roleColor, Color.Lerp(roleColor, Color.white, 0.6f));
            main.gravityModifier = -0.35f;
            var emission = ps.emission;
            emission.rateOverTime = 70f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.45f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(2.5f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.8f);
            FadeOut(ps);
            ps.Play();
        }

        private void IndividualityDown()
        {
            // 灰色の霧が体から抜けて、下に落ちていく
            var ps = Make("VFX_IndividualityDown", Chest, false);
            var main = ps.main;
            main.duration = 0.7f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(Gray.r, Gray.g, Gray.b, 0.55f), new Color(0.45f, 0.45f, 0.5f, 0.35f));
            main.gravityModifier = 0.15f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 26) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
            FadeOut(ps);
            ps.Play();
        }

        private void HandleEquipped(OutfitDefinition outfit)
        {
            Puff(Chest, new Color(1f, 1f, 1f, 0.8f), 30);
            Sparkle(Chest, roleColor, 24);
            if (WorldFx.Instance != null && outfit != null) WorldFx.Instance.ShowText(Head, $"{outfit.displayName}を着た！", roleColor, 2.2f, 60);
        }

        private void HandleEnding(GameResult result)
        {
            switch (result)
            {
                case GameResult.Clear: StartCoroutine(ClearRoutine()); break;
                case GameResult.GameOverFaded: StartCoroutine(FadedRoutine()); break;
                default:
                    if (playerAnimator != null) playerAnimator.PlaySlump();
                    Puff(Chest, new Color(0.5f, 0.5f, 0.55f, 0.6f), 16);
                    break;
            }
        }

        private IEnumerator ClearRoutine()
        {
            if (playerAnimator != null) playerAnimator.PlayCelebrate();
            LightPillar();
            yield return new WaitForSecondsRealtime(0.6f);

            // outfits.md 5 章の 2：クリアは必ず段階 2 の姿で終わる
            if (outfits != null && role != null && role.outfits != null)
            {
                bool changed = false;
                foreach (var outfit in role.outfits)
                {
                    if (outfit == null || outfit.stage <= 0 || outfits.IsWearing(outfit)) continue;
                    outfits.WearForEnding(outfit);
                    changed = true;
                }
                if (changed) Puff(Chest, new Color(1f, 1f, 1f, 0.9f), 40);
            }
            Sparkle(Chest, roleColor, 60);
            Sparkle(Head, Color.white, 30);

            // 画面の真ん中に EndBannerView が「クリア！」を出すときは、頭の上の文字は出さない（重なるので）
            string name = role != null ? role.displayName : "なりたい自分";
            if (WorldFx.Instance != null && FindAnyObjectByType<Team5.UI.EndBannerView>() == null) WorldFx.Instance.ShowText(Head + Vector3.up * 0.3f, $"{name}になれた！", roleColor, 2.2f, 72);
        }

        private IEnumerator FadedRoutine()
        {
            if (playerAnimator != null) playerAnimator.PlaySlump();
            var ps = Make("VFX_Faded", Chest, true);
            var main = ps.main;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(Gray.r, Gray.g, Gray.b, 0.5f), new Color(0.8f, 0.8f, 0.85f, 0.3f));
            main.gravityModifier = -0.12f;
            var emission = ps.emission;
            emission.rateOverTime = 45f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 1.6f));
            FadeOut(ps);
            ps.Play();

            // 体が少しずつ小さくなって消えていく
            Vector3 start = transform.localScale;
            for (float t = 0f; t < 2f; t += Time.unscaledDeltaTime)
            {
                transform.localScale = start * Mathf.Lerp(1f, 0.05f, Mathf.SmoothStep(0f, 1f, t / 2f));
                yield return null;
            }
        }

        // ---------- 粒の部品 ----------

        private void LightPillar()
        {
            var ps = Make("VFX_RoleComplete", transform.position, false);
            var main = ps.main;
            main.duration = 1.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Color.Lerp(roleColor, Color.white, 0.4f));
            main.gravityModifier = -0.1f;
            var emission = ps.emission;
            emission.rateOverTime = 160f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone; // 真上に立ちのぼる光の柱
            shape.angle = 4f;
            shape.radius = 0.6f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            FadeOut(ps);
            ps.Play();

            // 足元に広がる光の輪
            var ring = Make("VFX_RoleComplete_Ring", transform.position + Vector3.up * 0.1f, false);
            var ringMain = ring.main;
            ringMain.duration = 0.3f;
            ringMain.startLifetime = 0.7f;
            ringMain.startSpeed = 5f;
            ringMain.startSize = 0.18f;
            ringMain.startColor = roleColor;
            var ringEmission = ring.emission;
            ringEmission.rateOverTime = 0f;
            ringEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            var ringShape = ring.shape;
            ringShape.shapeType = ParticleSystemShapeType.Circle;
            ringShape.radius = 0.2f;
            ringShape.radiusThickness = 0f;
            ringShape.rotation = new Vector3(90f, 0f, 0f);
            FadeOut(ring);
            ring.Play();
        }

        private void Puff(Vector3 position, Color color, int count)
        {
            var ps = Make("FxPuff", position, false);
            var main = ps.main;
            main.duration = 0.4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startColor = color;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.4f;
            limit.dampen = 0.25f;
            FadeOut(ps);
            ps.Play();
        }

        private void Sparkle(Vector3 position, Color color, int count)
        {
            var ps = Make("FxSparkle", position, false);
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.gravityModifier = 0.3f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            FadeOut(ps);
            ps.Play();
        }

        // follow：プレイヤーに付いていくか
        private ParticleSystem Make(string name, Vector3 position, bool follow)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            if (follow) go.transform.SetParent(transform, true);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.useUnscaledTime = true;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Material;
            return ps;
        }

        private static void FadeOut(ParticleSystem ps)
        {
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
        }
    }
}
