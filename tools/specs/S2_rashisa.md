=== CD-06 ===
## 目的
個性の濃さ・村の平和度・ロールへの進み具合の3つの数値を管理する。ほかのすべての仕組みがこの数値を読み書きする。動作確認用のデバッグパネルもここで作る。

## 作るもの
- `Scripts/Core/PlayerParameters.cs` の実装
- `Scripts/DebugTools/DebugPanel.cs`、`Prefabs/UI/DebugPanel.prefab`（F1 で開閉）

## 仕様
- 初期値・最大値は `GameSceneContext.Instance.Balance` から読む
  | 種類 | 初期値 | 最大値 |
  |---|---|---|
  | Individuality | individualityInitial | individualityMax |
  | Peace | peaceInitial | peaceMax |
  | RoleProgress | 0 | roleProgressMax |
- `Add(type, delta, reason)`：値を 0〜最大値に丸めて更新。値が変わったときだけ `OnChanged(type, old, new)` を呼ぶ。`reason` は `Debug.Log($"[Param] {type} {old}->{new} ({reason})")` に出す
- `GetNormalized(type)`：値 ÷ 最大値
- DebugPanel（**開発用。ビルドでも F1 で開ける**）
  - 3つのパラメーターのスライダー（動かすと Add で反映）
  - 「残り時間を 10 秒にする」ボタン
  - 「見つかる」ボタン（Surveillance の OnCaught と同じ処理を起こす。CD-08c ができるまではボタンだけ）
  - 「全アイテムを1個ずつもらう」ボタン（Role の recipes の材料をすべて Inventory に追加）

## 完了条件
- [ ] Add で値が変わり、0〜最大値からはみ出さない
- [ ] OnChanged が値が変わったときだけ呼ばれる
- [ ] F1 でデバッグパネルが開き、スライダーで値を変えられる

## 確認手順
1. Game シーンを再生し F1 を押す。スライダーを動かして Console にログが出ることを確認

=== CD-07 ===
## 目的
このゲームの核になる見た目。個性が薄いと半透明・灰色・輪郭なし、濃いと色づいて輪郭がはっきりする表現を作る。

## 作るもの
- `Art/Shaders/SG_PlayerIndividuality.shadergraph`（URP Lit ベースの Shader Graph）
- `Art/Materials/MAT_PlayerBody.mat`（上のシェーダーを使う）
- `Art/Shaders/SG_Outline.shadergraph` と `MAT_PlayerOutline.mat`（裏返しのメッシュを少し太らせて描く「背面法」の輪郭）
- `Scripts/Player/PlayerAppearance.cs`

## 仕様
- シェーダーのプロパティ（すべて 0〜1 の `_Individuality` で連動させる）
  | 個性 | 彩度 | 不透明度 | 輪郭の太さ | 色 |
  |---|---|---|---|---|
  | 0 | 0（灰色） | 0.25 | 0 | ベース色 |
  | 0.5 | 0.5 | 0.7 | 0.01 | ベース色 |
  | 1 | 1 | 1 | 0.02 | ベース色 ×（ロールの色を 30% 混ぜる） |
- プロパティ名：`_Individuality`（Float）、`_BaseMap`（Texture）、`_BaseColor`、`_RoleColor`、`_OutlineWidth`、`_OutlineColor`
- Surface Type = Transparent。半透明でも自分の体が透けて見えないよう Depth Write をオン
- 個性が低いとき「霧っぽく」見せるため、ノイズで不透明度を少し揺らす（任意）
- PlayerAppearance
  - `PlayerParameters.OnChanged`（Individuality のとき）を購読
  - 表示値を `Mathf.MoveTowards` で 0.5 秒かけて目標値に近づける（パッと変わらないように）
  - 子の Renderer すべてに `MaterialPropertyBlock` で `_Individuality` と `_RoleColor`（`GameManager.CurrentRole.roleColor`）を設定
  - 衣装を付け替えたとき（CD-12）にも反映されるよう、`public void RefreshRenderers()` を用意
- 仮モデル（カプセル）で動けばよい。DS-04 のモデルが来たら差し替え

## やらないこと
- 変化した瞬間のエフェクト（AN-03）

## 完了条件
- [ ] デバッグパネルで個性を 0 → 100 に動かすと、灰色・半透明 → 色つき・不透明・輪郭ありに、なめらかに変わる
- [ ] ロールの色が個性 100 のときに混ざる

## 確認手順
1. Game シーンを再生し、F1 のスライダーで個性を動かして見た目を確認
2. スクリーンショットを個性 0 / 50 / 100 の 3 枚撮って PR に貼る

