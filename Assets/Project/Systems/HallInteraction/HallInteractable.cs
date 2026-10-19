using UnityEngine;

namespace BuyBidBlow.Hall
{
    public abstract class HallInteractable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public abstract bool Interact(HallSeatActor actor);
    }
}
