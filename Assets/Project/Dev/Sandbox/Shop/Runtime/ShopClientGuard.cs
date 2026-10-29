using Mirror;
using UnityEngine;

namespace BuyBidBlow.Sandbox.Shop
{
    // 상점 서버(ShopSandbox 호스트)는 경기에 들어온 모든 클라이언트에게 상점 메시지를 보낸다.
    // ShopSite가 없는 씬(메인 메뉴에서 시작한 클론, 원본 씬 등)의 클라이언트는 그 메시지를 모르면
    // Mirror가 "failed to unpack and invoke message"로 연결을 끊으므로, 어느 씬에서든 빈 처리를 걸어 둔다.
    // 이 경우 상점 기능만 꺼진 채(요청을 보내지 않음) 정상적으로 경기에 참가한다.
    public sealed class ShopClientGuard : MonoBehaviour
    {
        bool hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Shop Client Guard") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<ShopClientGuard>();
        }

        // Mirror는 클라이언트가 꺼질 때(Shutdown) 핸들러를 모두 지우므로, 켜질 때마다 다시 건다.
        void Update()
        {
            if (!NetworkClient.active) { hooked = false; return; }
            if (hooked) return;
            hooked = true;
            if (!ShopSite.Instance) InstallIgnoreHandlers();
        }

        public static void InstallIgnoreHandlers()
        {
            NetworkClient.ReplaceHandler<ShopAvailable>(_ => { });
            NetworkClient.ReplaceHandler<ShopReply>(_ => { });
            NetworkClient.ReplaceHandler<ShopLeashState>(_ => { });
        }
    }
}
