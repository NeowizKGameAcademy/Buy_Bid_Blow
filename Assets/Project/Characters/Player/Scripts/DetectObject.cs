using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BuyBidBlow
{
    public class DetectObject : MonoBehaviour
    {
        [SerializeField] private Image cursor;
        [SerializeField] private Sprite[] cursorImage = new Sprite[2];
        [SerializeField] private float detectDistance = 3.0f;
        [SerializeField] private LayerMask[] detectLayer;

        public bool isDetected { get; private set; }
        public LayerMask detectingLayer { get; private set; }

        private void Update()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * detectDistance, Color.red);

            for (int i = 0; i < detectLayer.Length; i++)
            {
                if (Physics.Raycast(ray, out hit, detectDistance, detectLayer[i]))
                {
                    isDetected = true;
                    detectingLayer = detectLayer[i];
                }
                else
                {
                    isDetected = false;
                    detectingLayer = 0;
                }
            }

            ChangeCursor(isDetected);
        }

        private void ChangeCursor(bool Detected)
        {
            if(Detected)
                cursor.sprite = cursorImage[1];
            else
                cursor.sprite = cursorImage[0];
        }
    }
}
