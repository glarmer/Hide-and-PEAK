using HarmonyLib;
using HideAndSeekMod;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hide_and_PEAK.Patches;

public class CharacterMovementUpdatePatch
    {
        private static bool _isFrozen = false;
        private static float _frozenHeight;
        private static float _frozenStamina = 0f;

        [HarmonyPatch(typeof(CharacterMovement), "Update")]
        [HarmonyPostfix]
        static void Postfix(CharacterMovement __instance)
        {
            Character character = Character.localCharacter;
            if (character == null || !__instance.character.IsLocal || character.refs?.ragdoll == null) return;

            if (Plugin.ConfigurationHandler.FreezeAction != null && 
                Plugin.ConfigurationHandler.FreezeAction.WasPerformedThisFrame())
            {
                _isFrozen = !_isFrozen;
                if (_isFrozen)
                {
                    _frozenHeight = character.GetBodypart(BodypartType.Torso).Rig.position.y;
                    _frozenStamina = character.data.currentStamina;
                }

                Plugin.Log.LogInfo($"[CharacterFreezePatch] Freeze toggled: {_isFrozen}");

                
                HideAndSeekManager.Instance?.View.RPC(
                    "RPC_SetFrozenState",
                    Photon.Pun.RpcTarget.All,
                    character.refs.view.ViewID,
                    _isFrozen,
                    _frozenHeight,
                    _frozenStamina
                );
            }

            if (!_isFrozen) return;

            character.data.currentStamina = _frozenStamina;

            character.input.movementInput = Vector2.zero;
            character.input.jumpIsPressed = false;
            character.input.crouchIsPressed = false;

            character.GetBodypart(BodypartType.Torso).Rig.position =
                character.GetBodypart(BodypartType.Torso).Rig.position with { y = _frozenHeight };

            foreach (Bodypart part in character.refs.ragdoll.partList)
            {
                if (part?.Rig != null)
                {
                    part.Rig.linearVelocity = Vector3.zero;
                    part.Rig.angularVelocity = Vector3.zero;
                }
            }

            character.data.isGrounded = true;
            character.data.sinceGrounded = 0f;
            character.data.sinceJump = 0f;
        }
    }