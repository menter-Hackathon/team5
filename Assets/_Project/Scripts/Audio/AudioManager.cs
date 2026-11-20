using System.Collections;
using Team5.Core;
using Team5.Items;
using Team5.NPC;
using Team5.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Team5.Audio
{
    // BGM と SE を鳴らす（CD-17）。Title シーンに置き DontDestroyOnLoad
    // ・BGM：タイトル／表／裏／クリア／ゲームオーバー。表 ⇔ 裏は同じ長さの曲なので、2 つを同時に流して 0.5 秒で音量を入れ替える
    // ・SE：シーンが読み込まれるたびに、そのシーンのイベントを購読して鳴らす
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioLibrary library;
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float seVolume = 0.8f;
        [SerializeField] private float crossfadeSeconds = 0.5f;
        [SerializeField] private float parameterSeThreshold = 5f; // 個性がこれ以上動いたら上がる／下がる音

        private AudioSource omote;   // 表の BGM（タイトル・結果の曲もここで鳴らす）
        private AudioSource ura;     // 裏の BGM
        private AudioSource seSource;
        private Coroutine fade;
        private bool inGame;         // 表・裏の 2 曲を流している
        private float omoteWeight;   // 0：裏だけ　1：表だけ

        private GameSceneContext context;
        private GameEndJudge judge;
        private OutfitController outfits;
        [SerializeField] private float clockWarningSeconds = 30f;
        private AudioSource craftSource, taskSource, clockSource;
        private Team5.Data.RecipeDefinition activeRecipe;
        private TaskSpot[] taskSpots = System.Array.Empty<TaskSpot>();
        private Coroutine subscribeRoutine;
        private float lastParameterSe;
        private bool endingPlayed; // 勝敗が決まったときに結果の曲を鳴らした（結果画面でもう一度鳴らさない）

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            omote = CreateSource("BGM_A");
            ura = CreateSource("BGM_B");
            seSource = CreateSource("SE");
            craftSource = CreateSource("CraftLoop");
            taskSource = CreateSource("TaskLoop");
            clockSource = CreateSource("ClockLoop");
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
            Unsubscribe();
        }

        private AudioSource CreateSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        // ---------- BGM ----------

        public void PlayBgm(BgmId id)
        {
            if (library == null || !library.TryGetBgm(id, out var entry)) return;
            if (id == BgmId.Omote || id == BgmId.Ura)
            {
                StartGameBgm(id == BgmId.Omote);
                return;
            }
            inGame = false;
            StopFade();
            ura.Stop();
            if (omote.clip == entry.clip && omote.isPlaying) return;
            omote.clip = entry.clip;
            omote.loop = entry.loop;
            omote.volume = bgmVolume;
            omote.Play();
        }

        // 表と裏を頭から同時に流し、今のフェーズのほうだけ聞こえるようにする
        private void StartGameBgm(bool watched)
        {
            if (library == null || !library.TryGetBgm(BgmId.Omote, out var o) || !library.TryGetBgm(BgmId.Ura, out var u)) return;
            StopFade();
            inGame = true;
            omote.clip = o.clip; omote.loop = true;
            ura.clip = u.clip; ura.loop = true;
            omoteWeight = watched ? 1f : 0f;
            ApplyWeight();
            omote.Play();
            ura.Play();
            ura.timeSamples = 0;
        }

        private void CrossfadeTo(bool watched)
        {
            if (!inGame) { StartGameBgm(watched); return; }
            StopFade();
            fade = StartCoroutine(Crossfade(watched ? 1f : 0f));
        }

        private IEnumerator Crossfade(float target)
        {
            float speed = crossfadeSeconds > 0f ? 1f / crossfadeSeconds : 100f;
            while (!Mathf.Approximately(omoteWeight, target))
            {
                omoteWeight = Mathf.MoveTowards(omoteWeight, target, speed * Time.unscaledDeltaTime);
                ApplyWeight();
                yield return null;
            }
            fade = null;
        }

        private void ApplyWeight()
        {
            omote.volume = bgmVolume * omoteWeight;
            ura.volume = bgmVolume * (1f - omoteWeight);
        }

        private void StopFade()
        {
            if (fade != null) StopCoroutine(fade);
            fade = null;
        }

        // ---------- SE ----------

        public void PlaySe(SeId id)
        {
            if (library == null) return;
            var clip = library.GetSe(id);
            if (clip != null) seSource.PlayOneShot(clip, seVolume);
        }

        // ---------- 状態・シーンに合わせる ----------

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Title) PlayBgm(BgmId.Title);
            else if (state == GameState.Result && GameManager.Instance != null && !endingPlayed)
                PlayBgm(GameManager.Instance.LastResult == GameResult.Clear ? BgmId.Clear : BgmId.GameOver);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Unsubscribe();
            HookButtons();
            if (scene.name == SceneNames.Title) PlayBgm(BgmId.Title);
            else if (scene.name == SceneNames.Game)
            {
                endingPlayed = false;
                subscribeRoutine = StartCoroutine(SubscribeGameNextFrame());
            }
        }

        // GameSceneContext・プレイヤーの準備ができてから購読する
        private IEnumerator SubscribeGameNextFrame()
        {
            yield return null;
            subscribeRoutine = null;
            context = GameSceneContext.Instance;
            if (context == null) yield break;

            var surveillance = context.Surveillance;
            if (surveillance != null)
            {
                surveillance.OnPhaseChanged += HandlePhaseChanged;
                surveillance.OnCaught += HandleCaught;
                StartGameBgm(surveillance.CurrentPhase == Phase.Omote);
            }
            if (context.Tasks != null)
            {
                context.Tasks.OnTaskAssigned += HandleTaskAssigned;
                context.Tasks.OnTaskCompleted += HandleTaskCompleted;
                if (context.Tasks.Current != null && context.Tasks.Current.State == TaskState.Active)
                    HandleTaskAssigned(context.Tasks.Current);
                context.Tasks.OnTaskFailed += HandleTaskFailed;
            }
            taskSpots = FindObjectsByType<TaskSpot>(FindObjectsSortMode.None);
            if (context.Crafting != null)
            {
                context.Crafting.OnCrafted += HandleCrafted;
                context.Crafting.OnCraftProgress += HandleCraftProgress;
            }
            if (context.Parameters != null) context.Parameters.OnChanged += HandleParameterChanged;
            if (context.Player != null && context.Player.TryGetComponent(out outfits)) outfits.OnEquipped += HandleEquipped;
            judge = FindFirstObjectByType<GameEndJudge>();
            if (judge != null) judge.OnEnding += HandleEnding;
        }

        private void Unsubscribe()
        {
            if (subscribeRoutine != null) StopCoroutine(subscribeRoutine);
            subscribeRoutine = null;
            activeRecipe = null;
            taskSpots = System.Array.Empty<TaskSpot>();
            StopLoops();
            if (context != null)
            {
                if (context.Surveillance != null)
                {
                    context.Surveillance.OnPhaseChanged -= HandlePhaseChanged;
                    context.Surveillance.OnCaught -= HandleCaught;
                }
                if (context.Tasks != null)
                {
                    context.Tasks.OnTaskAssigned -= HandleTaskAssigned;
                    context.Tasks.OnTaskCompleted -= HandleTaskCompleted;
                    context.Tasks.OnTaskFailed -= HandleTaskFailed;
                }
                if (context.Crafting != null)
                {
                    context.Crafting.OnCrafted -= HandleCrafted;
                    context.Crafting.OnCraftProgress -= HandleCraftProgress;
                }
                if (context.Parameters != null) context.Parameters.OnChanged -= HandleParameterChanged;
            }
            if (outfits != null) outfits.OnEquipped -= HandleEquipped;
            if (judge != null) judge.OnEnding -= HandleEnding;
            context = null; outfits = null; judge = null;
        }

        // シーンのボタンすべてに「ポッ」を付ける（非表示のパネルの中のものも）
        private void HookButtons()
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                button.onClick.AddListener(() => PlaySe(SeId.Button));
        }

        private void Update()
        {
            if (context == null || endingPlayed) { StopLoops(); return; }
            bool paused = Time.timeScale == 0f;
            bool crafting = context.Crafting != null && context.Crafting.IsCrafting;
            if (!crafting) activeRecipe = null;
            SeId? recipeSound = activeRecipe == null ? null : RecipeSound(activeRecipe.recipeId);
            SetLoop(craftSource, recipeSound, crafting, paused);

            SeId? actionSound = null;
            var current = context.Tasks != null ? context.Tasks.Current : null;
            if (current != null && current.State == TaskState.Active)
                foreach (var spot in taskSpots)
                    if (spot != null && spot.IsActing && spot.SpotId == current.Definition.targetSpotId)
                        actionSound = spot.SpotId == "training_ground" ? SeId.Swing
                            : spot.SpotId == "canteen" ? SeId.Eat : (SeId?)null;
            SetLoop(taskSource, actionSound, actionSound.HasValue, paused);
            var timer = context.Timer;
            SetLoop(clockSource, SeId.ClockWarning, timer != null && timer.IsRunning
                && timer.Remaining > 0f && timer.Remaining <= clockWarningSeconds, paused);
        }

        private static SeId? RecipeSound(string id) => id == "soup" ? SeId.Boil : id == "paitan" ? SeId.Pour : (SeId?)null;

        private void HandleCraftProgress(Team5.Data.RecipeDefinition recipe, float progress)
        {
            if (recipe == null) return;
            if (activeRecipe != recipe && !RecipeSound(recipe.recipeId).HasValue) PlaySe(SeId.Craft);
            activeRecipe = recipe;
        }

        private void SetLoop(AudioSource source, SeId? id, bool active, bool paused)
        {
            var clip = active && id.HasValue && library != null ? library.GetSe(id.Value) : null;
            if (clip == null) { source.Stop(); source.clip = null; return; }
            if (source.clip != clip)
            {
                source.clip = clip;
                source.loop = true;
                source.volume = seVolume * 0.65f;
                source.Play();
            }
            if (paused) source.Pause();
            else source.UnPause();
        }

        private void StopLoops()
        {
            foreach (var source in new[] { craftSource, taskSource, clockSource })
                if (source != null) { source.Stop(); source.clip = null; }
        }

        private void HandlePhaseChanged(Phase phase)
        {
            if (!endingPlayed) CrossfadeTo(phase == Phase.Omote); // 勝敗が決まったあとは結果の曲のまま
        }
        private void HandleCaught(VisionSensor watcher) => PlaySe(SeId.Caught);
        private void HandleTaskAssigned(TaskInstance task) => PlaySe(SeId.TaskAssigned);
        private void HandleTaskCompleted(TaskInstance task) => PlaySe(SeId.TaskComplete);
        private void HandleTaskFailed(TaskInstance task) => PlaySe(SeId.TaskFail);
        private void HandleCrafted(Team5.Data.RecipeDefinition recipe)
        {
            activeRecipe = null;
            craftSource.Stop(); craftSource.clip = null;
            PlaySe(SeId.Crafted);
        }
        private void HandleEquipped(Team5.Data.OutfitDefinition outfit) => PlaySe(SeId.Equip);

        private void HandleParameterChanged(ParameterType type, float before, float after)
        {
            if (type != ParameterType.Individuality || Mathf.Abs(after - before) < parameterSeThreshold) return;
            if (Time.unscaledTime - lastParameterSe < 0.4f) return; // 同時に重ならないように
            lastParameterSe = Time.unscaledTime;
            PlaySe(after > before ? SeId.ParameterUp : SeId.ParameterDown);
        }

        // 勝敗が決まった瞬間に結果の曲を鳴らす（演出と合わせる）
        private void HandleEnding(GameResult result)
        {
            endingPlayed = true;
            StopLoops();
            PlayBgm(result == GameResult.Clear ? BgmId.Clear : BgmId.GameOver);
        }
    }
}
