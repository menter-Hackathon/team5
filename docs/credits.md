# クレジット

使用した素材と利用規約を記録します。

| 素材名 | 種類 | 入手先URL | ライセンス | 使っている場所 |
|---|---|---|---|---|
| M PLUS Rounded 1c（Bold / ExtraBold） | フォント | https://fonts.google.com/specimen/M+PLUS+Rounded+1c | SIL Open Font License 1.1 | 見出し・ボタン・お題・通知（`Art/Fonts/`） |
| Noto Sans JP | フォント | https://fonts.google.com/noto/specimen/Noto+Sans+JP | SIL Open Font License 1.1（`Art/Fonts/OFL.txt`） | 説明・アイテム名などの本文（`Art/Fonts/`） |

## 音（BGM・SE）

下表の従来音源はチームで作ったオリジナルの音です（`tools/audio/gen_audio.py` で合成。外部の音源・サンプルは使っていません）。そのためクレジット表記は不要で、公開リポジトリに置いても規約の問題はありません。外部サイトの素材に差し替えたときは、この表の行をその素材の入手先とライセンスに書き換えてください。

| 素材名 | 種類 | 入手先URL | ライセンス | 使っている場所 | 再生 |
|---|---|---|---|---|---|
| BGM_Title.ogg | BGM | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | タイトル画面。村のテーマを鐘とパッドでゆっくり（88BPM・16小節・43.6秒） | ループ |
| BGM_Omote.ogg | BGM | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | 表（見られている）。村のテーマ・ハ長調・木琴とウクレレ風（104BPM・24小節・55.4秒） | ループ |
| BGM_Ura.ogg | BGM | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | 裏（見られていない）。BGM_Omote と同じ小節・テンポ・長さをハ短調にし、低いはじく音と時計の音で緊張感 | ループ |
| BGM_Clear.ogg | BGM | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | クリアのファンファーレ（5.1秒） | 1回だけ |
| BGM_GameOver.ogg | BGM | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | ゲームオーバーの短い曲（6.4秒） | 1回だけ |
| SE_Pickup.wav | SE | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | 素材を拾う：上がる2音の鈴 | 1回だけ |
| SE_Craft.wav | SE | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | 作業台で作っている：木をたたく3回 | 1回だけ |
| SE_Crafted.wav | SE | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | できあがり：キラキラ上がる和音 | 1回だけ |
| SE_Caught.wav | SE | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | 見つかった：「ブッ」と下がる警告音 | 1回だけ |
| SE_TaskComplete.wav | SE | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | お題達成：3音の明るいジングル | 1回だけ |
| SE_TaskFail.wav | SE | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | お題失敗：下がる2音 | 1回だけ |
| SE_Equip.wav | SE | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | 着替え：シュッ＋鈴 | 1回だけ |
| SE_Button.wav | SE | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | ボタン：短いポッ | 1回だけ |
| SE_ParameterUp.wav | SE | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | 数値が上がる：上がる音 | 1回だけ |
| SE_ParameterDown.wav | SE | 自作（`tools/audio/gen_audio.py`） | チームのオリジナル（表記不要） | 数値が下がる：下がる音 | 1回だけ |


### ローカルの効果音差し替え（未公開）

ユーザー提供のMP3を `Assets/_Project/Audio/SE/LocalSoundEffectLab/` に配置。フォルダとmetaはGit対象外。
`Data/Audio/AudioLibrary.asset` の現在の差し替え参照もローカル用なので、そのまま公開しない。
元の自作音源は削除していません。

| 元ファイル | 用途 | 出典 |
|---|---|---|
| ぷよん.mp3 | アイテム取得 | 効果音ラボ（ユーザー確認済み） |
| パッ.mp3 | 村のお題提示（達成音とは別） | 効果音ラボ（ユーザー確認済み） |
| 衣擦れ2.mp3 | 着替え | ユーザー提供・個別出典未確認 |
| 振り子時計.mp3 | 残り30秒以下の警告 | ユーザー提供・個別出典未確認 |
| ぐつぐつ煮る.mp3 | 鶏ガラスープの調理中 | ユーザー提供・個別出典未確認 |
| グラスに水を注ぐ.mp3 | 鶏白湯ラーメンの仕上げ中 | ユーザー提供・個別出典未確認 |
| 剣の素振り2.mp3 | 素振りのお題作業中 | ユーザー提供・個別出典未確認 |
| ラーメンを食べる.mp3 | 食堂のお題作業中 | ユーザー提供・個別出典未確認 |

効果音ラボ規約：https://soundeffect-lab.info/agreement/ （商用利用可・素材の再配布禁止）。