=== CD-08 ===
## 目的
NPC と村長がプレイヤーを見張る仕組みの親 Issue。中身は 3 つのサブ Issue（CD-08a / CD-08b / CD-08c）に分けている。

## 進め方
- CD-08a（巡回）と CD-08b（視界）は同時に進められる
- 両方できたら CD-08c（監視マネージャー）
- サブ Issue がすべて閉じたら、この Issue も閉じる

## 完了条件
- [ ] CD-08a・CD-08b・CD-08c がすべて完了している
- [ ] Game シーンで、NPC が巡回し、見られている間は「表」、見られていない間は「裏」に切り替わる

=== CD-08a ===
## 目的
NPC と村長が決まったルートを歩き回るようにする。

## 作るもの
- `Scripts/NPC/NpcPatrol.cs`
- `Prefabs/NPC.prefab`：Capsule（高さ1.7m、色を変えたマテリアル）＋ NavMeshAgent ＋ NpcPatrol ＋ VisionSensor（CD-08b）。Layer/Tag は `NPC`
- `Prefabs/VillageChief.prefab`：NPC.prefab のバリアント（少し大きく、色違い）
- Game シーンに NPC 2 体・村長 1 体を置き、CD-02 の Waypoint を割り当てる

## 仕様
- フィールド：`Transform[] waypoints`、`float waitSeconds = 2f`（各点での待ち時間）、`bool loop = true`、`float moveSpeed = 2f`
- 順番に waypoint へ移動 → 着いたら waitSeconds 待つ → その間に**左右を見回す**（±60度をゆっくり往復。視界の向きが変わるので、プレイヤーに隙ができる）→ 次へ
- 最後まで行ったら loop なら最初へ、loop でなければ逆順
- `public void Pause(float seconds)`（プレイヤーと会話しているときなどに止める）
- NavMeshAgent の速度は moveSpeed

## 完了条件
- [ ] NPC がルートを歩き、各点で止まって見回す
- [ ] 3 体が別々のルートを歩く

## 確認手順
1. Game シーンを再生し、Scene ビューで NPC が巡回するのを確認

=== CD-08b ===
## 目的
NPC がプレイヤーを「見ているか」を判定する。

## 作るもの
- `Scripts/NPC/VisionSensor.cs`（BS-02 の雛形を実装）

## 仕様
- フィールド：`float viewDistance = 8f`、`float viewAngle = 90f`（左右合計）、`float eyeHeight = 1.5f`、`LayerMask obstacleMask`（Obstacle）、`float checkInterval = 0.1f`
- `public bool CanSeePlayer { get; }`：checkInterval ごとに更新
  1. プレイヤーとの距離が viewDistance 以内
  2. NPC の正面からの角度が viewAngle / 2 以内
  3. 目の高さからプレイヤーの胸（高さ 1.0m）へ Raycast し、Obstacle に当たらない
- `public event Action<bool> OnSightChanged;`（見える／見えないが切り替わったとき）
- OnEnable で `GameSceneContext.Instance.Surveillance.Register(this)`、OnDisable で Unregister
- **視界を見えるようにする**：足元に扇形のメッシュ（半透明。見えていないとき黄色、見えているとき赤）を描く。プレイヤーが死角を読めるようにするため、ゲームでも表示する
- Gizmos でも扇形を描く（Scene ビュー用）

## 完了条件
- [ ] プレイヤーが視界に入ると CanSeePlayer が true になり、扇形が赤くなる
- [ ] 建物の陰に隠れると false になる
- [ ] 視界の扇形がゲーム画面で見える

## 確認手順
1. Sandbox シーンに Player、NPC、Cube（Obstacle）を置き、Cube の裏に隠れたり出たりして確認

=== CD-08c ===
## 目的
すべての NPC の視界をまとめて「今見られているか」を決め、表フェーズと裏フェーズを切り替える。見られている間に「らしくない行動」をしたら平和度を下げる。

## 作るもの
- `Scripts/NPC/SurveillanceManager.cs` の実装
- `Scripts/UI/WatchedIndicatorView.cs`（画面上部に目のアイコン：見られていると開いた目、見られていないと閉じた目。仮は文字「見られている！」でよい）

