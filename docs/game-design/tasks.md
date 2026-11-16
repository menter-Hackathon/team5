# 表のお題・裏の行動（PL-02）

ロールごとに「表のお題」（村に押し付けられる行動）と「裏の行動」（なりたいロールに近づく行動）をまとめます。

- **表のお題**の表は、そのまま `TaskDefinition`（`Assets/_Project/Data/Tasks/Task_〇〇.asset`）のデータになります。上から順に出題されます（TaskManager は `imposedTasks` を順番に出す）。
- **裏の行動**の表は、`RecipeDefinition`（クラフト・料理）と `OutfitDefinition`（着替え）のもとになります。材料の個数・置き場所・数値は PL-03 / PL-04 で決めます。
- 種類（type）は 3 つから選びます：`ActionAtSpot`（場所で E 長押し）/ `BringItem`（アイテムを渡す）/ `TalkToNpc`（誰かに話しかける）
- 「場所ID / 持っていくもの」の列：ActionAtSpot は場所ID、BringItem は itemId ×個数、TalkToNpc は NPC の ID を書きます。
- 「かかる秒数」は ActionAtSpot のときだけ（`actionSeconds`）。「制限時間」は `timeLimitSeconds`（秒）。

> **ステータス：案（メンバー確認待ち）**
> DS-03 の場所一覧（`docs/game-design/map.md`）はまだないので、この文書の最後に **場所IDの一覧（DS-03 へ渡す案）** を書きました。DS-03 はこの ID をそのまま使ってください。名前を変える場合は、この文書も一緒に直してください。
> itemId（`wood` など）は PL-03、NPC の ID（`chief` など）は PL-08 で確定します。ここでは仮の名前です。

---

## ★ 1. ramen_chef（MVP）

押し付けられる「らしさ」：**勇者** ／ なりたいロール：**鶏白湯職人**

### 表のお題

| taskId | title | NPCのセリフ（request） | 種類（type） | 場所ID / 持っていくもの | かかる秒数 | 制限時間 |
|---|---|---|---|---|---|---|
| `swing` | 素振りをする | 「勇者なら朝の素振りを欠かすな」 | ActionAtSpot | `training_ground` | 3 | 30 |
| `eat_ramen` | 辛ラーメンを食べる | 「村の食堂で辛ラーメンを食べなさい。勇者は辛さに耐えるものだ」 | ActionAtSpot | `canteen` | 4 | 40 |
| `bring_wood` | 薪を届ける | 「村長の家に薪を3本持ってきなさい」 | BringItem | `wood` ×3 | - | 60 |
| `report_guard` | 見張りに報告する | 「勇者なら村の見張りに異常がないか聞いてきなさい」 | TalkToNpc | `guard` | - | 40 |

- 3 種類のお題がすべて入っているので、CD-09a / CD-09b の動作確認にもそのまま使えます。
- `eat_ramen` は会議の例「食堂で決められたものを食べる」。本当は自分の鶏白湯を作りたいのに、決められた辛ラーメンを食べさせられる、という皮肉です。
- `bring_wood` の薪は森（`forest`）で拾う想定。届け先は村長の家（`chief_house`）の DeliveryPoint。
- 会議の例「決められた道しか通れない」はお題ではなく、移動のルール（CD-21）で表します。

### 裏の行動

| 行動 | 使う場所 | 必要なもの | 結果 |
|---|---|---|---|
| 鶏ガラスープを煮込む | 作業台 `kitchen` | 鶏ガラ `chicken_bone` ×2、水 `water` ×1 | 鶏ガラスープ `soup_base` |
| 麺を打つ | 作業台 `kitchen` | 小麦粉 `flour` ×1、水 `water` ×1 | 麺 `noodle` |
| 鶏白湯ラーメンを仕上げる | 作業台 `kitchen` | 鶏ガラスープ `soup_base` ×1、麺 `noodle` ×1 | 鶏白湯ラーメン `paitan`（Goal・推しアイテム） |
| エプロンに着替える | どこでも（見られていない所） | 料理人のエプロン `chef_apron` ×1 | 見た目が段階 1（色がつく） |
| 頭にタオルを巻く | どこでも（見られていない所） | 頭タオル `chef_towel` ×1 | 見た目が段階 2（鶏白湯職人の姿） |

