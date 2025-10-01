using HideAndSeekMod;
using TMPro;
using UnityEngine;

namespace Hide_and_PEAK.UI;

public class PlayerStats : MonoBehaviour
{
    private TMP_Text _text;
    private float _deathTime = 0;
    public float _currentTime = 0;
    public float _startTime = 0;
    public static PlayerStats Instance;

    public string _currentTimeString = "";

    private string FormatTime(float totalSeconds)
    {
        int seconds = Mathf.FloorToInt(totalSeconds);
        return $"{seconds / 3600}:{(seconds % 3600) / 60:00}:{seconds % 60:00}";
    }

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Plugin.Log.LogWarning("[PlayerStats] Instance already exists, destroying old one.");
            Destroy(Instance);
            Instance = this;
            return;
        }
        _text = GetComponent<TMP_Text>();
        if (_text == null)
        {
            Plugin.Log.LogError("[PlayerStats] No TMP_Text found on object!");
            enabled = false;
            return;
        }
        _text.autoSizeTextContainer = true;
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        _text.alignment = TextAlignmentOptions.Top;
        _text.color = Color.white;
        _text.fontSize = 34f;
        _text.outlineColor = Color.black;
        _text.outlineWidth = 0.06f;
        _text.lineSpacing = -30f;
        ResetTimer();
    }

    public void ResetStats()
    {
        ResetTimer();
    }

    private void ResetTimer()
    {
        _startTime = Time.time;
    }
    
    public void SetCurrentTimeFromHost(float currentTimeFromHost)
    {
        
        _startTime = Time.time - currentTimeFromHost;
        _currentTime = currentTimeFromHost;
        _currentTimeString = FormatTime(_currentTime);

        Plugin.Log.LogInfo($"[PlayerStats] Synced _currentTime from host: {_currentTimeString}");
    }

    private void Update()
    {
        if (_text == null || Character.localCharacter == null) return;

        CharacterStats stats = Character.localCharacter.refs.stats;
        _currentTime = Time.time - _startTime;

        _currentTimeString = FormatTime(_currentTime);
        if (HideAndSeekManager.Instance.IsGameActive)
        {
            
            _text.text = $"{_currentTimeString}\n{stats.heightInMeters}m";
        }
        else
        {
            _text.text = $"{stats.heightInMeters}m";
        }
    }

    public static void InitiatePlayerStats()
    {
        if (Instance == null)
        {
            var ascentUI = FindAnyObjectByType<AscentUI>();
            if (ascentUI == null)
            {
                Plugin.Log.LogError("[Plugin] Could not find AscentUI to clone!");
                return;
            }

            RectTransform playerStatsTransform = Instantiate(ascentUI.transform, ascentUI.transform.parent) as RectTransform;
            playerStatsTransform.name = "Player Stats UI";

            Destroy(playerStatsTransform.GetComponent<AscentUI>());
            playerStatsTransform.gameObject.AddComponent<PlayerStats>();
            
            playerStatsTransform.anchorMin = playerStatsTransform.anchorMax = new Vector2(0.5f, 1f);
            playerStatsTransform.pivot = new Vector2(0.5f, 1f);
            playerStatsTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            Instance.ResetStats();
        }
    }
}