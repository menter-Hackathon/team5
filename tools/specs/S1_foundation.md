=== BS-01 ===
## 目的
全員が同じUnityプロジェクトを開ける状態にする。チーム開発で一番トラブルが起きやすいところなので最初にやる。

## 作るもの
- リポジトリ直下に Unity プロジェクト（`Assets/` `Packages/` `ProjectSettings/` がリポジトリ直下に来る）
- `Assets/_Project/` 以下のフォルダ一式（docs/architecture.md の「2. フォルダとファイル名」のとおり。空フォルダは `.gitkeep` を置く）
- シーン `Assets/_Project/Scenes/Title.unity` `Game.unity` `Result.unity`（中身は空でよい）と、各メンバー用 `Scenes/Sandbox/Sandbox_<名前>.unity`
- README.md の「開発環境」に Unity のバージョン（例：6000.0.XXf1）を追記

## 手順
1. Unity Hub で **Unity 6 LTS（6000.0 系）の最新パッチ**を入れる。決めたバージョンを Discord で全員に共有
2. Unity Hub →「New project」→ テンプレート **Universal 3D** → 場所は一時フォルダで作成
3. 作ったプロジェクトの `Assets` `Packages` `ProjectSettings` を、clone したリポジトリ直下にコピー（`Library` などはコピーしない）
4. Unity でリポジトリのフォルダを開き直す
5. Edit > Project Settings > Editor で次を確認
   - Version Control → Mode：**Visible Meta Files**
   - Asset Serialization → Mode：**Force Text**
6. Window > Package Manager で次を入れる：**Input System**、**Cinemachine**（3.x）、**AI Navigation**
   - Input System を入れると「新しい入力に切り替えるか」と聞かれるので Yes（Active Input Handling = Input System Package (New)）
7. Tags and Layers に Layer `Player` `NPC` `Interactable` `Obstacle`、Tag `NPC` を追加
8. File > Build Profiles（Build Settings）のシーン一覧に Title(0) / Game(1) / Result(2) を登録
9. テンプレートに入っているサンプル（`Assets/Settings` 以外の TutorialInfo やサンプルシーン）は削除してよい
10. `git status` で `Library/` `Temp/` `Logs/` `UserSettings/` が出てこないことを確認してからコミット

## やらないこと
- スクリプトを書くこと（BS-02 でやる）

## 完了条件
- [ ] 別の人が clone して、同じバージョンの Unity で開いたときにエラーが出ない
- [ ] Title / Game / Result シーンが Build Profiles に登録されている
- [ ] 3つのパッケージが入っている
- [ ] Library などの不要なファイルがコミットされていない
- [ ] README に Unity のバージョンが書いてある

## 確認手順
1. 別のフォルダに `git clone` し直して Unity Hub の「Add」から開く
2. Console にエラーが出ていないこと、Game シーンを再生できることを確認

=== BS-02 ===
## 目的
docs/architecture.md の「4. 共通の型」を、中身が空の状態でまとめて作る。これがあると、各担当がお互いの完成を待たずに同時に作業できる。

## 作るもの
docs/architecture.md の 4.1〜4.5 に出てくるすべての型を、次のファイルに作る（1ファイル1型、名前空間は `Team5.〇〇`）。
- `Scripts/Core/`：`GameState.cs` `GameResult.cs` `ParameterType.cs` `Phase.cs` `SceneNames.cs` `GameResultSummary.cs` `GameManager.cs` `GameSceneContext.cs` `GameTimer.cs` `PlayerParameters.cs` `GameEndJudge.cs`
- `Scripts/Data/`：`GameBalance.cs` `RoleDefinition.cs` `TaskType.cs` `TaskDefinition.cs` `ItemCategory.cs` `ItemDefinition.cs` `ItemAmount.cs` `RecipeDefinition.cs` `OutfitDefinition.cs`
- `Scripts/Player/`：`IInteractable.cs` `PlayerInteractor.cs`（クラスだけ）
- `Scripts/NPC/`：`SurveillanceManager.cs` `VisionSensor.cs`（クラスだけ）
- `Scripts/Tasks/`：`TaskState.cs` `TaskInstance.cs` `TaskManager.cs`
- `Scripts/Items/`：`Inventory.cs` `CraftingSystem.cs`
- Assembly Definition は**作らない**（全部 Assembly-CSharp でよい）
- 仮データ：`Data/GameBalance.asset`、`Data/Roles/Role_Sample.asset`（displayName「サンプル職人」、imposedRoleName「勇者」）
- `Game.unity` に空の GameObject「GameSystems」を置き、`GameSceneContext` `GameTimer` `PlayerParameters` `GameEndJudge` `SurveillanceManager` `TaskManager` `Inventory` `CraftingSystem` を付け、`GameSceneContext` の各欄に割り当てる
- `Title.unity` に GameObject「GameManager」を置き `GameManager` を付ける

