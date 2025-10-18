using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HideAndSeekMod;
using UnityEngine;

namespace Hide_and_PEAK.Patches;

public class VoiceObscuranceFilterPatch
{
    private static readonly ConditionalWeakTable<VoiceObscuranceFilter, OriginalFilterState> OriginalStates = new();

    private class OriginalFilterState
    {
        public float LowPassCutoff;
        public float EchoWetMix;
        public float EchoDryMix;
        public float EchoDecayRatio;
        public float EchoDelay;
        public float ReverbLevel;

        public float SpatialBlend;
        public float MaxDistance;
        public float MinDistance;
        public AudioRolloffMode RolloffMode;
        public float DopplerLevel;
    }

    [HarmonyPatch(typeof(VoiceObscuranceFilter), "Update")]
    [HarmonyPrefix]
    private static bool Prefix(VoiceObscuranceFilter __instance)
    {
        
        
        if (!Plugin.ConfigurationHandler.ConfigSeekerVoice.Value) return true;
        if (!HideAndSeekManager.Instance.IsGameActive)
        {
            ResetFilter(__instance);
            return true;
        }

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
            if (!OriginalStates.TryGetValue(__instance, out _))
            {
                var src = __instance.GetComponent<AudioSource>();
                var state = new OriginalFilterState
                {
                    LowPassCutoff = __instance.lowPass != null ? __instance.lowPass.cutoffFrequency : 22000f,
                    EchoWetMix = __instance.echo != null ? __instance.echo.wetMix : 0f,
                    EchoDryMix = __instance.echo != null ? __instance.echo.dryMix : 1f,
                    EchoDecayRatio = __instance.echo != null ? __instance.echo.decayRatio : 0f,
                    EchoDelay = __instance.echo != null ? __instance.echo.delay : 10f,
                    ReverbLevel = __instance.reverb != null ? __instance.reverb.reverbLevel : 0f,
                    SpatialBlend = src != null ? src.spatialBlend : 1f,
                    MaxDistance = src != null ? src.maxDistance : 500f,
                    MinDistance = src != null ? src.minDistance : 1f,
                    RolloffMode = src != null ? src.rolloffMode : AudioRolloffMode.Logarithmic,
                    DopplerLevel = src != null ? src.dopplerLevel : 1f
                };
                OriginalStates.Add(__instance, state);
            }
            
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
    
    private static void ResetFilter(VoiceObscuranceFilter __instance)
    {
        if (!OriginalStates.TryGetValue(__instance, out var state))
            return;

        if (__instance.lowPass != null)
            __instance.lowPass.cutoffFrequency = state.LowPassCutoff;

        if (__instance.echo != null)
        {
            __instance.echo.wetMix = state.EchoWetMix;
            __instance.echo.dryMix = state.EchoDryMix;
            __instance.echo.decayRatio = state.EchoDecayRatio;
            __instance.echo.delay = state.EchoDelay;
        }

        if (__instance.reverb != null)
            __instance.reverb.reverbLevel = state.ReverbLevel;

        var src = __instance.GetComponent<AudioSource>();
        if (src != null)
        {
            src.spatialBlend = state.SpatialBlend;
            src.maxDistance = state.MaxDistance;
            src.minDistance = state.MinDistance;
            src.rolloffMode = state.RolloffMode;
            src.dopplerLevel = state.DopplerLevel;
        }

        OriginalStates.Remove(__instance);
    }
}