using UnityEngine;

namespace BuyBidBlow.Common
{
    // Implemented by scene objects the player can use with the interact key.
    public interface IInteractable
    {
        // False hides the prompt and ignores the interact key.
        bool CanInteract { get; }
        string Prompt { get; }
        bool Interact(GameObject player);
    }
}
