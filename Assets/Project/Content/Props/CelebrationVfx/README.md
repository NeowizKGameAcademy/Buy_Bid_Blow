# Celebration VFX

Unity 6 / URP 기본 Particle System으로 만든 로컬 시각 효과. 외부 에셋과 VFX Graph가 필요 없다. 사운드는 포함하지 않는다.

| 프리팹 | 연출 | 대략적인 종료 시간 |
|---|---|---|
| Fireworks | 금빛·민트·코랄 불꽃 3연발, 잔광 꼬리 | 3.5초 |
| Fanfare | 양옆 색종이 분수, 금빛 별, 중앙 플래시 | 3.5초 |
| Money | B 문양 지폐와 금빛 동전이 회전하며 낙하 | 4.7초 |

## 확인

`Tools > BuyBidBlow > VFX > Open Demo`로 데모 씬을 열고 Play.
개별 버튼, Play all 버튼, Auto replay를 사용할 수 있다.
데모는 빌드 씬 목록에 추가하지 않는다.

## 사용

프리팹을 씬에 배치하거나 원하는 위치에 Instantiate하면 활성화될 때 재생된다.
루트의 `CelebrationVfx.Play()`로 다시 재생하고 `Stop()`으로 입자를 즉시 비운다.
UnityEvent에서도 두 메서드를 연결할 수 있다.

- `Play On Enable`: 활성화 시 자동 재생.
- `Destroy When Finished`: 일회성 생성 오브젝트를 종료 후 제거. 풀링/씬 배치에서는 꺼둔다.
- 루트 Transform Scale로 전체 크기를 조정한다. 기본 크기는 월드 단위 기준이다.
- 입자는 월드 공간에서 움직인다. 발사 후 발사체 오브젝트를 이동해도 기존 입자는 따라가지 않는다.
- 각 자식 Particle System에서 색상, 개수, 속도 등을 편집할 수 있다.
- 게임 이벤트나 네트워크 RPC에 자동 연결하지 않았다. 각 클라이언트에서 필요한 위치에 재생한다.
- 바닥 충돌은 사용하지 않는다. 바닥을 통과하기 전에 자연스럽게 사라지도록 위치/수명을 조정할 수 있다.

`Build Celebration Pack`은 같은 경로의 생성 에셋과 데모를 다시 만든다.
커스텀한 프리팹은 복제한 뒤 수정하면 재생성 시 보존된다.

개발 검증 진입점: `BuyBidBlow.Dev.CelebrationVfxBuilder.BuildAndVerify`.
컴파일 후 세 효과의 입자 발생, Stop 시 비우기, 종료, 머티리얼 지원을 검사하며
`Library/CelebrationVfxPreview.png`에 미리보기를 저장한다.
