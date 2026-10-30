# 개발 규칙과 현재 구현 상태

프로젝트 용어, 코드 표기, 개발·검증 방법과 구현 상태를 정리한다. 폴더 배치와 의존 관계는 [프로젝트 구조](../Assets/Project/README.md)를 따른다.

## 용어

코드·문서·커밋에서 같은 의미로 쓴다. 한국어 문서에서는 괄호 안 표현을 쓴다.

| 용어 | 의미 |
|---|---|
| `Room` (방) | 참가자·준비 상태·방장을 가진 대기 단위. 여러 Match를 연속으로 진행할 수 있다 |
| `Match` (경기, 게임 세션) | Room에서 시작해 최종 결과 확정으로 끝나는 게임 한 판. DB 저장과 중복 방지의 단위다 |
| `Round` (라운드) | Match 안의 시간 구간. 길이는 경기 설정에 따르며 라운드 중 주가는 고정되고 Closing Bell로 끝난다 |
| `Closing Bell` | 라운드 마감. 숨겨진 변동을 적용해 새 주가를 공개한다 |
| `Player` (플레이어) | 접속한 참가자. `PlayerId`로 식별하며 연결(connection)과 구분한다 |
| `Host` (방장) | Room 설정·시작 권한을 가진 Player. 서버 호스트가 아니다 |
| `Company` (회사) | 주식 종목. Straw·Timber·Brick |
| `Trade` (거래) | 거래 창구에서 주식을 사고파는 행위. `Buy`(매수)·`Sell`(매도) |
| `Auction` (경매) | Lot을 파는 경매. 한 경매에 Lot이 여러 개일 수 있다. 방식은 `Open`·`Secret`·`Dutch` |
| `Bid` (입찰) | 경매에 금액을 제시하는 행위. 낙찰자는 `Winner`, 낙찰가는 `FinalPrice` |
| `Lot` (경매 상품) | 경매에 오르는 물건. 종류별로 나뉘며 `Intel`이 그중 하나다 |
| `Intel` (정보) | Lot 종류. 숨겨진 주가 변동을 알려준다. `AnalystReport`(변동폭)·`InsiderMemo`(방향) |
| `Portfolio` (자산) | Player의 현금과 보유 주식. 본인에게만 공개한다 |
| `Price` / `Change` | 현재 주가 / Closing Bell에 적용되는 주가 변동 |

- Mirror의 `NetworkMatch`는 가시성 그룹이며 우리 용어의 Room에 대응한다. 코드에서 `match`라고만 쓰면 항상 우리 Match를 뜻한다.
- `Session`은 쓰지 않는다. 경기는 Match, 접속은 Connection으로 구분한다.

## 코드 표기

**Mirror 멤버**: Command는 `Cmd*`, ClientRpc는 `Rpc*`, TargetRpc는 `Target*`로 시작한다. SyncVar hook은 `On*Changed`다.

**값 표기**
- 식별자는 `*Id`다. `PlayerId`, `AuctionId`처럼 쓰며 0은 "없음"이다.
- 시각은 `*At`다. 서버 절대 시각을 `double` 초로 표현한다. 예: `RoundClosesAt`. 기간은 `*Seconds`로 쓴다.
- 금액은 `int` 달러다. float를 쓰지 않는다. `Cash`, `Price`, `Amount`처럼 쓴다.
- 수량은 주식 수를 `Shares`, 요청 수량을 `Quantity`로 쓴다.

**C# 스타일**: 타입·메서드·프로퍼티·public 필드는 PascalCase다. private 필드는 접근 제한자를 명시하고 밑줄 없는 camelCase로 쓴다(`private CueCatalog catalog;`). 인터페이스는 `I`로 시작한다.

## 개발과 검증

별도 서버 프로세스 없이 UI를 개발할 때는 별도 Fake 구현 대신 Mirror 호스트 모드를 쓴다.

- 호스트 모드는 동기화 누락을 숨긴다. 원격 클라이언트(에디터 + 빌드 등)로 주기적으로 확인한다.

## 상호작용 구현 원칙

오브젝트의 게임 역할은 기존 컴포넌트·인터페이스로 판별하고, 레이어는 물리 충돌·레이캐스트·카메라 범위에 사용한다. 감지·안내·실행은 같은 상호작용 대상을 기준으로 처리한다. 좌석의 서버 승인 절차는 유지한다.

## 현재 구현 상태

### 경기 구조와 밸런스 (2026-10-30)

