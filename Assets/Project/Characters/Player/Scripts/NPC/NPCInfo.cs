using UnityEngine;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow
{
    public class NPCInfo : MonoBehaviour
    {
        [SerializeField] private Company company;

        public Company Company => company;
    }
}
