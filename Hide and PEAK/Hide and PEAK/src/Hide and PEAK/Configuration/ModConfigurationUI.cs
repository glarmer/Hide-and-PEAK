using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hide_and_PEAK.Configuration;

public class ModConfigurationUI : MenuWindow
{
    private List<Option> _options = new List<Option>();
    public bool _visible;
    private int _selectedIndex;

    private bool _waitingForBinding = false;
    private Option _bindingTarget;

    private Texture2D _whiteTex;
    private Texture2D _rainbowTex;
    private GUIStyle _titleStyle;
    private GUIStyle _rowStyle;
    private GUIStyle _hintStyle;
    private GUIStyle _buttonStyle;

    private string titleText;
    private string hintText;

    private int RowHeight = 32;
    private int PanelWidth = 460;
    private int Pad = 12;

    private int TitleFontSize = 22;
    private int OptionFontSize = 16;
    private int HintFontSize = 14;

    private const int ButtonWidth = 30;
    private const int ButtonHeight = 30;
    private const int ButtonHintSpacing = 4;

    public static ModConfigurationUI Instance;

    private void Awake()
    {
        if (Instance != null) Destroy(Instance);
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartClosed();

        titleText = $"Hide and PEAK Settings | v{Plugin.Version}";
        hintText = "Open/Close • Click: Change • Scroll Wheel: Adjust Numerical Values";
    }

    public void Init(List<Option> options)
    {
        _options = options ?? new List<Option>();
        _selectedIndex = 0;
    }

    private void EnsureStyles()
    {
        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = TitleFontSize,
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold
        };

