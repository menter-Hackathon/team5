# 行動別の効果音

8種類のMP3、Unityのmeta、AudioLibrary.assetの割り当てを同梱しています。ブランチを取得してUnityで開くとインポートされるため、手動での音源追加は不要です。

音源は `Assets/_Project/Audio/SE/LocalSoundEffectLab/` に配置しています（既存のフォルダ名を維持）。差し替える場合は `Assets/_Project/Data/Audio/AudioLibrary.asset` のInspectorで該当IDのClipを変更してください。

## 割り当て

| ID | 音源名 | 再生タイミング |
|---|---|---|
| Pickup | ぷよん.mp3 | 拾得成功時 |
| TaskAssigned | パッ.mp3 | 村からのお題提示時。初回のお題を含む |
| Equip | 衣擦れ2.mp3 | 着替え成功時 |
| ClockWarning | 振り子時計.mp3 | 残り30秒以下の間。AudioManagerのClock Warning Secondsで変更可能 |
| Boil | ぐつぐつ煮る.mp3 | soupレシピの作業中 |
| Pour | グラスに水を注ぐ.mp3 | paitanレシピの作業中 |
| Swing | 剣の素振り2.mp3 | training_groundでお題を実行中 |
| Eat | ラーメンを食べる.mp3 | canteenでお題を実行中 |

調理・素振り・食事・時計は、動作中だけループします。中断・完了・シーン遷移・ゲーム終了で停止し、ポーズ中は一時停止します。麺などそれ以外のレシピには既存のCraft音を使用します。お題の成功・失敗の音も従来どおりです。

音源はこのゲームの演出に組み込んで使用しています。素材単体の再配布や、他作品への転用許諾を意味しません。出典・利用条件は [credits.md](credits.md) を確認してください。

## 確認

Titleから開始し、拾得・お題の初回提示と次のお題・着替え・スープと仕上げ・素振りと食事・残り30秒を確認します。
操作を離す、クラフトを移動で中断する、ポーズ・再開する、結果画面へ移る場合に、ループ音が残らないことも確認します。
