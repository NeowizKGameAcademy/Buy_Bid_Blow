# Project 구조

BuyBidBlow의 게임 코드와 콘텐츠를 관리한다. 독립 오디오 엔진은 `Assets/CueAudio`, 외부 DLL은 `Assets/Plugins`, Mirror는 기존 `Assets/Mirror`에 둔다.

## 폴더 역할

| 폴더 | 책임 |
|---|---|
| `Systems` | 게임 규칙·상태. 순수 C#(`BuyBidBlow.Systems.asmdef`, `noEngineReferences`). 기능별 폴더 `Auction`·`Market`·`Intel`·`Portfolio`·`Round`·`Room` |
| `Systems/Types` | 식별자·값 타입, 요청·이벤트 struct, enum. Command 인자와 동기화 값으로도 그대로 쓴다 |
| `Network` | Mirror 연결. NetworkManager, Match를 소유하고 Tick을 구동하는 `MatchHost`, NetworkBehaviour(`*Net`). `BuyBidBlow.Game.asmdef`가 여기 있다 |
| `Characters` | 행위자. 내 캐릭터(입력·카메라·이동), 다른 플레이어 아바타, 입찰 팻말 |
| `World` | 상호작용 지점. 거래 창구·시세 전광판·시계·알림판·종 등 씬에 놓이는 오브젝트 |
| `UI` | 화면별 UI. `Menu`·`Lobby`·`HUD`·`Auction`·`Market`·`Portfolio`·`Intel`·`Result` |
| `Common` | 여러 오브젝트가 함께 쓰는 컴포넌트·인터페이스 (예: `IInteractable`, 동기화 값 표시) |
| `Content` | 여러 곳에서 함께 쓰는 에셋. 레벨(`Content/Levels`)·공용 모델·머티리얼, 게임 설정 데이터 |
| `Audio/Clips`, `Audio/Cues` | 게임용 오디오 파일·JSON 큐 정의 |
| `Scenes` | 실행 씬 |
| `Dev` | 에디터 도구·실험·디버그·테스트 |

## 오브젝트 폴더

`Characters`·`World`·`UI`는 시스템을 이용하기만 하는 오브젝트를 담는다. 오브젝트 하나가 폴더 하나이며, 그 오브젝트만 쓰는 스크립트·프리팹·모델·머티리얼을 함께 둔다.

```
World/TradingCounter/
├─ TradingCounter.cs
├─ TradingCounter.prefab
└─ Models/, Materials/
```

- 한 오브젝트만 쓰는 스크립트는 그 오브젝트 폴더에 둔다. 여러 오브젝트가 쓰면 `Common`으로 옮긴다.
- 한 오브젝트만 쓰는 에셋은 그 오브젝트 폴더에, 여러 곳에서 쓰면 `Content`에 둔다.
- 씬 배치는 `Scenes`의 씬이 담당한다. 거래소 씬의 오브젝트에는 `NetworkIdentity`를 붙이지 않는다. 요청은 `PlayerNet`으로 보내고, 표시는 `*Net`의 동기화 값을 읽는다.

## 의존 관계

- 어셈블리는 `Game → Systems` 두 개다. Systems는 UnityEngine·Mirror·UI·MySQL에 의존하지 않으며 asmdef로 강제한다.
- `Network`의 `BuyBidBlow.Game.asmdef`가 Game 어셈블리를 정의하고, `Characters`·`World`·`UI`·`Common`은 각 폴더의 `BuyBidBlow.Game.asmref`로 같은 어셈블리에 포함된다. 새 최상위 코드 폴더를 만들면 asmref도 함께 둔다. 없으면 `Assembly-CSharp`로 컴파일된다.
- 서버와 클라이언트는 같은 어셈블리(Game)에 있다. 경계는 컴파일러가 아니라 아래 규칙으로 지킨다.
  - 게임 상태 변경은 서버에서 `Match.Handle`·`Match.Tick`을 통해서만 한다. `Match`와 `MatchHost`는 서버에서만 생성한다(`OnStartServer`).
  - Client 코드는 `*Net`의 SyncVar·hook·Rpc로 받은 값만 읽는다. `Match`·`MatchHost`를 직접 참조하지 않는다.
  - 서버 전용 메서드에는 `[Server]`, 클라이언트 전용 메서드에는 `[Client]`를 붙인다.
  - 비공개 값(현금·보유 주식·예약금·획득 정보)은 플레이어 오브젝트의 Owner 동기화로만 보낸다.
