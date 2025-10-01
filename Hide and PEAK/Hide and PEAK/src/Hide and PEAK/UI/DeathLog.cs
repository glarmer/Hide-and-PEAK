using System;
using System.Collections.Generic;
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

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Plugin.Log.LogWarning("[DeathLog] Instance already exists, destroying old one.");
            Destroy(Instance);
            Instance = this;
            return;
        }
        _text = GetComponent<TMP_Text>();
        if (_text == null)
        {
            var tmpUgui = GetComponent<TextMeshProUGUI>();
            if (tmpUgui != null)
            {
                _text = tmpUgui;
            }
            else
            {
                Plugin.Log.LogError("[DeathLog] No TMP_Text/TextMeshProUGUI found on object!");
                enabled = false;
                return;
            }
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

        var sb = new System.Text.StringBuilder();

        for (var i = _deaths.Count - 1; i >= 0; i--)
        {
            var (message, expires) = _deaths[i];
            var remaining = expires - Time.time;
            
            if (remaining <= 0)
            {
                _deaths.RemoveAt(i);
                continue;
            }

            var t = Mathf.Clamp01(remaining / 5f);
            var alpha = Mathf.RoundToInt(t * 255);
            
            string fadedMessage = message.Replace("{alpha}", alpha.ToString("X2"));

            sb.Append(fadedMessage).Append('\n');
        }

        _text.text = sb.ToString();
    }

    public void AddDeath(PhotonView seeker, PhotonView hider)
    {
        Plugin.Log.LogInfo("[DeathLog] Adding death");
        
        string seekerHex = "FF5555";
        string hiderHex = "55AAFF";
        if (seeker.Owner != null && seeker.Owner.CustomProperties.TryGetValue("NameColor", out var sHex)) seekerHex = (string)sHex;
        if (hider.Owner != null && hider.Owner.CustomProperties.TryGetValue("NameColor", out var hHex)) hiderHex = (string)hHex;
        var seekerName = $"<color=#{seekerHex}{{alpha}}>{seeker.Owner.NickName}</color>";
        var hiderName  = $"<color=#{hiderHex}{{alpha}}>{hider.Owner.NickName}</color>";
        
        var middle = $"<color=#FFFFFF{{alpha}}> found </color>";

        var death = $"{seekerName}{middle}{hiderName}";
        var displayUntil = Time.time + 5f;

        _deaths.Add((death, displayUntil));
        Plugin.Log.LogInfo("[DeathLog] Death added");
    }
    
    public void AddWorldDeath(PhotonView hider)
    {
        Plugin.Log.LogInfo("[DeathLog] Adding death");
        
        string seekerHex = "FFFFFF";
        string hiderHex = "55AAFF";
        if (hider.Owner != null && hider.Owner.CustomProperties.TryGetValue("NameColor", out var hHex)) hiderHex = (string)hHex;
        var seekerName = $"<color=#{seekerHex}{{alpha}}>Death</color>";
        var hiderName  = $"<color=#{hiderHex}{{alpha}}>{hider.Owner.NickName}</color>";
        
        var middle = $"<color=#FFFFFF{{alpha}}> found </color>";

        var death = $"{seekerName}{middle}{hiderName}";
        var displayUntil = Time.time + 5f;

        _deaths.Add((death, displayUntil));
        Plugin.Log.LogInfo("[DeathLog] Death added");
    }

    public static void InitiateDeathLog()
    {
        if (Instance == null)
        {
            var ascentUI = FindAnyObjectByType<AscentUI>();
            if (ascentUI == null)
            {
                Plugin.Log.LogError("[DeathLog] Could not find AscentUI parent to attach!");
                return;
            }
            
            var go = new GameObject("DeathLog UI", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
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
                if (ourText != null)
                {
                    ourText.font = ascentText.font;
                    Plugin.Log.LogInfo($"[DeathLog] Copied font from AscentUI: {ascentText.font.name}");
                }
            }
            else
            {
                Plugin.Log.LogWarning("[DeathLog] Could not find font on AscentUI, using default");
            }

            go.AddComponent<DeathLog>();
            rt.SetAsLastSibling();
        }
        else
        {
            Destroy(Instance.gameObject);
            Instance = null;
            InitiateDeathLog();
        }
    }
}