## 仕様
- 登録された VisionSensor のどれか1つでも CanSeePlayer なら `IsPlayerWatched = true`、CurrentPhase = Omote。全員見ていなければ Ura
- 切り替わったときだけ `OnPhaseChanged` を呼ぶ
- `ReportAction(bool suspicious)`
  - suspicious かつ IsPlayerWatched のとき「見つかった」
    - 前回見つかってから `Balance.caughtCooldownSeconds` 経っていなければ何もしない（連続で減りすぎないように）
    - `Parameters.Add(Peace, -Balance.caughtPeacePenalty, "caught")`
    - TimesCaught++、`OnCaught(見ていたセンサー)` を呼ぶ
    - 見ていた NPC を 1.5 秒止めてプレイヤーの方を向かせる（`NpcPatrol.Pause`）
    - true を返す
  - それ以外は false
- 見つかったら画面に「見つかった！ 平和度 -25」と 1.5 秒表示（WatchedIndicatorView で）
- 表/裏の判定ルールの細部（自動切り替えか、プレイヤー操作か）は PL-08 で変わる可能性がある。今は**自動**で作る

## 完了条件
- [ ] NPC の視界に入ると「表」、外れると「裏」になり、表示が切り替わる
- [ ] 見られている間に拾う（suspicious な行動）と平和度が 25 減る
- [ ] 見られていないときに拾っても減らない
- [ ] 連続で拾っても 2 秒に 1 回しか減らない

## 確認手順
1. Game シーンで ItemPickup を NPC の視界の中と外に置き、それぞれ拾って平和度の変化を F1 パネルで確認

=== CD-09 ===
## 目的
NPC から出される「表のお題」の仕組みの親 Issue。2 つのサブ Issue（CD-09a / CD-09b）に分けている。

## 進め方
- CD-09a（進行管理）→ CD-09b（お題をこなす場所とアクション）の順
- サブ Issue がすべて閉じたら、この Issue も閉じる

## 完了条件
- [ ] CD-09a・CD-09b が完了している
- [ ] サンプルロールのお題を受けて、こなすと平和度が上がり個性が下がる

=== CD-09a ===
## 目的
表のお題を順番に出し、成功・失敗を管理する。

## 作るもの
- `Scripts/Tasks/TaskManager.cs`、`TaskInstance.cs` の実装
- 仮データ：`Data/Tasks/Task_Sample_Swing.asset`（ActionAtSpot、spot = `training_ground`、3 秒）、`Task_Sample_Deliver.asset`（BringItem）、`Task_Sample_Talk.asset`（TalkToNpc）を作り、Role_Sample.imposedTasks に入れる
- `Scripts/UI/TaskView.cs`（画面右上に「お題：素振りをする（残り 25 秒）」と進み具合のバーを出す仮UI）

## 仕様
- `Begin(role)`：role.imposedTasks を順番に出す。最初のお題はすぐ、2つ目以降は前のお題が終わってから `Balance.taskIntervalSeconds` 秒後
- 全部出し終わったら最初に戻ってくり返す
- お題を出すとき：`Current = new TaskInstance { State = Active, TimeRemaining = def.timeLimitSeconds }`、`OnTaskAssigned`
- 毎フレーム TimeRemaining を減らし、0 になったら（timeLimitSeconds > 0 のときだけ）失敗：`Add(Peace, -failPeacePenalty)`、`OnTaskFailed`
- 完了したら：`Add(Peace, +peaceReward)`、`Add(Individuality, -individualityCost)`、CompletedCount++、`OnTaskCompleted`
- 進行の報告（CD-09b から呼ばれる）
  - `ReportSpotAction(spotId, seconds)`：Current が ActionAtSpot で targetSpotId が一致したら Progress += seconds / actionSeconds。1 以上で完了
  - `TryDeliver(item)`：BringItem で requiredItem が一致し、Inventory に requiredCount 以上あれば Remove して完了
  - `ReportTalk(npcId)`：TalkToNpc で targetSpotId が npcId と一致したら完了
- **お題の達成判定（PL-05 で決まる）**：報酬の倍率を返す `float EvaluateScore(TaskInstance t)` を用意し、今は常に 1 を返す。報酬 × スコアで加算する

## 完了条件
- [ ] ゲーム開始でお題が表示される
- [ ] 時間内に終わらないと失敗して平和度が下がる
- [ ] 完了すると平和度が上がり個性が下がる
- [ ] 次のお題が間隔をあけて出る

## 確認手順
1. Game シーンを再生し、1つ目のお題を放置して失敗、2つ目をこなして成功、の両方を確認（こなす部分は CD-09b ができるまでデバッグ用に `ReportSpotAction` をキーで呼んでよい）

=== CD-09b ===
## 目的
お題を実際にこなすための「場所」と「動作」を作る。

