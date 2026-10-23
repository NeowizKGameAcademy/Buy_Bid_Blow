using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Dev
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class HallWalkthrough : MonoBehaviour
    {
        public Camera view;
        float pitch;
        float verticalSpeed;
        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || view == null) return;
            if (mouse.rightButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
            if (keyboard.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
            if (Cursor.lockState != CursorLockMode.Locked) return;
            var delta = mouse.delta.ReadValue() * .10f;
            transform.Rotate(0, delta.x, 0);
            pitch = Mathf.Clamp(pitch - delta.y, -85, 85);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            var controller = GetComponent<CharacterController>();
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            var velocity = (transform.right * x + transform.forward * z).normalized * (keyboard.leftShiftKey.isPressed ? 6 : 3);
            velocity.y = verticalSpeed;
            controller.Move(velocity * Time.deltaTime);
        }
        void OnDisable() { Cursor.lockState = CursorLockMode.None; }
    }
}