- 호스트 모드는 동기화 누락을 숨긴다. 원격 클라이언트(에디터 + 빌드 등)로 주기적으로 확인한다.
- 경계 실수가 잦아지면 Server·Client 어셈블리를 분리한다. Systems 경계가 유지되면 분리 비용이 작다.
- CueAudio는 게임 폴더 경로와 게임 규칙에 의존하지 않는다. 게임 쪽에서 큐 데이터를 제공한다.
- `Content`는 코드 의존 계층이 아니다. 프리팹은 필요한 컴포넌트를 참조할 수 있다.

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

**폴더 이름**: 최상위 폴더는 내용의 용도로 짓는다. `Scripts`·`Prefabs`처럼 파일 종류로 짓지 않는다. Systems와 UI에서 같은 기능은 같은 폴더 이름을 쓴다. 인게임 기능은 `Auction`·`Market`(주가·거래)·`Intel`·`Portfolio`·`Round`다.

**네임스페이스**: 폴더 경로를 따른다. `BuyBidBlow.Systems.{기능}`(예: `BuyBidBlow.Systems.Auction`), `BuyBidBlow.Network`, `BuyBidBlow.Characters`, `BuyBidBlow.World`, `BuyBidBlow.UI.{화면}`, `BuyBidBlow.Common`이다. 개발 도구는 `BuyBidBlow.Dev`, 오디오 엔진은 `CueAudio`를 쓴다.

**접미사**

| 접미사 | 의미 | 위치 |
|---|---|---|
| 접미사 없음 | 규칙 개념 그대로 (`Match`, `Auction`, `Portfolio`) | `Systems` |
| `*Request` · `*Event` | Match 입력 요청 / 처리 결과 이벤트 | `Systems/Types` |
| `*Net` | Mirror NetworkBehaviour (SyncVar·Command·Rpc) | `Network` |
| `*Host` | Systems 객체를 소유하고 네트워크에 반영하는 서버 측 구동자 (`MatchHost`) | `Network` |
| `*View` | UI 컴포넌트. 로직이 커질 때만 `*Presenter`로 분리한다 | `UI/*` |
| `*Repository` | 저장 인터페이스·구현 (저장 기능 도입 시) | 미정 |
| `*Tests` | 테스트 클래스. 메서드 이름은 `조건_결과` 형식이다 | `Dev/Tests` |

서버 없이 UI를 개발할 때는 별도 Fake 구현 대신 Mirror 호스트 모드를 쓴다.

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

게임 코드 어셈블리는 `BuyBidBlow.Systems`(참조 없음)와 `BuyBidBlow.Game`(→ Systems, Mirror) 두 개다. 다른 패키지 참조는 코드에서 필요해질 때 Game asmdef에 추가한다. CueAudio·데모·테스트는 각자의 전용 asmdef를 쓴다.
서버·클라이언트 빌드 제외 설정, 플러그인 플랫폼 제한은 아직 적용하지 않았다.

## CueAudio 사용

- `Tools > CueAudio > Open Demo`로 샘플 씬을 열고 Play 모드에서 시험한다.
- 큐 편집·연결 조회는 `Cue Browser`, 실행 중 재생 추적은 `Runtime Monitor`에서 한다.
- 엔진과 에디터는 `Assets/CueAudio`, 샘플 데이터는 `Audio`, 데모와 테스트는 `Dev`에 있다. CueAudio 및 데모·테스트 전용 asmdef를 추가했다.
- 자세한 사용법과 현재 범위는 [CueAudio README](../CueAudio/README.md)를 참고한다.
