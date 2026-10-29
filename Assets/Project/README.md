# Project 구조

BuyBidBlow의 게임 코드와 콘텐츠를 관리한다. 독립 오디오 엔진은 `Assets/CueAudio`, 외부 DLL은 `Assets/Plugins`, Mirror는 기존 `Assets/Mirror`에 둔다.

## 폴더 역할

| 폴더 | 책임 |
|---|---|
| `Systems` | 게임 규칙·상태. 순수 C#(`BuyBidBlow.Systems.asmdef`, `noEngineReferences`). 기능별 폴더 `Auction`·`Market`·`Lot`·`Portfolio`·`Round`·`Room` |
| `Systems/Types` | 식별자·값 타입, 필요한 처리 결과·이벤트, enum. Command 인자와 동기화 값으로도 그대로 쓴다 |
| `Network` | 코드 파일은 `GameNetworkManager.cs` 하나만 둔다. 접속·종료와 플레이어 생성을 처리하고, 서버에서 Systems의 경기 생성·관리 기능을 호출하며 경기별 Tick을 구동한다. `BuyBidBlow.Game.asmdef`가 여기 있다 |
| `Characters` | 행위자. 내 캐릭터·카메라·착석·상호작용, 다른 플레이어 아바타, 입찰 팻말. 입력·이동 구현도 이 영역의 책임 |
| `Content/Props` | 씬에 놓이는 소품. 거래 창구·시세 전광판·시계·알림판·종·회사 배너 등. 상호작용 여부로 폴더를 나누지 않는다 |
| `UI` | 화면별 UI. `Menu`·`Lobby`·`HUD`·`Auction`·`Market`·`Portfolio`·`Lot`·`Result` |
| `Common` | 여러 오브젝트가 함께 쓰는 컴포넌트·인터페이스 (예: `IInteractable`, 동기화 값 표시) |
| `Content` | 게임 콘텐츠. 레벨·소품·공용 에셋을 담으며, `Content/Props`에는 소품별 동작 코드와 프리팹·에셋도 함께 둔다. 캐릭터 모델은 그 모델을 쓰는 `Characters` 폴더에 둔다(늑대 → `Characters/Player/Wolf`, 돼지 → `Characters/NPC/Pig`) |
| `Audio/Clips`, `Audio/Cues` | 게임용 오디오 파일·JSON 큐 정의 |
| `Scenes` | 실행 씬 |
| `Dev` | 에디터 도구·실험·디버그·테스트 |

## 오브젝트 폴더

`Characters`·`Content/Props`·`UI`는 시스템을 이용하기만 하는 오브젝트를 담는다. 오브젝트 하나가 폴더 하나이며, 그 오브젝트만 쓰는 스크립트·프리팹·모델·머티리얼을 함께 둔다.

```
Content/Props/TradeCounter/
├─ TradeCounter.cs
├─ TradeCounter.prefab
└─ Models/, Materials/
```

- 한 오브젝트만 쓰는 스크립트는 그 오브젝트 폴더에 둔다. `PlayerNet` 같은 NetworkBehaviour도 예외가 아니며, `PlayerNet`은 `Characters/Player` 아래에 둔다. 여러 오브젝트가 쓰는 일반 컴포넌트는 `Common`, `GameNetworkManager`는 `Network`에 둔다.
- 한 오브젝트만 쓰는 에셋은 그 오브젝트 폴더에, 여러 곳에서 쓰면 `Content`에 둔다.
- 씬 배치는 `Scenes`의 씬이 담당한다. 거래소 씬의 오브젝트에는 `NetworkIdentity`를 붙이지 않는다. 요청은 `PlayerNet`으로 보내고, 표시는 `*Net`의 동기화 값을 읽는다.

## 의존 관계

- 어셈블리는 `Game → Systems` 두 개다. Systems는 UnityEngine·Mirror·UI·MySQL에 의존하지 않으며 asmdef로 강제한다.
- `Network`의 `BuyBidBlow.Game.asmdef`가 Game 어셈블리를 정의하고, `Characters`·`Content/Props`·`UI`·`Common`은 각 폴더의 `BuyBidBlow.Game.asmref`로 같은 어셈블리에 포함된다. 새 최상위 코드 폴더를 만들면 asmref도 함께 둔다. 없으면 `Assembly-CSharp`로 컴파일된다.
- 서버와 클라이언트는 같은 어셈블리(Game)에 있다. 경계는 컴파일러가 아니라 아래 규칙으로 지킨다.
  - 게임 상태 변경은 서버에서 Systems의 기능별 메서드와 `Match.Tick`을 통해서만 한다. `PlayerNet` 등의 Command는 `Auction.Bid` 같은 시스템 메서드를 직접 호출한다. `GameNetworkManager`는 서버에서 경기 생성·관리를 연결하고 경기별 `Tick(now)`를 한 번씩 호출한다. 별도 `MatchHost`나 통합 `GameRequest`는 두지 않는다.
  - Client 코드는 `*Net`의 SyncVar·hook·Rpc로 받은 값만 읽는다. 서버의 `Match`와 시스템 인스턴스를 직접 참조하지 않는다.
  - 서버 전용 메서드에는 `[Server]`, 클라이언트 전용 메서드에는 `[Client]`를 붙인다.
  - 비공개 값(현금·보유 주식·예약금·낙찰 Lot)은 플레이어 오브젝트의 Owner 동기화로만 보낸다.
