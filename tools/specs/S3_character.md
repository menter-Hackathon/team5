=== DS-04 ===
## 目的
初期状態のプレイヤー（霧のようにぼやけた姿、または村人の制服）を作る。色や輪郭が変わる表現（CD-07）が乗る前提で作る。

## 作るもの
- `Art/Characters/CHR_Player.fbx`（モデル＋リグ）
- `Art/Characters/Textures/CHR_Player_Base.png`
- `Prefabs/Player.prefab` のカプセルを差し替えたもの（PR で差し替えまでやる。コード担当に頼んでもよい）

## 仕様
| 項目 | 決めごと |
|---|---|
| 作り方 | Blender で作る / VRoid から出力 / 無料素材（要ライセンス確認）のどれでもよい |
| 画風 | DS-01 のトンマナに合わせる（カジュアルなデフォルメ。頭身 2.5〜3 程度） |
| 大きさ | 身長 **1.6m**（Unity の 1 = 1m）。足元が原点 |
| 向き | Z+ が正面 |
| ポリゴン | 10,000 三角形以下 |
| テクスチャ | 1024×1024 の PNG 1 枚。**色は薄い単色ベース**（CD-07 のシェーダーで彩度を上げ下げするため、元の色がそのまま「個性 100 の色」になる） |
| リグ | **Humanoid** で Unity に取り込める骨格（Mixamo の自動リグでもよい） |
| 書き出し | FBX、Scale 1、Apply Transform オン |
| 付け位置 | 頭・胴・右手に衣装を付ける位置がわかるようにする（ボーン名を PR に書く） |

- Unity での取り込み設定：Rig = Humanoid、Material は CD-07 の `MAT_PlayerBody` に差し替え

## やらないこと
- アニメーション（AN-01）、衣装（DS-06）

## 完了条件
- [ ] FBX が Unity に取り込まれ、Humanoid のアバター設定でエラーがない
- [ ] Player.prefab のカプセルと差し替えて、歩かせても問題ない
- [ ] CD-07 のシェーダーで個性 0 / 100 の見た目が変わる

## 確認手順
1. Game シーンで再生し、F1 パネルで個性を動かしてモデルの見た目が変わることを確認
2. スクリーンショットを PR に貼る

=== DS-05 ===
## 目的
「らしさ」を押し付けてくる村人たちと村長を作る。

## 作るもの
- `Art/Characters/CHR_Villager.fbx`（村人。色違いで 2〜3 人に見せられるとよい）
- `Art/Characters/CHR_Chief.fbx`（村長。ひと目で偉い人とわかる見た目）
- `Prefabs/NPC.prefab` と `VillageChief.prefab` のカプセルを差し替え

## 仕様
- 大きさ・向き・リグ・書き出しは DS-04 と同じルール
- 村人は**全員同じ制服**（押し付けられた「らしさ」の象徴）。村長だけ違う服
- テクスチャ 1024×1024 以下、ポリゴン 8,000 三角形以下

## 完了条件
- [ ] NPC と村長のモデルがそれぞれの Prefab に入っている
- [ ] 巡回（CD-08a）させても問題ない

## 確認手順
1. Game シーンを再生し、NPC が新しいモデルで巡回していることを確認

=== DS-06 ===
## 目的
なりたいロールに近づくにつれて着られる服・装備を作る。

## 作るもの
- MVP のロールについて、PL-07 で決めた段階ごとの衣装
  - `Art/Characters/Outfits/OUT_<ロール>_<部位>_<段階>.fbx`（例：`OUT_RamenChef_Body_2.fbx`）
  - 衣装の Prefab：`Prefabs/Outfits/Outfit_<ロール>_<部位>_<段階>.prefab`
- 対応する `Data/Outfits/Outfit_〇〇.asset` の prefab 欄に割り当てる

## 仕様
- 衣装はプレイヤーとは**別のモデル**にして、付け位置（Attach_Body / Attach_Head / Attach_Hand）に付けたときにぴったり合うように作る
- 体に沿う服（エプロンなど）は、プレイヤーと同じボーンにスキニングする方法と、固い小物（帽子・道具）として付ける方法がある。難しければ**帽子・道具・エプロンのような小物中心**でよい
- 段階 2（ロールの完成形）は、ひと目で「〇〇職人だ」とわかるものにする
- マテリアルは CD-07 の `MAT_PlayerBody` を使う（個性の表現を揃えるため）

