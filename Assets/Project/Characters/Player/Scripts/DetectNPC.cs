using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BuyBidBlow
{
    public class DetectNPC : MonoBehaviour
    {
        [SerializeField] private Image cursor;
        [SerializeField] private Sprite[] cursorImage = new Sprite[2];
        [SerializeField] private float detectDistance = 3.0f;
        [SerializeField] private LayerMask npcLayer;

        private void Update()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * detectDistance, Color.red);

            if (Physics.Raycast(ray, out hit, detectDistance, npcLayer))
            {
                Debug.Log($"NPC °¨ÁöµÊ: {hit.collider.name}");

                cursor.sprite = cursorImage[1];
            }
            else
            {
                cursor.sprite = cursorImage[0];
            }
        }
    }
}
