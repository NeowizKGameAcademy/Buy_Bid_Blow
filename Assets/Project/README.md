# Project 구조

BuyBidBlow의 게임 코드와 콘텐츠를 관리한다. 독립 오디오 엔진은 `Assets/CueAudio`, 외부 DLL은 `Assets/Plugins`, Mirror는 기존 `Assets/Mirror`에 둔다.

## 폴더 역할

| 폴더 | 책임 |
|---|---|
| `Client/Lobby`, `Client/InGame` | 기능별 UI Model·View·Presenter. 현재 인게임 HUD는 `InGame/HUD`에 둔다 |
| `Client/Player` | 입력·카메라·이동 표현 |
| `Client/Integrations` | Mirror 클라이언트·Discord 연결 |
| `Server/Rooms/Application`, `Server/InGame/Application` | 요청 조율·권한·중복 검사·처리 순서·저장 조율 |
| `Server/Rooms/Domain`, `Server/InGame/Domain` | 방과 경기의 규칙·상태. 경매·시장·정보·라운드·자산 |
| `Server/Integrations` | Mirror 서버·DB 연결 구현 |
| `Shared/Types` | 양쪽이 공유하는 식별자·값 타입 |
| `Shared/Protocol` | Command 인자·동기화 상태에 쓰는 값 타입과 enum |
| `Shared/Networking` | 클라이언트·서버 계약. NetworkBehaviour(SyncVar·Command·Rpc)와 서버가 구현할 핸들러 인터페이스 |
| `Content` | 캐릭터·소품 모델, 프리팹, 레벨 구성 에셋, 게임 설정 데이터 |
| `Audio/Clips`, `Audio/Cues` | 게임용 오디오 파일·JSON 큐 정의 |
| `Scenes` | 실행 씬 |
| `Dev` | 에디터 도구·실험·디버그·테스트 |

## 의존 관계

- `Client → Shared ← Server`. 클라이언트와 서버 구현을 서로 직접 참조하지 않는다.
- 서버 Domain은 Mirror·UI·MySQL 구현에 의존하지 않는다. Application이 요청과 저장을 조율한다.
- Client Model은 수신 상태를 보관하며, 최종 게임 판정은 서버에서 수행한다.
- Shared에는 통신 형식을 두고 미래 주가 등 서버의 실제 비밀 상태를 넣지 않는다.
- CueAudio는 게임 폴더 경로와 게임 규칙에 의존하지 않는다. 게임 쪽에서 큐 데이터를 제공한다.
- 콘텐츠 폴더는 코드 의존 계층이 아니다. 프리팹은 필요한 컴포넌트를 참조할 수 있다.

## 용어

코드·문서·커밋에서 같은 의미로 쓴다. 한국어 문서에서는 괄호 안 표현을 쓴다.

| 용어 | 의미 |
|---|---|
| `Room` (방) | 참가자·준비 상태·방장을 가진 대기 단위. 여러 Match를 연속으로 진행할 수 있다 |
| `Match` (경기, 게임 세션) | Room에서 시작해 최종 결과 확정으로 끝나는 게임 한 판. DB 저장과 중복 방지의 단위다 |
| `Round` (라운드) | Match 안의 100초 구간. 라운드 중 주가는 고정되고 Closing Bell로 끝난다 |
| `Closing Bell` | 라운드 마감. 숨겨진 변동을 적용해 새 주가를 공개한다 |
| `Player` (플레이어) | 접속한 참가자. `PlayerId`로 식별하며 연결(connection)과 구분한다 |
| `Host` (방장) | Room 설정·시작 권한을 가진 Player. 서버 호스트가 아니다 |
| `Company` (회사) | 주식 종목. Straw·Timber·Brick |
| `Trade` (거래) | 거래 창구에서 주식을 사고파는 행위. `Buy`(매수)·`Sell`(매도) |
| `Auction` (경매) | 정보 하나를 파는 경매. 방식은 `Open`·`Secret`·`Dutch` |
| `Bid` (입찰) | 경매에 금액을 제시하는 행위. 낙찰자는 `Winner`, 낙찰가는 `FinalPrice` |
| `Intel` (정보) | 경매 상품. `AnalystReport`(변동폭)·`InsiderMemo`(방향) |
| `Portfolio` (자산) | Player의 현금과 보유 주식. 본인에게만 공개한다 |
| `Price` / `Change` | 현재 주가 / Closing Bell에 적용되는 주가 변동 |

