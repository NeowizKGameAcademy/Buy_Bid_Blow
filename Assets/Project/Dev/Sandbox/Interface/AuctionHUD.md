# 경매 HUD 예시

SC_InGameInterface 씬과 PF_InGameHUD_Example 프리팹에 Auction - Hidden Example을 추가했습니다. 기본은 비활성이며 게임 연결과 키 입력 처리는 없습니다. 모든 문구는 Text, 아이콘·딤은 Image, 위치는 RectTransform으로 편집합니다. 공개·비공개 프리뷰는 임시 복제 HUD로 렌더해 저장된 일반 화면에는 영향을 주지 않습니다.

| 요소 | 내용 | 배치 / 기준 1920×1080 |
|---|---|---|
| Auction Status - Top Center | 1차/2차, 방식, 경매 남은 시간, 공개 최고가/최고 입찰자 또는 내 비공개 제출액 | 상단 중앙 (0,-38), 690×108 |
| Information Lot - Right | 방향 정보, 두 대상 라운드와 각 라운드의 두 회사 아이콘 | 우상단 (-48,-268), 330×258 |
| Bid Controls - Bottom Center | 입력 예정 금액, 좌우 $1 증감, E 입찰/제출/수정, 공개 취소 불가 또는 비공개 X 취소 | 하단 중앙 (0,42), 740×180 |
| Reserved Cash - Below Assets | 입찰로 묶인 현금, 실제 거래 가능 현금 | 좌상단 (56,-244), 350×42 |
| Feedback - Hidden | 입찰 완료, 재입찰 필요, 현금 부족, 마감 등의 메시지 예시 | 하단 중앙 (0,238), 600×54, 비활성 |
| Private Result - Hidden | 내 낙찰 결과, 낙찰자의 회사별 상승/하락/보합 정보, 닫기 안내 | 중앙 위 (0,260), 600×230, 비활성 |

경매 시간은 60초 상품 마감까지의 시간, 우상단은 240초 경매 라운드의 남은 시간입니다. 예시는 공개 00:38/라운드 03:38, 비공개 00:38/라운드 02:38입니다. 같은 개최의 공개 상품 대상은 2R·5R, 비공개 상품은 3R·4R로 중복을 피했습니다. 라운드별 회사 두 개가 표시되고 실제 방향은 상품 소개에 노출하지 않습니다.

공개 최고 입찰은 취소 불가, 비공개는 마감 전 제출 수정/취소 가능을 안내합니다. 비공개 화면에 다른 사람의 제출액·낙찰자·낙찰가는 표시하지 않습니다. 동액 우선순위는 현재 금액의 서버 제출 시각 기준이며 실제 판정은 이후 시스템 연결 대상입니다. 최소 입찰가는 미확정이므로 표기하지 않았습니다.

Private Result와 Feedback은 상태별 문구를 교체하기 위한 편집용 예시입니다. 비공개 실패자에게 정보·낙찰가를 보여주는 상태가 아닙니다. 경매장 접근 시에만 하단 컨트롤을 표시하는 등의 실제 표시 조건은 이후 연결해야 합니다. 기존 Auction Interaction - Reserved 빈 영역은 그대로 유지했습니다.

## 수동 확인

Hierarchy에서 HUD - Static Editable Example > Auction - Hidden Example을 켭니다. 일반 상호작용 안내는 끄면 됩니다. 공개가 기본 예시입니다. 비공개는 Auction Status 안의 Public Highest Bid를 끄고 Sealed Status를 켜고, Bid Controls 안의 Public Rule을 끄고 Sealed Rule을 켭니다. 프리뷰 PNG에서는 라운드·타이머·상품 대상까지 상태에 맞춰 별도로 구성했습니다. 키 배치는 제안용입니다.

- AuctionPublicPreview.png: 공개 경매 시안
- AuctionSealedPreview.png: 비공개 경매 시안
- AuctionVerification.txt: 저장 및 구성 검증
## 2층 경매장 시안

SC_AuctionSecondFloorInterface.unity: 기존 홀을 복사한 별도 예시 씬. 카메라 (0,5.65,-5.6), 2층 바닥 y=4 기준 눈높이 1.65m. 돼지 진행자·입찰대·늑대 참가자 2명과 공개 경매 HUD를 배치했습니다. AuctionSecondFloorPreview.png는 실제 Unity 1920×1080 렌더입니다. 원본 홀과 일반 HUD 씬의 카메라는 변경하지 않았습니다.