- 기본 설정은 `Content/Match/MatchSettings.json`이다. 총 13라운드, 각 120초이며 매 라운드 시작 후 60초 동안 경매 1회를 진행한다. 홀수 라운드는 공개, 짝수 라운드는 비공개다. 남은 60초에는 거래를 계속할 수 있다.
- `AuctionSlots`가 한 개이면 위 단일 경매 규칙을 사용한다. 기존 두 슬롯 설정은 개발 씬과 기존 경매 회귀 테스트를 위해 유지한다. 단축 테스트에서도 실제 경매와 `MatchTimeline`은 같은 시간 배율을 사용한다.
- 단일 경매 상품은 현재 라운드 마감 시점의 두 회사 등락 정보 한 건이다. `Lot.First`를 사용하며 `Second.Round`는 0이다. `HasSecond`로 추가 정보의 유무를 구분하고, 빈 두 번째 정보는 지급하거나 표시하지 않는다. 소유자 전용 `PlayerAssetsNet` 동기화를 유지한다.
- 시작 현금은 400, 시작 주식은 0주다. 회사별 시장 재고는 참가자 수 × 10 + 10이다. `StartingPriceMultiplier` 2를 시작 가격 추첨 결과에만 적용하며, 하한가 5와 상승 확률 35%·50%·75%는 유지한다.
- 주가 변동 폭은 1~3·4~7·8~12 중 하나를 선택한 뒤 구간 안에서 균등 추첨한다. 짚의 구간 확률은 10%·30%·60%, 목재는 25%·50%·25%, 벽돌은 60%·30%·10%다. 방향은 상승 확률로 별도 추첨한다. 기본 규칙에서 전체 미래 주가를 미리 생성하고, 가격 하한을 반영한 실제 방향을 정보 상품으로 제공하는 방식은 유지한다.
- 이번 범위에는 가챠·운반·파티 상점 추가, 이동·접속·착석·거래·예약금 계산 변경을 포함하지 않는다. 상품 표시와 테스트만 필요한 범위에서 조정한다.
- 검증: 관련 EditMode 75개 통과(`Logs/ExpansionEditMode.xml`). PlayMode는 핵심 검사 15개 통과 후 클라이언트 재컴파일로 설정·오디오 검사 5개를 별도 실행해 통과했다(`Logs/ExpansionPlayModeCore.xml`, `Logs/ExpansionPlayMode.xml`). 앞 실행의 설정 화면 실패 1건·오디오 생략 4건은 뒤 실행으로 재검증했다. 단축 진행의 13라운드 완주, 정보 지급·표시, 거래, 예약금, 착석·퇴석, 결과·재시작, 일정·알림을 확인했다.
- 초기 전체 EditMode 실행의 CueAudio 검색 테스트 2건은 Mirror 예제 조명 에셋 읽기 오류로 실패했다(`Logs/ExpansionStage1EditMode.xml`). 관련 기능만 선택한 75개 검증과 구분하며, 외부 예제 에셋은 수정하지 않았다. 원격 클라이언트를 연결한 다인 플레이와 실제 26분 경기의 밸런스 검증은 미실시다.

### 플레이어 조작과 상호작용 (2026-10-29)

- 기존 `PlayerController`, `PlayerMove`, 전용 입력 액션과 생성 코드는 제거했다. 이동·점프·마우스 시점 조작 및 기존 E 상호작용 입력은 현재 연결돼 있지 않다.
- `PlayerCamera`, `PlayerSeating`, `SeatController`, `PlayerInteraction`은 유지한다. 퇴석·커서·노트북·거래·경매 UI의 자체 입력도 남아 있으므로 모든 입력이 제거된 상태는 아니다.
- `PlayerNet`과 `HallSession`은 현재 `PlayerSeating`을 통해 로컬 플레이어를 참조한다. 향후 조작 구현 시 플레이어 식별과 착석 기능의 책임을 함께 검토한다.
- 현재 `IInteractable` 구현체는 `Seat`, `Door`이며 감지·안내·실행 통일은 아직 진행 전이다. 후속 작업은 [상호작용 정리 TODO](Analysis/ResponsibilityReview.md)에 기록한다.
- 현재 빌드는 플레이어가 서버를 겸하는 Mirror 호스트 방식이다. 전용 서버 씬 `SC_Server`, 서버 빌드·실행 도구와 관련 로컬 실행 배치 파일은 제거했다.

### 빌드 제한

서버·클라이언트 빌드 제외 설정, 플러그인 플랫폼 제한은 아직 적용하지 않았다.

## 점검 결과 (2026-10-29)

코드·설정의 정적 확인 결과다. 규칙이 모든 기존 코드에 적용된 상태는 아니며, 아래 불일치는 코드 변경 없이 기록한다.

- `PlayerNet.SetReady`, `Customize`는 Command지만 `Cmd` 접두사가 없다.
- `Door.openAngle`, `Seat.sittingPoints` 등 public 필드가 camelCase이고, `Door.closedRotation`, `Seat.occupants` 등 private 필드의 접근 제한자가 생략돼 있다.
- `HallSession` 클래스명에는 사용하지 않기로 한 `Session` 용어가 남아 있다.
- `DetectObject`는 레이어만으로도 대상을 감지하며, `HUDController`는 레이어 7을 거래 대상으로 판단한다. 상호작용 원칙으로의 전환은 미완료다.
- 기존 조작 코드 제거와 유지 컴포넌트는 위 구현 상태 설명과 일치한다. 금액의 `int` 사용과 라운드 시각의 `double` 사용도 확인했다.
- 원격 클라이언트 정기 검증의 준수 여부는 정적 검사만으로 판단할 수 없다. 이번 점검에서 Unity 실행·빌드·멀티플레이 테스트는 수행하지 않았다.

## 관련 문서

- [CueAudio 설명과 사용법](../Assets/CueAudio/README.md)
