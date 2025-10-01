using System.Collections.Generic;

namespace Hide_and_PEAK;

[System.Serializable]
public class MatchResult
{
    public string Date;
    public float Duration;
    public List<PlayerResult> PlayerResults = new List<PlayerResult>();
}