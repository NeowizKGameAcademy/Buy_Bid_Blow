using System.Collections;
using BuyBidBlow.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    // The local player's motor in the furnished hall: the twin staircases (z 3..5) lead from the floor
    // up to the mezzanine at 4 m, climbing outward along X.
    public sealed class HallStairsTests
    {
        readonly HostedMatch match = new HostedMatch();

        [UnityTearDown]
        public IEnumerator TearDown() => match.Stop();

        [UnityTest]
        public IEnumerator Staircases_LeadUpToTheMezzanine([Values(-1, 1)] int side)
        {
            yield return match.Start();
            var motor = Object.FindFirstObjectByType<CharacterMotor>();
            // Drive the motor directly; the player's own input would overwrite the intent every frame.
            motor.GetComponent<PlayerInputReader>().enabled = false;
            var outward = Vector3.right * side;
            motor.Teleport(new Vector3(3.5f * side, .05f, 4), Quaternion.LookRotation(outward));
            for (float end = Time.time + 3.5f; Time.time < end;)
            {
                motor.SetIntent(new CharacterIntent { Move = outward });
                yield return null;
            }
            motor.SetIntent(default);
            var p = motor.transform.position;
            Assert.Greater(p.y, 3.8f, "reached the mezzanine level, stopped at " + p);
            Assert.Greater(Mathf.Abs(p.x), 10f, "walked off the top of the stairs");
            Assert.IsTrue(motor.IsGrounded, "standing, not falling");
        }

        // The mezzanine between the staircases is open floor up to the auctioneer behind the rostrum
        // (the railing's exported bounding plates once made a false floor and ceiling there).
        [UnityTest]
        public IEnumerator Mezzanine_WalksUpToTheAuctioneer()
        {
            yield return match.Start();
            var motor = Object.FindFirstObjectByType<CharacterMotor>();
            motor.GetComponent<PlayerInputReader>().enabled = false;
            // From the mezzanine behind the right staircase, straight at the auctioneer.
            var auctioneer = GameObject.Find("Auctioneer Pig").transform.position;
            var start = new Vector3(6, 4.05f, 7.5f);
            var toward = Vector3.ProjectOnPlane(auctioneer - start, Vector3.up).normalized;
            motor.Teleport(start, Quaternion.LookRotation(toward));
            float closest = float.MaxValue;
            for (float end = Time.time + 2f; Time.time < end;)
            {
                motor.SetIntent(new CharacterIntent { Move = toward });
                yield return null;
                closest = Mathf.Min(closest, Vector3.ProjectOnPlane(motor.transform.position - auctioneer, Vector3.up).magnitude);
            }
            motor.SetIntent(default);
            var p = motor.transform.position;
            Assert.Less(closest, 1.2f, "came up beside the auctioneer, ended at " + p);
            Assert.That(p.y, Is.EqualTo(4f).Within(.05f), "on the mezzanine floor itself");
        }
    }
}
