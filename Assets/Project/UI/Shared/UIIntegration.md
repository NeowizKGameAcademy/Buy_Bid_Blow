# 추가 UI 통합 인계

## 범위

실행용 표시 컴포넌트와 프리팹은 `Assets/Project/UI`, 검수 씬·예시 데이터는 `Dev/Sandbox/UIShowcase`, 조립·검사 도구는 `Dev/Editor/UIShowcase`에 있습니다. 실제 실행 씬, Build Settings, 서버·플레이어·게임 규칙은 연결하거나 수정하지 않습니다. 기존 청록 패널 이미지를 Shared/Modal/Art로 복사하여 Dev 자산 의존성을 제거했고 버튼은 기존 Menu 스프라이트를 재사용합니다. 새 생성 이미지나 새 어셈블리는 사용하지 않습니다.

UIView는 경기 상태를 계산하지 않고 전달된 값을 표시합니다. 데모 입력은 게임 서비스 Fake가 아닌 정적인 UI specimen입니다. 호스트·원격 검증은 통합 단계의 책임입니다.

## 프리팹과 호출

|경로(UI 아래)|호출·데이터|출력|
|---|---|---|
|Shared/Modal/PF_ErrorModal|ModalView.SetButtons(labels), Show(title,body,cause)|ActionRequested(index), OpenChanged(bool)|
|Shared/Modal/PF_ButtonStates|기본·선택·눌림·비활성의 비교용 공통 버튼 샘플|버튼 자체는 경기 기능 없음|
|Lobby/Connection/PF_Connection_Connecting, Failed, Disconnected, HostClosed|ConnectionPanelView.ShowState(state,reason,endpoint)|버튼 요청|
|Menu/Pause/PF_PauseMenu|PauseMenuView.OpenMenu()|0 계속 / 1 설정 / 2 조작 / 3 나가기|
|Menu/Pause/PF_LeaveConfirm|SetButtons("취소","나가기"), Show(title,body)|0 취소 / 1 나가기|
|Menu/Pause/PF_Controls|Show("조작 안내",content)|0 닫기|
|Loading/PF_Loading|LoadingView.Show(title,detail,float? progress), SetProgress(value), Hide()|실제 로딩 수행 안 함|
|Result/PF_MatchResults|MatchResultView.ShowResults(entries,revealBreakdown,canReturn)|0 로비 복귀 요청|
|Round/PF_RoundStart|RoundStartView.Show(round,total,isAuction,seconds)|시간 후 자동 숨김|
|Auction/Result/PF_AuctionResult|ShowPublic(winner,price,isLocalWinner), ShowSecret(won,ownBid), ShowUnsold()|낙찰자 0 정보 / 1 닫기, 미낙찰·유찰 0 닫기|
|Shared/Notification/PF_NotificationStack|NotificationStackView.Push(title,detail,tone,seconds), Clear()|최대 3개 표시, 최대 12개 대기|
|Market/Notifications/PF_MarketNotices|MarketNoticeView.TradeCompleted(company,quantity,cashDelta,buy), Rejected(reason), BetSettled(company,stake,payout,won)|개인 토스트|
|Lobby/Notifications/PF_LobbyNotices|LobbyNoticeView.Joined(nickname), Left(nickname)|공개 입장·퇴장 토스트|
|Menu/Settings/PF_Settings|SettingsPanelView.ShowSettings(current)|ApplyRequested(SettingsValues)|

PF_Connection 4개는 같은 표시 코드·배경을 공유하는 저장 상태입니다. 실제 통합은 네 개 모두 만들 필요 없이 한 인스턴스에 ShowState를 호출해도 됩니다. 모든 모달 프리팹은 비활성으로 저장되어 있으므로 Show/ShowState/OpenMenu 등으로 엽니다. 토스트 루트는 Instantiate 후 SetActive(true)해야 합니다. Canvas는 프리팹에 포함하지 않습니다.

## 배치와 입력

