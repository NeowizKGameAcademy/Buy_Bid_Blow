# 책임 소재 점검 (진단 2026-10-28 / 수정 현황 2026-10-29)

초기 1~3차 점검의 항목 번호를 유지하면서 관련 구현·빌드 경로·검증 코드를 대조해 진단과 처방을 조정한 문서다. A~K 본문은 2026-10-28의 수정 전 진단을 보존한다. 이후 코드 수정과 실제 검증 결과는 아래 현황을 기준으로 읽는다.

- 경로는 `Assets/Project/` 기준. 클라이언트 빌드 씬은 `SC_MainMenu`, `SC_ExchangeFurnished`, 전용 서버 빌드 씬은 `SC_Server`(홀 없음 — `Dev/Editor/Build/GameBuild.cs`).
- 폴더·책임·의존 관계의 기준은 [Project README](../../Assets/Project/README.md)다. 현재 `Game → Systems` 두 어셈블리와 GNM의 경기 생성·관리 연결/Tick 역할을 기본으로 한다. 새 계층·MatchHost·Gateway 도입은 별도 구조 변경 제안이며 결함 수정의 선행조건이 아니다.
- 원칙과 기법은 [RefactoringGuide.md](RefactoringGuide.md)를 참고한다. 줄 번호는 조사 당시 위치이므로 코드 변경 후에는 파일·메서드 이름으로 다시 확인한다.

## 2026-10-29 수정 현황

기존 `Game → Systems` 구조와 GNM의 경기 생성·Tick 역할을 유지하면서, 확인된 결함 18개 항목의 실행 경로를 수정했다. 아래 항목은 파일 분리 여부가 아니라 실제 동작을 기준으로 묶었다.

| 항목 | 반영 내용 |
|---|---|
| A1 | 엔진에 획득 정보를 보존하고, 플레이어의 Owner 동기화로만 전달. 공개 낙찰 공지와 참가자별 비공개 결과를 연결하고 기존 정보 수첩을 실제 HUD에 연결 |
| A2·G9 | 서버 순위 → 기존 결과 화면 → 방장의 로비 복귀 → 준비·좌석·개인 상태 초기화 → 다음 경기 시작 경로 연결 |
| A3 | 이모트·점프 의도를 Command/Rpc로 전달하고 원격 Animator의 점프 상태 연결 |
| B1 | 서버에서 좌석 ID·종류·슬롯·점유 검증. 공식 서버 씬 생성 시 홀의 좌석 목록을 복사하므로 홀 없는 전용 서버에서도 동일하게 검증 |
| B3 | 로컬 좌석 비활성화·착석 실패·컴포넌트 종료가 서버 좌석 해제로 수렴하도록 수정 |
| B4 | 최고 입찰자 판정을 표시 닉네임 대신 안정적인 참가자 ID로 변경 |
| C2 | `PlayerAssetStatus`가 직접 소유자 상태를 구독. 거래 컨트롤러가 꺼져 있어도 HUD 자산 갱신, 첫 수신 전에는 미수신 표시 |
| C3 | 경매 정보의 4라운드 범위·두 경매 슬롯의 순서와 겹침 검증. 입장 정원을 현재 규칙의 지원 인원으로 제한하고 결과 화면에 실제 라운드 수 제공 |
| F1 | EventSystem을 로비 화면의 활성 상태와 분리하고 실제 홀에 연결 |
| F2 | 단축 라운드에 비례해 경매 슬롯도 단축. 공개·비공개 경매 모두 마감·정산하며 예약금을 중복 차감하거나 남기지 않음 |
| F4 | 연결 ID와 참가자 ID 분리, 소유자에게 전달한 재접속 토큰으로 같은 서버 프로세스의 진행 중 경기에 복귀. 현금·예약금·획득 정보·외형 복원 |
| F6 | 공개 시장 변경 버전을 관찰해 열린 거래 패널의 다른 참가자 거래 후 재고도 갱신 |
| F7·K1 | 요청 실패·베팅·비공개 입찰에서 공개 시장을 재작성하지 않음. 요청자와 공개 경매에서 실제 밀려난 입찰자 등 변경된 소유자만 갱신 |
| G1 | 요청 종류·성공 여부·회사·상태 버전을 갖는 응답으로 변경. 관련 패널만 응답을 처리하고 소유자 스냅샷 적용 후 결과 표시 |
| H1·H2 | 공개 입찰 성공 시 팻말 연출, 입찰 종료 후 착석 자세 복귀. 서 있을 때도 공개 상품 설명은 보이며 입력만 경매석으로 제한 |

관련 확인 경로도 함께 보완했다. B6·I3의 모달 중 이동·점프·라디얼 입력을 차단하고, 수첩을 닫은 Esc가 같은 프레임의 일어서기 요청으로 이어지지 않게 했다. D6은 경기 진입 시 활성 플레이어의 접촉 대상을 다시 연결했다. K2는 별도 시작 알림 대신 동기화된 `inMatch`로 진입하고, K3은 미승인 Ready를 차단한다.

재접속은 서버 재시작 후 복원이나 다른 기기의 계정 로그인을 포함하지 않는다. 진행 중 입찰은 이탈 전 상태를 보존하며, F3의 자동 취소·몰수 정책은 추가하지 않았다. F5 착석 중 이모트, B2·K4 위치 신뢰, H3 자산 화면 범위, H4 다중 방은 별도 설계 결정으로 남는다. G10 외형 연속 입력과 J5 성능 전체를 검증 완료로 바꾸지 않는다.

### 이번 수정의 실행 검증

| 검사 | 실제 결과 | 로컬 실행 증거 |
|---|---|---|
| Windows 전용 서버·클라이언트 빌드 | 두 빌드 성공, 프로세스 종료 코드 0. 서버는 `SC_Server`, 클라이언트는 `SC_MainMenu`·`SC_ExchangeFurnished` 포함 | [최종 빌드 로그](../../.utmp/defect-fix/build-final.log) |
| 새 규칙 회귀 검사 | 4/4 통과. 단축 경기의 두 경매 정산·실패 요청의 자산 불변·잘못된 일정 거절·중복 정산 방지 | [NUnit XML](../../.utmp/defect-fix/regression-verified.xml) |
| 전체 EditMode | 65개 중 63개 통과, 2개 실패. 실패는 CueAudio의 전체 씬 탐색 중 수정하지 않은 Mirror 샘플의 `LightingData.asset` 직렬화 오류 | [NUnit XML](../../.utmp/defect-fix/rules-final.xml) |
| 전체 PlayMode | 13개 통과, 실패 0, 커서 잠금 1개는 게임 창이 없는 배치 실행이라 Ignore. 결과 화면의 실제 버튼 이벤트로 복귀하도록 회귀 검사 보강 | [NUnit XML](../../.utmp/defect-fix/play-verified-noaudio.xml) |
| 홀 없는 전용 서버 + 독립 UDP 연결 | 22개 검사 통과. 동명이인·좌석 경합·소유자 전용 전송·비공개 입찰 델타·재접속 상태 보존·정산·다음 경기 확인 | [검사 기록](../../.utmp/defect-fix/network.json) |
| 빌드된 서버 + 실제 Unity 원격 클라이언트 | 15개 검사 통과. 소유자 hook/응답 순서·다른 참가자 거래 후 패널 재고·원격 점프/이모트/팻말·정보 수첩·결과 버튼·다음 경기 확인 | [검사 기록](../../.utmp/defect-fix/remote.json) |

실제 홀에서 [정보 수첩](../../.utmp/defect-fix/notebook.png)과 [결과 화면](../../.utmp/defect-fix/results.png)을 캡처해 표시 내용도 확인했다. `.utmp`의 로그·XML·이미지는 이 체크아웃의 로컬 실행 증거이며 Git 추적 대상이 아니다. 전체 PlayMode 최종 실행에는 `-noaudio`를 사용했다. 직전 실행은 FMOD 출력 장치 초기화 오류 때문에 1개가 실패했으며, 이를 기능 실패와 구분해 기록한다. 별도 PC 간 LAN, OS 커서 잠금 수동 검사를 이번 PASS에 포함하지 않는다.

재현 진입점은 `Dev/Tests/EditMode/Auction/MatchRegressionTests.cs`, `Dev/Tests/PlayMode/Hall/MatchRegressionPlayTests.cs`, `Dev/Editor/Lobby/NetworkDefectVerification.RunBatch`, `Dev/Editor/Lobby/RemoteMatchVerification.RunBatch`다. 마지막 검사는 별도 서버를 `-port 17778 -minPlayers 1 -roundSeconds 4`로 먼저 실행한다.

## 판정 기준

| 표시 | 의미 | 작업 원칙 |
|---|---|---|
| 확인된 결함 | 요구 기능 누락이나 잘못된 값·상태로 이어지는 코드 경로를 확인. 실행 재현을 완료했다는 뜻은 아님 | 해당 경로의 최소 수정과 회귀 확인부터 |
| 재현 필요 | 타이밍·입력·씬 수명·성능 등 실제 영향 확인이 남음 | 재현 조건과 기대 동작을 먼저 확인 |
| 설계 결정 | 허용 동작·운영 범위·이탈 정책 등에 선택이 필요 | 현행 결정과 구분하고 선택 전 구현을 확정하지 않음 |
| 선택적 개선 | 현재 고장 근거는 없고 가독성·변경 편의의 이점이 있는 후보 | 현재 변경을 방해할 때만 작은 대안부터 |

여러 성격이 섞인 항목은 본문에서 구분한다. 정정한 항목도 추적을 위해 번호를 남긴다. 직접 참조·조건문·배열·sentinel·공통 기본값의 존재만으로 결함을 판정하지 않는다. 검증 코드의 존재, 이번 실행 여부, 현재 PASS도 구분한다.

## 목차

