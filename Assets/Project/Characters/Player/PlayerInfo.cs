using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Characters
{
    public class PlayerInfo : MonoBehaviour
    {
        [SerializeField] private string playerId = "local-player";
        [SerializeField] private string displayName = "Player";
        [SerializeField] private TieColor tieColor;
        [SerializeField] private bool isReady;

        public string PlayerId => playerId;
        public PlayerId ServerId => new PlayerId(playerId);
        public string DisplayName => displayName;
        public TieColor TieColor => tieColor;
        public bool IsReady => isReady;
    }
}