## 作るもの
- `Scripts/Tasks/TaskSpot.cs`（IInteractable）：ActionAtSpot 用
- `Scripts/Tasks/DeliveryPoint.cs`（IInteractable）：BringItem 用（村長などに渡す）
- `Scripts/NPC/NpcTalkable.cs`（IInteractable）：TalkToNpc 用
- `Scripts/UI/ActionProgressView.cs`（プレイヤーの頭の上に進み具合の円ゲージ）
- Game シーンの `Spot_〇〇` に TaskSpot を付ける

## 仕様
- TaskSpot
  - フィールド：`string spotId`
  - Prompt：お題の対象なら `"[E長押し] 素振りをする"`、そうでなければ空（CanInteract = false）
  - IsSuspicious = **false**（表のお題は見られていてもよい）
  - E を押している間だけ `ReportSpotAction(spotId, Time.deltaTime)` を呼び続ける。その間 `PlayerMover.CanMove = false`。離すと中断（進み具合は残る）
  - CD-03 の Interact は「押した瞬間」なので、長押しは PlayerInteractor に `public bool IsHoldingInteract { get; }` を追加して判定する（PlayerInteractor の変更はこれだけ）
- DeliveryPoint：Prompt `"渡す：{requiredItem.displayName}"`、Interact で `TryDeliver`。IsSuspicious = false
- NpcTalkable：フィールド `string npcId`。Interact で `ReportTalk(npcId)`、NPC を 2 秒止める（`NpcPatrol.Pause(2)`）。IsSuspicious = false
- 動作中はアニメーション用に `public event Action<bool> OnActing` を TaskSpot に用意（AN-01 が使う）

## 完了条件
- [ ] ActionAtSpot：場所で E を長押しするとゲージがたまり、たまるとお題完了
- [ ] BringItem：アイテムを持って渡すと完了し、持ち物から減る
- [ ] TalkToNpc：対象の NPC に話しかけると完了
- [ ] 対象でない場所では何も起きない

## 確認手順
1. Role_Sample の 3 種類のお題を順番にこなす

=== CD-10 ===
## 目的
裏フェーズでマップから素材を集め、持ち物として持っておけるようにする。

## 作るもの
- `Scripts/Items/Inventory.cs` の実装
- `Scripts/UI/InventoryView.cs`、`Prefabs/UI/InventoryPanel.prefab`（Tab で開閉。アイコンと個数の一覧）
- 仮データ：`Data/Items/Item_Sample_A.asset` 〜 `Item_Sample_C.asset`（素材3種）
- Game シーンの各所に ItemPickup（CD-03）を 6 個置く

## 仕様
- 中身は `Dictionary<ItemDefinition, int>`。0 個になったら消す
- `Add` / `Remove` のたびに `OnChanged`。Remove は足りなければ何もせず false
- `Has(IEnumerable<ItemAmount>)`：すべて足りていれば true
- InventoryView：開いている間はゲームを止めない（一時停止しない）。アイコンがなければ displayName の文字を出す
- 持ち物の数に上限はない

## 完了条件
- [ ] 拾うと持ち物に入り、Tab で一覧が見える
- [ ] 同じアイテムは個数がまとまる
- [ ] Remove で減り、0 で消える

## 確認手順
1. Game シーンで素材を拾い、Tab で一覧を確認

=== CD-11 ===
## 目的
集めた素材からレシピに沿って物を作る。作るとなりたいロールへの進み具合と個性が上がる。裏フェーズの中心となる行動。

## 作るもの
- `Scripts/Items/CraftingSystem.cs` の実装
- `Scripts/Items/CraftStation.cs`（IInteractable。作業台）
- `Scripts/UI/CraftView.cs`、`Prefabs/UI/CraftPanel.prefab`（作れるレシピの一覧。材料が足りないものは灰色）
- 仮データ：`Data/Recipes/Recipe_Sample.asset`（Item_Sample_A ×1 + B ×1 → Item_Sample_Goal）、`Item_Sample_Goal.asset`（category = Goal）

## 仕様
- CraftStation：Prompt `"作る"`、IsSuspicious = **true**、Interact で CraftView を開く（対象は `CurrentRole.recipes`）
- `StartCraft(recipe)`
  - CanCraft でなければ何もしない
  - IsCrafting = true、`PlayerMover.CanMove = false`
  - craftSeconds かけて進め、毎フレーム `OnCraftProgress(recipe, 0〜1)`
  - **作っている間は 0.5 秒ごとに `Surveillance.ReportAction(true)`** を呼ぶ。見つかったら（true が返ったら）中断：材料は減らさない、`OnCraftInterrupted`
  - 完了したら材料を Remove、consumeOnCraft でなければ output を Add、`Add(RoleProgress, +roleProgressReward)`、`Add(Individuality, +individualityReward)`、`OnCrafted`
  - 終わったら CanMove = true
