# 서버 모드 Player HUD

현재 기본 규칙: 13라운드, 경매 2·6·10R은 180초(공개 60초 + 비공개 60초 + 거래 60초), 일반 라운드 60초. 회사별 초기 공급은 실제 시작 참가 인원 × 5 + 5주이며 추가 발행 없이 매도분만 반환한다. 수첩은 Tab으로 열고 Q 또는 Tab으로 닫는다. 안내 프리팹·메인 메뉴 HowToPlay도 동일하게 갱신했다.

검증: UpdatedMatchRulesTests 통과 (`Logs/UpdatedMatchRulesTests.xml`). 실제 설정 JSON으로 3인/6인 경기 13라운드 전체의 60/180초 시간표, 20/35주 재고와 총 19분을 검증했다. Mirror 호스트 경기에서는 실제 참가 1명에 10주를 수신하고, Q/Tab 입력과 13R 종료·결과창을 확인했다. 이 호스트 실행의 라운드 시간만 1초로 단축했으며 기본 180초는 앞선 실제 설정 기반 전체 경기 검사에서 확인했다.

- 실제 씬: `Assets/Project/Scenes/SC_ExchangeFurnished.unity`
- 위치: 네트워크로 생성된 `Player/Network Player HUD` (`Player.prefab`의 중첩 프리팹)
- `PlayerNet.SetGameplayActive`가 본인 Player의 Canvas만 활성화한다. 원격 Player·서버·로비에서는 HUD가 비활성화된다. `Gameplay Presentation`은 구형 씬 리그이므로 계속 꺼 둔다.
- 디자인 원본: Dev `PF_PlayerHUD`의 `Assets - Top Left`, `Round Timer - Top Right`. Dev 원본은 수정하지 않는다.
- 편집 대상: `UI/HUD/PF_NetworkPlayerHUD.prefab`. Text/Image/RectTransform은 개별 편집 가능하다.

## 역할

`NetworkPlayerHUD`는 MatchNet.Current의 공개 상태와 PlayerNet.LocalPortfolio의 본인 스냅샷만 표시한다. 거래·입찰·시간 진행을 실행하지 않는다. 현금에는 예약금이 포함되며 거래 가능 금액은 Available을 그대로 사용한다. 총자산은 현금과 공개 현재가로 평가한 주식의 합이다.

라운드 이벤트는 다음 Refresh에서 반영해 같은 동기화 패킷의 경매 상태까지 읽는다. 초기 활성화 시 놓친 현재 라운드도 한 번 표시하고, 반복 갱신으로 알림을 다시 열지 않는다. RoundStartView는 자체 시간으로 닫힌다. 종료/연결 해제 시 알림과 오래된 표시를 지운다.

기존 HUDController는 조준점과 상호작용 안내를 계속 담당한다. 해당 씬의 기존 Player Money만 비활성화해 현금이 중복 표시되지 않게 했다.

## 개인 정보 수첩과 경매 품목

`NetworkNotebookController`가 Tab으로 수첩을 열고 닫으며 Q도 닫기로 동작한다. 위/아래 방향키로 스크롤한다. 거래창을 연 동안은 수첩을 열지 않는다. 수첩을 읽는 동안 이동·시점·점프·상호작용·입찰·퇴석 입력을 막고, 닫는 Q가 같은 프레임에 퇴석으로 전달되지 않게 한다.

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

병합 후 구조 검증: `LocalPlayerHUDTests`와 기존 `MatchFeedbackTests` 모두 통과 (`Logs/HUDIntegrationTests.xml`). 로컬 HUD 활성화, 원격 HUD 비활성화, 자기 PlayerMove/TradePanelController 참조, 마우스 거래·베팅·낙찰·결과·로비 복귀를 확인했다. 원격 실제 동시 접속은 별도다.

`LocalPlayerHUDSetup.Apply`는 실제 Player 프리팹에 공통 HUD를 중첩하고 씬의 중복 HUD를 제거한다. 전용 서버 씬도 동일한 Player.prefab을 사용한다. 서버에서는 Player의 입력·Canvas·카메라를 끈다. 수첩과 피드백은 부모 Player를 우선 참조하므로 씬의 비활성 구형 Player를 잘못 선택하지 않는다. 로비 복귀 시 로컬 Player를 명시적으로 비활성화한다.

- NetworkHUDSetup.Apply: 시안 추출과 실제 씬 배치. 재실행하면 이 HUD만 교체한다.
- NetworkHUDVerification.Run: 대기, 현금/보유량/평가액, 예약금, 라운드/단계, 시작 알림/중복 방지, 종료, 연결 해제, Dev 컴포넌트 미포함 검사 통과.
- NetworkHUDPlayVerification.Run: 실제 홀 씬 Play에서 HUD 활성화, Canvas 양수 Scale, 타이머 갱신, 시작 알림 자동 종료 검사 통과. 주입한 네트워크 상태로 검증하며 실제 다중 클라이언트 연결 검증은 별도다.
- 검증 결과: `Library/NetworkHUDVerification.result`, `Library/NetworkHUDPlayVerification.result`.
