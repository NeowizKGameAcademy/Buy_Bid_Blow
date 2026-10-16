# 거래소 패널 사용 안내

## 파일

- PF_TradingPanel.prefab: Brick 데이터를 기본으로 한 공통 패널. Canvas 아래에 놓습니다.
- PF_TradingPanel_Brick.prefab / Straw.prefab / Timber.prefab: 공통 프리팹의 회사별 Variant.
- SC_TradingInterface.unity: 기존 홀을 복사한 거래소 HUD 예시 씬. 왼쪽은 기존 Red 돼지 모델, 오른쪽은 Brick 패널입니다.
- TradingBrickPreview.png / TradingStrawPreview.png / TradingTimberPreview.png: 실제 Unity 렌더. 회사별 UI 확인용이며 돼지는 동일한 기존 Red 모델입니다.
- SpriteRequirements.md: 제작 목록과 임포트·상태 요구사항.
- Verification.txt: 임포트와 프리팹 구조 검사.

## 편집 구조

기준 1920×1080 Canvas, CanvasScaler Scale With Screen Size / Match 0.5. 패널 루트 우상단 앵커·피벗 (1,1), Anchored Position (-64,-190), 크기 860×850. 모든 자식은 좌상단 기준 고정 위치입니다. 자동 LayoutGroup과 런타임 상태 연결을 사용하지 않습니다.

| 그룹 | 위치 (x,y) / 좌상단에서 아래 방향 | 크기 |
|---|---|---|
| Company Header | 0,0 | 860×180 |
| Trade Tabs | 35,210 | 790×62 |
| Quantity Control | 35,292 | 790×174 |
| Stock Availability | 35,485 | 790×48 |
| Payment Summary | 15,557 | 830×221 |
| Close Hint | 300,796 | 260×50 |

회사명·가격·증감·보유·시장 재고·구매 가능·수량·합계·거래 후 현금·키 안내는 모두 일반 Unity UI Text입니다. 글자가 들어간 스프라이트가 없습니다. 각 아이콘과 스프라이트 배경은 별도 Image입니다. 버튼은 Tab 3개, 수량 2개, 매수 확인, 닫기 7개이며 ColorTint와 Explicit Navigation을 설정했습니다. Normal/Highlighted/Pressed/Selected/Disabled를 Inspector에서 편집할 수 있습니다. 클릭 이벤트는 비워 두었습니다.

BG_TradingGlass와 Glass Dim - Opacity Editable의 Image 알파로 배경 농도를 조절합니다. 수량·결제 그룹 내부의 Inset Edge / Inset Fill도 독립 Image입니다. 키캡 E, ESC와 화살표는 별도 Text입니다.

## 실제 게임 연결 전 주의

매수 탭이 선택된 정적인 배치 예시입니다. 탭 전환·수량 변경·매수·닫기 동작과 서버 검증은 연결되지 않았습니다. 예시 씬에는 입력 모듈/게임플레이 이벤트를 추가하지 않았으며 키보드 네비게이션은 향후 게임의 EventSystem에서 사용하기 위한 설정입니다. 패널 프리팹은 Canvas나 게임 상태 컴포넌트를 포함하지 않습니다.

공통 프리팹을 수정하면 회사별 Variant가 상속합니다. 회사명과 아이콘, 가격 등만 Variant에서 덮어씁니다. 기존 일반 HUD와 경매 HUD 씬은 그대로 보존했습니다.

## 이미지 제작

내장 image_gen으로 글자 없는 재질 자산 5개를 각각 제작했습니다. 각 생성 프롬프트는 SpritePrompts.json에 저장합니다. SpriteImport 시 투명 여백을 제외한 Sprite rect를 지정하며 PNG 원본 픽셀은 수정하지 않았습니다. 패널 질감은 시안과 같은 방향으로 재제작한 자산이며 스크린샷을 잘라 붙인 UI가 아닙니다.
