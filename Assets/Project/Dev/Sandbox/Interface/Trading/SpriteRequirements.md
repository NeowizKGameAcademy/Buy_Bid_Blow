# 거래소 패널 HUD — 스프라이트 요구사항

기준: 사용자가 선택한 Brick 거래창 시안. 왼쪽 돼지 / 오른쪽 패널. 기준 해상도 1920×1080. 모든 문구·가격·수량·키 표시와 화살표는 Unity Text로 분리하고 스프라이트에 굽지 않습니다. 런타임 거래 연결 없이 편집용 UI 프리팹으로 구성합니다.

| 순서 | 자산 | 요구사항 / 사용 |
|---|---|---|
| 1 | BG_TradingGlass | 짙은 스모키 청록 반투명, 아주 옅은 가죽 결, 둥근 외곽과 하단 절삭 느낌, 장식 최소. 넓은 내부 영역. 패널 전체 배경. |
| 2 | BG_CompanyPlaque | 가로형 호두나무 명패, 얇은 황동 모서리와 리벳, 글자 없는 중앙. 회사명 Text 별도. |
| 3 | BG_CompanyMedallion | 원형 황동 테두리 + 검은 청록 중심, 투명 바깥, 중앙 무문양. 기존 Straw/Timber/Brick 아이콘 Image 삽입. |
| 4 | BTN_QuantityKey | 작은 목재 키캡 + 아이보리 상판, 약한 입체감, 정면, 화살표/문자 없음. 좌우 동일 자산 사용. |
| 5 | BTN_ConfirmBrass | 가로형 따뜻한 황동, 얕은 베벨과 은은한 금속결, 중앙 무문자, 둥근 모서리. E와 매수 확정은 Text. |
| 6 | BG_Inset / BG_KeyHint | 기존 BG_RoundedDim을 9-slice로 사용. 수량·결제 영역은 얇은 옅은 테두리, 키 안내는 어두운 바탕. |
| 7 | IC_Straw / IC_Timber / IC_Brick | 기존 개별 투명 스프라이트 재사용. 회사별 프리팹의 명패 아이콘만 교체. |
| 8 | TabUnderline / TabDiamond / Separator | Unity Image / RectTransform으로 제작하는 단순 도형. 탭 글자는 Text. |

PNG: 투명 바깥 영역, alphaIsTransparency, sRGB, mipmap off, 무압축, Clamp/Bilinear, FullRect. 명패·버튼은 정면 투영, 바깥 여백을 제외한 Sprite rect 지정. 유리 배경은 Image 알파로 농도를 조절합니다. 기본 도형은 9-slice, 세부 질감 자산은 원 비율 유지.

버튼 상태는 Button ColorTint로 각각 Normal / Highlighted(키보드 선택) / Pressed / Selected / Disabled를 설정합니다. 선택은 황동 강조, 비활성은 낮은 채도와 밝기. 탭 선택 밑줄은 별도 편집 오브젝트입니다. Button 이벤트는 비워둡니다.

작업 순서: 요구사항 → 개별 재질 스프라이트 → 투명도/임포트 검수 → 앵커와 패널 계층 → 회사별 예시 → 프리팹 저장 → 실제 Unity 화면 렌더 및 검사.
