using BuyBidBlow.UI.Shared;
using UnityEngine;
namespace BuyBidBlow.UI.Lobby
{
    public sealed class LobbyNoticeView : MonoBehaviour
    {
        public NotificationStackView Stack;
        public void Joined(string nickname) => Stack.Push("참가자 입장", $"{nickname}님이 참가했습니다.", NoticeTone.Success);
        public void Left(string nickname) => Stack.Push("참가자 퇴장", $"{nickname}님이 퇴장했습니다.");
    }
}
