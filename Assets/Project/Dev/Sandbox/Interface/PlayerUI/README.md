# 통합 플레이어 UI — 로컬 경기

SC_PlayerUI에서 Play하면 MatchSettings.json으로 실제 로컬 경기가 시작됩니다. 시작 현금 $200, 0주, 기본 참가 3명(나 + 거래하지 않는 대기 참가자 2명), 회사별 재고 12주입니다. AI와 네트워크는 포함하지 않습니다.

## 조작
- WASD 이동, 마우스 시점, Space 점프, 조준 후 E 창구/의자 이용, Esc 닫기/퇴석.
- 거래창: 위/아래 매수·매도·하락 베팅, 좌/우 수량, E 확정. 같은 회사 베팅 탭에서 E로 취소·전액 환불. 다른 회사에 베팅하려면 기존 베팅부터 취소.
- 경매 의자: 좌/우 $1 조절, E 입찰/비공개 수정, C 비공개 취소, Esc 퇴석. 퇴석해도 입찰은 유지됩니다.
- I: 본인이 낙찰받은 방향 정보 열기/닫기. 비공개 낙찰 결과는 본인 성공·실패만 표시.
- Local Match Runtime의 clockSpeed 기본값 1. Inspector에서 높이면 대기 시간을 줄여 확인할 수 있습니다. seed를 고정하면 주가 일정 재현.
- 최소 입찰가는 아직 합의 전이므로 기존 로직의 $1 임시값 유지.

## 역할 분리
| 구성 | 책임 |
|---|---|
| MatchEngine (Systems/Match) | 로컬 권위 경기 상태. 현재 라운드/마감, 기존 주가 생성기·상품 생성기·경매·자산·거래 서비스 조합, 베팅·종료 순위. UI/Unity 오브젝트에 의존하지 않음 |
| LocalTradeService / PlayerAssetState | 거래 검증과 자산 변경, 예약금 제외 현금, 유한 재고 차감·매도 반환. 기존 호출은 옵션 재고 인자를 생략하면 기존 동작 유지 |
| AuctionRound / OpenAuction / SecretAuction | 공개·비공개 시간 슬롯, 입찰 검증·예약·해제·정산·동액 처리 |
| MatchUIBridge | 한 로컬 플레이어의 요청 전달, 실제 스냅샷으로 HUD/거래창/경매/월드 시세판 갱신. 봉투 라운드 연결. 네트워크 연동 시 요청/스냅샷 경로를 교체할 경계 |
| PlayerUIController | 입력, 탐색/거래/경매 전환, 카메라 착석·복원, 이동/감지 잠금 |
| TradingPanelView (UI/Market/TradingPanel) | 전달된 데이터 표시, 예상 합계·거래 후 현금, 탭·버튼 상태. 자산 변경 없음 |
| TradePreviewSource | 에디터 배치 캡처의 예시값. Play에서는 MatchUIBridge의 실제 데이터 사용 |
| UIInteractionTarget / DetectObject | 창구·의자 식별 / 기존 레이캐스트와 조준점 |
| AuctionPanelView (UI/Auction/Panel) | 경매 헤더·입찰 조작·상품 표시. 입찰하지 않음 |
| AuctionEnvelopeDisplay (Content/Props/AuctionEnvelope) | 월드 봉투 회전과 라운드 글자. 게임 정산과 UI 배치에 의존하지 않음 |
| 편집 가능한 HUD 프리팹 | Text/Image/RectTransform 디자인. 3개 회사 패널과 공통 고정 HUD |

기존 MatchManager와 Move & Trade 씬은 유지합니다. 새 통합 씬에서는 별도 MatchEngine이 경기 진행을 맡아 싱글턴 중복을 피합니다. PF_PlayerUIRig에는 로컬 런타임도 포함합니다. 다른 씬에서 재사용하면 시세판과 월드 봉투 참조를 해당 씬 대상으로 지정해야 합니다.

## 마감 처리
마감 시각 이상 요청 거절 → 경매 마감 정산 → 종가 적용 → 실제 종가 하락일 때 베팅 총 2배 지급 → 다음 라운드. 17R 종료 시 현금 + 주식 평가액으로 순위, 동액이면 총 보유 주식 수, 이것도 같으면 공동 순위.
회사별 총 발행량은 시작 인원×4로 유지하며 매도분만 재고로 돌아옵니다. 수량/현금/재고 부족은 요청 전체 거절합니다.

## 검증
MatchIntegrationVerification.txt: 핵심 로직 시나리오.
MatchPlayVerification.txt: 실제 Play에서 UI/자산/경매/모드 전환을 연결한 시나리오.
멀티플레이 권한/동기화, 실제 플레이어 입장 수 연동, AI는 별도 작업입니다.
## 거래소 배치와 이동 개선
거래소는 홀 중심 기준 180도 회전하여 반대편 벽으로 이동했습니다. 창구 3개, 시세판, 창구 상호작용 콜라이더, 창구 앞 정적 늑대 모델을 함께 옮겼습니다. 경매장은 그대로입니다.
PlayerMove의 stableControls 옵션은 이 테스트 리그에서만 켭니다. 대각선 정규화, 지면/공중 가속, 발밑 SphereCast와 바닥 법선 검사, 점프 입력 버퍼 0.12초와 코요테 0.1초, 키를 누르는 순간만 점프, 물리 프레임 회전을 사용합니다. 마우스 감도는 0.05에서 0.15도/픽셀로 조정했습니다.
창을 닫을 때 원래 카메라 기울기를 복원하고, 포커스나 커서 잠금이 없을 때는 탐색 이동을 입력하지 않습니다. 캡슐 마찰을 제거하여 벽에 붙어 버티는 현상을 줄였습니다.

## 정보 수첩
Notebook/SpriteRequirements.md에 12종 스프라이트 요구사항, Notebook/SpritePrompts.json에 image_gen 제작 프롬프트를 기록했습니다. PNG 원본과 trimmed Sprite 자산(UI/Notebook/Sprites), PF_InformationNotebook과 InformationNotebookView는 UI/Notebook에 있습니다.
I로 열기/닫기, Esc 닫기, 위/아래 방향키로 한 라운드씩 스크롤합니다. 17라운드 중 8행을 표시합니다. 수첩을 열어도 경기 시간은 계속 진행하며, 이동·시점·거래·입찰 입력은 잠급니다. 닫으면 이전 거래/착석 상태를 유지합니다.
InformationNotebookView는 MatchUIBridge.OwnedInformation만 받습니다. 미래 가격 일정은 참조하지 않으며 낙찰받지 않은 칸은 빈칸입니다. 제목·회사명·라운드·방향·안내는 일반 Text, 배경·아이콘·칸·밑칠은 개별 Image입니다. 에디터의 NotebookAssembled.png는 디자인 검수용 샘플이며 실제 Play 데이터는 LiveNotebook.png에서 확인합니다.
제작 도구: built-in image_gen. 알파/빈칸 테두리 검사 및 실제 Play의 미보유 칸, 낙찰 정보 4칸, 스크롤 끝, 입력 잠금/복원 검증 통과.
