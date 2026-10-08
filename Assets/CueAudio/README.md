# CueAudio

Unity 6000.2용 독립 큐 오디오 모듈. 게임 코드는 큐 ID 또는 `CueReference`로 재생하고, 클립·버스·재생 정책은 데이터로 설정한다. Mirror나 게임 폴더에 대한 엔진 의존성은 없다.

## 바로 사용하기

1. Unity의 스크립트 컴파일이 끝나면 **Tools > CueAudio > Open Demo**를 선택한다.
2. Play를 누르고 Game 뷰의 클릭음, 입찰음, 좌우 3D, 음악 A/B 버튼을 사용한다.
3. **Tools > CueAudio > Runtime Monitor**에서 활성 재생, 버스 볼륨, 최근 요청 200개와 거절 사유를 확인한다.
4. **Tools > CueAudio > Cue Browser**에서 `CueAudioDemoCatalog`를 선택한다. 큐를 선택하고 **Find Connections**를 누르면 씬·프리팹·ScriptableObject의 연결과 코드 후보를 확인할 수 있다.

샘플 씬은 `Assets/Project/Dev/Sandbox/CueAudioDemo.unity`에 있으며 빌드 씬 목록에 추가되지 않는다. 데모 음원은 직접 생성한 합성 테스트 톤이다. 실제 게임용 음원으로 교체해 사용한다. 데모 생성기는 기존 파일을 덮어쓰지 않는다.

## 새 큐 만들기

1. Cue Browser의 **Create Catalog**로 카탈로그와 JSON을 함께 만든다. 기존 카탈로그도 선택할 수 있다.
2. **+ Add Cue**를 누르고 고유 ID를 정한다. AudioClip을 **Add clip (drag here)**에 놓으면 클립 바인딩이 자동 등록된다.
3. 클립 선택 방식, 버스, 볼륨, 피치 범위, 루프, 3D 비율, 쿨다운, 동시 재생 수를 설정한다.
4. **Validate**, **Save JSON**을 누른다. 카탈로그의 에셋 연결은 즉시 저장되고 큐 설정은 Save JSON으로 저장된다. 초안 편집은 Undo를 지원한다.
5. 씬 루트에 `CueAudioHost`를 추가하고 해당 카탈로그를 연결한다. 또는 제공되는 `CueAudioHost.prefab`을 사용한다. AudioListener는 씬에 하나가 필요하다.
6. 소리를 낼 오브젝트에 `CueEmitter`를 추가한다. Catalog와 Cue를 선택하고 버튼 OnClick/UnityEvent에 **CueEmitter.Play()**를 연결한다.

`Listen`은 원본 클립 미리듣기다. 큐 볼륨·피치·공간·믹서 설정은 Play 모드의 **Play Cue** 또는 데모에서 확인한다. 미리듣기는 Unity 내부 API 어댑터를 사용하며, 다른 Unity 버전에서 제공되지 않으면 안내를 표시한다. 큐 JSON 변경은 Play 모드를 다시 시작하면 반영된다.

큐 ID는 안정적인 연결 키다. ID를 변경하거나 큐를 삭제하면 기존 참조는 자동 변경되지 않는다. 먼저 사용처를 확인하고 관련 필드와 코드를 수정한다. 없는 ID는 Inspector에 `Missing`으로 표시된다.

## 코드에서 사용

```csharp
using CueAudio;
using UnityEngine;

public sealed class BidAudio : MonoBehaviour
{
    [SerializeField] private CueReference accepted;
    private AudioHandle music;

    public void OnBidAccepted()
    {
        CueAudioHost.Instance.Engine.Play(accepted, owner: this);
    }

    public void StartMusic()
    {
        var audio = CueAudioHost.Instance.Engine;
        music = audio.Crossfade(music, "bgm.demo_a", seconds: 1, owner: this);
    }
}
```

`PlayAt(cue, position, owner)`는 요청 당시의 고정 위치에서 재생한다. 움직이는 오브젝트를 따라가는 재생은 아직 지원하지 않는다. 3D 재생은 큐의 `spatialBlend`가 0보다 커야 한다. `Play()`로 3D 큐를 재생하면 월드 원점에서 들린다.