- 流れ：素材を集める（鶏ガラ・水・小麦粉）→ スープと麺を作る → 鶏白湯ラーメンを完成させる。設計書のサンプル名（`Recipe_PaitanSoup` / `Item_ChickenBone`）とそろえています。
- 素材の置き場所の目安（PL-03 で確定）：鶏ガラ＝畑の裏（`field`）、水＝井戸（`well`）、小麦粉＝食堂の倉庫（`canteen`）、エプロン＝食堂の倉庫、頭タオル＝森（`forest`）。
- 作業台 `kitchen` は食堂の裏手に置き、NPC の巡回のすき間で使えるようにする（DS-03 のバランスの目安）。

---

## 2. band_man（M2）

押し付けられる「らしさ」：**長男** ／ なりたいロール：**バンドマン**

### 表のお題

| taskId | title | NPCのセリフ（request） | 種類（type） | 場所ID / 持っていくもの | かかる秒数 | 制限時間 |
|---|---|---|---|---|---|---|
| `sweep_home` | 家の前を掃除する | 「長男なら家の前くらい毎朝掃きなさい」 | ActionAtSpot | `home` | 4 | 40 |
| `bring_water` | 水を届ける | 「長男なら村長の家に水を2杯運んでおきなさい」 | BringItem | `water` ×2 | - | 60 |
| `greet_chief` | 村長にあいさつする | 「家を継ぐ者は、毎日村長にあいさつに行くものだ」 | TalkToNpc | `chief` | - | 40 |

### 裏の行動

| 行動 | 使う場所 | 必要なもの | 結果 |
|---|---|---|---|
| ギターをゲットする | 作業台 `barn` | 木材 `wood` ×2、糸 `string` ×1 | リュート `lute` |
| 曲を作る | 作業台 `barn` | リュート `lute` ×1、紙 `paper` ×1 | はじめての曲 `original_song`（Goal） |
| 派手なジャケットに着替える | どこでも（見られていない所） | ライブジャケット `band_jacket` ×1 | 見た目が段階 1〜2 |

---

## 3. librarian（M2・余裕があれば）

押し付けられる「らしさ」：**鍛冶屋の跡取り** ／ なりたいロール：**司書**

### 表のお題

| taskId | title | NPCのセリフ（request） | 種類（type） | 場所ID / 持っていくもの | かかる秒数 | 制限時間 |
|---|---|---|---|---|---|---|
| `hammer` | ハンマーを振る | 「跡取りなら毎日ハンマーを振りなさい」 | ActionAtSpot | `forge` | 5 | 40 |
| `bring_ore` | 鉱石を届ける | 「鍛冶場に鉱石を2つ持ってきなさい」 | BringItem | `ore` ×2 | - | 60 |
| `ask_master` | 親方に教わる | 「親方のところへ行って、今日の仕事を聞いてきなさい」 | TalkToNpc | `blacksmith` | - | 40 |

### 裏の行動

| 行動 | 使う場所 | 必要なもの | 結果 |
|---|---|---|---|
| 古い本を直す | 作業台 `library` | 古い本 `old_book` ×1、のり `glue` ×1 | 本 `book` |
| 本棚を作る | 作業台 `library` | 木材 `wood` ×3 | 本棚 `bookshelf` |
| 小さな図書館を開く | 作業台 `library` | 本 `book` ×3、本棚 `bookshelf` ×1 | 村の図書館 `village_library`（Goal） |
| 司書のめがねをかける | どこでも（見られていない所） | めがね `librarian_glasses` ×1 | 見た目が段階 1〜2 |

---

## 4. progamer（余裕があれば）

押し付けられる「らしさ」：**農家の子** ／ なりたいロール：**プロゲーマー**

