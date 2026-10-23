# HowToPlay 파일철 패널

SC_MainMenu에서 HowToPlay를 누르면 열립니다. A/W 이전 · D/S 다음 페이지 이동, Esc 닫기. 하단 이전·다음, 우측 색인, 닫기 버튼은 마우스 클릭도 지원합니다. 다시 열면 첫 페이지로 돌아갑니다. 열려 있는 동안 메인 메뉴 선택과 입력을 막고, 닫으면 HowToPlay 버튼으로 선택을 복구합니다.

- Prefabs/PF_HowToPlay.prefab: 패널 구조. 메인 메뉴 Canvas 하위에서 사용.
- HowToPlayPanel.cs: 페이지와 인덱스 표시, 키 입력.
- ../MainMenuActions.cs: 패널 열기·닫기, 메뉴 입력과 포커스.
- ../Editor/HowToPlayBuilder.cs: 스프라이트 임포트 및 재조립. 재실행하면 패널 편집 내용을 생성 기준으로 교체하므로 수동 수정 후에는 주의.
- ../Editor/HowToPlayVerification.cs: 배치 Play 검증. 편집기에서 일반 실행용 아님.
- Sprites: 텍스트 없는 원본 PNG와 9-slice용 Sprite asset.
- Examples: 실제 UI 화면을 복사한 예시. 현재 게임 상태와 연결되지 않는 설명용 이미지.
- Concepts/Refined/Page_1~7.png: 시안에 맞춘 최신 Unity 조립 결과.
- PageContent.md / SpriteRequirements.md / GenerationPrompts.json: 내용·자산 요구사항·내장 imagegen 제작 프롬프트.

각 Page 아래 Text·Image를 개별 수정할 수 있습니다. 텍스트는 이미지가 아닌 일반 Unity UI Text이며 Pretendard 글꼴을 사용합니다. 1920×1080 기준 Canvas Scaler를 따릅니다. 실제 게임 로직·레벨·이동 시스템은 변경하지 않습니다.

시안 정렬 보완: HowToPlayStyle.cs가 큰 붉은 페이지 번호, Pretendard Black 제목, 회색 Bold 본문, 붉은 원형 단계·콜아웃, 구분선을 적용합니다. 거래 페이지는 실제 PF_TradingPanel_Straw를 복제한 편집 가능한 설명용 UI이며 버튼은 비활성화되어 실제 거래를 수행하지 않습니다. 원본 인게임 프리팹은 변경하지 않습니다.

2026-10-22: All seven pages use three horizontal rows with large editable text and matching examples. E or click toggles detailed rules; Esc collapses rules before closing the tutorial. Navigation and per-page detail toggles verified in HowToPlayReviewTests. Review captures: Logs/HowToPlayReadable/Page_1.png through Page_7.png; Rules_1.png through Rules_7.png. Clerk example is rendered from PF_Staff_Green, not a replacement character illustration.
