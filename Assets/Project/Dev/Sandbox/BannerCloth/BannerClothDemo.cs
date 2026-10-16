using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Dev.BannerCloth
{
    public sealed class BannerClothDemo : MonoBehaviour
    {
        public Cloth banner;
        public CapsuleCollider contact;
        public bool automatic = true;
        public float wind = 1.5f;
        public Vector3 WindAcceleration => new Vector3(0.15f, 0, wind * (1 + .35f * Mathf.Sin(Time.time * 1.7f)));

        void Update()
        {
            banner.externalAcceleration = WindAcceleration;
            if (automatic)
                contact.transform.position = new Vector3(Mathf.Sin(Time.time * .6f) * 1.7f, 1.6f, Mathf.Sin(Time.time * .9f) * 1.2f);
            else if (Keyboard.current != null)
            {
                var k = Keyboard.current;
                var direction = new Vector3((k.rightArrowKey.isPressed ? 1 : 0) - (k.leftArrowKey.isPressed ? 1 : 0), 0,
                    (k.upArrowKey.isPressed ? 1 : 0) - (k.downArrowKey.isPressed ? 1 : 0));
                contact.transform.position += direction * (Time.deltaTime * 1.5f);
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 340, 180), GUI.skin.box);
            GUILayout.Label("BANNER CLOTH / 6 x 2 m / 1,281 vertices");
            automatic = GUILayout.Toggle(automatic, "Automatic capsule contact");
            GUILayout.Label("Manual: arrow keys move capsule X / Z");
            GUILayout.Label("Wind: " + wind.ToString("F1"));
            wind = GUILayout.HorizontalSlider(wind, 0, 5);
            GUILayout.Label("Top corners pinned. Capsule pushes cloth.");
            GUILayout.EndArea();
        }
    }
}
