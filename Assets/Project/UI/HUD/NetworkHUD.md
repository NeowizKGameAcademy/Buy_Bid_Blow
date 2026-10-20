# 서버 모드 Player HUD

- 실제 씬: `Assets/Project/Scenes/SC_ExchangeFurnished.unity`
- 위치: `Gameplay Presentation/PF_NetworkPlayerHUD`
- `HallSession`이 경기 시작 시 Gameplay Presentation을 활성화한다. 로비에서 숨겨지는 것은 정상이다.
- 디자인 원본: Dev `PF_PlayerHUD`의 `Assets - Top Left`, `Round Timer - Top Right`. Dev 원본은 수정하지 않는다.
- 편집 대상: `UI/HUD/PF_NetworkPlayerHUD.prefab`. Text/Image/RectTransform은 개별 편집 가능하다.

## 역할

`NetworkPlayerHUD`는 MatchNet.Current의 공개 상태와 PlayerNet.LocalPortfolio의 본인 스냅샷만 표시한다. 거래·입찰·시간 진행을 실행하지 않는다. 현금에는 예약금이 포함되며 거래 가능 금액은 Available을 그대로 사용한다. 총자산은 현금과 공개 현재가로 평가한 주식의 합이다.

라운드 이벤트는 다음 Refresh에서 반영해 같은 동기화 패킷의 경매 상태까지 읽는다. 초기 활성화 시 놓친 현재 라운드도 한 번 표시하고, 반복 갱신으로 알림을 다시 열지 않는다. RoundStartView는 자체 시간으로 닫힌다. 종료/연결 해제 시 알림과 오래된 표시를 지운다.

기존 HUDController는 조준점과 상호작용 안내를 계속 담당한다. 해당 씬의 기존 Player Money만 비활성화해 현금이 중복 표시되지 않게 했다.

## 개인 정보 수첩과 경매 품목

`NetworkNotebookController`가 Tab으로 수첩을 열고 닫으며 Esc도 닫기로 동작한다. 위/아래 방향키로 스크롤한다. 거래창을 연 동안은 수첩을 열지 않는다. 수첩을 읽는 동안 이동·시점·점프·상호작용·입찰·퇴석 입력을 막고, 닫는 Esc가 같은 프레임에 퇴석으로 전달되지 않게 한다.

서버 MatchEngine.InformationAwarded → 낙찰자의 PlayerNet.TargetAwardInformation → 본인 LocalInformation → InformationNotebookView 순서로 연결한다. 방향 데이터는 MatchNet에 넣지 않는다. 새 경기와 연결 종료 시 개인 정보 목록을 지운다. 경매 품목 패널은 공개된 라운드 두 개와 각 회사 아이콘만 표시한다.

거래창은 거래 후 현금 라벨과 금액의 RectTransform 영역을 분리한다. HUDRefinementSetup.Apply가 세 회사 거래창, 경매 품목, 수첩 조립을 재적용한다. HUD를 원본에서 다시 추출했다면 이 작업도 실행해야 한다.

전체 라운드 수는 GameNetworkManager가 MatchSettings에서 MatchNet.totalRounds로 전달한다. 개인 자산은 기존 TargetRpc 경로를 유지한다.

## 검증

## 경기 피드백 / 결과 / 마우스 거래

`NetworkMatchFeedback`는 기존 MarketNoticeView, AuctionResultView, MatchResultView를 사용한다. PlayerNet의 TargetRpc로 전달된 본인 요청 결과, 베팅 정산, 공개 낙찰 결과와 자신의 비공개 낙찰 결과를 화면에 표시한다. 진행 중인 하락 베팅 배지는 LocalBet와 LocalBetStake에서 읽는다.

최종 순위는 서버가 총자산 내림차순 → 보유 주식 수 내림차순으로 계산하며 두 값이 같으면 공동 순위다. 시작 참가자 목록을 보관해 경기 중 이탈자의 이름도 유지한다. 타인의 현금·주식 평가액·보유 수량은 전달하지 않고 결과창에서도 숨긴다. 결과 스냅샷을 클라이언트에 캐시해 HUD가 늦게 켜져도 표시한다. 방장의 로비 복귀 요청은 서버가 권한과 경기 종료 여부를 검사한 뒤 전원에게 전환을 전달한다.

거래창 Button의 targetGraphic에 raycastTarget을 켜서 탭, 수량, 확인, 닫기를 마우스로 조작한다. Enter가 UI 모듈과 단축키 양쪽에서 들어와도 같은 프레임에 거래를 두 번 전송하지 않는다. 늑대 발바닥 포인터는 CursorController가 적용하며 해당 컴포넌트를 끄면 기본 포인터로 복원한다.

재조립: HUDRefinementSetup.Apply 후 MatchFeedbackSetup.Apply 순서. MatchFeedbackSetup은 게임 씬을 다시 만들지 않고 실제 씬이 참조하는 프리팹을 갱신한다.

`MatchFeedbackTests.MouseTradingBetsAuctionsResultsAndReturnUseServerEvents` PlayMode 검사 통과. 실제 Mirror 호스트 연결로 마우스 raycast 매수·매도·베팅, 진행 배지, 정산 알림, 공개 및 비공개 입찰/낙찰, 정보 지급, 결과 점수, 결과창 버튼을 통한 전원 로비 복귀를 확인했다. 결과: Logs/MatchFeedbackTests3.xml. 원격 다중 클라이언트 동시 접속은 별도다. RPC 변경으로 접속 프로토콜 버전은 6이며 서버·클라이언트 모두 동일한 새 빌드를 사용해야 한다.

### 기존 HUD 검증

- NetworkHUDSetup.Apply: 시안 추출과 실제 씬 배치. 재실행하면 이 HUD만 교체한다.
- NetworkHUDVerification.Run: 대기, 현금/보유량/평가액, 예약금, 라운드/단계, 시작 알림/중복 방지, 종료, 연결 해제, Dev 컴포넌트 미포함 검사 통과.
- NetworkHUDPlayVerification.Run: 실제 홀 씬 Play에서 HUD 활성화, Canvas 양수 Scale, 타이머 갱신, 시작 알림 자동 종료 검사 통과. 주입한 네트워크 상태로 검증하며 실제 다중 클라이언트 연결 검증은 별도다.
- 검증 결과: `Library/NetworkHUDVerification.result`, `Library/NetworkHUDPlayVerification.result`.