- `Cancel()`：途中でやめる（材料は減らさない）。Esc か移動キーで呼ぶ

## 完了条件
- [ ] 作業台で E → レシピ一覧が開く
- [ ] 材料が足りないレシピは作れない
- [ ] 作ると進み具合と個性が上がり、材料が減る
- [ ] 作っている途中に見つかると中断し、材料は減らない

## 確認手順
1. F1 の「全アイテムを1個ずつもらう」→ 作業台で作る → パラメーターの変化を確認
2. NPC の視界の中で作り、中断されることを確認

=== CD-12 ===
## 目的
手に入れた服を選んで着替え、見た目を変える。服装を変えていくことが「自分らしくなっていく」ことの一番わかりやすい表現になる。

## 作るもの
- `Scripts/Items/OutfitController.cs`（プレイヤーに付ける）
- `Scripts/UI/OutfitView.cs`（InventoryPanel に「着替え」タブを追加）
- 仮データ：`Data/Outfits/Outfit_Sample_Stage1.asset`、`Outfit_Sample_Stage2.asset`

## 仕様
- プレイヤーの子に付け位置の空オブジェクト `Attach_Body` `Attach_Head` `Attach_Hand` を作る（DS-04 のモデルではボーンの子にする）
- `Equip(OutfitDefinition o)`：同じ attachPoint の服を外して、o.prefab を生成して付ける。`PlayerAppearance.RefreshRenderers()` を呼ぶ
- 初めて着た服なら `Add(Individuality, +o.individualityReward)`
- 着替えは**見られているとまずい**：着替え開始時に `ReportAction(true)`。見つかったら着替えない
- 持ち物に `category = Outfit` のアイテムがあり、それに対応する OutfitDefinition（`CurrentRole.outfits` から item で探す）があれば、着替えタブに出る
- 今着ている服の stage の最大値を `public int CurrentStage` で公開（結果画面などで使う）

## 完了条件
- [ ] 服を拾って着替えタブから着ると、見た目が変わる
- [ ] 初めて着たとき個性が上がる
- [ ] 見られている間は着替えられない

## 確認手順
1. Outfit_Sample の服を ItemPickup で置き、拾って着替える

=== CD-18 ===
## 目的
ロールは複数ある想定。MVP の仕組みを使って、2 つ目以降のロールを遊べるようにする。

## 作るもの
- PL-01〜PL-03 で決まった追加ロールのデータ一式（`Role_〇〇.asset`、`Task_〇〇.asset`、`Item_〇〇.asset`、`Recipe_〇〇.asset`、`Outfit_〇〇.asset`）
- 必要なら Game シーンに素材・お題の場所を追加

## 仕様
- **コードはできるだけ変えない**。データを足すだけで遊べるのが目標（変える必要が出たら、どこをなぜ変えたかを PR に書く）
- 複数ロールの素材が同じマップに置かれていてよい（会議で出た「マップには複数ロールのアイテムが転がっていて、自分のロールのものを探す」案）
- 自分のロールに関係ない素材を拾ってもペナルティはない

## 完了条件
- [ ] 2 つ以上のロールで、それぞれクリアまで遊べる
- [ ] ロールを変えるとお題・レシピ・衣装が変わる

## 確認手順
1. GameManager の defaultRole を差し替えて（または CD-20 の選択画面で）各ロールを最後まで遊ぶ

=== CD-21 ===
## 目的
押し付けられた「らしさ」の一つとして会議で出た「決められた道しか通ってはいけない」ルールを入れる。見られている間に道から外れると「らしくない行動」になる。

## 作るもの
- `Scripts/NPC/ForbiddenZone.cs`（Trigger の BoxCollider に付ける）
- Game シーンの道の外側（畑・森など）に ForbiddenZone を配置

## 仕様
- プレイヤーが ForbiddenZone の中にいる間、0.5 秒ごとに `Surveillance.ReportAction(true)` を呼ぶ
- 中に入ったら画面に「道から外れている」と小さく表示（見られていたら赤、見られていなければ灰色）
- ゾーンの範囲は Scene ビューで半透明の赤で見えるように Gizmos を描く。ゲーム中は見えない
- どこを禁止にするかは PL-08 の決定に合わせる

## 完了条件
- [ ] 見られている間に禁止ゾーンへ入ると平和度が下がる
- [ ] 見られていなければ入っても下がらない

## 確認手順
1. NPC の視界の中で道から外れ、平和度が下がることを確認
