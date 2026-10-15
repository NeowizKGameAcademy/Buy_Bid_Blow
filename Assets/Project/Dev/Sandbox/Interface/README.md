# In-game Interface — 편집용 예시

- 씬: `SC_InGameInterface.unity`
- 독립 HUD 프리팹: `PF_InGameHUD_Example.prefab`
- 실제 Unity 렌더: `InterfacePreview.png`

최근 확정한 테두리 없는 반투명 HUD 구성을 구현했습니다. 기존 AuctionHall-Deco를 복사했으며 원본과 로비 씬은 수정하지 않았습니다. 모든 숫자·문구는 **일반 Unity UI Text**, 모든 아이콘·배경은 **Image**, 위치는 **RectTransform**입니다. HUD에 런타임 제어 컴포넌트나 게임 상태 연결은 없습니다. 수치는 정적인 예시입니다.

## 계층과 편집

`HUD - Static Editable Example`의 자식을 직접 편집합니다.

| 그룹 | 내용 | 앵커 / 피벗 | Anchored Position | 크기 |
|---|---|---|---|---|
| Assets - Top Left | Total, Cash, Holdings, Downside Bet | (0,1) / (0,1) | (48,-38) | 440×214 |
| Round Timer - Top Right | Round, Time, Phase | (1,1) / (1,1) | (-48,-36) | 245×170 |
| Crosshair | 중앙 조준점 | (0.5,0.5) / (0.5,0.5) | (0,0) | 6×6 |
| Interaction Hint - Bottom Center | 키캡 E, 안내 문구 | (0.5,0) / (0.5,0) | (0,42) | 350×56 |
| Auction Interaction - Reserved | 이후 경매 패널용 빈 영역, 비활성 | (0.5,0) / (0.5,0) | (0,106) | 760×250 |

Canvas: Screen Space Overlay. CanvasScaler: Scale With Screen Size, 기준 1920×1080, Match=0.5. 자식에는 자동 LayoutGroup을 사용하지 않아 입력한 위치가 유지됩니다.

왼쪽 위 그룹의 자식은 좌측 위 기준입니다.

- Total: (18,-0), 400×32. `Label`과 `Value` 텍스트가 분리되어 있습니다.
- Cash: (18,-44), 270×90. 현금 아이콘과 `$150` 텍스트가 분리되어 있습니다.
- Holdings: (326,-38), 100×104. 회사별 아이콘과 수량을 세 행으로 구성했습니다.
- Downside Bet: (18,-151), 410×42. 아래 화살표·회사 아이콘·대상 라운드·참가금/보상을 분리했습니다.

`Background - opacity editable`의 Image Color A를 조절하면 배경 불투명도가 바뀝니다. 현재 자산 0.17, 타이머 0.13, 안내 0.15이며 소프트 페이드 스프라이트 자체가 가장자리로 갈수록 투명해집니다. 바깥 테두리는 없습니다. 키캡에만 네 개의 얇은 Image로 테두리를 구성했습니다. Text의 Shadow 컴포넌트도 직접 조절할 수 있습니다.

## 자산

- `IC_Cash`, `IC_Straw`, `IC_Timber`, `IC_Brick`: 이번 작업에서 내장 image_gen으로 개별 제작한 PNG. 요구사항과 최종 프롬프트는 `SpriteRequirements.json`에 있습니다.
- `IC_Direction`: 기존에 생성한 방향키 아이콘을 복사해 아래 방향으로 회전했습니다.
- `BG_SoftFade`: 이 UI를 위해 코드로 생성한 단순 소프트 그라데이션 PNG. 배경 Image의 컬러·알파를 자유롭게 변경할 수 있습니다.

스프라이트 임포트 시 투명 여백을 제외한 영역을 지정했으며 원본 아이콘 PNG 픽셀은 수정하지 않았습니다. 압축·밉맵을 끄고 투명도를 보존했습니다.

## 배경과 검수

정적인 1인칭 카메라 높이는 1.65m, 화각은 75°입니다. 경매장과 늑대 모델을 배경으로 배치했고, 전광판·창구 명칭도 World Space Canvas의 편집 가능한 Text입니다. 창구와 배경 모델은 예시용이며 실제 거래 기능이 없습니다.

Unity 배치 모드에서 씬·프리팹 저장, 텍스트 13개, 스프라이트 6개, HUD의 기본 UI 컴포넌트 구성, 1920×1080 렌더를 확인했습니다. 결과는 `Verification.txt`에 있습니다.

생성 도구는 `Editor/InterfaceSceneBuilder.cs`에만 있으며 HUD에 붙어 있지 않습니다. 생성 메뉴는 기존 씬이 있으면 덮어쓰지 않아 수동 편집을 보호합니다.