## 完了条件
- [ ] MVP ロールの衣装がすべての段階でそろっている
- [ ] CD-12 の着替えで付けたとき、位置がずれない

## 確認手順
1. F1 で衣装を持ち物に入れ、着替えタブから着てスクリーンショットを PR に貼る

=== AN-01 ===
## 目的
プレイヤーに動きをつける。

## 作るもの
- `Animations/Player/ANM_Player_Idle.anim` `_Walk` `_Pickup` `_Action`（お題の動作：例 素振り）`_Craft`（料理・作業）`_Caught`（見つかってびくっとする）
- `Animations/Player/Player.controller`（Animator Controller）
- `Scripts/Player/PlayerAnimator.cs`（コード担当と一緒に作るか、頼んでもよい）

## 仕様
- Mixamo などの無料モーションを使ってよい（Humanoid でリターゲット）。ライセンスを docs/credits.md に書く
- Animator のパラメーター（名前を変えないこと）
  | 名前 | 型 | 入れる側 |
  |---|---|---|
  | `Speed` | Float | PlayerMover の移動速度（0〜1 に正規化） |
  | `Pickup` | Trigger | ItemPickup を拾ったとき |
  | `Acting` | Bool | TaskSpot.OnActing |
  | `Crafting` | Bool | CraftingSystem.IsCrafting |
  | `Caught` | Trigger | Surveillance.OnCaught |
- Idle ⇔ Walk は Speed で Blend Tree
- PlayerAnimator：上の表のイベントを購読して Animator に値を入れるだけのスクリプト

## 完了条件
- [ ] 歩く・止まる・拾う・お題・作る・見つかる で動きが変わる

## 確認手順
1. Game シーンで一通りの行動をして、動きを録画（GIF でよい）して PR に貼る

=== AN-02 ===
## 目的
NPC と村長に動きをつける。

## 作るもの
- `Animations/NPC/ANM_NPC_Idle.anim` `_Walk` `_LookAround`（見回し）`_Notice`（気づいて指をさす・怒る）
- `Animations/NPC/NPC.controller`、`Scripts/NPC/NpcAnimator.cs`

## 仕様
- パラメーター：`Speed`（NavMeshAgent の速度）、`LookAround`（Bool：待機中の見回し）、`Notice`（Trigger：OnCaught で自分が見ていたとき）
- AN-01 と同じく無料モーション可

## 完了条件
- [ ] NPC が歩く・見回す・気づく で動きが変わる

## 確認手順
1. Game シーンで NPC の前で素材を拾い、気づく動きを確認

=== AN-03 ===
## 目的
このゲーム一番の見せ場。色が薄くなる・濃くなる瞬間と、なりたいロールになれた瞬間の演出を作る。デモ動画で使う。

## 作るもの
- `Art/VFX/VFX_IndividualityUp.prefab`（色が濃くなる：体から色のついた粒が出る）
- `Art/VFX/VFX_IndividualityDown.prefab`（色が抜ける：灰色の霧が体から抜けていく）
- `Art/VFX/VFX_RoleComplete.prefab`（クリア時：光に包まれて衣装が変わる）
- `Art/VFX/VFX_Faded.prefab`（ゲームオーバー：体が消えていく）
- `Scripts/Player/PlayerVfx.cs`（イベントを購読して VFX を出す）

## 仕様
- Particle System（または VFX Graph）で作る
- PlayerVfx：`PlayerParameters.OnChanged` で Individuality が 5 以上上がったら Up、5 以上下がったら Down を再生
- クリア・ゲームオーバー時の演出は、GameEndJudge が EndGame を呼ぶ前に **2 秒待って演出を見せる**必要がある → CD-13 の担当と相談して、GameEndJudge に「演出待ち」を入れてもらう
- 色はロールの roleColor を使う

## 完了条件
- [ ] 個性の上下で演出が出る
- [ ] クリアとゲームオーバー（色が消える）で演出が出てから結果画面に移る

## 確認手順
1. F1 パネルで個性を上下させる、クリアする、個性を 0 にする、を録画して PR に貼る
