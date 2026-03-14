using UnityEngine;

namespace Hide_and_PEAK.UI
{
    public static class UIHelper
    {
        private static Font _unicodeFont;

        public static Font GetUnicodeFont()
        {
            if (_unicodeFont == null)
                _unicodeFont = Resources.Load<Font>("Fonts/NotoSans-Regular");

            return _unicodeFont;
        }

        public static Texture2D MakeTex(int width, int height, Color col)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan)
                col = col.linear;

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            result.SetPixels(pix);
            result.Apply();

            result.filterMode = FilterMode.Point;
            result.wrapMode = TextureWrapMode.Clamp;

            return result;
        }

        public static GUIStyle CreatePanelStyle()
        {
            return new GUIStyle(GUI.skin.box)
            {
                font = GetUnicodeFont(),
                normal = { background = MakeTex(4, 4, new Color(0.12f, 0.12f, 0.12f, 0.85f)) },
                border = new RectOffset(12, 12, 12, 12),
                padding = new RectOffset(15, 15, 15, 15)
            };
        }

        public static GUIStyle CreateLabelStyle(int fontSize, TextAnchor alignment, bool bold = false)
        {
            return new GUIStyle(GUI.skin.label)
            {
                font = GetUnicodeFont(),
                fontSize = fontSize,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = alignment,
                normal = { textColor = Color.white },
                richText = true
            };
        }

        public static GUIStyle CreateButtonStyle(int fontSize = 18)
        {
            return new GUIStyle(GUI.skin.button)
            {
                font = GetUnicodeFont(),
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                padding = new RectOffset(12, 12, 8, 8)
            };
        }

        public static GUIStyle CreateColouredButton(GUIStyle baseStyle, Color baseColor)
        {
            var style = new GUIStyle(baseStyle);

            style.normal.background = MakeTex(4, 4, baseColor);
            style.hover.background = MakeTex(4, 4, baseColor * 1.2f);
            style.active.background = MakeTex(4, 4, baseColor * 0.8f);

            return style;
        }

        public static void DrawBackground(Rect rect, Color color)
        {
            GUI.DrawTexture(rect, MakeTex(1, 1, color));
        }
    }
}