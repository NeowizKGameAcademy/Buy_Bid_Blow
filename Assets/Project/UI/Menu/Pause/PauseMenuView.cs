using BuyBidBlow.UI.Shared;
namespace BuyBidBlow.UI.Menu
{
    public sealed class PauseMenuView : ModalView
    {
        // No Time.timeScale, network or player movement changes are made here.
        public void OpenMenu()
        {
            SetButtons("계속하기", "설정", "조작 안내", "방 나가기");
            EscapeAction=0; Show("메뉴", "온라인 경기는 계속 진행됩니다.");
        }
    }
}
