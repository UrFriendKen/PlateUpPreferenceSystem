using Controllers;
using HarmonyLib;
using Kitchen;
using Kitchen.Modules;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PreferenceSystem.Patches
{
    [HarmonyPatch]
    static class SelectElement_Patch
    {
        const float LOWER_THRESHOLD = 0.5f;
        const float MAX_SPEED = 5f;
        const float MIN_DELAY = 0.05f;
        static float _progress = 0f;
        static float _heldTime = 0f;

        [HarmonyPatch(typeof(SelectElement), "HandleInteraction")]
        [HarmonyPrefix]
        static void HandleInteraction_Prefix(ref InputState state)
        {
            List<Orientation> scrollDirection = new List<Orientation>();
            bool isPressed = false;

            if (state.MenuLeft == ButtonState.Pressed || state.MenuLeft == ButtonState.Held)
            {
                isPressed = state.MenuLeft == ButtonState.Pressed;
                scrollDirection.Add(Orientation.Left);
            }
            if (state.MenuRight == ButtonState.Pressed || state.MenuRight == ButtonState.Held)
            {
                isPressed = state.MenuRight == ButtonState.Pressed;
                scrollDirection.Add(Orientation.Right);
            }
            if (state.MenuUp == ButtonState.Pressed || state.MenuUp == ButtonState.Held)
            {
                isPressed = state.MenuUp == ButtonState.Pressed;
                scrollDirection.Add(Orientation.Up);
            }
            if (state.MenuDown == ButtonState.Pressed || state.MenuDown == ButtonState.Held)
            {
                isPressed = state.MenuDown == ButtonState.Pressed;
                scrollDirection.Add(Orientation.Down);
            }

            if (scrollDirection.Count != 1)
            {
                _heldTime = 0f;
                _progress = 0f;
                return;
            }

            if (isPressed)
            {
                _heldTime = 0f;
                _progress = 0f;
            }

            if (isPressed || (_heldTime > LOWER_THRESHOLD && _progress > MAX_SPEED / _heldTime * MIN_DELAY))
            {
                switch (scrollDirection.First())
                {
                    case Orientation.Left:
                        state.MenuLeft = ButtonState.Pressed;
                        break;
                    case Orientation.Right:
                        state.MenuRight = ButtonState.Pressed;
                        break;
                    case Orientation.Up:
                        state.MenuUp = ButtonState.Pressed;
                        break;
                    case Orientation.Down:
                        state.MenuDown = ButtonState.Pressed;
                        break;
                }
                _progress = 0f;
            }
            _progress += Time.unscaledDeltaTime;
            _heldTime = Mathf.Clamp(_heldTime + Time.unscaledDeltaTime, 0, MAX_SPEED);
        }
    }
}
