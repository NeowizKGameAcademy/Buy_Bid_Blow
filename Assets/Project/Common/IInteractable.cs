using UnityEngine;

namespace BuyBidBlow.Common
{
    // Implemented by everything the player can use with the interact key. The target decides what using it
    // means (open a trade panel, ask the server for a seat, ...); PlayerInteraction only finds and calls it.
    public interface IInteractable
    {
        // False hides the prompt and ignores the interact key.
        bool CanInteract { get; }
        string Prompt { get; }
        // 로컬 플레이어(루트 오브젝트)의 상호작용을 처리하고 처리 여부를 반환
        bool Interact(GameObject player);
    }
}
