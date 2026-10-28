using UnityEngine;
using UnityEngine.InputSystem;

namespace DallasDash.Core
{
    /// Swipe (touch) and arrow/WASD (keyboard) mapped to the four runner actions. Polled once per frame.
    public static class RiderInput
    {
        public enum Action { None, Left, Right, Jump, Roll }

        const float SwipeMinPixels = 50f;
        static Vector2 touchStart;
        static bool tracking;
        static int lastFrame = -1;
        static Action lastAction;
        /// Replaces device input when set (screenshot autopilot).
        public static System.Func<Action> Override;

        public static Action Poll()
        {
            if (Time.frameCount == lastFrame) return lastAction;
            lastFrame = Time.frameCount;
            lastAction = Read();
            return lastAction;
        }

        static Action Read()
        {
            if (Override != null) return Override();
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) return Action.Left;
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) return Action.Right;
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) return Action.Jump;
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) return Action.Roll;
            }

            var ts = Touchscreen.current;
            if (ts == null) return Action.None;
            var t = ts.primaryTouch;
            if (t.press.wasPressedThisFrame)
            {
                touchStart = t.position.ReadValue();
                tracking = true;
            }
            else if (tracking && t.press.isPressed)
            {
                Vector2 d = t.position.ReadValue() - touchStart;
                if (d.magnitude < SwipeMinPixels) return Action.None;
                tracking = false;
                if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x < 0 ? Action.Left : Action.Right;
                return d.y > 0 ? Action.Jump : Action.Roll;
            }
            else if (t.press.wasReleasedThisFrame)
            {
                tracking = false;
            }
            return Action.None;
        }
    }
}
