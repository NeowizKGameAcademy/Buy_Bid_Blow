using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Dev
{
    public sealed class PigAnimationMuseum : MonoBehaviour
    {
        public Animator[] exhibits;
        public string[] labels;
        public Transform[] viewpoints;
        public Camera museumCamera;
        public Transform overview;
        private float yaw, pitch;
        private bool paused;
        private float playbackSpeed = 1;
        private Vector2 scroll;
        private bool showPanel = true;

        private void Awake()
        {
            ApplyPlayback();
            GoTo(-1);
        }

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.tabKey.wasPressedThisFrame) showPanel = !showPanel;
            var mouse = Mouse.current;
            if (mouse == null || !museumCamera) return;
            if (mouse.rightButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
            if (keys.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
            if (Cursor.lockState != CursorLockMode.Locked) return;
            Vector2 delta = mouse.delta.ReadValue() * .1f;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -85, 85);
            var view = museumCamera.transform;
            view.rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 input = new Vector3(
                (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                (keys.eKey.isPressed ? 1 : 0) - (keys.qKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            view.position += view.TransformDirection(input.normalized) * (keys.leftShiftKey.isPressed ? 8 : 3) * Time.unscaledDeltaTime;
        }

        private void ApplyPlayback()
        {
            if (exhibits == null) return;
            foreach (var animator in exhibits)
                if (animator) animator.speed = paused ? 0 : playbackSpeed;
        }

        private void Restart()
        {
            foreach (var animator in exhibits)
                if (animator) { animator.Play("Exhibit.Loop", 0, 0); animator.Update(0); }
        }

        private void GoTo(int index)
        {
            if (!museumCamera) return;
            Transform point = index < 0 ? overview : viewpoints[index];
            if (!point) return;
            museumCamera.transform.SetPositionAndRotation(point.position, point.rotation);
            yaw = point.eulerAngles.y;
            pitch = Mathf.DeltaAngle(0, point.eulerAngles.x);
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnDisable() => Cursor.lockState = CursorLockMode.None;

        private void OnGUI()
        {
            if (!showPanel) return;
            float scale = Mathf.Clamp(Screen.height / 900f, .75f, 1.5f);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUILayout.BeginArea(new Rect(18, 18, 310, Mathf.Min(630, Screen.height / scale - 36)), GUI.skin.box);
            GUILayout.Label("PIG ANIMATION MUSEUM");
            GUILayout.Label("RMB: capture / WASD: move / Q,E: down,up");
            GUILayout.Label("Shift: fast / Esc: release / Tab: hide panel");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(paused ? "Play all" : "Pause all")) { paused = !paused; ApplyPlayback(); }
            if (GUILayout.Button("Restart all")) Restart();
            GUILayout.EndHorizontal();
            GUILayout.Label($"Playback speed: {playbackSpeed:0.00}x");
            float next = GUILayout.HorizontalSlider(playbackSpeed, .1f, 2);
            if (!Mathf.Approximately(next, playbackSpeed)) { playbackSpeed = next; ApplyPlayback(); }
            if (GUILayout.Button("Overview")) GoTo(-1);
            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < exhibits.Length; i++)
            {
                GUILayout.Space(8);
                if (GUILayout.Button(labels[i])) GoTo(i);
                if (exhibits[i])
                {
                    var info = exhibits[i].GetCurrentAnimatorStateInfo(0);
                    GUILayout.Label($"Cycle: {Mathf.Repeat(info.normalizedTime, 1):0.00}");
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = oldMatrix;
        }
    }
}

