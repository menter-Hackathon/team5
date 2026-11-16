# アイテム・レシピ（PL-03）

MVP ロール **ramen_chef**（押し付け：勇者 → なりたい：鶏白湯職人）で使うアイテムとレシピをまとめます。

- **アイテム**の表は `ItemDefinition`（`Assets/_Project/Data/Items/Item_〇〇.asset`）のデータになります。
- **レシピ**の表は `RecipeDefinition`（`Assets/_Project/Data/Recipes/Recipe_〇〇.asset`）のデータになります。
- 種類（category）：`Material`（素材）/ `Food`（食べ物）/ `Outfit`（服）/ `Goal`（最終目標）/ `Misc`（その他）
- 置く場所は PL-02（[tasks.md](tasks.md)）の場所IDを使います。DS-03 の `map.md` ができたら、そちらに合わせます。
- ここに書くのは **目的のアイテムだけ** です。クリアに関係しない寄り道アイテムは PL-09 で決めて、この文書に追記します。

> **ステータス：案（メンバー確認待ち）**
> 数値（秒数・進み具合・個性）は PL-04 / PL-06 / PL-11 で調整する可能性があります。

---

## ★ ramen_chef（MVP）

### アイテム

| itemId | 名前 | 種類（category） | 置く場所 | 個数 | 説明（30字以内） |
|---|---|---|---|---|---|
| `chicken_bone` | 鶏ガラ | Material | 畑の裏 `field` | 2 | 鶏白湯のもと |
| `water` | 水 | Material | 井戸 `well` | 2 | スープにも麺にも使う井戸水 |
| `flour` | 小麦粉 | Material | 食堂の倉庫 `canteen` | 1 | 麺を打つための粉 |
| `soup_base` | 鶏ガラスープ | Food | -（作る） | - | 白くにごるまで煮込んだスープ |
| `noodle` | 麺 | Food | -（作る） | - | こっそり打った手打ち麺 |
| `paitan` | 鶏白湯ラーメン | Goal | -（作る） | - | 夢の一杯 |
| `chef_apron` | 料理人のエプロン | Outfit | 食堂の倉庫 `canteen` | 1 | これを着たら料理人 |
| `chef_towel` | 頭タオル | Outfit | 森 `forest` | 1 | 職人のしるし。汗止めにも |
| `wood` | 薪 | Material | 森 `forest` | 3 | 村長に届ける薪（表のお題用） |

- `soup_base` `noodle` `paitan` はマップに置かず、レシピで作ります（`worldPrefab` は不要）。
- `wood` は裏の行動ではなく、表のお題 `bring_wood`（BringItem・`wood` ×3）で使います。ロールに必要なので、ここに入れました。
- `chef_apron` `chef_towel` の見た目の段階（1 / 2）と個性の数値は PL-07（outfits.md）で決めます。
- 素材の個数は「ちょうど足りる数」にしています。余分があると同じレシピを何回も作って進み具合を稼げてしまうためです。

### レシピ

作る場所はすべて作業台 `kitchen`（食堂の裏の台所）です。

| recipeId | 名前 | 材料（itemId×個数） | できるもの | かかる秒数 | 進み具合 + | 個性 + |
|---|---|---|---|---|---|---|
| `soup` | 鶏ガラスープ | `chicken_bone`×2, `water`×1 | `soup_base` | 3 | 30 | 15 |
| `noodle` | 手打ち麺 | `flour`×1, `water`×1 | `noodle` | 3 | 30 | 15 |
| `paitan` | 鶏白湯ラーメン | `soup_base`×1, `noodle`×1 | `paitan` | 5 | 40 | 20 |

- 3 つとも `consumeOnCraft = false`（できたものを持ち物に入れる）にします。`soup_base` と `noodle` は次のレシピの材料になり、`paitan` は最終アイテムとして持っておくためです。
- `paitan` は時間が長い（5 秒）ぶん、見つかって中断されやすい「最後の山場」にしています。

### 確認：最終アイテムまでたどり着けるか

```
鶏ガラ×2（畑の裏）─┐
水×1（井戸）───────┴→ [soup]   鶏ガラスープ ─┐
小麦粉×1（食堂の倉庫）┐                         ├→ [paitan] 鶏白湯ラーメン（Goal）
水×1（井戸）──────────┴→ [noodle] 麺 ──────────┘
```

| 素材 | 置く数 | 使う数 |
|---|---|---|
| `chicken_bone` | 2 | 2（soup） |
| `water` | 2 | 2（soup 1 + noodle 1） |
| `flour` | 1 | 1（noodle） |

- マップに置いた素材だけで `paitan` まで作れます。足りない材料はありません。

### 確認：勝利条件に届くか

| 作るもの | 進み具合 | 合計 | 個性 |
|---|---|---|---|
| `soup` | +30 | 30 | +15 |
| `noodle` | +30 | 60 | +15 |
| `paitan` | +40 | **100** | +20 |

- 進み具合は 3 つのレシピを全部作ると **ちょうど 100**（`roleProgressToWin = 100`）になり、`paitan` を作った瞬間にクリアになります。
- 進み具合が上がるのはクラフトだけです（architecture.md の `RecipeDefinition.roleProgressReward`。着替えは個性だけが上がる）。なので、どれか 1 つでも作らないとクリアできません。
- 個性はレシピだけで +50、着替え 2 回（PL-07）で残りが上がる想定です。
- 勝利条件が PL-06 で変わったら（例：「`paitan` を持っていること」も条件にする）、この表の数値を見直します。

---

## M2 以降のロール

band_man / librarian / progamer のアイテムとレシピは、MVP が動いてから追加します（材料の名前は [tasks.md](tasks.md) の「裏の行動」の表を使う）。