- 부모 Canvas: ScreenSpaceOverlay, CanvasScaler 1920×1080, Match .5. 공통 모달은 화면 전체에 stretch하고 뒤의 dim이 raycast를 차단합니다.
- 폰트: 프로젝트 Pretendard Bold, 일반 uGUI Text. 이미지·텍스트·앵커·색상은 모두 Inspector에서 수정 가능. Text.supportRichText=false.
- 입력: ModalView가 방향키·Enter·Esc를 처리합니다. Button Navigation=None, 클릭 리스너 없음(키보드 전용). 실험 EventSystem은 move/submit/cancel 액션을 비워 중복 입력을 방지합니다. 통합 담당자도 기존 UI 입력 경로와 중복되지 않도록 한 경로만 활성화해야 합니다.
- 방향키는 활성·상호작용 가능한 버튼만 순환합니다. Show 시 첫 사용 가능 버튼 선택, Hide 시 유효한 이전 선택으로 복귀. 같은 프레임에 연쇄 입력되지 않도록 모달은 열린 프레임 입력을 건너뜁니다.
- OpenChanged에 로컬 이동·시점·레이캐스트 입력 차단을 연결하세요. 다른 모달을 열기 전에 현재 모달을 닫으세요. Time.timeScale과 네트워크 시간은 건드리지 않습니다.
- 게임 메뉴에서 설정/조작/나가기 요청을 받은 통합 담당자가 해당 패널을 열고 복귀 위치를 관리합니다. Showcase의 연결은 Dev 전용 예시입니다.
- 설정은 ↑↓로 5개 설정 행과 3개 하단 버튼을 이동, ←→ 값 변경, Enter 실행, Esc 미적용 변경 버리고 닫기. 기본값은 draft에만 반영. ApplyRequested에서 저장·오디오 버스·화면 API·감도 적용을 구현하세요. UI는 AudioListener·Screen·PlayerPrefs를 직접 바꾸지 않습니다.
- 로그는 자산 HUD 아래 왼쪽, 로비 알림은 오른쪽에 배치. 스택이 가득 차면 대기하며 대기 한도를 넘으면 가장 오래된 대기 로그를 버립니다. 개인 실패 로그는 모달로 바꾸지 않습니다. unscaledDeltaTime 사용.

## 데이터와 네트워크 연결 원칙

- 라운드 시작: Common/MatchNet.RoundStarted를 구독하되 늦게 들어온 사용자는 현재 상태도 한 번 읽어 초기화하세요. 경매 여부·전체 라운드 수는 전달하세요.
- 접속: ConnectionController.Changed와 GameNetworkManager의 승인·끊김 이벤트를 연결하세요. 현재 모든 끊김이 Failed로 합쳐져 있으므로 명시적 서버 종료와 일반 연결 끊김 구분은 연결 담당자가 원인 코드/상태로 제공해야 합니다. UI 문구를 파싱하지 마세요.
- 취소·재시도·나가기·메인 복귀는 버튼 요청만 보냅니다. UI가 StopClient/StopHost/LoadScene을 직접 호출하지 않습니다. Escape가 꺼진 끊김 패널은 복귀 버튼으로만 닫습니다.
- 거래·베팅 로그는 소유자에게 도착한 성공/실패 응답 이후에만 호출하세요. 현금 변화로 성공을 추측하지 마세요. 서버의 실제 stake/payout을 전달하세요.
- 비공개 경매 ShowSecret은 타인의 이름·가격을 인자로 받지 않습니다. 공개 경매에는 공개 결과만 전달하세요. 정보 확인 버튼은 별도 개인 수첩을 열도록 연결하세요.
- 결과 entries는 서버가 확정한 Rank·Nickname·TotalAssets·Cash·StockValue·TotalShares·IsLocal입니다. 표시 코드는 정렬/동점 계산을 하지 않으며 전달 순서대로 표시합니다. Rank=1이 여러 명이면 모두 공동 승자로 표시합니다. 1~6행 지원, 실제 경기 최소 인원 규칙은 서버 책임입니다.
- revealBreakdown=false이면 타인의 현금·주식 평가·주식 수는 —로 숨깁니다. 최종 세부 자산 전원 공개 정책은 아직 확정하지 않습니다. canReturn=false이면 복귀 버튼 비활성. 서버에서 권한이 바뀌면 SetButtonEnabled(0,true) 후 Footer 문구도 갱신하세요.
- 로비 입·퇴장: 동기화된 참가 명단의 추가/삭제 이벤트로 호출. 최초 명단 전체를 새 입장 알림으로 쏟아내지 말고 초기 snapshot과 변경을 구분하세요. 닉네임은 삭제 전에 보관해 전달하세요.
- 로딩 progress=null은 실제 % 없는 진행 애니메이션입니다. 값을 알 때만 0~1 전달. 현재 로비→경기는 같은 홀에서 표시 전환하므로 로딩 UI를 강제로 끼우지 않습니다.

## 검수와 개발 도구

Dev/Sandbox/UIShowcase/SC_UIShowcase.unity를 열고 Play. PgUp/PgDn 21상태, F5 다시 열기, F6 거래 실패 로그 추가, F7 입장 로그 추가, Home 첫 상태. 예시 데이터는 실제 게임 로직에 연결되지 않습니다. PreviewHud는 설명용이며 게임 HUD가 아닙니다.

Tools > Buy Bid Blow > UI > Build UI Showcase는 생성 프리팹과 실험 씬을 재작성하므로 수동 편집 후 무심코 재실행하지 마세요. 실행 씬·Build Settings는 수정하지 않습니다. 최신 캡처는 Dev/Sandbox/UIShowcase/Previews에 있습니다.
