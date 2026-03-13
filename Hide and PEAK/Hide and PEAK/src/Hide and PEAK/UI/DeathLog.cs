using System;
using System.Collections.Generic;
using System.Text;
using HideAndSeekMod;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace Hide_and_PEAK.UI;

public class DeathLog : MonoBehaviour
{
    private TMP_Text _text;

    public static DeathLog Instance;

    private readonly List<(string message, float expires)> _deaths = new();
    private readonly StringBuilder _sb = new(256);

    private const float Lifetime = 5f;

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Plugin.Log.LogWarning("[DeathLog] Instance already exists, replacing.");
            Destroy(Instance.gameObject);
        }

        Instance = this;

        _text = GetComponent<TMP_Text>();
        if (_text == null)
        {
            Plugin.Log.LogError("[DeathLog] No TMP_Text found!");
            enabled = false;
            return;
        }

        _text.autoSizeTextContainer = true;
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        _text.alignment = TextAlignmentOptions.TopRight;
        _text.color = Color.white;
        _text.fontSize = 26f;
        _text.outlineColor = Color.black;
        _text.outlineWidth = 0.06f;
        _text.lineSpacing = -30f;

        _text.text = "";
    }

    private void Update()
    {
        if (_text == null) return;

        _sb.Clear();

        _sb.Append("\n ");
        _sb.Append("\n ");

        float now = Time.time;

        for (int i = _deaths.Count - 1; i >= 0; i--)
        {
            var (message, expires) = _deaths[i];

            float remaining = expires - now;

            if (remaining <= 0f)
            {
                _deaths.RemoveAt(i);
                continue;
            }

            float t = Mathf.Clamp01(remaining / Lifetime);
            int alpha = Mathf.RoundToInt(t * 255);

            _sb.Append(message.Replace("{alpha}", alpha.ToString("X2")));
            _sb.Append('\n');
        }

        _text.text = _sb.ToString();
    }

    public void AddDeath(PhotonView seeker, PhotonView hider)
    {
        Plugin.Log.LogInfo("[DeathLog] Adding death");

        string seekerHex = "FF5555";
        string hiderHex = "55AAFF";

        if (seeker.Owner != null && seeker.Owner.CustomProperties.TryGetValue("NameColor", out var sHex))
            seekerHex = (string)sHex;

        if (hider.Owner != null && hider.Owner.CustomProperties.TryGetValue("NameColor", out var hHex))
            hiderHex = (string)hHex;

        string seekerName = FilterName(seeker.Owner?.NickName);
        string hiderName = FilterName(hider.Owner?.NickName);

        var seekerTag = $"<color=#{seekerHex}{{alpha}}>{seekerName}</color>";
        var hiderTag = $"<color=#{hiderHex}{{alpha}}>{hiderName}</color>";

        var middle = $"<color=#FFFFFF{{alpha}}> found </color>";

        var death = $"{seekerTag}{middle}{hiderTag}";

        _deaths.Add((death, Time.time + Lifetime));

        Plugin.Log.LogInfo("[DeathLog] Death added");
    }

    public void AddWorldDeath(PhotonView hider)
    {
        Plugin.Log.LogInfo("[DeathLog] Adding world death");

        string seekerHex = "FFFFFF";
        string hiderHex = "55AAFF";

        if (hider.Owner != null && hider.Owner.CustomProperties.TryGetValue("NameColor", out var hHex))
            hiderHex = (string)hHex;

        string hiderName = FilterName(hider.Owner?.NickName);

        var seekerTag = $"<color=#{seekerHex}{{alpha}}>Death</color>";
        var hiderTag = $"<color=#{hiderHex}{{alpha}}>{hiderName}</color>";

        var middle = $"<color=#FFFFFF{{alpha}}> found </color>";

        var death = $"{seekerTag}{middle}{hiderTag}";

        _deaths.Add((death, Time.time + Lifetime));

        Plugin.Log.LogInfo("[DeathLog] Death added");
    }

    private string FilterName(string input)
    {
        if (string.IsNullOrEmpty(input) || _text?.font == null)
            return "Unknown";

        input = input.Replace("<", "").Replace(">", "");

        TMP_FontAsset font = _text.font;
        var sb = new StringBuilder(input.Length);

        foreach (char c in input)
        {
            if (font.HasCharacter(c))
                sb.Append(c);
        }

        return sb.Length > 0 ? sb.ToString() : "Unknown";
    }

    public static void InitiateDeathLog()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }

        var ascentUI = FindAnyObjectByType<AscentUI>();

        if (ascentUI == null)
        {
            Plugin.Log.LogError("[DeathLog] Could not find AscentUI parent!");
            return;
        }

        var go = new GameObject(
            "DeathLog UI",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        var rt = go.GetComponent<RectTransform>();

        rt.SetParent(ascentUI.transform.parent, false);
        rt.localScale = Vector3.one;

        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);

        rt.anchoredPosition = new Vector2(-16f, -16f);
        rt.sizeDelta = new Vector2(600f, 200f);

        var ascentText = ascentUI.GetComponent<TMP_Text>();

        if (ascentText != null && ascentText.font != null)
        {
            var ourText = go.GetComponent<TextMeshProUGUI>();
            ourText.font = ascentText.font;

            Plugin.Log.LogInfo($"[DeathLog] Copied font from AscentUI: {ascentText.font.name}");
        }
        else
        {
            Plugin.Log.LogWarning("[DeathLog] Could not find font on AscentUI, using default");
        }

        go.AddComponent<DeathLog>();

        rt.SetAsLastSibling();
    }
}