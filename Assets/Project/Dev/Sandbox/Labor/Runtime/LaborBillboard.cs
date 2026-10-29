using UnityEngine;

namespace BuyBidBlow.Sandbox.Labor
{
    // 일터 안내 글자가 항상 카메라 쪽을 보게 한다(수평 회전만).
    public sealed class LaborBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (!cam && Camera.allCamerasCount > 0) cam = Camera.allCameras[0];
            if (!cam) return;
            var away = transform.position - cam.transform.position;
            away.y = 0;
            if (away.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(away);
        }
    }
}
