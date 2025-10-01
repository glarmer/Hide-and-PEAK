using HarmonyLib;
using UnityEngine;

namespace Hide_and_PEAK.Patches;

public class VoiceObscuranceFilterPatch
{
    [HarmonyPatch(typeof(VoiceObscuranceFilter), "Update")]
    [HarmonyPrefix]
    private static bool Prefix(VoiceObscuranceFilter __instance)
    {
        if (!Plugin.ConfigurationHandler.ConfigSeekerVoice.Value) return true;
        
        var remoteCharacter = __instance.GetComponentInParent<Character>();
        if (remoteCharacter == null || remoteCharacter.view == null) return true;
        
        var localCharacter = Character.localCharacter;
        if (localCharacter == null || localCharacter.view == null) return true;
        
        if (remoteCharacter.name.Equals(localCharacter.name)) return true;
        
        remoteCharacter.view.Controller.CustomProperties.TryGetValue("Team", out var remoteTeamObj);
        localCharacter.view.Controller.CustomProperties.TryGetValue("Team", out var localTeamObj);
        
        var remoteIsSeeker = remoteTeamObj != null && (Team)remoteTeamObj == Team.Seeker;
        var localIsSeeker = localTeamObj != null && (Team)localTeamObj == Team.Seeker;
        
        if (remoteIsSeeker && localIsSeeker)
        {
            if (__instance.lowPass != null) __instance.lowPass.cutoffFrequency = 22000f;
        
            if (__instance.echo != null)
            {
                __instance.echo.wetMix = 0f;
                __instance.echo.dryMix = 1f;
                __instance.echo.decayRatio = 0f;
                __instance.echo.delay = 10f;
            }
        
            if (__instance.reverb != null) __instance.reverb.reverbLevel = 0f;
            
            var source = __instance.GetComponent<AudioSource>();
            if (source != null)
            {
                source.spatialBlend = 0f;
                source.maxDistance = 10000f;
                source.minDistance = 0f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.dopplerLevel = 0f;
            }
        
            return false;
        }
        
        return true;
    }
}