using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Team5.UI
{
    // ゲーム内の文字をきれいに見せる。UI は今までどおり Text（legacy）で作り、見た目だけ TextMeshPro（SDF）で描き直す。
    // Text は中身のデータとして残し、透明にする。文字・色・大きさ・揃え・有効/無効は毎フレーム写す。
    // 途中で作られた Text（チュートリアル・地図など）も拾う。どのシーンでも自動で動く
    public class TextUpgrader : MonoBehaviour
    {
        private const float ScanInterval = 0.25f;

        private class Pair
        {
            public Text source;
            public TextMeshProUGUI view;
            public string lastText;
        }

        private static TextUpgrader instance;
        private static readonly Dictionary<Font, TMP_FontAsset> fontAssets = new Dictionary<Font, TMP_FontAsset>();
        private static Material hiddenMaterial;
        private static Font fallbackFont; // Arial などの組み込みフォントは、ゲームのフォントに置き換える

        private readonly List<Pair> pairs = new List<Pair>();
        private readonly HashSet<Text> known = new HashSet<Text>();
        private float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("TextUpgrader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<TextUpgrader>();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + ScanInterval;
                Scan();
            }
            for (int i = pairs.Count - 1; i >= 0; i--)
            {
                var p = pairs[i];
                if (p.source == null || p.view == null)
                {
                    if (p.source != null) known.Remove(p.source);
                    pairs.RemoveAt(i);
                    continue;
                }
                Sync(p);
            }
        }

        private void Scan()
        {
            foreach (var text in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text == null || known.Contains(text)) continue;
                known.Add(text);
                var view = CreateView(text);
                if (view == null) continue;
                var pair = new Pair { source = text, view = view };
                pairs.Add(pair);
                Sync(pair);
            }
        }

        private static TextMeshProUGUI CreateView(Text source)
        {
            Font font = source.font;
            if (font == null || font.name == "LegacyRuntime" || font.name == "Arial")
            {
                if (fallbackFont == null) fallbackFont = FindGameFont();
                font = fallbackFont;
            }
            var asset = GetFontAsset(font);
            if (asset == null) return null; // 作れないときは元の Text のまま

            var go = new GameObject("TMP", typeof(RectTransform));
            go.layer = source.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(source.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = ((RectTransform)source.transform).pivot;

            var view = go.AddComponent<TextMeshProUGUI>();
            view.font = asset;
            view.raycastTarget = false;
            view.richText = source.supportRichText;
            view.alignment = ToAlignment(source.alignment);
            view.textWrappingMode = source.horizontalOverflow == HorizontalWrapMode.Wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            view.overflowMode = TextOverflowModes.Overflow;
            view.lineSpacing = (source.lineSpacing - 1f) * 30f;
            view.extraPadding = true;

            // 縁取り・影は TextMeshPro の縁取りと下敷きに置き換える
            var outline = source.GetComponent<Outline>();
            var shadow = source.GetComponent<Shadow>();
            if (outline != null || shadow != null)
            {
                var mat = new Material(view.fontSharedMaterial);
                if (outline != null)
                {
                    mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
                    mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
                    mat.SetColor(ShaderUtilities.ID_OutlineColor, outline.effectColor);
                    outline.enabled = false;
                }
                else
                {
                    mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                    mat.SetColor(ShaderUtilities.ID_UnderlayColor, shadow.effectColor);
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, Mathf.Clamp(shadow.effectDistance.x * 0.4f, -1f, 1f));
                    mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, Mathf.Clamp(shadow.effectDistance.y * 0.4f, -1f, 1f));
                    mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.3f);
                    shadow.enabled = false;
                }
                view.fontSharedMaterial = mat;
            }

            // 元の Text は透明にして、データとしてだけ使う
            if (hiddenMaterial == null)
            {
                hiddenMaterial = new Material(Shader.Find("UI/Default")) { name = "HiddenLegacyText" };
                hiddenMaterial.color = new Color(1f, 1f, 1f, 0f);
            }
            source.material = hiddenMaterial;
            return view;
        }

        private static void Sync(Pair p)
        {
            var s = p.source;
            var v = p.view;
            if (!ReferenceEquals(p.lastText, s.text))
            {
                p.lastText = s.text;
                v.text = s.text;
            }
            if (v.enabled != s.enabled) v.enabled = s.enabled;
            Color c = s.color;
            c.a *= s.canvasRenderer.GetAlpha();
            if (v.color != c) v.color = c;
            if (!Mathf.Approximately(v.fontSize, s.fontSize)) v.fontSize = s.fontSize;
            var style = ToStyle(s.fontStyle);
            if (v.fontStyle != style) v.fontStyle = style;
            var align = ToAlignment(s.alignment);
            if (v.alignment != align) v.alignment = align;
            if (s.resizeTextForBestFit != v.enableAutoSizing)
            {
                v.enableAutoSizing = s.resizeTextForBestFit;
                v.fontSizeMin = s.resizeTextMinSize;
                v.fontSizeMax = s.resizeTextMaxSize;
            }
        }

        private static TMP_FontAsset GetFontAsset(Font font)
        {
            if (font == null) return null;
            if (fontAssets.TryGetValue(font, out var cached)) return cached;
            TMP_FontAsset asset = null;
            try
            {
                asset = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TextUpgrader] {font.name} を TextMeshPro にできませんでした: {e.Message}");
            }
            if (asset != null) asset.name = font.name + " SDF (runtime)";
            fontAssets[font] = asset;
            return asset;
        }

        private static Font FindGameFont()
        {
            foreach (var f in Resources.FindObjectsOfTypeAll<Font>())
                if (f.name.StartsWith("MPLUSRounded")) return f;
            return null;
        }

        private static FontStyles ToStyle(FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Bold: return FontStyles.Bold;
                case FontStyle.Italic: return FontStyles.Italic;
                case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
                default: return FontStyles.Normal;
            }
        }

        private static TextAlignmentOptions ToAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                default: return TextAlignmentOptions.BottomRight;
            }
        }
    }
}
