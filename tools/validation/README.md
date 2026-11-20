# バンドマンの3工程の検証

Unity 6000.0.85f2を使用します。普段のプロジェクトとは別のコピーで実行してください。

1. このフォルダの `BandProgressionCheck.cs` を、コピー側の `Assets/Editor/` に配置します。
2. Unityを次の引数で起動します。

```sh
Unity -batchmode -nographics -projectPath /absolute/path/to/copy -executeMethod BandProgressionCheck.Run -logFile /absolute/path/to/band-check.log
```

終了コード0と `BAND_CHECK_ALL_PASS` が成功条件です。マップ内の4材料、追加ピックアップの見た目・当たり判定・障害物への埋まり込み、全ロールのレシピ依存関係、3工程の材料消費、キャンセル、進捗30/60/100、最終工程でのクリアを確認します。

実機ではTitleからバンドマンを選び、紙と鉛筆で楽譜、チューナーとアンプで演奏準備、両方を使って「初めての曲で演奏」を実行してください。新しい3材料は既存の共通ピックアップの見た目を使用します。専用モデル・演奏アニメーション・演奏曲の追加はこの変更には含めません。
