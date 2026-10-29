using System.Collections;
using System.Collections.Generic;
using BuyBidBlow.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    // CharacterMotor on generated geometry: a floor, a wall and two ledges, away from the scene origin.
    public sealed class CharacterMotorTests
    {
        static readonly Vector3 Origin = new Vector3(500, 0, 500);
        readonly List<GameObject> spawned = new List<GameObject>();
        CharacterMotor motor;

        [SetUp]
        public void SetUp()
        {
            Box("Floor", new Vector3(0, -.5f, 0), new Vector3(40, 1, 40));
            var go = new GameObject("Motor Test Body"); spawned.Add(go);
            go.transform.position = Origin;
            var capsule = go.AddComponent<CapsuleCollider>(); capsule.radius = .3f; capsule.height = 1.8f; capsule.center = Vector3.up * .9f;
            go.AddComponent<Rigidbody>();
            motor = go.AddComponent<CharacterMotor>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown() { foreach (var g in spawned) Object.Destroy(g); spawned.Clear(); }

        [UnityTest]
        public IEnumerator FlatGround_WalksAtMoveSpeed()
        {
            yield return Drive(Vector3.forward, 1f);
            float moved = motor.transform.position.z - Origin.z;
            Assert.That(moved, Is.InRange(4.2f, 5.1f), "about a second of walking");
            Assert.That(motor.transform.position.y, Is.EqualTo(0).Within(.05f), "stays on the floor");
            Assert.IsTrue(motor.IsGrounded);
        }

        [UnityTest]
        public IEnumerator Wall_StopsAndSlidesAlong()
        {
            Box("Wall", new Vector3(0, 1.5f, 2), new Vector3(20, 3, .4f));
            yield return Drive((Vector3.forward + Vector3.right).normalized, 1.2f);
            var p = motor.transform.position - Origin;
            Assert.Less(p.z, 1.8f - .29f, "does not pass the wall");
            Assert.Greater(p.x, 2f, "slides along it");
            Assert.That(p.y, Is.EqualTo(0).Within(.05f), "does not climb it");
        }

        [UnityTest]
        public IEnumerator LowLedge_StepsUp()
        {
            Box("Step", new Vector3(0, .15f, 4), new Vector3(6, .3f, 4));
            yield return Drive(Vector3.forward, 1.2f);
            var p = motor.transform.position - Origin;
            Assert.Greater(p.z, 2.5f, "got onto the step");
            Assert.That(p.y, Is.EqualTo(.3f).Within(.05f), "standing on top of it");
        }

        [UnityTest]
        public IEnumerator HighLedge_Blocks()
        {
            Box("Ledge", new Vector3(0, .3f, 4), new Vector3(6, .6f, 4));
            yield return Drive(Vector3.forward, 1.2f);
            var p = motor.transform.position - Origin;
            Assert.Less(p.z, 2f - .29f, "stopped at the ledge");
            Assert.That(p.y, Is.EqualTo(0).Within(.05f));
        }

        [UnityTest]
        public IEnumerator Jump_RisesAndLands()
        {
            int jumped = 0, landed = 0; float peak = 0;
            motor.Jumped += () => jumped++; motor.Landed += () => landed++;
            yield return null;
            motor.SetIntent(new CharacterIntent { Jump = true });
            for (float end = Time.time + 1.5f; Time.time < end;) { peak = Mathf.Max(peak, motor.transform.position.y); yield return null; }
            Assert.AreEqual(1, jumped, "one jump");
            Assert.That(peak, Is.InRange(1f, 1.35f), "about the configured height");
            Assert.AreEqual(1, landed, "landed again");
            Assert.IsTrue(motor.IsGrounded);
        }

        [UnityTest]
        public IEnumerator Block_StopsUntilUnblocked()
        {
            var source = new object();
            motor.Block(source);
            yield return Drive(Vector3.forward, .5f);
            Assert.That(motor.transform.position.z, Is.EqualTo(Origin.z).Within(.001f), "blocked");
            motor.Unblock(source);
            yield return Drive(Vector3.forward, .5f);
            Assert.Greater(motor.transform.position.z, Origin.z + 1, "moves again");
        }

        [UnityTest]
        public IEnumerator Impulse_PushesAndFades()
        {
            yield return null;
            motor.AddImpulse(Vector3.right * 6);
            yield return Drive(Vector3.zero, 1f);
            float x = motor.transform.position.x - Origin.x;
            Assert.Greater(x, .5f, "pushed");
            Assert.Less(motor.Velocity.magnitude, .01f, "fades out");
        }

        IEnumerator Drive(Vector3 direction, float seconds)
        {
            for (float end = Time.time + seconds; Time.time < end;)
            {
                motor.SetIntent(new CharacterIntent { Move = direction });
                yield return null;
            }
            motor.SetIntent(default);
        }

        void Box(string name, Vector3 center, Vector3 size)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; spawned.Add(g);
            g.transform.position = Origin + center; g.transform.localScale = size;
            Physics.SyncTransforms();
        }
    }
}
