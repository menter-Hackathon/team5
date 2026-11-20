using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // UI の小さな動き（ポン・ゆれ・光る・すべり込み）。同じ対象に重ねて呼ぶと前の動きを止めてやり直す
    public static class UiFx
    {
        // 窓の見出しにある「（Tab で閉じる）」などの操作の説明を消す（操作は Esc の一時停止で見られる）
        private static readonly string[] KeyNotes = { "（Tab で閉じる）", "（E で閉じる）", "（Tab）", "（C）" };

        public static void StripKeyNotes(Transform root)
        {
            if (root == null) return;
            foreach (var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                if (text.gameObject.name == "CloseHint") { text.gameObject.SetActive(false); continue; }
                foreach (var note in KeyNotes)
                    if (text.text.Contains(note)) text.text = text.text.Replace(note, "");
            }
        }

        private static readonly Dictionary<(Object, string), Coroutine> running = new(); // （対象, 動きの種類）ごと
        private static readonly Dictionary<RectTransform, Vector2> homes = new(); // 動かす前の位置（途中でやり直してもずれないように）

        private static Vector2 Home(RectTransform target)
        {
            if (!homes.TryGetValue(target, out var home)) homes[target] = home = target.anchoredPosition;
            return home;
        }

        private static void Run(MonoBehaviour host, Object target, string kind, IEnumerator routine)
        {
            if (host == null || !host.isActiveAndEnabled || target == null) return;
            var key = (target, kind);
            if (running.TryGetValue(key, out var old) && old != null) host.StopCoroutine(old);
            running[key] = host.StartCoroutine(routine);
        }

        // 一瞬大きくして戻す
        public static void Punch(MonoBehaviour host, Transform target, float amount = 0.25f, float seconds = 0.28f)
        {
            Run(host, target, "punch", PunchRoutine(target, amount, seconds));
        }

        // 元の大きさ（縮めて置いた UI もあるので 1 とは限らない）
        private static readonly Dictionary<Transform, Vector3> baseScales = new Dictionary<Transform, Vector3>();

        private static IEnumerator PunchRoutine(Transform target, float amount, float seconds)
        {
            if (!baseScales.TryGetValue(target, out var baseScale) || target == null) { baseScale = target.localScale; baseScales[target] = baseScale; }
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (target == null) yield break;
                float k = t / seconds;
                target.localScale = baseScale * (1f + amount * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f));
                yield return null;
            }
            if (target != null) target.localScale = baseScale;
        }

        // 左右にゆれる（失敗・見つかった）
        public static void Shake(MonoBehaviour host, RectTransform target, float strength = 12f, float seconds = 0.35f)
        {
            Run(host, target, "move", ShakeRoutine(target, strength, seconds));
        }

        private static IEnumerator ShakeRoutine(RectTransform target, float strength, float seconds)
        {
            Vector2 origin = Home(target);
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float fall = 1f - t / seconds;
                target.anchoredPosition = origin + new Vector2(Mathf.Sin(t * 70f) * strength * fall, 0f);
                yield return null;
            }
            target.anchoredPosition = origin;
        }

        // 色を一瞬変えて戻す
        public static void Flash(MonoBehaviour host, Graphic target, Color flash, float seconds = 0.4f)
        {
            Run(host, target, "color", FlashRoutine(target, flash, seconds));
        }

        private static IEnumerator FlashRoutine(Graphic target, Color flash, float seconds)
        {
            Color baseColor = target.color;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                target.color = Color.Lerp(flash, baseColor, t / seconds);
                yield return null;
            }
            target.color = baseColor;
        }

        // offset の位置からすべり込みながら現れる
        public static void SlideIn(MonoBehaviour host, RectTransform target, Vector2 offset, float seconds = 0.35f, float delay = 0f)
        {
            if (!target.TryGetComponent(out CanvasGroup group)) group = target.gameObject.AddComponent<CanvasGroup>();
            Run(host, target, "move", SlideRoutine(target, group, offset, seconds, delay));
        }

        private static IEnumerator SlideRoutine(RectTransform target, CanvasGroup group, Vector2 offset, float seconds, float delay)
        {
            Vector2 end = Home(target);
            group.alpha = 0f;
            target.anchoredPosition = end + offset;
            for (float t = 0f; t < delay; t += Time.unscaledDeltaTime) yield return null;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / seconds, 3f); // ease-out
                target.anchoredPosition = Vector2.LerpUnclamped(end + offset, end, k);
                group.alpha = k;
                yield return null;
            }
            target.anchoredPosition = end;
            group.alpha = 1f;
        }
    }
}