- CueAudio는 게임 폴더 경로와 게임 규칙에 의존하지 않는다. 게임 쪽에서 큐 데이터를 제공한다.
- `Content`는 콘텐츠 분류 폴더다. `Content/Props`의 코드는 해당 폴더의 asmref로 Game 어셈블리에 포함되며, 프리팹은 필요한 컴포넌트를 참조할 수 있다.

## 네이밍 규칙

**폴더 이름**: 최상위 폴더는 내용의 용도로 짓는다. `Scripts`·`Prefabs`처럼 파일 종류로 짓지 않는다. Systems와 UI에서 같은 기능은 같은 폴더 이름을 쓴다. 인게임 기능은 `Auction`·`Market`(주가·거래)·`Lot`·`Portfolio`·`Round`다.

**네임스페이스**: 역할별 폴더를 따른다. `Content/Props`는 콘텐츠 배치 경로이며 네임스페이스는 `BuyBidBlow.Props`를 유지한다. `BuyBidBlow.Systems.{기능}`(예: `BuyBidBlow.Systems.Auction`), `BuyBidBlow.Network`, `BuyBidBlow.Characters`, `BuyBidBlow.Props`, `BuyBidBlow.UI.{화면}`, `BuyBidBlow.Common`이다. 개발 도구는 `BuyBidBlow.Dev`, 오디오 엔진은 `CueAudio`를 쓴다.

**접미사**

| 접미사 | 의미 | 위치 |
|---|---|---|
| 접미사 없음 | 규칙 개념 그대로 (`Match`, `Auction`, `Portfolio`) | `Systems` |
| `*Event` | 필요한 시스템 처리 결과 이벤트 | `Systems/Types` |
| `*Net` | 오브젝트별 Mirror NetworkBehaviour (SyncVar·Command·Rpc) | 해당 오브젝트의 `Characters/*`·`Content/Props/*`·`UI/*` 폴더 (예: `PlayerNet` → `Characters/Player`). 경기 전체 공개 상태(라운드·주가·Closing Bell 결과)는 `Common/MatchNet`이, 경기 일정(라운드·경매 구간의 시작·끝 시각)은 `Common/MatchTimeline`이 갖고, 소품·HUD는 이를 읽어 표시한다. 이벤트는 static이다: 일어난 일은 `MatchNet.RoundTurned`(라운드 마감과 다음 라운드 시작이 한 번에)·`MatchNet.AuctionStageChanged`, 미리 알아야 하는 시각은 `MatchTimeline`, 내 데이터는 `PlayerNet.Local*` |
| `*View` | UI 컴포넌트. 로직이 커질 때만 `*Presenter`로 분리한다 | `UI/*` |
| `*Repository` | 저장 인터페이스·구현 (저장 기능 도입 시) | 미정 |
| `*Tests` | 테스트 클래스. 메서드 이름은 `조건_결과` 형식이다 | `Dev/Tests` |

## 씬과 개발 도구

- 실제 씬 파일은 `Scenes`, 레벨 모델·머티리얼·조립 프리팹 등은 `Content/Levels`에 둔다.
- `Dev/Editor`에는 에디터 전용 코드를, `Dev/Tests`에는 EditMode·PlayMode 테스트를 둔다.
- `Dev/Sandbox` 씬은 배포용 빌드 씬 목록에 넣지 않는다.
- `Dev`의 코드는 역할별 asmdef 4개로 빌드에서 빠진다. 하위 폴더에는 asmdef를 새로 만들지 않는다.
  - `Dev/BuyBidBlow.Dev.asmdef`: 런타임 코드(`Sandbox`·`Debug` 등). `UNITY_EDITOR`에서만 컴파일된다.
  - `Dev/Editor/BuyBidBlow.Dev.Editor.asmdef`: `UnityEditor`를 쓰는 코드. 에디터 전용이다.
  - `Dev/Tests/EditMode`·`Dev/Tests/PlayMode`의 asmdef: 테스트 전용이다.
- asmdef 아래에서는 Unity의 `Editor` 폴더 규칙이 작동하지 않는다. `UnityEditor`를 쓰는 코드는 반드시 `Dev/Editor` 아래에 둔다.
- `Dev` 아래에 `Resources` 폴더를 만들지 않는다. 안의 에셋이 참조 여부와 무관하게 빌드에 포함된다.
- 새 패키지 참조가 필요하면 asmdef 관리자에게 요청한다.

## 오디오 배치

CueAudio 엔진과 에디터는 `Assets/CueAudio`, 게임용·샘플 오디오 데이터는 `Audio`, 데모와 테스트는 `Dev`에 둔다. CueAudio는 전용 asmdef를 사용하고 데모·테스트는 `Dev`의 역할별 asmdef에 포함한다.

## 관련 문서

- [개발 규칙과 현재 구현 상태](../../Docs/Development.md): 용어·코드 표기·검증 방법·현재 구현 상태·오디오 사용법.