        _rowStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = OptionFontSize,
            padding = new RectOffset(10, 10, 4, 4)
        };

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = HintFontSize,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = OptionFontSize,
            alignment = TextAnchor.MiddleCenter
        };
    }

    private void CalculatePanelWidth()
    {
        float maxWidth = _titleStyle.CalcSize(new GUIContent(titleText)).x;
        foreach (var option in _options)
        {
            float w = _rowStyle.CalcSize(new GUIContent($"{option.Label}: {option.DisplayValue()}")).x;
            if (w > maxWidth) maxWidth = w;
        }

        int hintWidth = CalculateHintWidth();
        maxWidth = Mathf.Max(maxWidth, hintWidth);

        PanelWidth = Mathf.Clamp((int)maxWidth + Pad * 2, 460, Screen.width - Pad * 2);
    }

    private int CalculateHintWidth()
    {
        float lineHeight = _hintStyle.CalcHeight(new GUIContent("Test"), 9999);
        float maxAllowedHeight = lineHeight * 2;

        int testWidth = 200;
        while (testWidth < Screen.width - Pad * 2)
        {
            float h = _hintStyle.CalcHeight(new GUIContent(hintText), testWidth);
            if (h <= maxAllowedHeight)
                return testWidth;
            testWidth += 20;
        }
        return Screen.width - Pad * 2;
    }

    private void Scale(int scale)
    {
        if (scale < 0 && HintFontSize < 2) return;

        TitleFontSize += scale * 2;
        OptionFontSize += scale * 2;
        HintFontSize += scale * 2;

        RowHeight = OptionFontSize + 16;

        CalculatePanelWidth();
    }

    public void ToggleMenu()
    {
        _visible = !_visible;
        if (_visible) Open();
        else Close();
    }

    private void Update()
    {
        if (!_visible || _options.Count == 0) return;

        if (_waitingForBinding && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            foreach (var key in Keyboard.current.allKeys)
            {
                if (key.wasPressedThisFrame)
                {
                    _bindingTarget.StringEntry.Value = key.path;
                    _waitingForBinding = false;
                    _bindingTarget = null;
                    break;
                }
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            CycleSelection(reverse ? -1 : 1);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow)) CycleSelection(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) CycleSelection(1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ToggleSelected();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) AdjustInt(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) AdjustInt(1);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) AdjustInt(1);
        else if (scroll < 0f) AdjustInt(-1);
    }

    private void ToggleSelected()
    {
        var option = _options[_selectedIndex];
        if (option.IsDisabled()) return;

        switch (option.Type)
        {
            case Option.OptionType.Bool:
                option.BoolEntry.Value = !option.BoolEntry.Value;
                break;
            case Option.OptionType.Int:
                int next = option.IntEntry.Value + option.Step;
                if (next > option.MaxInt) next = option.MinInt;
                option.IntEntry.Value = next;
                break;
            case Option.OptionType.InputAction:
                _waitingForBinding = true;
                _bindingTarget = option;
                break;
        }
    }

    private void AdjustInt(int delta)
    {
        var option = _options[_selectedIndex];
        if (option.IsDisabled() || option.Type != Option.OptionType.Int) return;
        option.IntEntry.Value = Mathf.Clamp(option.IntEntry.Value + delta * option.Step, option.MinInt, option.MaxInt);
    }

    private void CycleSelection(int delta)
    {
        if (_options.Count == 0) return;

        int startIndex = _selectedIndex;
        do
        {
            _selectedIndex = (_selectedIndex + delta) % _options.Count;
            if (_selectedIndex < 0) _selectedIndex += _options.Count;
            if (!_options[_selectedIndex].IsDisabled()) break;
        } while (_selectedIndex != startIndex);
    }

    private void OnGUI()
    {
        if (!_visible) return;

        EnsureStyles();
        CalculatePanelWidth();

        float titleHeight = _titleStyle.CalcHeight(new GUIContent(titleText), PanelWidth - Pad * 2);
        float hintHeight = _hintStyle.CalcHeight(new GUIContent(hintText), PanelWidth - Pad * 2);

        
        List<float> rowHeights = new List<float>();
        float contentHeight = 0f;
        foreach (var option in _options)
        {
            float currentHeight = RowHeight;

            if (option.Type == Option.OptionType.Colour)
            {
                float labelHeight = _rowStyle.CalcHeight(new GUIContent("Name Color"), PanelWidth - Pad * 2);
                float barHeight = 16f; 
                currentHeight = labelHeight + barHeight + 4; 
            }

            rowHeights.Add(currentHeight);
            contentHeight += currentHeight + 4;
        }

        int panelHeight = Pad + (int)titleHeight + 8 + (int)contentHeight + Pad + (int)hintHeight + ButtonHeight + ButtonHintSpacing;
        Rect panelRect = new Rect(20, 20, PanelWidth, panelHeight);

        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(panelRect, _whiteTex);
        GUI.color = Color.white;

        
        GUI.Label(new Rect(panelRect.x + Pad, panelRect.y + Pad, panelRect.width - Pad * 2, titleHeight), titleText, _titleStyle);

        
        float optionY = panelRect.y + Pad + titleHeight + 8;
        for (int i = 0; i < _options.Count; i++)
        {
            var option = _options[i];
            float currentRowHeight = rowHeights[i];
            Rect rowRect = new Rect(panelRect.x + Pad, optionY, panelRect.width - Pad * 2, currentRowHeight);

            if (rowRect.Contains(Event.current.mousePosition) && !option.IsDisabled())
            {
                GUI.color = new Color(1f, 1f, 1f, 0.24f);
                GUI.DrawTexture(rowRect, _whiteTex);
                GUI.color = Color.white;
                _selectedIndex = i;
            }

            switch (option.Type)
            {
                case Option.OptionType.Bool: DrawBoolOption(option, rowRect); break;
                case Option.OptionType.Int: DrawIntOption(option, rowRect); break;
                case Option.OptionType.InputAction: DrawInputActionOption(option, rowRect); break;
                case Option.OptionType.Colour: DrawColourOption(option, rowRect); break;
            }

            optionY += currentRowHeight + 4;
        }

        
        Rect hintRect = new Rect(panelRect.x + Pad, optionY, panelRect.width - Pad * 2, hintHeight);
        GUI.Label(hintRect, hintText, _hintStyle);

        
        float buttonY = hintRect.yMax + ButtonHintSpacing;
        if (GUI.Button(new Rect(panelRect.xMax - ButtonWidth * 2 - Pad, buttonY, ButtonWidth, ButtonHeight), "+", _buttonStyle))
            Scale(1);
        if (GUI.Button(new Rect(panelRect.xMax - ButtonWidth - Pad, buttonY, ButtonWidth, ButtonHeight), "-", _buttonStyle))
            Scale(-1);
    }

    private void DrawBoolOption(Option option, Rect rect)
    {
        GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        GUI.DrawTexture(rect, _whiteTex);
        GUI.color = Color.white;

        if (GUI.Button(rect, $"{option.Label}: {(option.BoolEntry.Value ? "On" : "Off")}", _rowStyle))
            option.BoolEntry.Value = !option.BoolEntry.Value;
    }

    private void DrawIntOption(Option option, Rect rect)
    {
        GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        GUI.DrawTexture(rect, _whiteTex);
        GUI.color = Color.white;

        if (GUI.Button(rect, $"{option.Label}: {option.IntEntry.Value}", _rowStyle))
            option.IntEntry.Value += option.Step;
    }

    private void DrawInputActionOption(Option option, Rect rect)
    {
        GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        GUI.DrawTexture(rect, _whiteTex);
        GUI.color = Color.white;

        string label = _waitingForBinding && option == _bindingTarget ? "Press any key..." : option.StringEntry.Value.Split("/")[^1].ToUpper();
        if (GUI.Button(rect, $"{option.Label}: {label}", _rowStyle) && !_waitingForBinding)
        {
            _waitingForBinding = true;
            _bindingTarget = option;
        }
    }

    private void DrawColourOption(Option option, Rect rowRect)
    {
        var r = Plugin.ConfigurationHandler.NameColourR;
        var g = Plugin.ConfigurationHandler.NameColourG;
        var b = Plugin.ConfigurationHandler.NameColourB;

        GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        GUI.DrawTexture(rowRect, _whiteTex);
        GUI.color = Color.white;

        float labelHeight = _rowStyle.CalcHeight(new GUIContent("Name Colour"), rowRect.width);
        Rect labelRect = new Rect(rowRect.x + 4, rowRect.y + 2, rowRect.width - 8, labelHeight);
        GUI.Label(labelRect, "Name Colour", _rowStyle);

        Rect barRect = new Rect(rowRect.x + 4, rowRect.y + labelHeight + 2, rowRect.width - 48, 16);
        EnsureRainbowTexture((int)barRect.width);
        GUI.DrawTexture(barRect, _rainbowTex);

        Rect previewRect = new Rect(rowRect.x + rowRect.width - 40, barRect.y, 36, barRect.height);
        GUI.color = new Color(r.Value / 255f, g.Value / 255f, b.Value / 255f);
        GUI.DrawTexture(previewRect, _whiteTex);
        GUI.color = Color.white;

        Event e = Event.current;
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && barRect.Contains(e.mousePosition))
        {
            float t = Mathf.Clamp01((e.mousePosition.x - barRect.x) / barRect.width);
            Color picked = ColourFromRainbow(t);
            r.Value = Mathf.RoundToInt(picked.r * 255);
            g.Value = Mathf.RoundToInt(picked.g * 255);
            b.Value = Mathf.RoundToInt(picked.b * 255);
            e.Use();
        }
    }

    private void EnsureRainbowTexture(int width = 256)
    {
        if (_rainbowTex != null) return;

        _rainbowTex = new Texture2D(width, 1);
        for (int i = 0; i < width; i++)
        {
            float t = i / (float)(width - 1);
            _rainbowTex.SetPixel(i, 0, ColourFromRainbow(t));
        }
        _rainbowTex.Apply();
    }

    private Color ColourFromRainbow(float t)
    {
        return Color.HSVToRGB(t, 1f, 1f);
    }

    private void OnDestroy()
    {
        if (_whiteTex != null) Destroy(_whiteTex);
        if (_rainbowTex != null) Destroy(_rainbowTex);
    }
}