## 仕様
- プロパティ・イベント・メソッドの**名前と引数は architecture.md と完全に同じ**にする
- メソッドの中身は空、または `Debug.Log("TODO: 〇〇")` だけ。戻り値があるものは `default` を返す
- `GameManager.Instance` と `GameSceneContext.Instance` はシングルトンとして**実装まで済ませる**（Awake で Instance を設定。GameManager は重複していたら Destroy、DontDestroyOnLoad）
- ScriptableObject には `[CreateAssetMenu(menuName = "Team5/〇〇")]` を付け、右クリック > Create > Team5 から作れるようにする

## やらないこと
- 各クラスの中身の実装（それぞれのIssueでやる）

## 完了条件
- [ ] コンパイルエラーがない
- [ ] architecture.md の型がすべて存在し、名前と引数が一致している
- [ ] Create > Team5 から GameBalance / Role / Task / Item / Recipe / Outfit が作れる
- [ ] Game シーンの GameSystems に全コンポーネントが付き、GameSceneContext の参照が埋まっている

## 確認手順
1. Unity の Console にエラーがないことを確認
2. Project ウィンドウで右クリック > Create > Team5 にメニューが6つ出ることを確認
3. Game シーンを再生してエラーが出ないことを確認

=== CD-01 ===
## 目的
プレイヤーが村を歩き回れるようにする。見た目は仮のカプセルでよい。

## 作るもの
- `Assets/_Project/Input/PlayerControls.inputactions`（Generate C# Class をオン、クラス名 `PlayerControls`、名前空間 `Team5.Input`）
  - Action Map `Player`：`Move`（Value / Vector2：WASD・矢印キー・左スティック）、`Interact`（Button：E・ゲームパッド South）、`Inventory`（Button：Tab・ゲームパッド North）、`Pause`（Button：Esc・Start）
- `Scripts/Player/PlayerInputReader.cs`：PlayerControls を持ち、`Vector2 Move`、`event Action OnInteract`、`event Action OnInventory`、`event Action OnPause` を公開
- `Scripts/Player/PlayerMover.cs`：`CharacterController` で移動
- `Prefabs/Player.prefab`：Capsule（高さ1.6m）＋ CharacterController ＋ PlayerInputReader ＋ PlayerMover。Layer/Tag は `Player`
- `Prefabs/CameraRig.prefab`：Cinemachine Camera（Follow = Player）

## 仕様
- 移動：入力方向へ `moveSpeed`（インスペクターで設定、初期 4.0 m/s）で動く。**カメラの向き基準ではなくワールドの XZ 平面**で動く（カメラは回らないので、W = 画面の上方向）
- 向き：移動している方向へ `turnSpeed`（初期 720 度/秒）で回転
- 重力をかける（地面から落ちない）。**ジャンプはしない**
- `public float SpeedMultiplier { get; set; } = 1f;` を用意し、移動速度にかける（個性が減ったときのデバフで CD-13 が使う）
- `public bool CanMove { get; set; } = true;` が false のときは動かない（クラフト中・会話中に使う）
- カメラ：**見下ろしで角度固定**。プレイヤーの斜め上（目安：高さ 12m、後ろ 8m、角度 約55度）から追いかける。回転しない。Cinemachine の `CinemachineFollow`（Binding Mode = World Space）を使う
- `GameManager.State` が `Paused` / `Result` のときは入力を無視する

