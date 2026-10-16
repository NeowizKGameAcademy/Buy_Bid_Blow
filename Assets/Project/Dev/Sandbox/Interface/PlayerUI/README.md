# Player UI sandbox 역할

- SC_PlayerUI: 기존 홀과 1층 거래/2층 경매 UI를 조합한 테스트 씬.
- PF_PlayerHUD: 자산/시간 고정 HUD, 회사별 거래창, 경매 패널, 조준점. 위치·이미지·일반 Text를 Inspector에서 편집.
- PF_PlayerUIRig: 카메라와 이동/감지/화면 전환 컨트롤러 및 HUD 묶음.
- PlayerMove / PlayerCamera: 기존 이동과 시점. 원본 소스는 변경하지 않음.
- DetectObject: 기존 레이캐스트 감지와 조준점 교체. 거래 계산을 하지 않음.
- UIInteractionTarget: 창구 회사 또는 경매 의자 종류와 착석 시점만 정의.
- PlayerUIController: 키 입력, 탐색/거래/경매 모드, 이동/감지 잠금, 카메라 복원, 해당 패널 열기/닫기.
- TradingPanelView: 전달받은 데이터로 글자/탭/수량/금액/거래 불가 상태를 표시. 자산을 변경하거나 서버 요청을 보내지 않음.
- TradePreviewSource / TradePreviewData: 화면 확인용 회사별 고정 데이터. 실제 연동 시 서버의 현재가·재고·보유·예약금 제외 현금으로 대체.
- TradingRestore (Editor): 배치 편집/프리팹 저장/렌더 검증 도구. 런타임 빌드에 포함되지 않음.
- AuctionEnvelopeDisplay: 별도 Envelope 폴더의 월드 상품 회전/라운드 표기. 거래창 및 HUD 전환과 독립.

## 입력
WASD 이동, 마우스 시점, E 상호작용, Esc 닫기/퇴석.
거래: 위/아래 탭, 좌/우 수량, E 확인(프리뷰 안내만 표시).

## 가독성
거래창은 우상단 앵커 (-32,-220), 배율 .92. 배경 딤 강화, 명패와 가격 영역 정렬, 결제 영역에 거래 가능 → 거래 후 표시, 패널 내부 키 안내.
매수/매도/베팅 탭 선택을 색과 밑줄로 표시. 보유 없음/현금 부족/하한가 베팅은 문구와 비활성 버튼으로 표시.
실제 거래/베팅/입찰, 네트워크 권한과 서버 검증은 아직 미연결. HUD와 시장 데이터도 예시값.