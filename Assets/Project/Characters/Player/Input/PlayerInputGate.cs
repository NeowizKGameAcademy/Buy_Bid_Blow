using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Characters
{
    // Whether gameplay input (move, look, jump, interact, seat and auction keys) is allowed right now.
    // Screens that take over the keyboard register a blocker, so input code needs no reference to UI.
    public static class PlayerInputGate
    {
        private static readonly List<Func<bool>> blockers = new List<Func<bool>>();

        public static bool Blocked
        {
            get
            {
                foreach (var blocker in blockers)
                    if (blocker()) return true;
                return false;
            }
        }

        // Holding Left Alt frees the cursor without blocking gameplay input.
        public static bool FreeCursorHeld => Keyboard.current != null && Keyboard.current.leftAltKey.isPressed;

        // Pass a static method so the same blocker is never added twice (domain reload is disabled).
        public static void AddBlocker(Func<bool> blocker)
        {
            if (!blockers.Contains(blocker))
                blockers.Add(blocker);
        }

        public static void RemoveBlocker(Func<bool> blocker) => blockers.Remove(blocker);
    }
}
