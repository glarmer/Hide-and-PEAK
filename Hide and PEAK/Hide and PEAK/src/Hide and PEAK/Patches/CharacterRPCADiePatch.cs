using HarmonyLib;
using UnityEngine;

namespace Hide_and_PEAK.Patches;

public class CharacterRPCADiePatch
{
    [HarmonyPatch(typeof(Character), "RPCA_Die")]
    [HarmonyPrefix]
    static bool Prefix(Character __instance)
    {
        Plugin.Log.LogInfo($"RPCA_Die Patch: Started...");
        __instance.view.Controller.CustomProperties.TryGetValue("Team", out var team);
        
        if ((Team)team == Team.Seeker && __instance.characterName.Equals(Character.localCharacter.characterName))
        {
            Plugin.Log.LogInfo($"RPCA_Die Patch: Saving death position...");
            Plugin.Instance.lastDeathPosition =
                Character.localCharacter.GetBodypart(BodypartType.Hip).transform.position +
                Character.localCharacter.transform.up;
            Plugin.Log.LogInfo($"RPCA_Die Patch: Death position saved as " + Plugin.Instance.lastDeathPosition);
        }

        return true;
    }
}