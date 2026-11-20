# らしさの村

「〇〇らしく」を求められる村で、言われたとおりに振る舞いながら、見られていない隙に「本当になりたい自分」へ少しずつ変わっていく3Dゲーム。

> 人に押し付けられた「らしさ」ではなく、自分が思う「らしさ」を追い求めよう。

メンターハッカソン チーム5

## 遊ぶ

- **ブラウザ版（Unity なしで遊べる）**：https://menter-hackathon.github.io/team5/ （PC の Chrome / Edge / Safari。最初の読み込みに数十秒）
- **Unity で開く**：Unity 6000.0.85f2 でこのフォルダを開き、`Assets/_Project/Scenes/Title.unity` を開いて ▶
- 操作：WASD / 矢印で移動、E で拾う・話す・作業台、E 長押しでお題の動作、C で着替え、Tab で持ち物、M で地図、Esc で一時停止。詳しくは [docs/how-to-play.md](docs/how-to-play.md)

## 作品の概要

村長に「勇者らしく」「農家の子らしく」などの「らしさ」を押し付けられる村が舞台です。昼のあいだ村長のお題をこなして村の平和度を保ちつつ、村人の視線（扇形の視界）が外れた隙に素材を集め、作業台で「本当になりたい自分」のアイテムを作り、服を着替えていきます。見つかれば平和度が下がり、お題ばかりこなすと個性が薄れていく。その間で揺れながら、なりたい自分（鶏白湯職人・バンドマン・司書・プロゲーマー）を完成させるとクリアです。

## 使った技術

| 分野 | 内容 | 主なコード |
|---|---|---|
| エンジン | Unity 6（6000.0.85f2）・URP・WebGL ビルド（GitHub Pages で公開） | `ProjectSettings/` |
| 視線と監視 | NPC ごとの扇形の視界（距離・角度・遮蔽物のレイキャスト）、見られている／いないの切り替えで BGM と演出が変わる | `Scripts/NPC/VisionSensor.cs` `SurveillanceManager.cs` |
| 「道から外れる」判定 | 村の道と広場のメッシュをエディタで 0.25m の格子に焼き、毎フレーム参照 | `Scripts/NPC/OffRoadWatcher.cs` |
| NPC の巡回 | NavMesh（AI Navigation）で巡回点を回り、止まって見回す | `Scripts/NPC/NpcPatrol.cs` |
| データ駆動のゲーム内容 | ロール・お題・アイテム・レシピ・衣装を ScriptableObject で定義。4 ロールを同じコードで動かす | `Scripts/Data/` `Data/` |
| 初見向けの案内 | 時間を止める説明カード、次の行き先を計算して矢印と地図の★で示す、村人の名札と口ぐせ | `Scripts/UI/TutorialView.cs` `ObjectiveView.cs` `GuideArrowView.cs` |
| 演出 | 屋根を消して建物の中を見せる、着替え・結果の通達・ハンコの演出、お題ごとの動きと道具 | `Scripts/Core/RoofHider.cs` `Scripts/UI/EndBannerView.cs` `Scripts/Player/PlayerAnimator.cs` |
| 3D 素材 | 村は Blender で制作（いろいろ）。小物・アイテムはチームの絵から Meshy AI で 3D 化し、Blender で軽くして取り込み | `Art/Environment/` `Art/Items/` `Art/Props/` |
| AI の使い方 | Claude Code・Codex でコードを書き、PlayMode テストでクリアまでの自動確認 | `docs/workflow.md` |

素材のクレジットは [docs/credits.md](docs/credits.md) にまとめています。

## ドキュメント

| ファイル | 内容 |
|---|---|
| [docs/requirements.md](docs/requirements.md) | 要件定義（何を作るか） |
| [docs/tasks.md](docs/tasks.md) | タスク一覧と前後関係 |
| [docs/workflow.md](docs/workflow.md) | 開発の進め方（ブランチ・PR・Unityのルール・AIの使い方） |
| [docs/how-to-play.md](docs/how-to-play.md) | 遊び方（操作・ルール・素材の場所） |
| [docs/architecture.md](docs/architecture.md) | 設計書（クラス名・フォルダ・データの形）。AIでコードを書くときに必ず渡す |
| [タスクのボード](https://github.com/orgs/menter-Hackathon/projects/1) | GitHub Project |
| docs/game-design/ | 企画タスクで決めた仕様 |

## 開発環境

- Unity **6000.0.85f2**（Unity 6 LTS）。Unity Hub の「インストール」→「エディターをインストール」から同じバージョンを入れる
- テンプレート：Universal 3D（URP）／パッケージ：Input System、Cinemachine 3.x、AI Navigation
- 開き方：Unity Hub →「Add」→ このリポジトリのフォルダを選ぶ

## スケジュール

| マイルストーン | 期限 |
|---|---|
| M0 企画・基盤 | 11/16 |
| M1 プロトタイプ | 11/17 21:30 会議で確認 |
| M2 コンテンツ拡充 | 11/19 21:30 会議で確認 |
| M3 コードフリーズ・提出 | 11/20 23:59 |
| M4 スライド提出 | 11/21 23:59 |
| 発表 | 11/22 |