`SetBusVolume("sfx", 0.7f)`와 `SetMasterVolume(0.5f)`는 활성·후속 재생에 적용된다. 카탈로그의 버스에 선택적으로 AudioMixerGroup을 연결한다. 최종 AudioSource 볼륨은 큐 × 버스 × 마스터 × 페이드이며, 믹서 그룹의 효과/게인은 그 뒤에 적용된다. 믹서 에셋이나 노출 파라미터가 없어도 사용할 수 있다. 볼륨 설정 영속 저장은 게임에서 담당한다.

## 연결 조회의 범위

- `CueReference`: 카탈로그 에셋 + 큐 ID가 일치하는 씬·프리팹·ScriptableObject 필드.
- UnityEvent: 해당 큐를 가진 `CueEmitter.Play()`로 연결된 persistent event. 실행 중 추가한 listener는 정적 검색 대상이 아니다.
- 코드: 정확한 문자열 리터럴이 들어 있는 줄을 후보로 표시한다. 주석도 포함되며 동적으로 조합하는 ID나 별도 상수를 추적하는 정적 분석기는 아니다.
- 런타임: 실제 요청 ID, 성공/거절 사유, 선택된 클립, owner, C# 호출 파일/줄을 기록한다. 래퍼를 거치면 그 래퍼의 호출 줄이 기록된다.

검색은 `Assets` 아래를 대상으로 한다. 열려 있는 씬은 저장하지 않은 현재 상태를 검사하고, 닫힌 씬은 preview scene으로 검사한 뒤 닫는다. 검색 결과를 누르면 해당 파일/오브젝트로 이동한다. 미저장 씬의 결과는 현재 에디터 세션에서만 유효하다. 프로젝트 전체 스캔은 대규모 프로젝트에서 시간이 걸릴 수 있다.

## 재생 정책과 수명

- JSON 형식은 `{"version":1,"cues":[...]}`이다. 피치 범위는 `{"x":0.97,"y":1.03}`으로 저장한다.
- 선택 방식: `first`, `random`, `randomNoRepeat`, `sequence`. 중복된 클립 ID는 검증 오류다.
- 동시 재생 수를 넘으면 `reject` 또는 그 큐의 가장 오래된 재생을 즉시 정지하는 `stopOldest`를 적용한다. 전체 풀 한도에 도달하면 요청을 거절한다.
- 쿨다운은 마지막 성공 요청 기준이며 거절된 요청은 시간을 갱신하지 않는다.
- 핸들은 풀 슬롯의 세대 번호를 확인하므로 오래된 핸들이 새로운 재생을 정지할 수 없다. `IsPlaying`은 엔진의 활성 재생 상태이며 실제 장치 출력을 보장하는 값이 아니다.
- `Crossfade`는 새 소리가 받아들여질 때만 이전 소리를 페이드아웃한다. 실패하면 기존 핸들을 반환한다. 겹치는 두 소리를 위한 여유 voice가 필요하다.
- 시간은 unscaled realtime을 사용한다. `Time.timeScale = 0`에도 재생과 페이드가 계속된다. `AudioListener.pause` 중에는 자동 종료 판정을 보류하지만 페이드 시간은 진행한다.
- 호스트는 기본적으로 씬 전환 중 유지된다. 루트 오브젝트에 놓아야 하며 씬마다 중복 호스트를 둘 경우 후속 호스트가 비활성화된다. 호스트 비활성화/파괴 시 모든 voice를 정리한다.
- emitter는 기본적으로 비활성화 시 자신이 요청한 소리를 모두 페이드아웃한다. `Stop On Disable`을 끄면 호스트가 관리하는 자연 종료/명시적 정지를 따른다.
- `UNITY_SERVER`에서는 호스트를 초기화하지 않는다. 기록은 Editor/Development Build에서 기본 활성화되고 일반 플레이어에서는 기본 비활성화된다.

## 검증

Unity Test Runner에서 `CueAudio.EditMode.Tests`, `CueAudio.PlayMode.Tests`를 실행한다. 검증 항목은 JSON 오류, 쿨다운, 동시 재생 제한, 비반복 선택, 풀 재사용, 오래된 핸들, 페이드, 크로스페이드 실패, 호출 기록, 사용처 탐색, 실제 프레임 진행 중 자연 종료와 호스트/emitter 수명이다.

정밀 음악 박자 동기화, 스트리밍/Addressables 로딩, follow-target, 실시간 JSON hot reload는 현재 범위에 포함하지 않는다. `IAudioClipResolver`는 검증/클립 해석 경계를 정의하며 현재 엔진은 `CueCatalog`의 직접 참조 바인딩을 사용한다.
