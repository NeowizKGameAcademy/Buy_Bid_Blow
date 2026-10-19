# 로비 UI 스프라이트 제작 기록

이 폴더는 로비 전용 아트 자산입니다. `SC_Lobby`의 uGUI HUD에 배치했습니다. 좌표와 키보드 조작은 `HudLayout.md`에 기록했습니다.

## 공통 요구사항과 제작 과정

1. 요소의 역할·모양·투명도·상태·재사용 방법을 `SpriteRequirements.json`에 기록했습니다.
2. 내장 image_gen으로 요소마다 독립 PNG를 생성했습니다. 최종 프롬프트도 같은 JSON에 기록했습니다.
3. 생성 결과에서 글자 없음, 형태, 투명 배경, 버튼 상태 구분을 확인했습니다. 원형 버튼은 과한 장식을 제거해 한 번 수정했습니다.
4. Unity 임포터가 여백을 제외한 스프라이트 영역을 설정합니다. PNG 원본 픽셀은 변경하지 않습니다.
5. 버튼 시트는 상태별 4개 서브스프라이트로 나누고, 신축 배경은 9-slice를 설정합니다.
6. `Verification.md`에 임포트 결과를, `SpriteCatalog.html`에 전체 미리보기를 기록합니다.

색상: 짙은 청록 `#123E38`, 황동 `#D7A84D`, 크림 `#FFF0C5`. 배경에는 은은한 입체감, 작은 기호에는 단순한 실루엣을 사용합니다. 모든 글자는 Unity 텍스트로 올립니다. 원본은 고해상도이며 아래 수치는 1920×1080 화면에서의 권장 표시 크기입니다.

| 순번 / 파일 | 역할 및 조건 | 권장 표시 크기 | 사용 방식 |
|---|---|---|---|
| 01_LogoPlate | 게임명용 빈 장식판, 중앙 문양 보존 | 450×150 | Simple, 비율 유지 |
| 02_Gavel | 독립 경매 망치 | 48×48 | Simple |
| 03_SubtitlePlate | 로비 부제용 빈 판 | 250×52 | 가로 9-slice |
| 04_SmallButtonStates | 인원·옵션·나가기 공용 배경, 4상태 | 130×56 | 9-slice, 아이콘·글자 별도 |
| 05_Person | 인원 표시 | 24×24 | Simple |
| 06_Options | 옵션 톱니바퀴 | 26×26 | Simple |
| 07_Exit | 문과 바깥 화살표 | 28×28 | Simple |
| 08_Nameplate | 플레이어 이름표 | 180×40 | 가로 9-slice |
| 09_HostCrown | 방장 왕관 | 30×30 | Simple |
| 10_LocalBadge | ‘나’ 텍스트용 빈 원형 배지 | 34×34 | Simple, 정원 유지 |
| 11_StatusBadge | 준비·꾸미는 중 공용 중립색 배경 | 140×34 | 9-slice, 준비 녹색·편집 황색 틴트 |
| 12_Check | 준비 상태와 준비 버튼의 체크 | 20×20 / 32×32 | Simple, 금색 버튼 위에서는 어두운 색으로 틴트 |
| 13_StatusDot | 편집 상태의 점 | 5×5 | 같은 스프라이트 3개 반복 |
| 14_CategoryHighlight | 반투명 세로 선택기의 현재 행 | 140×32 | 9-slice, 중앙 실제 알파 약36% |
| 15_CategoryChevron | 항목 선택 위쪽 꺾쇠 | 14×8 | 아래쪽은 180° 회전 |
| 16_Keycap | 키보드 안내용 빈 키캡 | 24×24 | 9-slice |
| 17_KeyboardArrow | 키캡용 위쪽 방향키 | 12×12 | 회전해 ↑↓←→ 사용 |
| 18_RoundButtonStates | 캐릭터 양옆 종류 변경 배경, 4상태 | 56×56 | Simple, 정원 유지 |
| 19_SideChevron | 좌우 변경용 굵은 꺾쇠 | 18×26 | 오른쪽은 좌우 반전 |
| 20_VacantNameplate | 빈 자리 반투명 배경 | 160×40 | 9-slice, 필요하면 UI 알파 추가 조절 |
| 21_VacantCircle | 빈 자리 점선 원 | 130×130 | Simple, 정원 유지 |
| 22_VacantPlus | 빈 자리 더하기 | 28×28 | Simple |
| 23_PlayerRing | 발밑 금색·청록 선택 링 | 월드 지름 약2m | 수평 평면/데칼, 원근은 카메라에서 적용 |
| 24_ReadyButtonStates | 큰 금색 준비 버튼, 4상태 | 300×76 | 9-slice, 체크·글자 별도 |

## 상태와 입력

버튼 시트의 순서는 **왼쪽 위 Normal / 오른쪽 위 Focused / 왼쪽 아래 Pressed / 오른쪽 아래 Disabled**입니다. Unity에서는 해당 이름의 서브스프라이트를 바로 선택할 수 있습니다. `Button.transition = SpriteSwap`에서 기본 이미지는 Normal, highlighted/selected는 Focused, pressed는 Pressed, disabled는 Disabled에 연결합니다. 키보드 탐색 시 선택 상태를 반드시 갱신합니다.

위·아래 키로 상의/하의/넥타이를 선택하고 좌·우 키로 종류를 바꿉니다. 세로 목록의 이동·크기·투명도는 UI 애니메이션으로 처리합니다. 마우스 아이콘과 하단 패널은 없습니다.

## Unity에서 사용

- 메뉴: `Tools > BuyBidBlow > Lobby > Import UI Sprites`.
- `LobbySpriteLibrary.asset`에 33개 스프라이트 참조가 있습니다. `Find("24_ReadyButtonStates_Focused")`처럼 조회할 수 있습니다.
- 배경의 테두리와 둥근 끝을 보존하려면 `Image.Type.Sliced`를 사용하고 표시 크기를 원본보다 줄일 때 pixels-per-unit multiplier를 조정합니다.
- 아이콘과 원형 자산은 `preserveAspect`를 사용합니다. 상태별 PNG 크롭 여백이 다를 수 있으므로 버튼 RectTransform과 클릭 영역은 고정하고 상태 이미지의 실제 화면 정렬을 확인합니다.
- 반투명 자산은 PNG의 알파를 유지했습니다. 선택 링은 중심이 투명한 링 형태입니다. 빈 자리 패널은 추가 UI 알파로 배경 노출을 조절할 수 있습니다.
- 고해상도 생성물에 미세한 가장자리 질감이 있으므로 최종 HUD 크기에서 확인하며, 외곽 광택을 클릭 영역으로 사용하지 않습니다.

24개 PNG = 단일 자산 21개 + 버튼 시트 3개. Unity에서 사용 가능한 독립 스프라이트는 **33개**입니다. 모든 버튼 상태를 별도 PNG로 다시 저장하는 대신 Unity 서브스프라이트로 제공합니다.
