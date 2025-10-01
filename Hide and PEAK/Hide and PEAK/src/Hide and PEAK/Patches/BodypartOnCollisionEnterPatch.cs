using System;
using HarmonyLib;
using HideAndSeekMod;
using Photon.Pun;
using UnityEngine;

namespace Hide_and_PEAK.Patches;

public class BodypartOnCollisionEnterPatch
{
    private static String _lastTouched = "";
    
    [HarmonyPatch(typeof(Bodypart), nameof(Bodypart.OnCollisionEnter))]
    [HarmonyPostfix]
    static void Postfix(Bodypart __instance, Collision collision)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (HideAndSeekManager.Instance == null || !HideAndSeekManager.Instance.IsGameActive) return;
        __instance.character.view.Controller.CustomProperties.TryGetValue("Team", out var teamVar);
        if ((Team)teamVar == Team.Hider) return;
        if (__instance.character.input.useSecondaryIsPressed)
        {
            Transform current = collision.collider.transform;
            while (current != null)
            {
                if (current.name.StartsWith("Character"))
                {
                    Character character = current.GetComponent<Character>();
                    if (character.characterName.Equals(Character.localCharacter.characterName)) return;
                    Plugin.Log.LogInfo($"Player: {character.characterName} touched by {__instance.character.characterName} of team {(Team) teamVar}");
                    character.view.Controller.CustomProperties.TryGetValue("Team", out teamVar);
                    Plugin.Log.LogInfo($"Player: {character.characterName}'s team: {(Team) teamVar}");
                    if ((Team)teamVar == Team.Hider && !character.characterName.Equals(_lastTouched))
                    {
                        _lastTouched = character.characterName;
                        HideAndSeekManager.Instance.CatchHider(__instance.character, character);
                    }
                    return;
                }
                current = current.parent;
            }
        }
    }
}