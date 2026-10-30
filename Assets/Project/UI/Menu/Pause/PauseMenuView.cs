using BuyBidBlow.UI.Shared;
namespace BuyBidBlow.UI.Menu
{
    public sealed class PauseMenuView : ModalView
    {
        // 일시정지 메뉴 버튼을 구성하고 메뉴 모달 표시
        // No Time.timeScale, network or player movement changes are made here.
        public void OpenMenu()
        {
            SetButtons("계속하기", "설정", "조작 안내", "방 나가기");
            EscapeAction=0; Show("메뉴", "온라인 경기는 계속 진행됩니다.");
        }
    }
}