| 섹션 | 내용 | 초기 점검 |
|---|---|---|
| [A](#review-a) | 미구현·미통합 기능 | 1차 |
| [B](#review-b) | 서버 검증과 클라이언트 UX | 1차 |
| [C](#review-c) | 규칙·상태·값의 출처 | 1차 |
| [D](#review-d) | 역할·의존 관계의 개선 후보 | 1차 |
| [E](#review-e) | 개발용 코드·미통합 자산·잔재 | 1차 |
| [F](#review-f) | 동작 경로·재현·미정 정책 | 2차 |
| [G](#review-g) | 응답·동기화·참조 수명 | 2차 |
| [H](#review-h) | 설계 문서와 구현의 차이 | 2차 |
| [I](#review-i) | 함수·파일 내부 개선 후보 | 3차 |
| [J](#review-j) | Unity 구조·직렬화·프레임 비용 | 3차 |
| [K](#review-k) | 네트워크 점검과 검증 범위 | 3차 |
| [작업 순서](#work-order) | 결함 중심의 최소 변경 순서 | 종합 |

## 한눈에 보기

**확인된 결함·요구 기능 누락**

- A1 정보·낙찰 결과 전달, A2·G9 종료 결과/방 복귀, F4 결정된 재접속 지원의 미구현.
- B1 좌석 존재·종류·슬롯 검증, B3 착석 상태 불일치 경로, B4 닉네임으로 본인 판정, F1 EventSystem 수명, F2 짧은 override의 미정산 경매, F6 다른 플레이어 거래 후 재고 미갱신.
- G1 응답 구분 누락, F7·K1 불필요한 전원 갱신과 활동 시점 단서. 요청자 신원 노출까지 확인한 것은 아님.

**재현·범위 확인**

- B6 커서·Esc 경합, D6 접촉 대상 탐색 시점, G10 빠른 외형 변경, I3 입력 차단 경로, I21 HUD 잔류 문구, K2 경기 시작 순서, K3 승인 전 Ready 처리.
- J5 성능 영향은 측정 전이다. 반복 탐색·할당과 필요한 타이머·물리 갱신을 구분한다.

**설계 결정**

- F3 이탈 시 입찰·예약금 유지/해제, F5 착석 중 이모트, B2·K4 거래 장소와 위치 신뢰 수준, H3 전체 자산 UI 범위, H4 다중 방 운영 범위.
- H5의 문서 모순은 현재 README 기준으로 정리한다. MatchController 신설을 기본 결론으로 삼지 않는다.

**선택적 개선과 판정 정정**

- D1~D5·G3~G6의 참조·역할 정리, I7~I23 중 선택적 개선으로 표시한 함수·데이터 표현, J2·J4의 추출·공유 에셋 후보는 현재 변경을 방해할 때 검토한다.
- J1 다섯 계층, 일괄 View/Presenter 분리, I12 Gateway 추가는 필수 순서에서 제외한다.
- I1은 샌드박스 초기화의 부수효과, I2는 정상 경로의 상태 불일치 재현 없음. I4의 서 있을 때 슬롯 0·취소 -1은 현재 계약만으로 버그가 아니다.
- G2 늦게 켜진 거래 UI는 포트폴리오 캐시를 복원한다. G7 공식 서버 빌드는 씬을 자동 갱신한다. K8 입장·정원·버전·타임아웃의 기존 에디터 검증이 있다.
- K6 파일 이동만으로 메시지 ID가 바뀌지 않는다. I23 override의 base.Update 미호출 자체는 상속 오류 근거가 아니다.

---

<a id="review-a"></a>

## A. 미구현·미통합 기능

### A1. [확인된 결함] 경매 결과를 아무에게도 전달하지 않음 — 낙찰자는 돈만 내고 정보를 못 받음

- `Systems/Match/MatchEngine.cs:23`이 `InformationAwarded`/`PublicResolved`/`SecretResolved`를 발생시키지만 `Network/GameNetworkManager.cs`는 하나도 구독하지 않음. 구독자는 Dev 샌드박스 `MatchUIBridge`와 에디터 검증뿐.
- 낙찰자 현금은 `SpendReserved`로 빠지는데 산 방향 정보(Lot)는 클라이언트로 가지 않음. 공개 낙찰자 공지, 비공개 성공/실패 통지도 없음.
- `UI/Notebook/InformationNotebookView.cs`의 `Present`도 런타임에서 부르는 곳이 없음. 그런데 `UI/Auction/Panel/AuctionPanelView.cs:43`은 "낙찰자만 방향 확인 · I 내 정보"라고 안내함(I키 처리는 Dev `PlayerUIController`에만 있음).

→ 기존 GNM의 경기 시작/정리 경로에서 엔진 결과 이벤트를 연결·해제하고, 낙찰 정보는 소유자 전용으로 전달한다. 공개 결과와 비공개 성공/실패의 수신 범위를 구분하고 기존 노트북을 연결한다. 별도 MatchController 추출은 선행조건이 아니다.

### A2. [확인된 결함] 매치 종료 후 순위 전달과 방 리셋이 없음

- `Network/GameNetworkManager.cs:173` — 끝나면 `engine.Standings()`를 서버 `Debug.Log`로만 찍고 클라이언트엔 `phase=Finished`만 감.
- `EndMatch`(`:142`)는 서버 시작·종료와 전원 퇴장(`:325`) 때만 불림 → 끝난 방은 `MatchStarted=true`로 남아 새 입장을 계속 거절.
- `UI/Result/MatchResultView.cs`는 "권위 있는 결과를 받는다"는 전제로 만들어졌지만 Dev 쇼케이스에서만 쓰임.

→ 서버가 구조화된 순위를 내려보내고, 로비 복귀 Command와 상태 리셋을 담당.

### A3. [확인된 결함] 이모트·점프 애니메이션의 원격 전달 누락

- 로컬 `Player.prefab`은 `Player Animation.controller`를 `PlayerController`/`SeatController`가 문자열 트리거로 직접 조작("Defeat", "Hadraise", "Player_Idle", "Seated" …) — `Characters/Player/PlayerController.cs:167-200`, `SeatController.cs:81-86`.
- 원격 아바타는 `WolfCharacter.controller`를 `BBBWolfCharacter` API로 움직임.
- `PlayerNet`에 이모트·점프 애니메이션 의도를 보내는 경로가 없다. 위치 동기화로 점프의 이동 자체는 보일 수 있으므로 점프가 전혀 안 보인다는 단정은 철회한다. 원격 자세·타이밍은 별도 클라로 확인한다.
- 착석 자세도 로컬은 "Seated"/"Player_Idle", 원격은 "Seated"/"Locomotion"으로 따로.
- 같은 파일의 Shift/E 구분은 컨트롤 경로에 "shift" 문자열이 있는지로 판단(`IsShiftControl`).

→ 우선 원격에 필요한 애니메이션 의도를 전달하고 로컬·원격의 자세와 타이밍을 확인한다. 공통 BBBWolfCharacter API 사용은 기존 컨트롤러와의 호환을 보며 정리한다. Shift 별도 액션은 리바인딩 등 입력 구성을 손댈 때 함께 검토하며 원격 연출 수정의 선행조건은 아니다.

### A4. [설계 결정] 상호작용 경로가 둘로 갈라짐

- 표시(`UI/HUD/DetectObject.cs`, `UI/HUD/HUDController.cs`)는 `IInteractable.CanInteract`/`Prompt`로 "E 문구"를 띄움.
- 실행(`Characters/Player/PlayerInteraction.cs`)은 `NPCInfo`와 `Seat`만 특수 처리하고 `IInteractable.Interact`를 부르지 않음 → `Door.interactable`을 켜면 "E Open"은 뜨는데 문이 안 열림.
- 거래 NPC는 `IInteractable`이 아니라 HUD가 레이어 번호 `7`을 하드코딩해 "Trade"를 띄움.
- `Content/Props/Door/Door.cs` 열림 상태는 네트워크 동기화도 없음(지금은 꺼져 있음).

→ 우선 표시한 안내와 실제 실행 대상이 일치하게 한다. 문 사용을 활성화할 때 실행 경로를 함께 연결하고, 착석 요청은 서버 승인 경로를 유지한다. NPC까지 인터페이스 하나로 통일하는 것은 반복 분기를 줄일 필요가 있을 때의 선택이다.

---

<a id="review-b"></a>

## B. 서버 검증과 클라이언트 UX

### B1. [확인된 결함] 착석·경매석 검증이 클라이언트 몫

- `Characters/Player/PlayerNet.cs:164-171` `CmdSit`은 좌석 ID를 길이 64 이하 아무 문자열로 받고 위치·거리를 확인하지 않음("reaching the seat is checked locally").
- `Network/GameNetworkManager.cs:251` `Bid`는 "앉아 있는지"만 확인 → 라운지 소파든, 의자가 없는 곳이든 아무 ID로 앉은 뒤 입찰 가능.
- 서버 좌석 슬롯 상한도 리터럴 `slot > 7`(`PlayerNet.cs:167`)로 실제 `Seat.sittingPoints`와 무관.

→ 서버가 좌석 목록(ID, 슬롯 수, 경매석 여부)을 알고 검사.

### B2. [설계 결정] 데스크 앞에서만 거래 가능하다는 규칙을 클라이언트만 지킴

- 거래 패널은 NPC 직원을 조준해야 열리지만(`PlayerInteraction.cs:26-30`), 서버 `Trade`(`GameNetworkManager.cs:238-240`)는 phase와 수량만 봄 → 클라이언트를 고치면 어디서든 어느 회사든 `CmdTrade` 가능.

→ 의도된 규칙이라면 서버가 위치나 "현재 상호작용 중인 데스크"를 검증.

### B3. [확인된 결함] 클라이언트만 일어서서 서버 착석 상태와 어긋날 수 있음

- `SeatController.cs:65-72` `OnServerSeat`에서 좌석을 못 찾으면 `RequestStand()`를 부르는데, `!IsSeated`면 바로 return → 서버엔 `hallSeat`가 남음.
- `PlayerSeating.OnDisable`(`:71`), `Seat.OnDisable`은 `ForceStand`로 로컬에서만 일어서고 `CmdStand`를 안 보냄.
- 서버 입찰 자격은 `hallSeat`만 보므로 서 있는데도 입찰이 통과될 수 있음.

→ 좌석을 못 찾거나 로컬 좌석이 비활성화될 때도 서버 착석 해제 요청이 전달되도록 한다. 로컬 해제는 서버 상태와 수렴하게 하고, 서버가 내린 해제 hook에서 다시 Command를 보내는 루프는 피한다.

### B4. [확인된 결함] "내가 최고 입찰자인가"를 닉네임 문자열로 판단

- 서버는 `MatchNet.highestBidder`에 닉네임만 실음(`GameNetworkManager.cs:199`), `RoomRoster.TryReserve`(`Systems/Room/RoomRoster.cs:14-20`)는 닉네임 중복을 허용.
- `UI/Auction/Panel/AuctionSeatPanel.cs:127` `highestBidder == local.nickname` → 같은 이름 두 명이 둘 다 "최고 입찰 중"으로 보이고, 입찰 버튼 라벨과 "거래 가능 $"(`AuctionPanelView.cs:52-54`)도 틀어짐.

→ 본인 전용 값 또는 공개된 안정적인 플레이어 식별자로 비교한다. 닉네임 중복 허용 여부는 별도 정책이며, 이 버그를 고치기 위해 중복 이름을 금지할 필요는 없다.

### B5. [선택적 개선] 방장 판정과 시작 조건을 클라이언트가 따로 계산

- 서버 `CanStartMatch`(`GameNetworkManager.cs:40-47`)는 `players.Length==roster.Count`(승인됐지만 아직 스폰 안 된 사람 없음)까지 봄.
- `UI/Lobby/LobbyHud.cs:215,239-241`과 `Common/TestBot.cs:58-60`은 인원·ready만 봄 → 버튼이 떠서 눌러도 서버(`:94`)가 응답 없이 무시. 방장 판정(가장 낮은 seat)도 세 곳에 복사.

→ 서버의 최종 검증은 유지한다. 클라의 버튼 활성화 예측 자체는 정상적인 UX다. 짧은 스폰 대기 중에도 시작 불가 사유를 표시하거나 재시도할 수 있게 한다. 시작 가능/방장 상태의 별도 동기화는 불일치가 반복되어 UX를 해칠 때 검토한다.

### B6. [재현 필요] 커서·Esc를 여러 스크립트가 각자 처리 — 버그 가능

- `Cursor.lockState`/`visible`을 직접 쓰는 곳: `UI/HUD/CursorController.cs`(매 프레임 다시 잠금), `UI/HUD/RadialMenuController.cs:60,88,104`(열린 동안 매 프레임 풀고 닫을 때 강제로 잠금), `UI/Lobby/HallSession.cs:29,38`. CursorController는 라디얼 메뉴를 몰라서 같은 프레임에 잠금/해제를 주고받음.
- `CursorController.cs:56`이 Esc로 거래 패널을 `Close()`함. 중앙 취소 조정자로서 의도한 역할일 수 있으므로 이 참조만으로 책임 오류를 확정하지 않는다.
- 같은 키를 Input System 액션과 `Keyboard.current` 폴링이 나눠 처리한다. E 상호작용은 액션(`PlayerController.OnInteract`), 거래 확인 E/Enter·방향키는 TradePanelController, Esc는 SeatController와 CursorController에 있다. openedFrame은 열기 입력이 즉시 확인으로 처리되지 않게 하는 가드다. 리바인딩 시 두 입력 경로를 함께 고려해야 한다.

→ 라디얼 메뉴·거래·착석에서 같은 프레임 입력이 어떻게 소비되는지 재현하고, 커서 상태를 한 곳에서 조정한다. 우선 기존 패널 상태와 취소 순서를 연결한다. 입력 리바인딩·게임패드 범위를 정한 뒤 UI 액션맵 통일이 필요한지 판단한다. openedFrame 가드는 열기 입력의 즉시 확인 방지라는 목적이 있다.

---

<a id="review-c"></a>

## C. 규칙·상태·값의 출처

### C1. [선택적 개선] 규칙값 중복과 UI 미리보기 계산을 구분

- `TradingPanelView.Present`의 하한가 `price>5`는 엔진의 `Settings.MinPrice`와 별도로 적혀 있다. 배당 `price*2`와 `AuctionSeatPanel.MinimumBid`의 최초 최소 입찰가도 규칙이 바뀌면 함께 바꿔야 한다.
- 현재 입력 수량에 대한 예상 비용·매수 가능 수량·거래 후 현금 계산은 UI에 있어도 된다. 최종 상태 변경과 요청 유효성은 서버가 검증한다.
- `AuctionPanelView`의 예약금 미리보기도 표시용 계산이라는 사실만으로 잘못은 아니다. 서버 규칙과 실제로 어긋나는 입력을 확인해야 한다.

→ 변경 가능한 규칙값을 기존 설정/공개 데이터에서 제공하고 경계값을 비교한다. 모든 예상 금액을 서버에서 계산해 내려주거나 별도 Presenter를 도입할 필요는 없다.

### C2. [확인된 결함] 로컬 자산 상태가 두 군데, 동기화는 거래 패널 컨트롤러가

- `PlayerNet.LocalPortfolio`와 `PlayerAssetStatus`는 수신 캐시와 표시용 사본이다. 사본 자체가 결함은 아니지만 둘을 갱신하는 역할이 거래 UI 연결 컴포넌트에 묶여 있다. `LocalPortfolio`의 setter는 private이고 `PortfolioSnapshot`은 외부 변경 API가 없으므로 아무나 쓰는 mutable global은 아니다.
- HUD 현금(`HUDController.cs`)과 `AuctionSeatPanel`은 `PlayerAssetStatus`를 읽음 → LocalTradeController가 꺼지면 HUD 현금이 멈춤.
- `PlayerAssetStatus.cs:12` 기본 `cash = 100`, 실제 시작 현금은 `Content/Match/MatchSettings.json`의 `StartingCash: 200` → 첫 스냅샷 전까지 $100 표시 가능.

→ HUD·경매가 거래 패널의 활성 여부와 무관하게 최신 수신 상태를 읽도록 기존 갱신 경로를 정리한다. 첫 수신 전은 미수신으로 표현한다. 새로운 클라이언트 Model 계층이 필요한 것은 아니다. 늦은 거래 UI 활성화에는 이미 캐시 복원 경로가 있다(G2).

### C3. [확인된 결함] 설정 불일치 검증 누락과 고정 상수의 구분

아래 중 결함은 지원하지 않는 인원·경매 정보 범위가 시작까지 통과하는 경로다. 숫자가 반복된 나머지 사례는 목적·변경 빈도에 따라 선택적으로 정리한다.

- 정원 6: `RoomRoster.Capacity`, 오류 문구 "(최대 6명)", `GameNetworkManager.cs:336-337`의 리터럴 `6`, `PlayerNet.cs:54`의 배치 중심 `2.5`, `LobbyHud.cs:216-217`의 `"/ 6"`·`i<6`. JSON엔 `MaxPlayerCount: 6`이 따로 있음 → JSON을 줄이면 방에 6명이 들어온 뒤 `MatchSettings.GetInitialCompanySupply`(`MatchSettings.cs:35`)가 예외를 던져 시작 실패.
- 최소 인원: 두 씬의 minPlayers와 JSON의 MinPlayerCount가 있다. 시작 허용 인원 override와 규칙이 지원하는 인원 범위는 의미가 다를 수 있고, 서버 씬은 공식 빌드에서 자동 복사된다(G7).
- ConnectionController가 설정하는 maxConnections는 클라 접속에서 서버의 상한을 바꾸지 않는다. 서버가 쓰는 값은 SC_Server의 설정이다. 연결 상한과 승인 참가자 정원도 목적을 구분한다.
- 경매 정보 범위 4라운드가 `Systems/Auction/LotGenerator.cs:10,25`의 const에만 있고 `MatchSettings.Validate`(`:79-82`)가 검사하지 않음 → AuctionRounds에 15를 넣으면 검증은 통과하고 매치 시작 때 예외.
- 최소 입찰가 `1`이 설정 없이 `MatchEngine.cs:79-80`, Dev `MatchManager`, UI(`AuctionSeatPanel.cs:93`)에 각각 리터럴.
- 라운드 수 17: `UI/Result/MatchResultView.cs:38`("17 / 17 ROUND"), `UI/Notebook/InformationNotebookView.cs:19`(`totalRounds=17`).
- 회사 수 3·심볼: `UI/Ticker/TickerBoardDisplay.cs:57,65,83,103-106,117`(`%3`, `c<3`), `:175`(`{"STRW","TMBR","BRCK"}`). 서버는 `Company` enum 사용.

→ 우선 방 정원과 경기 설정의 호환, 경매 정보 범위 등 실제 시작 실패를 유발하는 설정을 검증한다. 서버 최소 시작 인원 override와 규칙의 지원 인원 범위는 목적을 구분한다. 고정된 회사 3개·탭 수·로비 배치값은 상수로 유지할 수 있으며 모든 숫자를 가변 설정으로 만들지 않는다. 이미 있는 JSON을 활용하고 SO 도입을 전제로 하지 않는다. 두 씬 설정은 공식 서버 빌드에서 자동 갱신된다(G7).

### C4. [선택적 개선] 외형 선택지·팔레트가 여러 곳에

- 개수: 서버 검증 `PlayerNet.cs:96`(`upper>2`, `lower>1`), UI `LobbyHud.cs:139`(`%3`, `%2`, `%6`), `LobbyScreen.cs:46-48`와 `BBBWolfAppearance`의 배열 길이가 별도다. 모델 배열에 옷만 추가하면 현재 UI에서도 선택되지 않는다. UI 범위만 늘리고 서버를 그대로 두면 거절될 수 있다는 유지보수 위험으로 정정한다.
- `Customize`에 `inMatch` 검사가 없다. 매치 중 외형 변경을 허용할지는 설계 결정이며, 금지한다면 서버에 가드를 둔다.
- 팔레트: `PlayerNet.Colors`(6색)와 `Systems/Types/TieColor.cs`(4색 enum, `PlayerInfo.tieColor`에서만 쓰고 읽는 곳 없음).
- 적용: `PlayerNet`의 `LateUpdate`(로비)와 `FollowInMatch`(원격)가 매 프레임 `SetOutfit`/`SetTieColor` 호출(매 프레임 렌더러 토글과 MaterialPropertyBlock 재설정), `UI/Lobby/HallSession.cs` `BeginMatch`에도 같은 코드.

→ 실제 외형 선택지를 늘릴 때 UI·서버 범위를 한 출처 또는 일치 검증으로 맞춘다. 카탈로그 SO는 선택 사항이다. 변경 비교나 hook으로 반복 적용을 줄일 수 있으며 초기 아바타 생성·매치 진입에도 외형이 적용되는지 확인한다.

### C5. [선택적 개선] 설정(음량·전체화면) 처리가 흩어짐

- `UI/Lobby/LobbyHud.cs:97-98` 옵션 메뉴가 `AudioListener.volume`/`Screen.fullScreen`을 직접 변경, `UI/Menu/MainMenuActions.cs` `Volume`도 직접.
- `UI/Menu/Settings/SettingsPanelView.cs`의 `ApplyRequested`는 런타임에 구독자가 없음(Dev 쇼케이스만). 어디서도 저장하지 않음.
- 닉네임은 `DirectConnectionSettings.Save`가 저장하고 `LobbyHud.cs:61`이 `PlayerPrefs`를 직접 읽음. 닉네임 키 `PreferenceKey`는 순수 규칙 계층 `Systems/Room/Nickname.cs:8`에 있음.

→ 설정 저장이 필요한 범위를 정하고 기존 적용·저장 함수를 모아 공유한다. 두 화면에서 음량을 적용한다는 이유만으로 서비스 인터페이스가 필요한 것은 아니다.

### C6. [선택적 개선] 라운드 시간표 규칙 두 벌

- `Systems/Round/RoundClock.cs`는 테스트에서만 쓰이고, 실제 마감 계산은 `MatchEngine.StartRound`(`:78`)가 override까지 포함해 따로 함.

→ 시간 규칙을 바꿀 때 실제 엔진 경로와 테스트를 먼저 맞춘다. RoundClock의 Dev 사용 의도를 확인하고 통합 또는 제거를 선택한다. 테스트에서 사용하는 코드를 호출처 없는 죽은 코드로 간주해 바로 삭제하지 않는다.

---

<a id="review-d"></a>

## D. 역할·의존 관계의 개선 후보

### D1. [선택적 개선] GameNetworkManager의 경기 연결 코드

- 명령줄 파싱, 입장·명단·좌석 배정, 경기 생성·Tick·상태 공개·요청 중계, 끊김 시 화면 복귀가 한 파일에 있다(`Network/GameNetworkManager.cs`).
- 경기 생성·관리 기능을 연결하고 Tick을 구동하는 것은 README가 GNM에 부여한 책임이다. 메서드 수나 경기 코드의 존재만으로 God Object를 확정하지 않는다.
- A1·A2의 결과 전달 누락은 별도 MatchController가 없어 발생했다고 입증되지 않았다. 기존 경기 수명 경로에 이벤트 연결과 결과 전송을 추가할 수 있다.

→ 현재 수정에서 읽기 어려운 메서드·중복만 작게 정리한다. MatchController 추출은 현행 MatchHost 금지·폴더 규칙과 비교할 별도 구조 변경 제안이며 필수 작업에서 제외한다(H5).

### D2. [선택적 개선] PlayerNet의 연출·규칙값 분리 후보

- `PlayerNet`에는 로비 배치, 팔레트, 아바타 표시·따라가기, Command, 소유자 상태 수신, 좌석 점유 조회가 있다.
- 플레이어별 네트워크 입출력을 한 오브젝트에 모으는 것은 응집된 책임일 수 있다. 거래·입찰 Command가 함께 있다는 이유만으로 분리하지 않는다.
- `HallSession`의 `SeatPosition`/`Colors`, `Seat`의 `IsSeatTaken` 의존은 공유값·클라이언트 점유 조회다. 실제 수정 비용과 서버/클라 실행 위치를 따로 본다(C4·G3·G8).

→ 먼저 불필요한 서버 연출 실행과 반복 외형 적용을 줄인다. 배치·팔레트·연출 추출은 변경이 잦거나 독립 재사용할 때만 검토한다. 로컬 포트폴리오 수명 개선이 별도 Model 계층을 요구하지는 않는다.

### D3. [선택적 개선] UI와 경기 진입 조정의 역할

- `UI/Lobby/ConnectionController`는 접속 화면의 흐름, 주소·포트·닉네임 설정, StartClient/StopClient와 저장을 조정한다. 이 역할에서 네트워크 API를 호출하는 것만으로 위치가 잘못된 것은 아니다. `Network/`에는 GNM 코드 파일 하나만 둔다는 규칙이 있으므로 그곳으로의 이동 처방은 철회한다.
- `HallSession.BeginMatch`의 화면·위치·입력·속도 전환은 경기 진입 조정으로 묶일 수 있다. 플레이어 초기화가 복잡해지면 기존 Player 컴포넌트의 메서드로 묶을 수 있지만 HallSession을 화면 토글만 하게 만들 의무는 없다.
- `DetectObject`의 레이캐스트와 조준점 변경은 상호작용 안내를 위한 구성이다. 감지 결과를 여러 기능에서 재사용하게 되면 표시와 분리할지 검토한다.
- `LobbyHud.Refresh`의 FOV 계산은 화면 비율 대응이다. 별도 카메라 컴포넌트 신설보다 값 변경 시에만 계산할 수 있는지부터 본다.

→ 현재 오브젝트별 폴더를 유지하고, 실제로 독립 사용되는 기능과 반복 갱신만 작게 정리한다.

### D4. [선택적 개선] 캐릭터·UI 연결과 LocalTradeController 위치

- 캐릭터 입력은 거래·라디얼 UI를, HUD·커서는 캐릭터의 착석·자산 상태를 참조한다. 같은 Game 어셈블리 내 직접 참조 자체는 경계 위반이 아니다.
- `Common/LocalTradeController`는 UI·캐릭터·Mirror를 연결하는 거래 조정 역할이다. Common에 둘 일반 컴포넌트인지, 거래 화면 전용인지 점검할 수 있다.
- 실제 확인·수정할 대상은 B6의 커서 경합 가능성, F6의 갱신 누락, C2의 자산 갱신 수명이다. 이를 위해 모든 참조를 이벤트 인터페이스나 SO 채널로 바꿀 필요는 없다.

→ 기존 참조·상태 알림을 활용해 결함을 고친다. LocalTradeController가 거래 UI 전용이면 UI/Market 배치를 검토하되 직렬화 참조를 보존한다. 새 Presentation 계층 도입과는 분리한다.

### D5. [선택적 개선] 거래 패널의 UI 구조를 컨트롤러와 뷰가 둘 다 앎

- `UI/Market/TradePanelController.cs:47-62`가 `"Trade Tabs/Buy"`, `"Payment Summary/Confirm Purchase"` 등을 `transform.Find`로 바인딩하고, `TradingPanelView`도 같은 경로를 다시 Find. 탭은 컨트롤러에선 enum `Tab`, 뷰에선 `int`. `%3`, 패널 배열 크기 3, 인디케이터 `22+tab*265`px 하드코딩. 프리팹 이름 하나만 바뀌어도 버튼이 조용히 안 붙음.

→ 우선 바인딩을 뷰 내부 한 곳에 모아 캐시·검증하고 탭 타입을 맞춘다. 자주 바뀌는 경로는 직렬화 참조로 바꾸며, 외부가 뷰의 계층을 알아야 하는 부분에는 클릭 이벤트를 둘 수 있다. 별도 Presenter 신설은 필수가 아니다.

### D6. [재현 필요] 소품의 접촉 대상 탐색 시점과 로컬 착석 경로

- `RopeBarrier.Start`와 `BannerWind.Start`는 `FindWithTag("Player")`를 한 번 호출한다. 로컬 Player가 비활성 `Gameplay Presentation` 아래에서 시작하므로 탐색 시점에 대상을 얻지 못할 가능성이 있다. A73 로프의 `contacts`도 비어 있다. 실제 매치 진입 후 접촉 반응은 재현 필요이며, "항상 전혀 반응하지 않는다"는 단정은 철회한다.
- 원격 아바타도 접촉 대상으로 포함할지는 연출 범위의 선택이다. 로컬 탐색 문제를 고치면서 자동으로 요구 범위를 확대하지 않는다.
- `Seat.Interact`의 AddComponent·직접 착석은 오프라인 경로다. public 메서드라는 사실만으로 서버 입찰 권한을 우회하지는 않는다. 매치 진입 중 이 경로를 잘못 타는지는 K2와 함께 확인한다.

→ 로프·배너가 활성 플레이어를 확보하는 시점부터 확인하고 필요하면 경기 진입 시 기존 참조를 전달한다. 다수 대상의 동적 등록이 실제로 필요해질 때만 접촉 레지스트리를 검토한다.

### D7. [선택적 개선] 엔진이 표시용 문자열을 만듦

- `MatchEngine.BetDescription`/`Standings`의 문자열은 현재 서버 로그·Dev에서 쓴다. 로그용 문자열 자체는 규칙 계층 오염의 중대한 근거가 아니다. A2에서 클라이언트 결과를 연결할 때 순위 계산 결과와 화면 문구를 구분한다. 이때 순위 자료를 반환하면 되고 모든 로그 문자열을 먼저 없앨 필요는 없다.

---

<a id="review-e"></a>

## E. 개발용 코드·미통합 자산·잔재

### E1. [선택적 개선] 개발 도구가 게임 빌드에 들어감

- `Common/TestBot.cs` — `BuyBidBlow.Game` 어셈블리에 포함, `[RuntimeInitializeOnLoadMethod]`로 모든 빌드에서 부팅 훅이 돎. 씬 이름 `"SC_MainMenu"`를 따로 하드코딩하고 B5의 시작 규칙 사본도 가짐.
- `Common/ILocalMatch.cs` — Dev 전용 `MatchManager` 하나 때문에 런타임에 존재. 그 `MatchManager`는 MatchEngine을 안 쓰고 가격·MinBid·포트폴리오를 재구현해서(재고 무제한, 베팅 없음 등) 서버와 다르게 동작.
- `Characters/Player/PlayerInfo.cs` — 샌드박스 스텁. `DisplayName`/`TieColor`/`IsReady`는 아무도 안 읽고, `"local-player"`가 `LocalTradeController`에도 중복.
- `Content/Props/RopeBarriers/RopeBarrierSimulation.cs:105` `OnGUI` — `Barrier_A_Test`일 때만 뜨는 튜닝 슬라이더. 이름으로 리그를 찾는 분기, `postDistance`·기둥 이동(`1.6f`), `ResetRopes`의 기둥 재배치도 Dev 씬 `SC_RopeBarrierTest`에서만 쓰임. 게다가 점 13개를 하드코딩(`new Vector3[13]`, `i<13`, `s.p[12]`, `length/12`)하는데 `RopeBarrier`는 임의 개수를 허용 → 점 수를 바꾸면 IndexOutOfRange.
- `UI/Lobby/LobbyHud.cs` — `BuildLayout`/`UpgradeConnectionPanel`/`UpgradeDirectConnectionPanel`(에디터 빌더용, 레이아웃이 비면 런타임에 다시 만들기도 함), `:57` 런타임 Awake에서 `"Lobby Art Preview - Editor Only"`를 찾아 Destroy, `:209-218` `Refresh(bool preview)`의 가짜 미리보기 데이터.
- `UI/Ticker/TickerBoardDisplay.cs` — `BuildSurface`와 생성 헬퍼(에디터 빌더용), `:96` `PresentSnapshot`(호출처 Dev/Editor뿐). 화면 모드를 GameObject 이름 접미사(`_Left`/`_Right`)로 결정(`:37-39`).
- 디버그 로그: `Common/MatchNet.cs:82,89,97`(SyncVar hook마다), `PlayerController.cs:169`(이모트마다), `HallSession`.

→ 개발 자동 실행 훅·튜닝 UI의 배포 포함 여부와 로프 점 수 불일치를 각각 확인한다. ILocalMatch는 Game에서 Dev 구현을 참조하지 않게 하는 현재 연결부이며 미사용 추상화가 아니다. 샌드박스 경로를 정리할 때 사용처와 함께 판단한다. 에디터 빌더가 호출하는 런타임 생성 API·로그도 존재만으로 삭제 대상으로 잡지 않는다.

### E2. [선택적 개선] 만들어 놓고 빌드 씬에서 안 쓰는 뷰

프리팹이 어떤 게임 씬·프리팹에서도 참조되지 않고, 컴포넌트는 Dev 쇼케이스 씬에만 있음. 그 사이 실제 게임에선 같은 역할을 다른 코드(`ConnectionController` 메시지, `LobbyHud` 접속·옵션 패널)가 따로 함.

- `UI/Menu/Pause/PauseMenuView.cs`, `UI/Menu/Settings/SettingsPanelView.cs`
- `UI/Lobby/Connection/ConnectionPanelView.cs`, `UI/Lobby/Notifications/LobbyNoticeView.cs`
- `UI/Auction/Result/AuctionResultView.cs`, `UI/Result/MatchResultView.cs`, `UI/Round/RoundStartView.cs`
- `UI/Loading/LoadingView.cs`, `UI/Notebook/InformationNotebookView.cs`, `UI/Shared/Notification/NotificationStackView.cs`
- `UI/Market/Notifications/MarketNoticeView.cs`
- `Content/Props/AuctionEnvelope/AuctionEnvelopeDisplay.cs`(Dev 씬 두 곳에만, 라운드 기본값 6/9 하드코딩)
- `UI/Lobby/LobbyScreen.cs` — 옛 IMGUI 대기실. `SC_Lobby`·`SC_ExchangeFurnished`에 꺼진 채(`m_Enabled: 0`) 남음, `LobbyHud`와 기능 중복.

→ 미통합 예정 기능, 폐기된 구현, Dev 전용 자산을 구분한다. A1·A2에 필요한 노트북·결과창은 기존 자산을 우선 연결한다. 미참조라는 사실만으로 삭제하지 않으며, 폐기가 결정된 것만 참조와 사용 계획을 확인해 옮기거나 제거한다.

### E3. [선택적 개선] 호출부 없는 코드·잔재

- `GameNetworkManager.BroadcastClosing`(`:330`)은 호출처가 없다. 방 닫힘 알림을 없앨지 연결을 복구할지 판단한다. 기존 검증은 이 메시지를 기대하므로 바로 삭제할 근거는 아니다(K8).
- `MatchEngine.Changed`(`:22`)는 구독자 없음. `AuctionRound.OpenStarted/SecretStarted/IsFinished`, `LotGenerator.Remaining`, `MatchSettings.DefaultPlayerCount`는 테스트·Dev에서 쓰인다. 런타임에서 쓰지 않는 API와 호출처가 전혀 없는 코드를 구분한다.
- `UI/Lobby/DirectConnectionSettings.cs:17` `LocalIPv4` — 호출처 없음(호스트 기능 제거 잔재).
- 호스트 버튼 잔재: `LobbyHud`의 두 Upgrade(`:355`, `:383`)가 도움말에 "H: 방 만들기"를 써 넣고 `HideHostButton`(`:155-160`)이 문자열 치환으로 다시 지움(두 곳 공백이 달라 깨지기 쉬움). `Refresh:251`이 숨긴 Host 버튼을 계속 갱신, `FocusConnection:183,185`의 `"Host"` 분기는 도달 불가.
- `UI/HUD/RadialMenuController.cs` — `SelectedIndex`(`:39`), `GetItemMaterial`(`:257-263`) 호출처 없음, `SetHoveredIndex`(`:179`)는 대입만.
- `TickerFormat`이 `TickerPlot.cs`와 같은 파일에 있는 것은 정상적인 보조 타입 배치다. 독립 사용·파일 크기 때문에 탐색이 불편할 때만 분리한다.

---

<a id="review-f"></a>

## F. 동작 경로·재현·미정 정책

### F1. [확인된 결함] EventSystem이 로비와 함께 비활성화되는 경로

- `SC_ExchangeFurnished`엔 EventSystem이 없고, `UI/Lobby/LobbyHud.cs:63-66`이 런타임에 하나 만들어 **자기 자식으로** 붙임.
- LobbyHud는 `Lobby Presentation` 아래에 있고 `UI/Lobby/HallSession.cs:50` `BeginMatch`가 `lobbyPresentation.SetActive(false)` → EventSystem도 같이 꺼짐.
- 이 씬 구성에서는 거래 패널의 마우스 이벤트 처리가 막힐 것으로 판단한다. 키보드 폴링 경로와 별도로 매치 진입 후 EventSystem 활성·마우스 클릭을 실행 확인해야 한다.
- 같은 맥락: `LobbyHud.cs:126-132`가 LateUpdate마다 EventSystem에 `LobbyTextInputBridge`를 붙이고 모든 입력 모듈의 `inputOverride`를 덮어씀 — 로비 HUD가 전역 입력 설정을 가로챔.

→ EventSystem(과 입력 브리지)은 씬/UI 루트에 직접 둠. (코드·씬 계층으로 확인, 실행은 안 해 봄)

### F2. [확인된 결함] 짧은 `-roundSeconds`에서 경매 예약금 미정산

- `Systems/Match/MatchEngine.cs:78` `StartRound`가 override를 경매 라운드에도 적용. `Tick`(`:84`)은 `Auction?.Tick(Deadline)` 후 `StartRound`에서 `Auction=null`.
- override가 Open 슬롯 끝(시작+60초)보다 짧으면 `Close()`가 한 번도 안 불린 채 AuctionRound가 버려짐 → 입찰자 `Reserved`가 경기 끝까지 해제 안 됨. Secret 경매는 아예 안 열림. `Validate`는 override를 검사 안 함.
- 플레이테스트 옵션(`GameNetworkManager.cs:57`)이라 테스트 결과를 오염시킴.

→ 빠른 플레이테스트 옵션에서 경매 시간표도 일관되게 단축할지, 경매 라운드에는 override를 적용하지 않을지 정하고 미정산 예약금이 남지 않게 한다. 경매를 강제 닫는다면 공개/비공개 결과까지 정산한다. 1초 라운드 테스트의 시간 제한도 함께 맞춘다(K8).

### F3. [설계 결정] 매치 중 이탈자의 입찰·자산 유지 정책

- `OnServerDisconnect`는 roster를 해제하지만 MatchEngine의 자산·베팅·입찰은 유지한다. 재접속 지원을 생각하면 상태 유지 자체가 버그는 아니다.
- 현재 닉네임 조회가 연결 목록에 의존해 이탈한 최고 입찰자의 표시명이 사라질 수 있고, F4 때문에 이탈자가 복귀해 결과를 받지 못한다.
- `GameRules`의 접속 이탈 처리는 미결정 사항이다. 이탈 즉시 입찰 무효·예약 해제를 기본 처방으로 삼으면 연결 종료로 공개 입찰을 취소하는 악용이 가능하다.

→ F4의 안정적인 참가자 식별과 함께 이탈 표시·재접속 대기·입찰 유지/해제 정책을 정한다. 표시명 보존은 기존 참가자 데이터로 해결할 수 있다. 별도 매치 컨트롤러 추출이나 자동 입찰 취소를 선행하지 않는다.

### F4. [확인된 결함] 재접속 지원 결정과 달리 재접속이 구조적으로 불가능

- `Docs/Decisions.md:20` "클라이언트 재접속을 지원한다", `Assets/Project/README.md:60` "PlayerId로 식별하며 연결과 구분한다".
- 실제 `GameNetworkManager.cs:122` `PlayerIdOf = "conn-"+connectionId` → 다시 접속하면 다른 ID. 매치 중 입장 거절(`:301`), 매치 중 끊기면 메뉴 씬 로드 후 매니저 Destroy(`:274-280`).

→ 결정된 재접속 지원을 구현할 때 연결과 무관한 참가자 식별·복원 권한·매치 중 재입장 경로를 마련한다. 초기 지원 범위를 바꾸는 것은 별도 설계 결정이며 구현 상태에 맞추기 위해 문구만 삭제하지 않는다. F3의 입찰 정책과 함께 확인한다.

### F5. [설계 결정] 착석 중 이모트와 자세 전이

- `PlayerController.controlsBlocked`는 거래 패널만 반영한다. 착석 중 라디얼 메뉴를 열어 AnyState 이모트가 착석 자세를 덮을 수 있는 경로가 있다. 실제 의자 위 자세·걷기 전이는 플레이 재현 필요다.
- 착석 상태는 이미 `PlayerSeating`이 보유하고 `onSeatedChanged`를 내며 SeatController도 이를 구독한다. 상태의 주인과 알림이 전혀 없다는 기존 표현은 정정한다.
- 여러 소비자가 착석 여부를 읽는 것은 정상이다. 동적 AddComponent 때문에 참조를 매번 탐색하는 부분은 필요 시 별도로 정리할 수 있다.

→ 착석 중 이모트를 금지할지, 착석 전용 동작을 허용할지 정한다. 금지한다면 기존 상태·이벤트로 입력을 막고 허용한다면 Animator 전이를 맞춘다. 새 착석 상태 모델·이벤트 도입은 필수가 아니다.

### F6. [확인된 결함] 다른 플레이어 거래 후 열린 패널의 재고 미갱신

- `Common/MatchNet.cs:23-45`의 prices/stock/auction 필드에 hook·이벤트 없음.
- 거래 패널은 열 때, 내 포트폴리오 수신, Closing Bell 때만 다시 그림(`LocalTradeController.cs:183-194, 252-262`). 다른 플레이어가 사서 `stock`이 줄어도(서버는 요청자에게만 `SendPortfolio`) 열린 패널은 예전 재고 → 서버에서 거절.
- `MatchNet.Current` 교체 알림도 없어서 경매 패널·티커는 매 프레임 `Current`를 다시 읽음(`AuctionSeatPanel.cs:35-48`, `TickerBoardDisplay.cs:46-65`).

→ 우선 열린 거래 패널이 다른 플레이어 거래 후 최신 재고를 반영하게 한다. 가격·재고 변경 알림 또는 변경 비교를 활용하되 Mirror의 리스트 적용 도중 불완전한 데이터를 읽지 않게 한다. 경매·Current 알림 추가와 모든 소비자의 폴링 제거는 별도 선택이며 타이머는 계속 갱신할 수 있다.

### F7. [확인된 결함] 입찰마다 불필요한 전원 포트폴리오 전송

- `GameNetworkManager.Bid`(`:253-255`)는 Request의 요청자 전송 뒤 성공·실패, 공개·비공개에 관계없이 `SendPortfolios()`를 호출한다. 요청자는 같은 처리에서 포트폴리오를 두 번 받을 수 있다.
- 각 클라이언트는 자기 자산만 받지만, 값이 안 바뀐 수신도 다른 요청 활동의 시점·빈도를 추론할 단서가 된다. 이 전송만으로 누가 어떤 비공개 요청을 했는지 특정할 수 있다고 단정하지 않는다. 실제 관찰 가능성은 다인 수신 경로로 확인한다.
- Command 하나에 전원 RPC가 생기는 불필요한 전송 비용도 있다.

→ 요청자에게 필요한 결과·상태는 한 번 보내고, 공개 입찰에서 밀린 이전 최고 입찰자 등 실제 자산이 달라진 사람만 추가 갱신한다. 경매 정산에 따른 대상 갱신은 유지한다. K1의 공개 SyncList 재작성도 함께 줄인다.

---

<a id="review-g"></a>

## G. 응답·동기화·참조 수명

### G1. [확인된 결함] 서버 응답이 종류 없는 문자열 하나로 공유됨

- 거래·베팅·입찰 응답이 모두 `PlayerNet.LocalReply`(`static event Action<string>`, `PlayerNet.cs:110,129`) 하나로 옴. 구독자는 "패널이 열려 있으면 내 것"(`LocalTradeController.cs:183-187`), "경매석에 앉아 있으면 내 것"(`AuctionSeatPanel.cs`)으로 추측.
- 입찰 직후 일어서면 응답이 버려지고, 요청 둘이 동시면 엉뚱한 패널에 뜸. 성공/실패 분기 불가.
- 엔진도 결과를 한국어 문자열로 반환(`MatchEngine.cs:60,62-66,75`), `"입찰 실패: "+result`로 `AuctionError` enum 이름이 화면에 그대로 노출. `MatchNet.cs:13,36-37`의 `Phase`/`AuctionStage`/`PublicLot`는 Common에 정의, 회사는 `int`로 전송. `Decisions.md:26,30`("Command 인자·동기화 값은 Systems/Types의 타입과 enum")과 `UI/Shared/UIIntegration.md:44,46`("UI 문구를 파싱하지 마세요")에 어긋남.

→ 요청 종류·성공 여부·사유가 담긴 결과 타입을 `Systems/Types`에, 문구는 UI에서.

### G2. [설계 결정] Owner 동기화와 TargetPortfolio의 문서 차이

- Decisions는 비공개 상태를 `syncMode = Owner`로 보내도록 정했지만, 현재 `PlayerNet.SendPortfolio/TargetPortfolio`는 소유자 TargetRpc와 로컬 캐시를 사용한다. 전송 방식은 결정 문서와 다르다.
- **늦은 거래 UI 구독 경로는 있다.** `LocalTradeController.OnEnable`(`:47-49`)이 `PlayerNet.LocalPortfolio`를 다시 적용한다. 늦게 활성화된 구독자가 값을 받을 방법이 전혀 없다는 기존 주장은 철회한다.
- 재접속 복원은 F4의 참가자 복구와 최초 전송을 구현해야 하며, TargetRpc라는 사실만으로 불가능한 것은 아니다.

→ 현재 README의 Owner 동기화 요구와 실제 전송 방식을 맞출 필요는 별도로 검토한다. 기존 결정을 유지하면 필요한 소유자 상태만 전환하고, TargetRpc 유지가 필요하면 이유·초기 수신·복원 계약을 제안한다. 단순 UI 결함을 고치기 위해 먼저 별도 NetworkBehaviour를 만들 필요는 없다.

### G3. [선택적 개선] 플레이어 목록·로컬 플레이어를 얻는 길이 여럿

- `PlayerNet.All`(클라 `OnStartClient`에서만 채움, 읽는 곳은 `IsSeatTaken` 하나), `LobbyHud.cs:77,213`(LateUpdate마다 `FindObjectsByType<PlayerNet>`), `LobbyScreen.cs:26`, `TestBot.cs:58`, 서버는 `NetworkServer.connections` 직접 순회(`GameNetworkManager.cs:44,79,145,206,224,335`), `NetworkClient.localPlayer.GetComponent<PlayerNet>()` 6곳.
- `All`은 클라이언트 가시 목록이고 `NetworkServer.connections`는 서버 연결 목록이다. 실행 위치와 역할이 달라 둘이 존재하는 것 자체는 중복 진실이 아니다. 클라의 좌석 예측과 서버의 최종 점유 검사도 모두 필요하다.
- 로컬 `PlayerNet`은 `FindFirstObjectByType<PlayerMove>()`(`PlayerNet.cs:77`)로 "씬에 하나뿐인 몸"을 따라가는데, `HallSession`은 그 몸 참조를 이미 들고 텔레포트까지 하면서 연결은 안 해 줌.

→ 반복 Find가 필요한 곳부터 현재 클라 목록·로컬 플레이어 접근을 재사용하고 수명을 확인한다. 서버 연결과 경기 참가자를 구분할 필요는 있지만 새 컨트롤러·목록을 자동 추가하지 않는다. 로컬 몸 연결은 HallSession이 가진 참조를 전달하는 작은 방법을 검토한다.

### G4. [선택적 개선] 좌석의 네트워크 ID가 GameObject 이름

- `Content/Props/Seat/Seat.cs:19-24` `Id => name`이 서버로 가는 키. static `byId`는 같은 이름이면 경고 없이 덮어씀. 지금 12개는 이름이 다 달라 안전하지만, 이름을 바꾸거나 같은 이름으로 배치하면 서로 다른 의자가 하나로 합쳐짐.
- 이름 문자열도 서버 좌석 목록의 키로 쓸 수 있으므로 B1을 구현할 수 없는 구조라는 주장은 철회한다. 문제는 이름 중복·변경 시 키가 바뀌는 편집 위험이다.

→ 우선 좌석 ID의 중복·서버 목록 일치를 검증한다. 이름 변경과 식별을 분리할 필요가 생기면 직렬화 seatId를 도입한다. 명시 ID 도입 전에도 현재 이름으로 B1의 유효성 검사를 구현할 수 있다.

### G5. [선택적 개선] 비어 있는 참조를 런타임에 찾아 채우는 게 기본 동작

- `Characters/Player/PlayerController.cs:113-127` — `Player.prefab`에서 `playerMove`/`playerInteraction`/`playerAnimation`/`radialMenu`가 전부 `{fileID: 0}`. `GetComponentInChildren<Animator>()`로 첫 Animator를 고르는데 `SeatController`는 직렬화된 `model`을 씀 → Animator가 하나 더 생기면 걷기·이모트와 착석이 다른 Animator로 감.
- `PlayerMove.cs:43-47`(`playerRb`/`playerCamera`도 0), `RadialMenuController.cs:114-130`(`transform.Find("Emotion Menu")`, 이미지 없으면 `GetComponentsInChildren<Image>`), `RopeBarrier.cs:28-32`(`AddComponent<RopeBarrierSimulation>`, `Rope_00..` 본을 이름으로 찾거나 생성), `LobbyHud.cs:60`(`ConnectionController`가 없으면 `AddComponent`).
- 첫 Animator·이름 경로 탐색처럼 다른 대상을 고를 수 있는 연결은 검증 가치가 있다. 같은 오브젝트의 필수 컴포넌트를 GetComponent로 확보하거나 로프 본·시뮬레이션을 절차적으로 만드는 것은 정상적인 구성 방식일 수 있다.

→ 특정 의미의 Animator·외부 UI 참조는 명시 연결하거나 중복 후보를 검증한다. 필수 컴포넌트는 RequireComponent/GetComponent를 활용할 수 있다. 절차적 생성과 의도된 fallback을 일괄 금지하거나 모두 수동 Inspector 작업으로 바꾸지 않는다.

### G6. [선택적 개선] UI 내부 이름 경로에 기대는 곳 (D5 거래 패널 외)

- `UI/Auction/Panel/AuctionPanelView.cs:43-63,78-83` — `"Submit - Visual Only/Submit Label"`, `"Cash After"`, `"Leading Status"` 등. 없으면 조용히 건너뜀.
- `UI/Lobby/LobbyHud.cs:225-226,251-254` — 매 프레임 `"Ready Check"`, `"Editing Dots"`, `"Host"`, `"Join"`, `"Cancel"`, `"Help"`를 Find.
- `UI/HUD/RadialMenuController.cs:116`, `UI/Ticker/TickerBoardDisplay.cs:160`(`"Screen"`).

→ 우선 View 내부 한 곳에서 참조를 확보·캐시하고 필수 경로 누락을 검증한다. 프리팹 구조 변경이 잦거나 외부 코드까지 같은 경로를 안다면 직렬화 참조·표시 API로 의존을 줄인다. 모든 뷰에 새 Presenter를 도입할 필요는 없다.

### G7. [선택적 개선] 서버 씬 사본에는 빌드 시 자동 갱신이 있음

- 홀 씬과 SC_Server에 매니저 설정이 각각 직렬화된다. 하지만 `GameBuild.BuildServer`(`:76-79`)는 **빌드 직전에 UpdateServerScene을 호출**해 홀 씬의 매니저를 복사한다.
- 따라서 공식 서버 빌드에서 매번 수동 갱신을 해야 반영된다는 기존 주장은 철회한다. SC_Server를 직접 실행하거나 별도 빌드 경로를 쓰면 사본의 갱신 여부를 확인해야 한다.
- `matchSettingsFile`은 이미 JSON 에셋 참조다. 클라의 입장 응답 전 MinPlayers fallback과 서버에서만 쓰는 Inspector 필드는 역할을 설명할 여지가 있다.

→ 우선 공식 빌드의 자동 생성 경로와 수동 서버 씬 사용 시 주의점을 명확히 한다. 설정이 실제로 어긋나는 운영 경로가 있을 때 공유값·검증을 보완한다. SO 도입·전면 설정 이전은 필수가 아니다.

### G8. [선택적 개선] 클라이언트 연출 코드가 전용 서버에서도 매 프레임 돎

- `Characters/Player/PlayerNet.cs:56-62`(Awake), `:64-86`(LateUpdate·FollowInMatch)에 `isClient` 가드 없음 → 헤드리스 서버가 플레이어마다 매 프레임 `SetOutfit`/`SetTieColor`, 걷기 속도 계산, 로비에선 서버 transform을 `SeatPosition`으로 덮어씀(NetworkTransform은 ClientToServer).

→ LateUpdate의 클라 연출부터 실행 위치를 제한하고 전용 서버에서 확인한다. Awake의 서버 필수 초기화 여부까지 확인하지 않고 전부 건너뛰지 않는다. 별도 연출 컴포넌트 추출은 같은 문제가 반복될 때 선택한다.

### G9. [확인된 결함] static 로컬 상태를 지우면서 알리지 않음 / 매치 상태를 되돌리는 길이 없음

- `PlayerNet.cs:185` `OnStopLocalPlayer`가 `LocalPortfolio` 등을 지우지만 `LocalPortfolioChanged`를 안 냄 → HUD는 마지막 값 유지.
- `EndMatch`(`GameNetworkManager.cs:142-147`)는 `StandUp`만 하고 `PlayerNet.inMatch`·`ready`를 안 되돌림. 클라의 `clientInMatch`(`:83-88`)·`HallSession.IsPlaying`도 true로만 바뀌고 되돌리는 경로 없음.
- 지금은 끊기면 메뉴 씬을 다시 로드해서 안 드러나지만, A2의 방 리셋을 지금 `EndMatch`로 만들면 클라가 매치 모드로 남고 이전 현금이 보임.

→ 매치 종료를 서버→클라로 알리는 경로 + 로컬 상태 초기화도 같은 이벤트로.

### G10. [재현 필요] 로비 외형 변경을 아직 갱신 안 된 SyncVar로 계산 (경미)

- `UI/Lobby/LobbyHud.cs:136-140` `ChangeVariant`가 `p.top+d`를 로컬 SyncVar로 계산해 `Customize`로 보냄 → 왕복 시간 안에 두 번 누르면 입력 하나가 사라짐.

→ Command를 delta로 바꾸고 서버가 계산.

<a id="review-h"></a>

## H. 설계 문서와 구현의 차이

### H1. [확인된 결함] 입찰 팻말 연출이 없음

- `Decisions.md:35` "입찰하면 해당 캐릭터가 팻말을 드는 모습으로 표시", `README.md:12`, `SystemOverview.html:70,74` ↔ `BBBWolfCharacter`에 `Action.Bid`·`paddle`은 있지만 `PlayAction(Action.Bid)`를 부르는 곳은 Dev 검증뿐. `CmdBid`(`PlayerNet.cs:124`)는 `TargetReply`만 보냄 → 문서가 "관찰 재료"로 삼은 팻말 든 늑대가 안 나옴.

→ 서버가 Open 입찰을 수락한 사건을 참가자에게 전달해 기존 아바타 동작을 재생한다. 일회성 연출과 지속 상태를 구분하고 Secret 입찰 활동은 공개하지 않는다.

### H2. [확인된 결함] 경매 상품 공개 정보가 경매석에 앉은 사람에게만 보임

- `GameRules.md:87` "경매에 올라올 때 대상 라운드와 회사를 공개한다" ↔ `MatchNet.auctionLot`을 읽는 런타임 코드는 `AuctionSeatPanel.cs:117-121`뿐, `AuctionPanelView`는 `seated && live`일 때만 표시. 공개용 `AuctionEnvelopeDisplay`는 미연결(E2).

→ HUD나 홀 소품(봉투·전광판)이 `auctionLot`을 구독.

### H3. [설계 결정] Portfolio 패널이 없음

- `SystemOverview.html:78`, `README.md:14`(UI 목록에 `Portfolio`), `GameRules.md:48` ↔ `UI/Portfolio` 없음. 보유 주식은 거래 창구에서 그 회사 것만 보이고(`LocalTradeController.cs:97`), HUD는 현금만(`HUDController.cs:50`).

→ 전체 보유 자산을 확인하는 UX가 이번 플레이 범위에 필요한지 정한다. 필요하면 기존 자산 표시를 확장하고, 범위를 줄인다면 설계 변경으로 기록한다. UI/Portfolio 폴더가 없다는 사실만으로 새 패널을 의무화하거나 규칙을 삭제하지 않는다.

### H4. [설계 결정] 한 서버에 Room 하나·Match 하나 — 다중 Room/NetworkMatch 구조 없음

- `Decisions.md:25,26,31`(Room 하나에서 Match 여러 번, Match 인스턴스 여러 개를 한 프로세스에서, NetworkMatch ID로 가시성), `README.md:71` ↔ `GameNetworkManager.cs:31`(`roster` 하나), `:117-118`(`matchNet`·`engine` 하나), `NetworkMatch` 사용처 0. 방 코드 없이 IP 직접 접속.

→ 현재 빌드의 한 서버 한 방 제약과 장기 다중 방 설계를 구분해 적는다. 다중 방을 지금 구현할 필요는 운영 범위로 판단한다. 현재 구현에 맞추려고 장기 결정을 자동 폐기하지 않는다.

### H5. [설계 결정] 요청 경로의 문서 모순과 현행 기준

- README·Decisions는 Command가 Systems의 기능별 메서드를 호출하고 별도 MatchHost·통합 GameRequest를 두지 않도록 한다. SystemOverview에는 과거 `PlayerNet → MatchHost → Match` 설명이 남아 있다.
- 실제는 `PlayerNet → GameNetworkManager → MatchEngine`이며 중계·공개 상태 반영이 GNM에 있다. 경기 생성·Tick 연결은 GNM의 명시된 책임이다. `MatchEngine.Bid`가 경매 종류를 분기한다는 사실만으로 통합 MatchHost와 같은 구조라고 확정하지 않는다.

→ 현재 규칙의 기준은 README다. 요청 중계와 시스템 호출을 실제로 정리할 때 이 기준과 비교하고 구형 개요의 설명을 구분한다. D1 분리를 정당화하려고 MatchHost 금지를 먼저 삭제하는 처방은 철회한다. 구조 변경이 필요하다면 작은 대안과 이전 비용을 별도로 제안한다.

### H6. [선택적 개선] 초기 공급량 규칙

- `GameRules.md:44-45` "시작 인원 × 4" ↔ `MatchEngine.cs:29` `GetInitialCompanySupply(Math.Max(players.Count, MinPlayerCount))` → `-minPlayers 1`로 1~2명 테스트 시 회사별 12주. 의도된 편의지만 문서엔 없음.

### H7. [선택적 개선] 시세 전광판 한 오브젝트가 두 폴더에

- `README.md:13,23,32`(시세 전광판은 Content/Props, 오브젝트 하나 = 폴더 하나) ↔ 프리팹은 `Content/Props/TickerBoard/`, 스크립트는 `UI/Ticker/`(`BuyBidBlow.UI.Ticker`).

→ 해당 소품 배치를 정리할 때 README의 오브젝트별 배치 기준에 맞춘다. 스크립트·meta·직렬화된 타입 참조를 함께 보존하며, 현재 배치를 맞다고 간주해 규칙을 역으로 바꾸지 않는다.

### H8. [선택적 개선] 낡은 설명과 현행 구조 규칙을 구분

- README의 폴더 역할과 실제 배치에는 차이가 있다(Systems/Match, UI/Notebook·Ticker 등). 이는 규칙 위반·미구현·문서 노후화 중 무엇인지 판단해야 한다. `Content/Props/TradeCounter`는 배치 예시이므로 실제 폴더가 없어도 문서 오류가 아니다. Portfolio 부재도 규칙 삭제의 근거가 아니다.
- `README.md:58,64,66-67`, `SystemOverview.html:32,42` — "100초 구간", `Dutch`, `AnalystReport`/`InsiderMemo` 등 폐기된 규칙·이름.
- `Decisions.md:36-37` — 옛 Secret 규칙과 Dutch가 본문에 그대로(상단 주석으로만 대체 표시).
- `GameRules.md:123-127`의 구현 범위 설명은 갱신 필요하다. 경매·재고·베팅·순위 계산은 Systems에 있지만 결과의 클라 전달·UI 연결은 A1·A2처럼 미완료이므로 계산 구현과 플레이 완료를 구분해 적는다.
- `UI/Lobby/README.md:3-16,26`, `UI/Lobby/DirectConnection.md:5-7,18-24,37` — `SC_Lobby` 샌드박스, Host, `LobbyRoom`/`LobbyRoster`/`LobbyPlayer`/`LobbyNickname`, "게임 전환 미구현" 등 잔재.
- `UI/Menu/HowToPlay/README.md:8-9` — 빌더 경로가 `../Editor/HowToPlayBuilder.cs`라고 돼 있지만 실제는 `Dev/Editor/MainScene/`.

### H9. [선택적 개선] 명명 규칙과의 차이

- `README.md:93`(Command는 `Cmd*`, hook은 `On*Changed`) ↔ `PlayerNet.cs:89` `SetReady`, `:94` `Customize`(둘 다 `[Command]`), `MatchNet.cs:68` hook `OnClosingBell`.
- `README.md:97`(시각은 `*At`) ↔ `MatchEngine`/`OpenAuction`/`SecretAuction`의 `Deadline`.
- `README.md:84`(`Match`) ↔ `MatchEngine`. `README.md:101`(private 명시) ↔ `MatchEngine.cs:13-16,24` 등.

---

## 현재 코드에서 확인한 경계와 조건

- 조사한 게임 요청 경로에서 클라이언트가 서버 Systems 인스턴스를 직접 바꾸는 호출은 확인하지 못했다. 서버 처리에는 Server 속성 또는 NetworkServer.active 가드가 사용된다. G8처럼 연출의 실행 위치는 별도 확인 대상이다.
- 현재 선언 순서에 따른 hook의 필드 읽기 가정(hallSeatSlot→hallSeat, lastChanges→lastClosedRound→phase→round)은 코드와 맞는다. 별도 시작 메시지의 적용 순서(K2)까지 보장하는 것은 아니다.
- 비공개 입찰 금액은 `TargetPortfolio`(소유자 전용)로만 감. MatchNet엔 공개 경매 값과 Lot의 라운드·회사만, 방향·미래 가격은 전송 안 됨.
- 조사한 주요 이벤트 구독은 OnEnable/OnDisable에서 짝을 이룬다. 서버 코루틴은 연결 동일성과 `NetworkServer.active`를 확인한다. 초기 상태 재적용·종료 알림·새 구독은 각 변경에서 다시 확인한다.
- 조사 당시 Enter Play Mode의 도메인 리로드는 켜져 있었다. 이는 모든 static 수명 문제가 없다는 보장은 아니며 같은 실행 중 연결 종료·재접속은 G9·F4에서 별도로 다룬다.

---

초기 3차 점검의 I~K 번호도 유지한다. 기법 이름은 선택지이며, 현재 고장을 증명하거나 구조 변경을 의무화하는 근거로 대신 쓰지 않는다.

<a id="review-i"></a>

## I. 함수·파일 내부 개선 후보

### 현재 동작과 표현을 구분할 것

- **I1. [선택적 개선] 샌드박스 조회·초기화의 혼합** — `LocalTradeController.ResolveMatchManager`(`:217-239`)가 조회 외에 구독·최초 AddPlayer·자산 반영을 한다. 하지만 네트워크 Validate 경로는 앞에서 반환하므로 실제 서버 경기 참가 버그라는 판정은 철회한다. 샌드박스 유지 시 초기화임을 드러내는 이름이나 작은 함수 분리를 검토한다. bool 반환 자체는 문제가 아니다. 베팅의 가짜 수량 인자는 검증 함수를 손댈 때 분리할 수 있다.
- **I2. [선택적 개선] 선택 회사 상태의 사본** — `TradePanelController`와 `LocalTradeController`가 회사를 각각 보유한다. 정상 OpenPanel은 둘을 함께 설정하고 Validate는 패널이 열려 있는지도 검사한다. 닫은 뒤 hasSelectedCompany가 남는다는 사실만으로 정상 요청 거절이 발생하지 않는다. 재현되지 않은 버그 판정은 철회하며, 상태를 바꾸는 진입점이 늘어날 때 한쪽 값을 조회하도록 정리한다.
- **I3. [재현 필요] 라디얼 메뉴 중 입력 차단 범위** — `PlayerController`의 이동·회전은 radialMenu.IsOpen을 보지만 OnJump·OnInteract는 같은 검사를 하지 않는다. 메뉴 중 점프·상호작용의 기대 동작과 실제 입력을 확인한다. 같은 차단 조건이 맞다면 작은 함수로 공유하되 메뉴를 닫는 Shift 처리까지 막지 않는다.
- **I4. [선택적 개선] sentinel의 계약** — 베팅 없음 -1, 무제한 재고 -1, 준비 중 round=0 등은 경계와 소비자가 뜻을 알고 있으면 정상 표현이다. 착석은 빈 hallSeat로 판단하므로 서 있을 때 hallSeatSlot=0은 버그가 아니다. ModalView.Cancel도 EscapeAction=-1을 취소 동작으로 Submit에 전달하도록 작성되어 있다. 슬롯 0·취소 -1의 버그 판정은 철회한다. 허용되지 않는 값을 잘못 사용하는 경로가 생길 때 상수·검증·nullable·별도 타입 중 가장 작은 방법을 고른다.
- **I5. [선택적 개선] 공개 Lot에 없는 방향 필드** — `PublicLot`에는 공개할 라운드·회사만 있지만 `AuctionSeatPanel`은 뷰에 넘길 Info를 만들면서 Direction=default(Rise)를 채운다. 지금 방향은 표시하지 않으므로 현재 거짓 정보 노출로 확정하지 않는다. 방향을 읽는 소비자를 추가할 때 공개 정보 전용 표현으로 바꿔 미수신을 상승으로 해석하지 않게 한다. 좌석/슬롯의 SyncVar 순서는 현재 설명된 계약이며 SeatRef 전환은 별도 선택이다.
- **I6. [선택적 개선] 외부 쓰기와 규칙 진입점** — `MatchEngine.Trades`에 직접 접근하면 Accept를 거치지 않지만 현재 그런 서버 호출은 확인되지 않았고 클라가 이 필드만으로 서버 권한을 얻는 것도 아니다. 필요한 노출만 남기는 것은 유용하다. 설정 DTO의 public 필드, Mirror SyncVar, mutable struct를 일괄 record/readonly로 바꾸지 말고 실제 불변식·직렬화·쓰기를 확인한다. `PlayerNet.All`은 변경 가능한 목록이므로 소비자에서 임의 수정할 필요가 없다면 읽기 전용 노출을 검토한다.

### 현재 작업에서 읽기 어려울 때만 정리할 것

- **I7. [선택적 개선] 압축된 엔진 함수** — `MatchEngine.Tick`·Standings·생성자 루프를 줄바꿈하고 단계 이름을 드러내면 검토하기 쉬워진다. 이후 필요할 때 SettleBets·AdvanceRound 같은 함수로 나눈다. 한 줄 표현만으로 Long Function을 확정하거나 새 Bet 타입/API를 의무화하지 않는다.
- **I8. [선택적 개선] 가격표 인덱스 산술** — 엔진의 `prices[round][company]`와 전송용 `priceHistory[sample*3+company]`는 다른 표현일 수 있다. 배열 자체는 결함이 아니다. 인덱스 계산이 반복되거나 경계 오류를 낼 때 기존 접근 함수·상수부터 정리한다. PriceTable 클래스는 이런 오류를 실제로 줄일 때 선택하며 서버의 미래 가격을 클라에 공유하지 않는다.
- **I9. [선택적 개선] Open/Secret 분기** — 필드·마감 검사는 비슷하지만 예약·취소·동점·공개 규칙은 다르다. 비슷한 코드 모양만으로 공통 AuctionSlot 부모·다형성을 도입하지 않는다. 실제 같은 검증·표시 함수만 공유하고 다른 규칙은 명시적으로 유지한다.
- **I10. [선택적 개선] TradingPanelView.Present** — 긴 조건식·중첩 삼항을 의미 있는 변수·작은 함수·탭 enum/switch로 정리할 수 있다. `ref quantity` 보정은 화면 표시와 입력 상태 변경을 섞으므로 관련 변경에서 호출부로 옮길지 검토한다. 탭별 규칙 객체나 Presenter 신설을 첫 처방으로 삼지 않는다.
- **I11. [선택적 개선] LobbyHud 내비게이션·표시 조건** — OnGUI·Refresh의 숫자 포커스와 중첩 조건을 이름·함수·enum으로 드러낸다. 상태별 기능이 독립적으로 복잡해질 때만 State 객체를 검토한다.
- **I12. [선택적 개선] 네트워크/샌드박스 분기** — LocalTradeController·SeatController에 두 경로가 있다. 현행 결정은 별도 Gateway/Fake를 두지 않으므로 ITradeGateway·ISeatAuthority 추가 처방은 철회한다. 사용 중인 샌드박스의 필요를 확인하고 기존 호스트 개발 경로와 중복된 부분부터 줄인다(E1).
- **I13. [확인된 결함] 요청 후 불필요한 전송 / 표시 정리** — Request는 실패에도 시장을 재작성하고, Bid는 요청자 포트폴리오를 Request와 전원 갱신에서 중복 전송한다(F7·K1). 결과에 따라 필요한 수신자·값만 갱신한다. 거래·베팅·입찰 모두 같은 phase 오류 문구를 쓰는 것, 중복 로그·PublishAuction의 부수효과는 그 경로를 손댈 때 작은 함수·이름으로 정리한다.
- **I14. [선택적 개선] bool 인자** — buy는 매수/매도를 구별하는 단순 표현이다. 호출 의도가 불명확하면 named argument·enum·내부 함수 이름부터 검토한다. cancel=true에서 amount를 무시하는 API는 분리 가치가 더 크지만 모든 bool을 별도 Command로 바꿀 필요는 없다. 네트워크 시그니처 변경 시 K6을 확인한다.
- **I15. [선택적 개선] 반복 초기화의 실제 계약** — `Seat.Ensure`는 sittingPoints 길이가 실행 중 바뀌면 occupants를 교체한다. 그런 동적 변경을 지원하는지 먼저 확인하고 필요하면 금지·보존한다. `PlayerAssetStatus.GetShares`의 0 항목 보장은 외부에서 보이는 자산 변경과 구분한다. Door·Rope의 Initialize는 `Dev/Editor/AuctionHall/HallLevelVerification`·HallLevelBuilder가 Play 없이 직접 호출한다. 초기화를 Awake 한 번으로 옮기는 일괄 처방은 철회한다.
- **I16. [선택적 개선] 로프·착석 함수** — 솔버 단계·캡슐 수치의 이름을 드러내고 함수로 나눌 수 있다. PhysicsSnapshot 타입은 Capture/Restore를 여러 곳에서 반복하거나 복구 누락을 줄일 때 검토한다. 솔버의 반복 횟수·감쇠·중력은 의미가 있는 계산 상수이므로 전부 설정으로 만들지 않는다.
- **I17. [선택적 개선] ConnectionController의 반복 상태 처리** — Rejected/Error 핸들러·IsBusy 조건·사용하지 않는 out 값은 해당 경로 변경 시 정리할 수 있다. stopping 플래그와 Stopping 상태는 비동기 콜백에서 같은 의미인지 확인한 뒤 합친다.

### 이름·숫자·작은 중복

- **I18. [선택적 개선] 이름·타입 표현** — isGroud·Interction·Hadraise는 에셋과 묶인 오타다. 이름을 고칠 필요가 있을 때만 연결을 함께 이전한다(J3). detectingLayer의 실제 값은 레이어 인덱스이므로 LayerMask보다 int/의미 있는 이름이 적합할 수 있다. Seated()의 상태 변경, 로비 seat와 hallSeat의 뜻, 타입과 같은 헬퍼 이름도 가독성 개선 후보다. 첫 시선 입력 때 머리가 튄다는 추정은 해당 리그에서 별도 재현이 필요하다.
- **I19. [선택적 개선] 반복 리터럴의 의미** — 닉네임 길이·기본 포트처럼 이미 같은 의미의 상수가 있으면 재사용한다. 서버 입장 대기와 클라 연결 타임아웃은 서로 다른 절차일 수 있어 숫자가 같다는 이유로 통합하지 않는다. 티커 배치 좌표·노트북 행 수·모델 회전값은 변경 결합도를 보고 상수·레이아웃 참조로 정리한다.
- **I20. [선택적 개선] 이모트 번호 변환** — 1~4와 0~3 사이 변환이 혼란을 일으킬 때 한 함수·enum·메뉴 데이터로 정리한다. 두 인덱스 규약의 존재 자체는 버그가 아니다.
- **I21. [재현 필요] HUD 갱신과 잔류 문구** — 거래 패널 이벤트와 Update가 같은 갱신 함수를 부르지만 조준 대상은 매 프레임 바뀌므로 Update 전체를 삭제할 수는 없다. 값 변경 비교로 중복 작업을 줄일 수 있다. case 7 외 감지 레이어에서 문구가 남는지는 실제 대상·IInteractable 경로로 재현한다.
- **I22. [선택적 개선] 파일 내 반복 조회** — GNM의 connections→identity→PlayerNet 조회, 티커·로비의 작은 중복은 이번 변경에서 같은 의미임이 분명한 범위만 함수로 묶는다. 서로 다른 필터·시점까지 한 추상 API로 합치지 않는다.
- **I23. [선택적 개선] override와 데이터 묶음** — SettingsPanelView.Update는 자체 행·버튼 내비게이션을 구현한다. base.Update 미호출만으로 Refused Bequest라던 판정은 철회하며, 둘 다 호출하면 입력이 중복 처리될 수 있다. TradeResult 인자 6개·TradingPanelData 공개 필드 11개도 DTO라는 사실만으로 결함은 아니다. 잘못된 순서·조합을 실제로 줄일 수 있을 때 생성 API·작은 묶음을 검토한다.

---

<a id="review-j"></a>

## J. Unity 구조·직렬화·프레임 비용

### J1. [선택적 개선] 현재 두 어셈블리 경계를 유지

| 어셈블리 | 포함 | references |
|---|---|---|
| `BuyBidBlow.Systems` | `Systems/**` | 없음, `noEngineReferences: true` |
| `BuyBidBlow.Game` | `Network/` + Characters·Common·Content/Props·UI의 asmref | Systems, Mirror, kcp2k, InputSystem, TMP, UGUI |
| `BuyBidBlow.Dev` / `.Dev.Editor` | `Dev/**` | Game, Systems 등 (`UNITY_EDITOR`) |
| `…Tests.EditMode` | EditMode 테스트 | PlayMode 테스트, Game, Systems 등 |
| `…Tests.PlayMode` | PlayMode 테스트 | Dev, Game, Systems 등 |

- Systems와 Dev의 의존 경계는 분리되어 있다. Game의 서버·클라 코드를 같은 어셈블리에 두는 것은 현행 README의 의도적인 선택이다.
- Characters↔Network/Props/UI/Common의 타입 참조와 UI의 Mirror 사용은 존재한다. 이는 순환 어셈블리 의존이 아니며, 그 사실만으로 계층 위반을 확정하지 않는다. 서버의 Systems 접근과 클라의 동기화 값 접근이 섞이는 실제 경로부터 확인한다.
- EditMode→PlayMode 테스트 참조는 테스트 헬퍼 재사용 관계다. 실행 모드에 따른 문제·불필요한 결합이 생기면 헬퍼를 현행 Dev asmdef 4개 범위에서 정리한다. 새 TestUtils 어셈블리를 기본으로 추가하지 않는다.

→ 기존 Systems 경계·폴더 역할·서버/클라 접근을 지키는 작은 수정부터 한다. Core/World/Net/Presentation 다섯 계층 도입은 필수 순서에서 제외한다. 경계 실수가 반복되면 README가 제시한 Server·Client 분리나 좁은 검증 장치를 비용과 함께 별도로 제안한다.

### J2. [선택적 개선] 독립 계산이 실제로 복잡해질 때의 추출 후보

| 대상 | 우선 가능한 작은 조치 | 추출을 검토할 조건 |
|---|---|---|
| ConnectionController | 기존 상태·시간 경계 검증 활용 | 접속 시계·상태 전이를 독립 테스트하기 어려울 때 |
| CursorController | B6의 패널·착석·라디얼 상태 조정 | 입력 조합이 늘어 순수 정책 함수가 명확히 유리할 때 |
| LobbyHud | 조건·포커스 이름·함수 정리 | 내비게이션·파생 상태가 독립적으로 커질 때 |
| TradingPanelView | 예상 비용 계산과 표시를 작은 함수로 분리 | 복잡한 가격/수량 규칙을 여러 화면에서 반복할 때 |
| AuctionSeatPanel | 금액 보정·화면 데이터 구성을 구분 | 입력 상태 전이와 경매별 계산을 독립 검증할 때 |
| RopeBarrierSimulation | 점 수 가정 검증·솔버 단계 명명 | 다양한 점 수·솔버 테스트가 실제로 필요할 때 |
| RadialMenuController | 조각 선택 계산을 함수로 확인 | 각도 경계 테스트가 Unity 오브젝트와 무관할 때 |

ConnectionSession·LobbyViewModel·AuctionBidModel 등 새 타입을 전부 만드는 계획은 아니다. TickerFormat·TryPort/TryAddress·SeatPosition처럼 이미 직접 테스트 가능한 함수는 그대로 활용한다. 순수 Unity 연결 코드도 MonoBehaviour에 남을 수 있다.

### J3. [선택적 개선] 해당 이름·형식을 바꿀 때의 직렬화 확인

아래는 현재 고장 목록이나 모든 작업의 선행 정비 목록이 아니다. 이름·순서·타입을 실제 변경할 때 해당 연결을 확인한다.

- **Animator** — isGroud·Defeat/Flair/Hadraise/HipHopDance·Player_Idle/Player_Walking·Seated 등이 코드와 컨트롤러에 연결되어 있다. 수정 시 양쪽을 함께 확인한다. 해시 상수는 중복과 호출 비용을 줄일 수 있지만 파라미터 존재나 이름 일치를 보장하지 않는다.
- **직렬화된 enum** — BBBWolfCharacter.Action·Face·Company 값은 Animator·프리팹·인덱스 배열에 연결된다. 기존 정수값을 보존하고, 명시 값을 추가한다면 현재 에셋과 맞춘다. 모든 enum을 먼저 고칠 필요는 없다.
- **UnityEvent** — PlayerInput과 메뉴 버튼의 메서드명이 저장되어 있다. 이름 변경 시 사용 에셋과 연결을 검증한다. Inspector 연결은 정상적인 선택이며 일괄 코드 구독 전환은 필수가 아니다.
- **JSON** — MatchSettings 필드명과 JSON 키가 연결된다. StartingCash/StartingShares는 0도 유효해 누락을 값 검증만으로 구별하기 어렵다. 해당 설정을 바꿀 때 배포 JSON의 필수 키와 읽힌 값을 검사한다. 필드·키 집합을 무조건 완전히 같게 만드는 것까지 요구하지 않는다.
- **씬 이름** — PlayScene·menuScene·TestBot의 씬 참조를 실제로 이동·변경할 때 빌드 포함 여부를 확인한다. SceneNames 상수 신설은 반복 수정이 있을 때의 선택이다.
- **LobbySpriteLibrary 문자열 ID** — HUD가 요청하는 ID가 에셋에 있는지 확인한다. ID 변경 시 화면을 검증한다.
- **enum 표시명** — company.ToString()은 현재 화면에 쓰인다. 번역·코드명과 다른 표시명이 필요할 때 UI 표시명으로 분리한다.
- **전역 입력 래퍼 PlayerInput** — 생성 래퍼가 Unity InputSystem.PlayerInput과 이름이 겹치지만 현재 사용하는 곳은 없다. 래퍼를 실제 사용할 때 namespace·완전한 타입명을 정한다. 이를 이유로 입력 에셋 오타까지 한꺼번에 변경하지 않는다.
- **직렬화 필드 이름** — HUD의 Prompt·InteractionText·PlayerMoney 등 스타일을 맞추려 이름을 바꾼다면 FormerlySerializedAs나 에셋 이전을 사용한다. 스타일 통일만을 위한 위험한 대량 변경은 피한다.
- **기존 FormerlySerializedAs** — 현재 에셋에 옛 키가 안 보이더라도 과거 브랜치·에셋과의 이전 역할이 있을 수 있다. 삭제 이점이 작으므로 제거를 일반 정리 과제로 삼지 않는다.

### J4. [선택적 개선] 공유 에셋과 생성 사본의 관리

- 로프 16개에 반지름·흔들림·간격 기본값이 반복된다. 개별 배치에서 값을 다르게 쓰는 것은 정상이다. ropeLength가 본체에서 시뮬레이션으로 덮어써지는 관계는 어느 값이 권위 있는지 설명하거나 노출을 줄이면 된다. RopeSettings SO는 일괄 튜닝을 반복할 때 검토한다.
- 문 6개·경매석 7개·벤치/소파의 모양·동작을 함께 수정하는 일이 반복되면 베이스 프리팹/variant가 유리할 수 있다. 같은 기본값이라는 이유만으로 전부 전환하지 않는다. 개별 출구·회전값을 보존해야 한다.
- 메인 메뉴 씬과 PF_MainMenu/PF_HowToPlay는 빌더 생성·스냅샷 관계를 명확히 한다. 참조되지 않는 생성 프리팹을 수정해도 게임에 반영되지 않는 편집 혼란부터 줄인다. 생성 소스와 산출물을 단순 복붙 결함으로 동일 취급하지 않는다.
- 빌드에 없는 SC_Lobby의 설정 사본은 실사용 여부를 확인한다. 공식 서버 빌드는 홀 설정에서 SC_Server를 자동 갱신한다(G7).

→ 공통 수정의 실제 빈도와 값 불일치가 있는 곳부터 원본·산출물·개별 override를 구분한다. SO·variant화는 결함 수정의 선행조건이 아니다.

### J5. [재현 필요] 반복 탐색·할당과 필요한 주기 갱신

- AuctionSeatPanel은 착석 중 매 프레임 전체 표시를 구성하고, HUD 현금도 매 프레임 문자열을 만든다. 참조 캐시·값 변경 비교로 줄일 후보지만 실제 프레임/GC 영향은 측정하지 않았다.
- LobbyHud는 반복 Find·LINQ·컴포넌트 배열 확보·스프라이트 검색을 한다. 비용이 확인되면 각 화면의 기존 수명 안에서 캐시·갱신 범위를 줄인다.
- **티커는 이미 캐시한다.** TickerBoardDisplay.Update는 기록 개수가 바뀔 때만 history를 복사하고, 회사가 바뀔 때만 해당 화면을 그린다(`:46-67`). Current 확인과 타이머 갱신이 있다는 이유로 전체 폴링을 결함 취급하지 않는다.
- 서버 PublishAuction의 반복 LINQ·닉네임 조회·공개 Lot 구성은 상태 변경 시점으로 옮기거나 작은 캐시를 둘 수 있다. 측정 전 비용을 단정하지 않는다.
- 로프의 FixedUpdate 솔버·메시 갱신은 연출을 위한 작업이다. 접촉 대상이 없더라도 계산이 사인파만으로 대체되는 것은 아니다. 반복 횟수·점 수·휴면 조건은 품질과 비용을 측정해 판단한다.
- 문은 interactable=false여도 SetOpen으로 움직일 수 있다. 입력 허용 여부와 시뮬레이션 필요는 별개다. 목표 회전에 도달한 뒤 불필요한 작업을 줄일지는 측정·가독성 이점으로 판단한다.

→ 우선 반복 탐색·할당과 값이 같은 전체 UI 갱신을 줄인다. 타이머·입력·이동·물리의 주기 갱신은 유지할 수 있다. F6의 재고 알림을 모든 Update 제거 과제로 확대하지 않는다.

---

<a id="review-k"></a>

## K. 네트워크 점검과 검증 범위

### K1. [확인된 결함] 요청마다 불필요한 공개 SyncList 재작성

- `Request → PublishMarket`(`GameNetworkManager.cs:213-216,260-265`)은 phase 검사를 통과한 뒤 처리 결과가 실패하거나 시장이 안 바뀌어도 prices/stock을 Clear/Add한다. 현재 Mirror는 같은 값의 Clear/Add에도 변경 op를 만든다.
- 공개 갱신이 필요 없는 비공개 입찰·실패 요청에도 다른 클라이언트가 갱신 시점·빈도를 관찰할 단서가 생긴다. 이 델타에 요청자 신원이 들어가지는 않으므로 "누가 제출/취소했는지 노출"이라는 표현은 정정한다.

→ 바뀐 값만 인덱서로 반영하고 성공한 거래·Closing Bell 등 공개 값이 실제 바뀐 경로에 맞춰 전송한다. 초기 목록 생성은 유지하고 F7의 소유자 갱신과 함께 다인 수신을 확인한다. 속도 제한은 요청 빈도·서버 비용을 보고 정하며 이 수정의 전제는 아니다.

### K2. [재현 필요] 경기 시작 알림과 동기화 상태의 적용 순서

- RequestStartMatch는 PlayerNet.inMatch를 설정하고 별도 MatchStarting 메시지를 보낸다. 두 경로가 클라이언트에서 같은 시점에 적용된다는 계약이 없다. 항상 역순으로 도착한다고 단정할 수는 없다.
- HallSession 진입 뒤에도 local.inMatch가 false인 입력 프레임이 생기는지, 그때 SeatController가 오프라인 착석으로 빠지는지를 전용 서버·원격 클라로 확인해야 한다.
- SendToAll은 승인된 경기 참가자 목록으로 제한하지 않는다. 로컬 플레이어가 없는 수신자는 HallSession.BeginMatch가 에러 후 반환하므로 반드시 경기 화면에 들어간다는 기존 설명은 정정한다. clientInMatch는 먼저 true가 될 수 있어 거절·끊김 시 메뉴 복귀 경로는 별도 확인이 필요하다.

→ 우선 승인된 참가자에게만 시작을 알리고, 필요한 로컬 플레이어/경기 상태가 준비된 뒤 진입하도록 한다. inMatch hook을 기준으로 통일하거나 메시지 수신 후 준비를 기다리는 작은 방법을 선택한다. 이 확인을 위해 GNM을 먼저 분리할 필요는 없다.

### K3. [재현 필요] 입장 승인 전 Ready 요청

- Authenticator와 roster를 검사하는 OnServerReady override가 없다. 수정된 클라가 LobbyJoinRequest 없이 ReadyMessage를 보내 공개 오브젝트의 관찰자로 등록될 가능성을 정적 경로에서 확인했다.
- 승인 전 실제 어떤 spawn/동기화 데이터가 도착하는지, 거절·AdmissionTimeout 동안 관찰이 유지되는지는 별도 연결로 재현해야 한다. 정상 클라의 입장 흐름만 확인해서는 이 경로가 검증되지 않는다.

→ 우선 roster 승인 여부로 Ready를 허용하고 승인 전 spawn 수신이 차단되는지 확인한다. 입장 전체를 NetworkAuthenticator로 옮기는 것은 인증 흐름을 더 정리할 필요가 있을 때의 대안이다. 별도 매치 컨트롤러는 필요하지 않다.

### K4. [설계 결정] 클라이언트 위치의 신뢰 수준

- PlayerNet의 NetworkTransform은 ClientToServer이며 SC_Server에는 홀이 없다. 현재 좌표 수신에 게임 차원의 속도·순간이동 검증이 없다.
- 클라 좌표와 데스크/의자 좌표를 비교하면 거리 제한의 일부는 적용할 수 있지만 클라 이동 자체를 신뢰하는 한 완전한 치트 방지는 아니다. 검증 효과와 한계를 구분한다.

→ B1의 좌석 존재·슬롯·경매석 여부는 먼저 서버에서 검증할 수 있다. B2의 거래 장소 규칙을 적용할 때 좌표 데이터·거리·허용 이동 범위를 정한다. 전체 홀 물리나 서버 권위 이동을 자동으로 리팩토링 범위에 넣지 않는다.

### K5. [선택적 개선] 사용자가 알아야 할 요청 거절과 멱등 요청

- CmdSit은 좌석 경합 등에서 응답 없이 반환해 사용자가 실패 이유를 모를 수 있다. G1 결과 종류에 좌석 요청을 포함할 가치가 있다.
- 이미 서 있는 플레이어의 CmdStand처럼 반복해도 같은 상태인 요청은 조용히 성공 처리해도 된다. 모든 return에 실패 UI를 요구하지 않는다.
- SetReady/Customize 같은 로비 상태 변경을 매치 중에도 허용할지는 규칙을 정하고 필요한 서버 가드를 둔다. 응답 형식과 상태 변경 허용 여부는 별개다.

→ 먼저 플레이어가 취한 행동의 실패를 설명해야 하는 경로에 응답을 추가한다. 멱등 동작은 최종 상태 일치로 확인한다.

### K6. [선택적 개선] 이름·전송 형식 변경 시 프로토콜 호환

- 현재 Mirror의 메시지 ID는 타입 FullName, Command/Rpc ID는 메서드 FullName 해시에 기반한다(`Assets/Mirror/Core/NetworkMessages.cs`, RemoteCalls 및 Weaver).
- **파일 이동만으로 메시지 ID는 바뀌지 않는다.** 타입명·네임스페이스를 바꾸면 달라진다. GNM의 내부 함수만 정리하는 D1 변경도 그 자체로 핸드셰이크를 깨지 않는다.
- Command/Rpc 시그니처, SyncVar·전송 구조체의 필드, enum 값 등 wire 형식 변경은 같은 프로토콜 상수만으로 탐지되지 않을 수 있다. 혼합 빌드에서 오류나 잘못된 값이 생길 위험은 유효하다.

→ 실제 네트워크 표면을 바꿀 때 프로토콜 버전과 서버·클라 빌드를 함께 맞춘다. 구버전에 거절 사유를 전달하려면 핸드셰이크 이름·형식의 호환 범위를 유지한다. 리플렉션 해시를 새 프로토콜 값으로 만드는 것은 기본 처방에서 제외한다. 자동 비교를 도입한다면 필드 순서·직렬화 형식까지 무엇을 검사하는지 정의해야 한다.

### K7. [선택적 개선] 구독되지 않는 PhaseChanged·RoundStarted

현재 두 이벤트에 소비자가 없다는 사실만으로 폴링이 결함이거나 이벤트 API가 불필요하다고 확정하지 않는다. F6처럼 실제 갱신 누락을 고칠 때 필요한 알림을 활용하고, 단순 Current 확인·타이머 갱신은 남길 수 있다.

### K8. [재현 필요] 검증 코드의 존재와 현재 보장 범위

이 개정에서는 테스트를 실행하지 않았다. 아래는 소스상 검증 경로의 목록이며 현재 PASS나 전체 커버리지를 뜻하지 않는다.

**NUnit/EditMode·PlayMode에 있는 경로**

- 경매 규칙·LotGenerator·포트폴리오/TradeService·공개 가격·포맷·티커 표시.
- HostedMatch의 호스트 1인 거래·베팅·포트폴리오 수신, 착석·경매 패널·입찰.
- CursorLockTests는 배치 모드에서 Ignore되므로 커서 잠금의 실제 확인은 별도다.

**기존 에디터 검증에도 있는 경로**

- `Dev/Editor/Lobby/DirectConnectionVerification.cs`는 RoomRoster 정원·주소·포트 검사와 독립 UDP KCP 연결을 사용한다. 참가 승인, 6개 좌석, 7번째 거절, 버전 거절, 이탈 후 로비 재입장, 클라 타임아웃·취소를 검사하는 코드가 있다.
- 따라서 입장·거절·타임아웃·RoomRoster가 "전혀 안 덮임", 네트워크 검증이 "모두 호스트 1인"이라는 판정은 철회한다. 이 경로는 NUnit 테스트와 구분해 실행 여부를 기록한다.
- 직접 UDP 연결이 존재한다는 사실만으로 원격 아바타 hook·UI·게임 Command·다인 경매까지 검사하는 것은 아니다. 기존 LoopbackLobbyPeer는 입장과 방 닫힘 메시지 위주로 처리하므로 단순 파일 이동만으로 완전한 두 번째 Unity 클라이언트가 되지 않는다.

**범위가 부족하거나 추가 확인할 경로**

- 경기 시작 권한·거절, 종료 후 방 복귀, 재접속 복원, 원격 아바타 hook, 두 플레이어의 거래/입찰/좌석 경합, 베팅 정산, 설정 경계값·최종 순위의 검증 범위를 각 수정 시 확인한다.
- 검사할 테스트가 없으면 그 변경에 필요한 사례만 보강한다. 아래는 전체 선행 과제가 아니라 수정 대상별 확인 지점이다.

| 수정 대상 | 필요한 확인 | 기존 수단과 한계 |
|---|---|---|
| A1·H1·H2 경매 결과·표시 | 공개/비공개 수신자, 낙찰 정보, 늦은 UI 활성화 | 엔진 결과 이벤트 테스트와 실제 원격 수신/UI 연결을 구분 |
| A2·G9 종료·방 복귀 | 결과 전달, 상태 초기화, 다음 경기 시작 | 호스트 1인 완료 검사만으로 반복 경기 보장 불가 |
| B1·B3·B4 좌석·본인 판정 | 잘못된 좌석/슬롯 거절, 해제 수렴, 동명이인 | 다인 경합과 hook 경로를 실제 수신으로 확인 |
| F1·B6·I3 입력/UI | 활성 EventSystem, 메뉴 중 허용 입력, 열기 입력의 즉시 확인 방지 | 커서 잠금 검사의 배치 Ignore와 분리 |
| F2 빠른 경기 옵션 | 미정산 예약금 없음, Open/Secret 마감, 단축 실행 시간 | 경매 슬롯까지 일관되게 줄이는 테스트 설정 등으로 보강 |
| F6·F7·K1 시장·전송 | 다른 플레이어 거래 후 재고, 실패·비공개 요청의 공개 델타, 변경 대상만 자산 수신 | 호스트 한 명이나 입장 전용 peer만으로 불충분 |
| C3·G7 설정·빌드 | 정원/지원 인원 호환, 정보 범위, 자동 서버 씬 갱신 | 기존 JSON·BuildServer 경로를 활용 |
| G1 응답 | 요청 종류·실패 사유·수신 패널·요청 전후 자산 | 화면 문자열만으로 성공을 추론하지 않음 |
| K2·K3 시작/승인 | 지연된 상태에서 진입, 미승인 Ready 차단 | 전용 서버·원격 또는 메시지 처리 가능한 별도 연결 필요 |

**테스트 유지보수 시 주의점**

1. HostedMatch는 GNM private 필드명에 리플렉션으로 의존한다. 해당 필드를 바꾸면 함께 수정한다. 내부 설정 진입점이나 fixture 정리는 반복 비용이 커질 때 검토하며 모든 수정 전에 ConfigureForTest·InternalsVisibleTo를 추가할 의무는 없다.
2. 씬 오브젝트·UI 경로에 의존하는 테스트는 배치 변경 시 수정될 수 있다. 사용자에게 보이는 문구·시각 상태를 검사하는 assertion은 유효한 UI 계약일 수도 있으므로 전부 제거하지 않는다.
3. HallSeatTests의 직접 SetSeated 호출과 별도 SyncList 구성 테스트는 원격 hook·실제 복제를 보장하지 않는다. 테스트 이름과 보장 범위를 맞춘다.
4. 빠른 라운드 테스트는 모든 라운드를 1초로 만드는 옵션을 사용한다. 현재 미정산 경매(F2)를 검사하지 않으므로 기존 PASS만으로 예약금 정산을 보장하지 않는다. override 정책을 바꿀 때 테스트 시간표·시간 제한을 함께 맞추고, 경매 예약/정산 사례를 확인한다.
5. LoopbackLobbyPeer를 재사용하려면 UnityEditor 의존을 분리하고 필요한 메시지/상태 처리를 구현해야 한다. 실제 원격 UI·아바타 검증은 별도 클라이언트가 필요할 수 있다. 새 테스트 어셈블리 신설을 기본으로 하지 않는다.
6. DirectConnectionVerification은 LobbyClosing 수신을 기대하지만 BroadcastClosing 호출부가 없다. 재실행 실패 가능성이 있으므로 실행해 확인해야 한다. 남은 PASS 파일은 현재 통과 증거가 아니다.
7. DeskTradeTests는 17라운드에 bells>=16을 허용한다. 구독 시점과 첫 마감 수신을 확인하고 필요한 정확한 계약을 검증한다. 숫자만 17로 바꾸기 전에 첫 이벤트를 놓치는 경로를 조사한다.

---

<a id="work-order"></a>

## 작업 순서: 결함 중심의 최소 변경

아래는 구현 완료를 뜻하지 않는 권장 순서다. 각 단계에서 수정할 경로의 기존 검증을 확인하고 필요한 회귀 확인만 보강한다. 테스트 기반 전체 개편·GNM 분리·일괄 상수화는 공통 선행조건이 아니다.

1. **현재 입력·표시 경로 확인/수정** — F1 EventSystem 수명, B3 좌석 해제, B4 본인 판정, F6 재고 갱신부터 현재 구조에서 작게 고친다. B6·D6·I3는 재현 후 기대 동작에 맞춘다. F5의 허용 정책은 따로 정한다.
2. **규칙 검증·잘못된 정산/전송 수정** — B1 좌석 목록·종류·슬롯, C3 설정 경계, F2 경매 마감·예약금, F7·K1 변경 없는 전송을 줄인다. 실패 시 자산 불변과 공개/소유자 수신 범위를 확인한다.
3. **플레이 루프 연결** — A1 낙찰 정보와 기존 노트북, A2·G9 결과·초기화·다음 경기 진입, H1·H2 공개 연출을 기존 GNM·PlayerNet·MatchNet과 뷰에 연결한다. 새 MatchController·Presenter는 전제가 아니다.
4. **응답·시작·승인 경로 보완** — G1에 필요한 요청 종류·결과를 추가하고, K2·K3를 원격 연결로 확인한다. K5는 설명이 필요한 실패에만 응답한다. 이때 바꾸는 네트워크 표면에 대해서만 K6의 호환성을 확인한다.
5. **결정된 복구 기능과 미정 정책 분리** — F4 재접속은 현재 지원 결정에 맞춰 식별·복원 경로를 구현한다. F3 이탈 입찰 정책, B2·K4 위치 신뢰 수준, H3 전체 자산 UI, H4 다중 방의 실제 도입 범위를 별도로 정한다.
6. **관련 부분만 정리** — 위 작업에서 방해된 함수·참조·중복만 D·I·J 후보로 정리한다. J3는 실제 이름/타입 변경에 적용하고, J5는 필요한 측정 후 비용이 큰 곳만 줄인다. 미통합 뷰는 재사용하고 폐기 결정된 코드만 정리한다(E·H8).

**필수 작업에서 제외한 것**: Core/World/Net/Presentation 다섯 계층 도입, MatchController 추출, 일괄 View/Presenter 분리, 설정 전체 SO 전환, 모든 sentinel의 nullable화, 탭/경매의 일괄 State·다형성 전환, 샌드박스 Gateway 추가. 도입하려면 현재 작업의 구체적인 이점·더 작은 대안·이전 비용을 별도 제안한다.
