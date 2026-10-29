using UnityEngine;

namespace BuyBidBlow.Characters
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private GameObject cameraObject;

        [Header("Default Position")]
        [SerializeField]
        private Vector3 defaultLocalPosition = new Vector3(0f, 1.7f, 0f);

        public Transform CameraTransform => cameraObject.transform;

        // 카메라를 활성화하고 기본 로컬 위치·회전으로 초기화
        private void Awake()
        {
            cameraObject.SetActive(true);
            cameraObject.transform.localPosition = defaultLocalPosition;
            cameraObject.transform.localRotation = Quaternion.identity;
        }
    }
}
