using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuyBidBlow
{
    public class DetectNPC : MonoBehaviour
    {
        [SerializeField] private float detectDistance = 3.0f; // 감지 거리
        [SerializeField] private LayerMask npcLayer;

        private void Update()
        {
            
        }
    }
}
