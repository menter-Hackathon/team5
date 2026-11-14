# 開発の進め方

チーム開発が初めての人でも迷わないように、作業の流れとルールをまとめています。

---

## 1. 全体の流れ

```
Issueを選ぶ → 自分を担当にする → ブランチを作る → 作業 → Pull Request → レビュー → mainにマージ
```

1. **GitHub Project のボード**で、`Todo` 列から自分のIssueを選ぶ。
2. Issueの **前提タスク** が終わっているか確認する。終わっていなければ、担当者に声をかけるか、仮のもので進める。
3. Issueの Assignees に自分を入れ、ボードで `In Progress` に移す。
4. ブランチを作って作業する（下の「ブランチ」参照）。
5. 終わったら Pull Request（PR）を作り、本文に `Closes #Issue番号` と書く。マージすると Issue が自動で閉じる。
6. 誰か1人に見てもらってから main にマージする。

迷ったら、Issue にコメントを書いてから Discord などで声をかける。Issueに残しておくと、後から経緯を追えます。

---

## 2. Issue の見方

各Issueには次のことが書いてあります。

| 項目 | 意味 |
|---|---|
| 概要 | 何のためのタスクか |
| やること | チェックリスト。終わったらチェックを入れる |
| 完了条件 | ここまでできたら Done |
| 前提タスク | これが終わってから始めるのが理想。終わっていなければ仮で進めてよい |
| 後続タスク | このタスクが終わるのを待っている人がいる |

ラベル：

- 分類：`企画` `基盤` `コード` `デザイン` `アニメ` `サウンド` `発表`
- 優先度：`P0`（MVPに必須）、`P1`（入れたい）、`P2`（余裕があれば）
- `要審議`：チームで話し合って決める必要がある

**P0 から先に片付ける**。後続タスクが多いものほど早めに。

---

## 3. ブランチ

- `main` に直接 push しない。
- ブランチ名は `種類/Issue番号-内容`。

```
feature/12-player-move
design/30-player-model
docs/3-items
fix/45-timer-bug
```

```bash
git switch main
git pull
git switch -c feature/12-player-move
```

作業が終わったら：

```bash
git add .
git commit -m "プレイヤーの移動を追加 #12"
git push -u origin feature/12-player-move
```

その後 GitHub で Pull Request を作る。

---

## 4. Unity でのチーム開発ルール

Unity はチーム開発で衝突（コンフリクト）が起きやすいので、以下を守ってください。

### 4.1 バージョンを揃える
全員が **同じ Unity バージョン** を使う（BS-01 で決める）。違うバージョンで開くとファイルが書き換わり、衝突の原因になります。

### 4.2 同じシーンを同時に編集しない
シーンファイル（`.unity`）は複数人で同時に編集すると、マージがほぼ不可能になります。

- 自分の作業は **自分用のテストシーン**（`Assets/_Project/Scenes/Sandbox/名前.unity`）で行う。
- 作ったものは **Prefab** にして、メインのシーンにはPrefabを置くだけにする。
- メインのシーン（`Main.unity` など）を触るときは、チャットで「今から触ります」と宣言する。

### 4.3 .meta ファイルを必ず一緒にコミットする
Unity はファイルごとに `.meta` を作ります。これがないと参照が切れます。ファイルを移動・名前変更するときは **Unity エディタ上で** 行ってください。

### 4.4 コミットしないもの
`Library/` `Temp/` `Logs/` `obj/` `Build/` などは `.gitignore` で除外済みです。`git status` に大量のファイルが出たら、コミットする前に相談してください。

### 4.5 フォルダ構成

自分たちの作ったものはすべて `Assets/_Project/` に入れます。アセットストアなどから入れた素材は `Assets/` 直下のままでOKです。

```
Assets/
  _Project/
    Scenes/
      Sandbox/      … 個人のテスト用シーン
    Scripts/
      Core/         … GameManager、タイマー、パラメーター
      Player/
      NPC/
      Tasks/        … お題
      Items/        … アイテム・インベントリ・クラフト
      UI/
    Data/           … ScriptableObject（ロール・お題・アイテム・レシピ）
    Prefabs/
    Art/
      Models/
      Materials/
      Textures/
      UI/
    Animations/
    Audio/
      BGM/
      SE/
docs/               … 企画・仕様のドキュメント
```

### 4.6 大きなファイル
3Dモデルや音声で 50MB を超えるファイルは、入れる前に相談してください（GitHub の制限があります）。

---

## 5. 企画タスクの進め方

企画のIssue（`企画` ラベル）は、決まった内容を `docs/game-design/` に Markdown で書いて PR にします。

- 例：PL-01 → `docs/game-design/roles.md`
- 決まったら [requirements.md](requirements.md) の【未定】も更新する。
- 話し合いが必要なものは Issue のコメントで意見を出し合い、決まったら結論をコメントに書いてから閉じる。

---

## 6. 素材のライセンス

フリー素材（モデル・音・フォント）を使うときは、必ず利用規約を確認して `docs/credits.md` に記録してください。発表や公開のときに必要になります。

| 素材名 | 種類 | 入手先URL | ライセンス | 使っている場所 |
|---|---|---|---|---|
