using UnityEngine;

namespace BuyBidBlow
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private GameObject cameraObject;

        [Header("Default Position")]
        [SerializeField]
        private Vector3 defaultLocalPosition = new Vector3(0f, 1.7f, 0f);

        public Transform CameraTransform => cameraObject.transform;

        private void Awake()
        {
            cameraObject.SetActive(true);
            cameraObject.transform.localPosition = defaultLocalPosition;
            cameraObject.transform.localRotation = Quaternion.identity;
        }
    }
}
