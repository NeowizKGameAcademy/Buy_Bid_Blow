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
    // The cursor comes back locked after Esc closes the trade panel or stands the player up, even when
    // something else (the Editor does this on Esc) releases the lock a frame later.
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

            // Trade panel: open shows the cursor, Esc closes it and the lock comes back.
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

            yield return Press(Key.Escape);
            Assert.IsFalse(panel.IsOpen, "Esc closed the panel");
            yield return ReleasedLikeTheEditor();
            Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState, "locked again after closing the panel");
            Assert.IsFalse(Cursor.visible, "cursor hidden after closing the panel");

            // Seat: Esc stands up and keeps the cursor locked.
            seats.RequestSit(Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).First(s => s.auction));
            yield return HostedMatch.Until(() => seating.IsSeated, "seated");
            yield return Press(Key.Escape);
            yield return HostedMatch.Until(() => !seating.IsSeated, "Esc stood the player up");
            yield return ReleasedLikeTheEditor();
            Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState, "locked again after standing up");
            Assert.IsFalse(Cursor.visible, "cursor hidden after standing up");

            // Esc with nothing open still hands the cursor to the player, and it stays free.
            yield return Press(Key.Escape);
            yield return null; yield return null;
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "Esc while walking frees the cursor");
            Assert.IsTrue(Cursor.visible, "cursor shown after Esc while walking");
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
