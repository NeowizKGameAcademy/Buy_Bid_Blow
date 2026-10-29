using UnityEngine;

namespace BuyBidBlow.Sandbox.Shop
{
    // 간판 글자가 항상 카메라를 향하게 한다(좌우 회전만).
    public sealed class ShopBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var view = Camera.main;
            if (!view) return;
            var direction = transform.position - view.transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