## やらないこと
- アニメーション（AN-01）、モデルの差し替え（DS-04）
- インタラクト処理（CD-03）

## 完了条件
- [ ] WASD とゲームパッドの左スティックで移動できる
- [ ] 移動方向を向く
- [ ] カメラが一定の角度でついてくる
- [ ] SpeedMultiplier を 0.5 にすると半分の速さになる
- [ ] CanMove = false で止まる

## 確認手順
1. 自分の Sandbox シーンに Plane（10倍の大きさ）、Player.prefab、CameraRig.prefab を置く
2. 再生して上の完了条件をすべて試す（SpeedMultiplier と CanMove はインスペクターから変える）

=== CD-02 ===
## 目的
本番のモデルができる前に、箱と板で村のレイアウトを作る。全員の動作確認の場所になる。

## 作るもの
- `Scenes/Game.unity` に村のブロックアウト（Unity 標準の Cube / Plane だけで作る）
- 親 GameObject「Level」の下に、`Ground`、`Buildings`、`Roads`、`Spots`（お題やクラフトの場所の目印）、`NpcWaypoints` をまとめる
- `NavMeshSurface` を「Level」に付けてベイクする（NPC の巡回用）

## 仕様
- 広さ：**40m × 40m** 程度（1プレイ数分で回れる大きさ）
- 置くもの（DS-03 の配置図ができたら合わせる。それまでは仮の配置でよい）
  - 広場（中央）、村長の家、食堂、作業台のある場所、畑、森（端の方。死角になりやすい）
  - 道（Roads）：幅 2m の板。のちに「決められた道」ルール（CD-21）で使う
  - 建物は Layer `Obstacle`（NPC の視線をさえぎる）
- 色分け：地面＝緑、道＝ベージュ、建物＝グレー、目印＝黄色（仮のマテリアルを `Art/Materials/MAT_Blockout_〇〇.mat` で作る）
- `Spots` の下に空の GameObject を置き、名前を `Spot_<ID>` にする（例：`Spot_training_ground`、`Spot_kitchen`）。ID は PL-02 で決まったら合わせる
- `NpcWaypoints` の下に巡回ポイントを 3 ルート分（各 4〜6 点）置く。名前は `Route1_0`, `Route1_1` …

## やらないこと
- 本番のアセット（DS-07）

## 完了条件
- [ ] Game シーンに 40m 四方の村があり、プレイヤーが歩ける
- [ ] 建物が Obstacle レイヤーになっている
- [ ] NavMesh がベイクされている（Scene ビューで青く表示される）
- [ ] Spot と Waypoint の目印が置いてある

## 確認手順
1. Game シーンに Player.prefab を置いて再生し、村を一周できることを確認

=== CD-03 ===
## 目的
近くの物や NPC に E キーで行動できる仕組みを作る。拾う・お題・クラフト・会話のすべての土台になる。

## 作るもの
- `Scripts/Player/PlayerInteractor.cs`（BS-02 の雛形を実装）
- `Scripts/Items/ItemPickup.cs`（IInteractable の実装例。拾う）
- `Scripts/UI/InteractPromptView.cs` と `Prefabs/UI/InteractPrompt.prefab`（画面下に「[E] 拾う：鶏ガラ」と出す仮UI）

## 仕様
- PlayerInteractor
  - 毎フレーム、プレイヤーの前方 `range`（初期 1.5m）の球の中にある Layer `Interactable` のコライダーを探す（`Physics.OverlapSphere`）
  - 見つかった中で `CanInteract` が true のもののうち、一番近いものを「注目中」にする
  - `public IInteractable Current { get; }`、`public event Action<IInteractable> OnFocusChanged;`（null も通知する）
  - `PlayerInputReader.OnInteract` で `Current.Interact(this)` を呼ぶ
  - Interact を呼ぶ直前に `GameSceneContext.Instance.Surveillance.ReportAction(Current.IsSuspicious)` を呼ぶ
  - `public PlayerMover Mover { get; }` を公開（ほかの処理が CanMove を止められるように）
