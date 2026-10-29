using UnityEngine;

namespace BuyBidBlow.Common
{
    // Implemented by scene objects the player can use with the interact key.
    public interface IInteractable
    {
        // False hides the prompt and ignores the interact key.
        bool CanInteract { get; }
        string Prompt { get; }
        // 플레이어의 상호작용을 처리하고 처리 여부를 반환
        bool Interact(GameObject player);
    }
}
