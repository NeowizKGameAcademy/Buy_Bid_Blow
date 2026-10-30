using BuyBidBlow.UI.Shared;
namespace BuyBidBlow.UI.Lobby
{
    public enum ConnectionPanelState { Connecting, Failed, Disconnected, HostClosed }
    public sealed class ConnectionPanelView : ModalView
    {
        public ConnectionPanelState State { get; private set; }
        // 연결 상태에 맞는 제목·설명·버튼과 Esc 동작을 설정해 패널 표시
        public void ShowState(ConnectionPanelState state, string reason = "", string endpoint = "")
        {
            State = state; EscapeEnabled = state == ConnectionPanelState.Connecting || state == ConnectionPanelState.Failed;
            EscapeAction = state == ConnectionPanelState.Failed ? 1 : 0;
            switch (state)
            {
                case ConnectionPanelState.Connecting: SetButtons("취소"); Show("대기실에 접속 중", "서버의 응답을 기다리고 있습니다.\n" + endpoint, reason); break;
                case ConnectionPanelState.Failed: SetButtons("다시 시도", "돌아가기"); Show("접속할 수 없습니다", "주소와 서버 상태를 확인해주세요.", reason); break;
                case ConnectionPanelState.Disconnected: SetButtons("메인 메뉴로"); Show("연결이 끊겼습니다", "서버와의 연결이 종료되었습니다.", reason); break;
                default: SetButtons("메인 메뉴로"); Show("방이 종료되었습니다", "이 방에서 더 이상 플레이할 수 없습니다.", reason); break;
            }
        }
    }
}