### 表のお題

| taskId | title | NPCのセリフ（request） | 種類（type） | 場所ID / 持っていくもの | かかる秒数 | 制限時間 |
|---|---|---|---|---|---|---|
| `plow` | 畑を耕す | 「言われた畑を、言われたとおりに耕しなさい」 | ActionAtSpot | `field` | 5 | 40 |
| `bring_vegetable` | 野菜を届ける | 「とれた野菜を3つ、村長の家に届けなさい」 | BringItem | `vegetable` ×3 | - | 60 |
| `eat_porridge` | 麦がゆを食べる | 「農家の子は食堂で麦がゆを食べるものだ」 | ActionAtSpot | `canteen` | 3 | 30 |

### 裏の行動

| 行動 | 使う場所 | 必要なもの | 結果 |
|---|---|---|---|
| ゲーム盤を作る | 作業台 `attic` | 木材 `wood` ×1、紙 `paper` ×1 | ゲーム盤 `game_board` |
| 攻略ノートを書く | 作業台 `attic` | ゲーム盤 `game_board` ×1、紙 `paper` ×1 | 攻略ノート `strategy_note` |
| 大会の招待状を書く | 作業台 `attic` | 攻略ノート `strategy_note` ×1、羽ペン `quill` ×1 | 世界大会への招待状 `tournament_invite`（Goal） |
| ヘッドバンドを巻く | どこでも（見られていない所） | ヘッドバンド `gamer_headband` ×1 | 見た目が段階 1〜2 |

---

## 場所IDの一覧（DS-03 へ渡す案）

DS-03 の `map.md` と同じ列にしています。場所IDは CD-02 の `Spot_<ID>`（例：`Spot_training_ground`）と CD-09b の spotId に使います。英語・小文字・`_` 区切り。

| 場所ID（英語） | 名前 | 何をする場所か | 関係するロール |
|---|---|---|---|
| `plaza` | 広場 | 村の中央。NPC が集まり、見られやすい | 全員 |
| `chief_house` | 村長の家 | BringItem の届け先（村長に渡す） | 全員 |
| `canteen` | 食堂 | 決められたものを食べる（表）。倉庫に素材がある | ramen_chef / progamer |
| `kitchen` | 作業台（食堂の裏の台所） | 料理する（裏） | ramen_chef |
| `training_ground` | 訓練場 | 素振りをする（表） | ramen_chef |
| `field` | 畑 | 耕す（表）。裏に鶏ガラなどの素材 | progamer / ramen_chef |
| `well` | 井戸 | 水を汲む（素材） | ramen_chef / band_man |
| `forest` | 森 | 薪や素材を拾う。端の方で死角になりやすい | 全員 |
| `home` | 実家 | 家の前を掃除する（表） | band_man |
| `barn` | 納屋（作業台） | 楽器を作る・作曲する（裏） | band_man |
| `forge` | 鍛冶場 | ハンマーを振る（表）。鉱石の届け先 | librarian |
| `library` | 古い書庫（作業台） | 本を直す・本棚を作る（裏） | librarian |
| `attic` | 屋根裏（作業台） | ゲーム盤・攻略ノートを作る（裏） | progamer |

- MVP（M1）で必要なのは `plaza` `chief_house` `canteen` `kitchen` `training_ground` `field` `well` `forest` の 8 か所です。残りは M2 で追加するロールのぶん。
- 裏の行動の作業台（`kitchen` `barn` `library` `attic`）は、見られずに使えるタイミングがある位置に置いてください（DS-03 のバランスの目安）。

## NPC の ID（PL-08 へ渡す案）

TalkToNpc の対象です。巡回ルートや人数は PL-08 で決めます。

| npcId | 名前 | 使うお題 |
|---|---|---|
| `chief` | 村長 | `greet_chief`（BringItem の受け取りも村長） |
| `guard` | 見張り | `report_guard` |
| `blacksmith` | 鍛冶屋の親方 | `ask_master` |
