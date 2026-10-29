using System.Collections.Generic;
using BuyBidBlow.Systems.Shop;
using UnityEngine;

namespace BuyBidBlow.Sandbox.Shop
{
    // 파티 아이템 효과(클라이언트 표시 전용). 사용한 사람 주변에서 짧게 재생하고 스스로 사라진다.
    // 콜라이더가 없어 이동·상호작용에 영향이 없고, 화면 전체를 덮는 효과는 쓰지 않는다.
    public sealed class ShopPartyEffect : MonoBehaviour
    {
        const int ConfettiPieces = 36, BubblePieces = 12;
        const float HeadHeight = 2.3f;

        static readonly List<ShopPartyEffect> active = new List<ShopPartyEffect>();
        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        static readonly Color[] confettiColors =
        {
            new Color(1f, .35f, .35f), new Color(1f, .8f, .2f), new Color(.35f, .85f, .4f),
            new Color(.3f, .6f, 1f), new Color(.85f, .45f, 1f), new Color(1f, 1f, 1f)
        };

        sealed class Piece
        {
            public Transform transform;
            public Vector3 velocity, spin, baseScale;
            public float phase;
        }

        readonly List<Piece> pieces = new List<Piece>();
        ShopEffect effect;
        Transform owner;
        TextMesh emote;
        float started, seconds;

        public static void Play(Transform owner, ShopEffect effect, string emoteText, float seconds, Font font, bool mine)
        {
            if (!owner || seconds <= 0) return;
            // 같은 사람의 같은 효과가 남아 있으면 새로 시작한다(겹쳐 쌓이지 않게).
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i] && active[i].owner == owner && active[i].effect == effect) Destroy(active[i].gameObject);

            var go = new GameObject("Shop Party Effect " + effect);
            var fx = go.AddComponent<ShopPartyEffect>();
            fx.owner = owner;
            fx.effect = effect;
            fx.seconds = seconds;
            fx.started = Time.time;
            go.transform.position = owner.position;
            switch (effect)
            {
                case ShopEffect.Confetti: fx.SpawnConfetti(); break;
                case ShopEffect.Bubbles: fx.SpawnBubbles(); break;
                case ShopEffect.Emote: if (!mine) fx.SpawnEmote(emoteText, font); break;
            }
            active.Add(fx);
        }

        public static void ClearAll()
        {
            foreach (var fx in active) if (fx) Destroy(fx.gameObject);
            active.Clear();
        }

        void SpawnConfetti()
        {
            var center = owner.position + Vector3.up * 1.6f;
            for (int i = 0; i < ConfettiPieces; i++)
            {
                var dir = Random.insideUnitCircle.normalized;
                var start = center + new Vector3(dir.x, 0, dir.y) * .6f;
                var piece = Add(PrimitiveType.Quad, start, new Vector3(.07f, .11f, 1f), confettiColors[i % confettiColors.Length]);
                piece.velocity = new Vector3(dir.x, 0, dir.y) * Random.Range(.8f, 2f) + Vector3.up * Random.Range(2.5f, 4.5f);
                piece.spin = Random.insideUnitSphere * 720f;
            }
        }

        void SpawnBubbles()
        {
            for (int i = 0; i < BubblePieces; i++)
            {
                var dir = Random.insideUnitCircle;
                var start = owner.position + new Vector3(dir.x, 0, dir.y) * 1.1f + Vector3.up * Random.Range(.6f, 1.4f);
                var piece = Add(PrimitiveType.Sphere, start, Vector3.one * Random.Range(.12f, .26f), new Color(.65f, .9f, 1f, .45f));
                piece.velocity = Vector3.up * Random.Range(.3f, .7f);
                piece.phase = Random.Range(0f, 6.3f);
            }
        }

        void SpawnEmote(string text, Font font)
        {
            var go = new GameObject("Emote");
            go.transform.SetParent(transform, false);
            emote = go.AddComponent<TextMesh>();
            emote.text = text;
            emote.fontSize = 64;
            emote.characterSize = .05f;
            emote.anchor = TextAnchor.MiddleCenter;
            emote.alignment = TextAlignment.Center;
            emote.color = new Color(1f, .9f, .4f);
            if (font)
            {
                emote.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
        }

        Piece Add(PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            DestroyImmediate(go.GetComponent<Collider>());
            go.layer = 2; // Ignore Raycast
            go.transform.SetParent(transform, true);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.transform.rotation = Random.rotation;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = Mat(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var piece = new Piece { transform = go.transform, baseScale = scale };
            pieces.Add(piece);
            return piece;
        }

        static Material Mat(Color color)
        {
            if (materials.TryGetValue(color, out var material) && material) return material;
            // Sprites/Default는 빌드에 항상 포함되고 색의 알파(비눗방울 반투명)를 그대로 쓴다.
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader) { color = color };
            materials[color] = material;
            return material;
        }

        void Update()
        {
            float age = Time.time - started;
            if (!owner || age >= seconds) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            // 마지막 0.4초 동안 작아지며 사라진다.
            float fade = Mathf.Clamp01((seconds - age) / .4f);

            foreach (var piece in pieces)
            {
                if (effect == ShopEffect.Confetti)
                {
                    piece.velocity += Vector3.down * 5f * dt;
                    piece.velocity *= 1f - 1.5f * dt;
                    piece.transform.Rotate(piece.spin * dt, Space.Self);
                }
                else
                {
                    piece.velocity.x = Mathf.Sin(age * 2.5f + piece.phase) * .25f;
                    piece.velocity.z = Mathf.Cos(age * 2f + piece.phase) * .25f;
                }
                piece.transform.position += piece.velocity * dt;
                piece.transform.localScale = piece.baseScale * fade;
            }

            if (emote)
            {
                float pop = Mathf.Min(1f, age / .15f) * fade;
                emote.transform.position = owner.position + Vector3.up * (HeadHeight + Mathf.Sin(age * 3f) * .05f);
                emote.transform.localScale = Vector3.one * pop;
                var view = Camera.main;
                if (view)
                {
                    var direction = emote.transform.position - view.transform.position;
                    direction.y = 0;
                    if (direction.sqrMagnitude > .001f) emote.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                }
            }
        }

        void OnDestroy() => active.Remove(this);
    }
}