- ItemPickup
  - フィールド：`ItemDefinition item`、`int count = 1`、`bool suspicious = true`
  - Prompt：`$"拾う：{item.displayName}"`、IsSuspicious：`suspicious`
  - Interact：`GameSceneContext.Instance.Inventory.Add(item, count)` して自分を Destroy
- InteractPromptView：OnFocusChanged を購読して文字を出す／消す

## やらないこと
- インベントリの中身の実装（CD-10）。ここでは Inventory.Add を呼ぶだけ

## 完了条件
- [ ] Interactable レイヤーの物に近づくと「[E] 〇〇」と表示される
- [ ] 離れると消える
- [ ] E を押すと Interact が呼ばれ、ItemPickup が消える
- [ ] 複数あるときは一番近いものが選ばれる

## 確認手順
1. Sandbox シーンに Player と、ItemPickup を付けた Cube（Layer = Interactable、Collider 付き）を 2 つ置く
2. 近づいて表示を確認し、E で拾う。Console に TODO ログ（Inventory.Add）が出ればよい

=== CD-04 ===
## 目的
タイトル → 本編 → 結果 → タイトルの流れを作る。

## 作るもの
- `Scripts/Core/GameManager.cs` の実装
- `Scripts/UI/TitleView.cs`、`Scripts/UI/ResultView.cs`（仮UI）
- Title シーン：「はじめる」ボタン
- Result シーン：結果の文字と「タイトルへ」ボタン
- Game シーンに仮の「ゲームを終える（デバッグ）」ボタンは**作らない**（CD-06 のデバッグパネルで代用）

## 仕様
- `StartGame(RoleDefinition role)`：CurrentRole を保存 → `SceneManager.LoadScene(SceneNames.Game)` → 読み込み後 State = Playing
  - MVP ではタイトルの「はじめる」で `defaultRole`（インスペクターで設定、Role_Sample）を渡す。ロール選択画面は CD-20
- Game シーンの読み込みが終わったら、`GameSceneContext` が `Timer.StartTimer(Balance.timeLimitSeconds)` と `Tasks.Begin(role)` を呼ぶ（GameSceneContext の Start に書く）
- `EndGame(result, summary)`：LastResult と LastSummary を保存 → State = Result → Result シーンを読み込む
- `ReturnToTitle()`：State = Title → Title シーンを読み込む
- `SetPaused(bool)`：State を Paused / Playing にし、`Time.timeScale` を 0 / 1 にする。Esc（PlayerInputReader.OnPause）で切り替え
- State が変わるたびに `OnStateChanged` を呼ぶ
- Game シーンを**直接再生したとき**（GameManager がいないとき）も動くように：GameSceneContext は GameManager.Instance が null なら Role_Sample で自分で開始する
- ResultView：LastResult に応じて「クリア！」「平和度が0になった…」「時間切れ…」「色が消えてしまった…」を表示

## やらないこと
- 見た目の作り込み（CD-15）

## 完了条件
- [ ] タイトル →「はじめる」→ 本編 → EndGame で結果 →「タイトルへ」で一周できる
- [ ] Esc で一時停止・再開できる（Time.timeScale が変わる）
- [ ] Game シーンを直接再生しても動く

## 確認手順
1. Title シーンから再生して一周する（EndGame はデバッグパネルか一時的なキー操作で呼ぶ）
2. Game シーンを開いて直接再生し、エラーが出ないことを確認

=== CD-05 ===
## 目的
制限時間を管理し、時間切れを知らせる。

## 作るもの
- `Scripts/Core/GameTimer.cs` の実装

