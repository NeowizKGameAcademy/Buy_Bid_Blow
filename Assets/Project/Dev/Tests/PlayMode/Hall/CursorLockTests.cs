using System.Collections;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Props;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Market;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    // Q closes the trade panel and stands the player up with the cursor locked again, even when something
    // else (the Editor's Game view, for one) drops the lock; holding Left Alt frees it until released.
    public sealed class CursorLockTests
    {
        readonly HostedMatch match = new HostedMatch();
        Keyboard keyboard;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            yield return match.Stop();
        }

        [UnityTest]
        public IEnumerator CursorRelocksAfterClosingTheTradePanelOrStandingUp()
        {
            // Batch mode has no game window, so the OS cursor can't be locked there.
            if (Application.isBatchMode) Assert.Ignore("Needs the Editor's game window (run without -batchmode).");
            yield return match.Start();
            keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            var interaction = Object.FindFirstObjectByType<PlayerInteraction>();
            var detect = Object.FindFirstObjectByType<DetectObject>();
            var panel = Object.FindFirstObjectByType<TradePanelController>(FindObjectsInactive.Include);
            var seats = Object.FindFirstObjectByType<SeatController>();
            var seating = seats.GetComponent<PlayerSeating>();
            yield return null;
            Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState, "locked while playing");

            // Trade panel: open shows the cursor, Q closes it and the lock comes back.
            var staff = GameObject.Find("Staff_STRAW_Yellow");
            float eye = detect.transform.position.y - interaction.transform.position.y;
            detect.transform.SetParent(null, true);
            var eyePos = staff.transform.position + staff.transform.forward * 2.2f + Vector3.up * eye;
            detect.transform.SetPositionAndRotation(eyePos, Quaternion.LookRotation(staff.GetComponent<Collider>().bounds.center - eyePos));
            yield return null; yield return null;
            interaction.Interact();
            Assert.IsTrue(panel.IsOpen, "trade panel open");
            yield return null;
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "cursor free while the panel is open");
            Assert.IsTrue(Cursor.visible, "cursor shown while the panel is open");

            yield return Press(Key.Q);
            Assert.IsFalse(panel.IsOpen, "Q closed the panel");
            yield return ReleasedLikeTheEditor();
            Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState, "locked again after closing the panel");
            Assert.IsFalse(Cursor.visible, "cursor hidden after closing the panel");

            // Seat: Q stands up and keeps the cursor locked.
            seats.RequestSit(Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).First(s => s.auction));
            yield return HostedMatch.Until(() => seating.IsSeated, "seated");
            yield return Press(Key.Q);
            yield return HostedMatch.Until(() => !seating.IsSeated, "Q stood the player up");
            yield return ReleasedLikeTheEditor();
            Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState, "locked again after standing up");
            Assert.IsFalse(Cursor.visible, "cursor hidden after standing up");

            // Holding Left Alt frees the cursor; releasing it locks again.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftAlt));
            yield return null; yield return null;
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "Alt held frees the cursor");
            Assert.IsTrue(Cursor.visible, "cursor shown while Alt is held");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null; yield return null;
            Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState, "locked again after releasing Alt");
            Assert.IsFalse(Cursor.visible, "cursor hidden after releasing Alt");
        }

        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        // What the Editor's Game view does on Esc: drops the lock after the game has handled the key.
        static IEnumerator ReleasedLikeTheEditor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            yield return null; yield return null;
        }
    }
}