- Mirror의 `NetworkMatch`는 가시성 그룹이며 우리 용어의 Room에 대응한다. 코드에서 `match`라고만 쓰면 항상 우리 Match를 뜻한다.
- `Session`은 쓰지 않는다. 경기는 Match, 접속은 Connection으로 구분한다.

## 네이밍 규칙

**기능 이름**: Client·Server·Shared에서 같은 기능은 같은 폴더 이름을 쓴다. 인게임 기능은 `Auction`·`Market`(주가·거래)·`Intel`·`Portfolio`·`Round`다.

**네임스페이스**: `BuyBidBlow.{Client|Server|Shared}.{기능}` 형식이다. 예: `BuyBidBlow.Server.InGame.Auction`. 개발 도구는 `BuyBidBlow.Dev`, 오디오 엔진은 `CueAudio`를 쓴다.

**접미사**

| 접미사 | 의미 | 위치 |
|---|---|---|
| `*Net` | Mirror NetworkBehaviour (SyncVar·Command·Rpc 계약) | `Shared` |
| `*Handler` | `*Net`의 Command를 받는 서버 구현의 인터페이스 | `Shared` 정의, `Server/Integrations` 구현 |
| `*Service` | 요청 조율 (Application) | `Server/*/Application` |
| 접미사 없음 | Domain 개념 그대로 (`Match`, `Auction`, `Portfolio`) | `Server/*/Domain` |
| `*Repository` | 저장 인터페이스·구현 | 인터페이스는 Application, 구현은 `Server/Integrations/Persistence` |
| `*Model` · `*View` · `*Presenter` | 클라이언트 MVP | `Client/*` |
| `*Gateway` | Presenter가 쓰는 요청 인터페이스. Mirror 구현과 Fake 구현을 교체한다 | 인터페이스는 `Client/*`, Mirror 구현은 `Client/Integrations` |
| `Fake*` | 서버 없이 개발·테스트할 때 쓰는 대체 구현 | `Dev` |
| `*Tests` | 테스트 클래스. 메서드 이름은 `조건_결과` 형식이다 | `Dev/Tests` |

**Mirror 멤버**: Command는 `Cmd*`, ClientRpc는 `Rpc*`, TargetRpc는 `Target*`로 시작한다. SyncVar hook은 `On*Changed`다.

**값 표기**
- 식별자는 `*Id`다. `PlayerId`, `AuctionId`처럼 쓰며 0은 "없음"이다.
- 시각은 `*At`다. 서버 절대 시각을 `double` 초로 표현한다. 예: `RoundClosesAt`. 기간은 `*Seconds`로 쓴다.
- 금액은 `int` 달러다. float를 쓰지 않는다. `Cash`, `Price`, `Amount`처럼 쓴다.
- 수량은 주식 수를 `Shares`, 요청 수량을 `Quantity`로 쓴다.

**C# 스타일**: 타입·메서드·프로퍼티·public 필드는 PascalCase다. private 필드는 접근 제한자를 명시하고 밑줄 없는 camelCase로 쓴다(`private CueCatalog catalog;`). 인터페이스는 `I`로 시작한다.

## 씬과 개발 도구

- 실제 씬 파일은 `Scenes`, 레벨 모델·머티리얼·조립 프리팹 등은 `Content/Levels`에 둔다.
- `Dev/Editor`에는 에디터 전용 코드를, `Dev/Tests`에는 EditMode·PlayMode 테스트를 둔다.
- `Dev/Sandbox` 씬은 배포용 빌드 씬 목록에 넣지 않는다.
- `Dev/Debug`는 폴더 이름만으로 빌드에서 제외되지 않는다. 개발 전용 코드에는 조건부 컴파일 등을 적용한다.

## 현재 전환 범위

폴더 구조 전환 당시 기존 asmdef는 제거했다. 이후 작성된 CueAudio·데모·테스트에는 전용 asmdef를 추가했으며, 나머지 어셈블리는 코드 작성 시 의존 관계에 맞게 구성한다.
서버·클라이언트 빌드 제외 설정, 플러그인 플랫폼 제한은 아직 적용하지 않았다.

## CueAudio 사용

- `Tools > CueAudio > Open Demo`로 샘플 씬을 열고 Play 모드에서 시험한다.
- 큐 편집·연결 조회는 `Cue Browser`, 실행 중 재생 추적은 `Runtime Monitor`에서 한다.
- 엔진과 에디터는 `Assets/CueAudio`, 샘플 데이터는 `Audio`, 데모와 테스트는 `Dev`에 있다. CueAudio 및 데모·테스트 전용 asmdef를 추가했다.
- 자세한 사용법과 현재 범위는 [CueAudio README](../CueAudio/README.md)를 참고한다.