## 仕様
- `StartTimer(seconds)`：Remaining = seconds、Elapsed = 0、IsRunning = true
- `Update` で `Time.deltaTime` だけ減らす（一時停止中は timeScale = 0 なので自動で止まる）
- 毎フレーム `OnTick(Remaining)` を呼ぶ
- 0 になったら Remaining = 0、IsRunning = false、`OnTimeUp` を**1回だけ**呼ぶ
- `Stop()`：IsRunning = false（ゲーム終了時に呼ばれる）
- 秒数は `GameBalance.timeLimitSeconds` から渡される。**コードに 300 などの数字を書かない**

## 完了条件
- [ ] StartTimer(10) で 10 秒後に OnTimeUp が 1 回だけ呼ばれる
- [ ] 一時停止中は減らない
- [ ] Stop() 後は減らない

## 確認手順
1. GameBalance.asset の timeLimitSeconds を 10 にして Game シーンを再生
2. OnTimeUp で Debug.Log を出す一時コードか、CD-13 の終了判定で 10 秒後に終わることを確認

=== CD-13 ===
## 目的
勝ち負けを判定してゲームを終わらせる。ここまでできればプロトタイプ完成。個性が減りすぎたときのデバフもここで入れる。

## 作るもの
- `Scripts/Core/GameEndJudge.cs` の実装

## 仕様
判定は次の順で行い、どれか1つ成立したら `GameManager.EndGame(result, summary)` を呼んで以降は判定しない。
| 優先 | 条件 | GameResult |
|---|---|---|
| 1 | RoleProgress が `Balance.roleProgressToWin` 以上 | Clear |
| 2 | Peace が 0 以下 | GameOverPeace |
| 3 | Individuality が 0 以下 | GameOverFaded |
| 4 | `GameTimer.OnTimeUp` | GameOverTimeUp |

- `PlayerParameters.OnChanged` と `GameTimer.OnTimeUp` を購読して判定する（毎フレームのポーリングはしない）
- 終了時に `Timer.Stop()` を呼ぶ
- summary：終了時のパラメーター3つ、`Timer.Elapsed`、`Tasks.CompletedCount`、`Surveillance.TimesCaught` を詰める
- **デバフ**：Individuality が `Balance.debuffThreshold` 未満の間、プレイヤーの `PlayerMover.SpeedMultiplier = Balance.debuffSpeedMultiplier`。以上に戻ったら 1
- 勝利条件の細部（例：最終アイテムの完成を必須にする）は PL-06 で変わる可能性がある。変わったらこの表を更新する

## 完了条件
- [ ] デバッグパネルで RoleProgress を 100 にするとクリアになる
- [ ] Peace を 0 にすると GameOverPeace
- [ ] Individuality を 0 にすると GameOverFaded
- [ ] 時間切れで GameOverTimeUp
- [ ] Individuality が 20 未満で移動が遅くなる
- [ ] 結果画面に正しい結果が出る

## 確認手順
1. Title から開始し、CD-06 のデバッグパネルで各パラメーターを動かして4種類の終わり方をすべて確認

=== CD-16 ===
## 目的
初めて遊ぶ人（審査員を含む）が、ゲーム内の説明だけで遊び方がわかるようにする。

## 作るもの
- `Scripts/UI/TutorialController.cs`、`Prefabs/UI/TutorialPanel.prefab`
- `Data/Tutorial/TutorialSteps.asset`（`TutorialStepList : ScriptableObject`：各ステップに `string message`、`TutorialTrigger trigger`）
- `Scripts/Core/GameSceneContext.cs` の変更：チュートリアル中はタイマーとお題を始めず、チュートリアルが終わってから `Timer.StartTimer` と `Tasks.Begin` を呼ぶ

## 仕様
- ゲーム開始直後に表示。チュートリアル中はタイマーとお題を止めておき、最後のステップ（またはスキップ）の後に始める。NPC は巡回してよい
- `TutorialTrigger`：`PressAnyKey`（どれかのキーで次へ）、`Moved`（3m 歩いたら次へ）、`Interacted`（何かとインタラクトしたら次へ）、`OpenedInventory`
- 文章は PL-10 の台本どおりに TutorialSteps.asset に入れる（コードに文章を書かない）
- 「スキップ」ボタン（Tab 長押しでも可）で全部飛ばせる
- 2回目以降は `PlayerPrefs` に記録して自動でスキップ（タイトルに「チュートリアルをもう一度見る」チェックを置く）

