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
    }
}
