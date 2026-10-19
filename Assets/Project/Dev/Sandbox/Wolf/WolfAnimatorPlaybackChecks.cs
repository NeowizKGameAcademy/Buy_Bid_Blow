using System;
using System.Collections;
using System.IO;
using System.Linq;
using BuyBidBlow.Characters;
using UnityEngine;

namespace BuyBidBlow.Dev
{
    // Invoked by the editor playback check command; never included in a player build.
    public sealed class WolfAnimatorPlaybackChecks : MonoBehaviour
    {
        public BBBWolfCharacter[] characters;
        private int checks;
        private const string Result = "Library/WolfAnimatorPlayback.result";

        private IEnumerator Start()
        {
            // Exceptions from coroutine assertions are routed to the report.
            var routine = CheckAll();
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext()) break;
                    current = routine.Current;
                }
                catch (Exception e)
                {
                    File.WriteAllText(Result, "FAIL: " + e);
                    Debug.LogException(e);
                    yield break;
                }
                yield return current;
            }
            File.WriteAllText(Result, "PASS: " + checks + " playback assertions across sandbox and production prefab.");
            Debug.Log("Wolf Animator playback checks passed: " + checks);
        }

        private IEnumerator CheckAll()
        {
            yield return null;
            foreach (var character in characters)
            {
                Require(character && character.animator && character.animator.isInitialized, "Animator not initialized");
                var animator = character.animator;
                Require(animator.layerCount == 2, "Wrong layer count");
                var head = character.model.GetComponentsInChildren<Transform>(true)
                    .First(t => t.name == "mixamorig:Head");
                for (int i = 1; i <= 10; i++)
                {
                    character.PlayAction((BBBWolfCharacter.Action)i);
                    yield return new WaitForSecondsRealtime(.25f);
                    var info = animator.GetCurrentAnimatorStateInfo(0);
                    string[] states = { "", "Wolf_Walk", "Wolf_RaiseArms", "Wolf_Wave", "Wolf_Sit", "Wolf_Bid",
                        "Mixamo_Walking", "Mixamo_Defeat", "Mixamo_HandRaising", "Mixamo_Flair", "Mixamo_HipHopDancing" };
                    Require(info.IsName("Body." + states[i]), "Body state not entered: " + states[i]);
                    Require(character.paddle.activeSelf == (i == 5), "Incorrect paddle visibility: " + states[i]);
                    float time = info.normalizedTime;
                    yield return new WaitForSecondsRealtime(.1f);
                    Require(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > time, "Clip did not advance: " + states[i]);
                }

                character.PlayAction(BBBWolfCharacter.Action.MixamoWalking);
                yield return new WaitForSecondsRealtime(.25f);
                Quaternion before = head.localRotation;
                yield return new WaitForSecondsRealtime(.18f);
                Require(Quaternion.Angle(before, head.localRotation) > .001f, "Animation does not drive the skeleton");

                string[] faces = { "Happy", "Suspicious", "Shocked", "Blank" };
                for (int i = 0; i < 4; i++)
                {
                    character.SetExpression((BBBWolfCharacter.Face)i);
                    yield return new WaitForSecondsRealtime(.1f);
                    Require(animator.GetCurrentAnimatorStateInfo(1).IsName("Expression." + faces[i]), "Expression state missing");
                    Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Body.Mixamo_Walking"), "Expression interrupted body");
                    for (int j = 0; j < 4; j++)
                    {
                        var face = character.model.GetComponentsInChildren<Transform>(true).First(t => t.name == faces[j]);
                        Require(face.gameObject.activeSelf == (i == j), "Expression visibility wrong: " + faces[j]);
                    }
                }

                character.SetMovementSpeed(0);
                character.StopAction();
                yield return new WaitForSecondsRealtime(.25f);
                Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Body.Locomotion"), "Stop did not return to locomotion");
                Require(!character.paddle.activeSelf, "Paddle not hidden after Stop");
                character.SetMovementSpeed(1);
                yield return new WaitForSecondsRealtime(.1f);
                Require(animator.GetCurrentAnimatorClipInfo(0).Any(c => c.clip.name == "Mixamo_Walking" && c.weight > .9f),
                    "Locomotion does not select walking");

                character.PlayAction(BBBWolfCharacter.Action.Bid);
                yield return new WaitForSecondsRealtime(.25f);
                var bid = animator.GetCurrentAnimatorClipInfo(0).First(c => c.clip.name == "Wolf_Bid").clip;
                yield return new WaitForSecondsRealtime(bid.length + .3f);
                Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Body.Locomotion"), "Bid did not return automatically");
                Require(!character.paddle.activeSelf, "Paddle remains after Bid");

                character.appearance.SetOutfit(2, 1);
                character.appearance.SetTieColor(Color.blue);
                yield return new WaitForSecondsRealtime(.2f);
                var renderers = character.model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Require(renderers.First(r => r.name == "Wolf_Upper_Vest").enabled, "Vest not enabled");
                Require(!renderers.First(r => r.name == "Wolf_Upper_Suit").enabled, "Suit not disabled");
                Require(renderers.First(r => r.name == "Wolf_Lower_Shorts").enabled, "Shorts not enabled");
                var block = new MaterialPropertyBlock();
                renderers.First(r => r.name == "Wolf_Tie").GetPropertyBlock(block);
                Require(block.GetColor(Shader.PropertyToID("_BaseColor")) == Color.blue, "Tie color not applied");
                character.appearance.SetOutfit(0, 0);
                character.appearance.SetTieColor(new Color(.54f, .16f, .22f));
                character.SetExpression(BBBWolfCharacter.Face.BlankBluff);
                character.SetMovementSpeed(0);
                character.StopAction();
            }
        }

        private void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
    }
}
