using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Hide_and_PEAK;

public static class MatchHistoryManager
{
    private static readonly string savePath = Path.Combine(Application.persistentDataPath, "match_history.json");
    private static List<MatchResult> _matches = new List<MatchResult>();

    public static List<MatchResult> Matches
    {
        get
        {
            if (_matches == null)
            {
                LoadMatches();
            }
            return _matches;
        }
    }

    public static void SaveMatch(MatchResult match)
    {
        _matches.Add(match);
        string json = JsonUtility.ToJson(new MatchListWrapper { Matches = _matches }, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"[MatchHistory] Saved match to {savePath}");
    }

    private static void LoadMatches()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            var wrapper = JsonUtility.FromJson<MatchListWrapper>(json);
            _matches = wrapper.Matches ?? new List<MatchResult>();
        }
        else
        {
            _matches = new List<MatchResult>();
        }
    }

    [System.Serializable]
    private class MatchListWrapper
    {
        public List<MatchResult> Matches;
    }
}