## 完了条件
- [ ] 初回はチュートリアルが出て、操作すると次に進む
- [ ] スキップできる
- [ ] 2回目は出ない

## 確認手順
1. PlayerPrefs を消して（Edit > Clear All PlayerPrefs）Title から開始し、最後まで進める

=== CD-17 ===
## 目的
BGM と SE を鳴らす。

## 作るもの
- `Scripts/Audio/AudioManager.cs`（Title シーンに置き DontDestroyOnLoad）
- `Data/Audio/AudioLibrary.asset`（`AudioLibrary : ScriptableObject`：BGM と SE の `AudioClip` を名前付きで持つ）

## 仕様
- `AudioManager.Instance.PlayBgm(BgmId id)`、`PlaySe(SeId id)`
- `enum BgmId { Title, Omote, Ura, Clear, GameOver }`、`enum SeId { Pickup, Craft, Crafted, Caught, TaskComplete, TaskFail, Equip, Button, ParameterUp, ParameterDown }`
- BGM：GameManager.OnStateChanged と SurveillanceManager.OnPhaseChanged に合わせて切り替え。表 ⇔ 裏は **0.5 秒でクロスフェード**
- SE：各イベントを購読して鳴らす（Inventory.OnChanged で Pickup、CraftingSystem.OnCrafted で Crafted、Surveillance.OnCaught で Caught、TaskManager.OnTaskCompleted / OnTaskFailed など）
- 音量：BGM 0.5、SE 0.8 を初期値にしてインスペクターで調整できるようにする

## 完了条件
- [ ] タイトル・表・裏・結果で BGM が変わる
- [ ] 表 ⇔ 裏の切り替えでなめらかに変わる
- [ ] 主要な行動で SE が鳴る

## 確認手順
1. Title から通しで遊び、すべての BGM と SE が鳴ることを確認

=== CD-19 ===
## 目的
発表で使うビルドを作り、最後まで遊べることを確認する。11/20 23:59 でコードフリーズ。

## 手順
1. 11/20 の夕方までに、マージされていない PR を確認し、入れるもの・入れないものを決める
2. Build Profiles で **Windows と macOS** 向けにビルド（発表で使う PC に合わせる）
   - Player Settings：Company Name「team5」、Product Name（DS-02 で決めたタイトル）、解像度 1920×1080、フルスクリーン
3. 発表で使う PC でビルドを起動し、タイトルからクリア・ゲームオーバーまで通しで遊ぶ
4. 見つかった**致命的なバグだけ**直す（新機能は入れない）
5. 最終コミットにタグ `v1.0-freeze` を付けて push
6. ビルドを zip にして GitHub の Releases に置く

## 完了条件
- [ ] 発表用 PC でビルドが起動し、クリアまで遊べる
- [ ] タグ `v1.0-freeze` が付いている
- [ ] Releases にビルドの zip がある

=== PR-04 ===
## 目的
11/20（金）23:59 までに提出する。審査員が README を見るだけでゲームの内容と遊び方がわかる状態にする。

## 作るもの
- README.md の書き直し：
  - タイトルとロゴ、1行の説明
  - スクリーンショット 3 枚（タイトル、表フェーズ、ロールに変化した瞬間）
  - 遊び方（操作方法の表、勝利・敗北条件）
  - ダウンロード（Releases へのリンク）
  - 使った技術（Unity 6 / URP / Cinemachine / AI Navigation、工夫した点は PR-03 から抜粋）
  - チームメンバーと担当
  - クレジット（docs/credits.md へのリンク）
- 提出フォームの送信

## 完了条件
- [ ] README に上の項目がすべてある
- [ ] docs/credits.md に使った素材がすべて載っている
- [ ] 提出フォームを送信した（スクショを Discord に貼る）
