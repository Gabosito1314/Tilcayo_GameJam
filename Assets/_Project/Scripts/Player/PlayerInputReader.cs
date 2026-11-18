using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tilcayo.Player
{
    /// <summary>
    /// Lector de entrada agnóstico que soporta tanto el nuevo Input System como el sistema Legacy.
    /// </summary>
    public static class PlayerInputReader
    {
        public static float GetHorizontal()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                float move = 0f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move += 1f;
                return move;
            }
#endif
            try
            {
                return Input.GetAxisRaw("Horizontal");
            }
            catch
            {
                return 0f;
            }
        }

        public static bool GetJumpDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                return kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
            }
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetJumpHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                return kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed;
            }
#endif
            try
            {
                return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetAbilityDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                return kb.eKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame;
            }
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetPauseDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                return kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame;
            }
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
            }
            catch
            {
                return false;
            }
        }
    }
}
