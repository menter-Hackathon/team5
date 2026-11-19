# 音の作り方（SD-01 #44）

`gen_audio.py` は BGM 5 曲と SE 10 個を numpy だけで合成するスクリプトです（外部の音源は使っていません）。

```sh
python3 tools/audio/gen_audio.py /tmp/audio_out          # WAV を書き出す
for n in Title Omote Ura Clear GameOver; do
  ffmpeg -y -i /tmp/audio_out/BGM/BGM_$n.wav -ar 44100 -c:a libvorbis -q:a 5 Assets/_Project/Audio/BGM/BGM_$n.ogg
done
cp /tmp/audio_out/SE/*.wav Assets/_Project/Audio/SE/
```

- BGM_Omote と BGM_Ura は同じテンポ（104BPM）・同じ小節・同じ長さ（55.38 秒）。ハ長調とハ短調の同じメロディなので、2 つを同時に再生して音量だけクロスフェードすると、曲の位置がずれずに表⇔裏を切り替えられる。
- ループする曲（Title / Omote / Ura）は、最後の音の余韻を曲の頭に重ねて書き出しているので、終わりと頭が切れ目なくつながる。Unity では AudioSource の Loop をオンにするだけでよい。
- Clear / GameOver は 1 回だけ鳴らす短い曲（Loop オフ）。
- メロディやコードを変えたいときは `MEL_A` `MEL_B` `CH_A` `CH_B` を書き換えて再実行する。
