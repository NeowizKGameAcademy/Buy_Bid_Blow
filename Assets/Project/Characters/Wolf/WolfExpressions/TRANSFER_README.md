# BUY, BID, BLOW — Wolf Character Transfer
Unity 6000.2.8f1에서 제작. 캐릭터 프리팹, 메시/재질/텍스처, 10개 Legacy 애니메이션 클립, 얼굴 4종, 6의상 조합, 입찰 팻말, 런타임 스크립트 2개, 확인 씬을 포함한다.
## 가져오기
1. 대상 프로젝트를 백업하거나 버전 관리에 저장한다.
2. Assets > Import Package > Custom Package에서 BUY_BID_BLOW_Wolf_Character_v1.unitypackage 선택.
3. 모든 항목을 체크하고 Import. 컴파일/임포트가 끝날 때까지 기다린다.
4. Assets/BuyBidBlow/Scenes/SC_Wolf_Character.unity를 열고 Play하여 표정/의상/동작을 확인한다.
5. 실제 게임 씬에는 Assets/BuyBidBlow/Characters/WolfExpressions/PF_Wolf_Character.prefab을 배치한다. 테스트 GUI는 확인 씬에만 있다.
참조를 보존하도록 패키지에 .meta GUID를 포함한다. 같은 리소스를 이미 가져온 프로젝트에서는 Import가 기존 항목을 갱신할 수 있다.
## 포함 기능과 범위
기쁨 / 의심 / 놀람 / 무표정. 정장/셔츠/조끼 × 긴바지/반바지. 독립 넥타이 색상. 직접 제작 5클립 + Mixamo Walking, Defeat, Hand Raising, Flair, Hip Hop Dancing.
BBBWolfCharacter.SetExpression, SetOutfit, SetTieColor, PlayAction 사용. 자동 재생은 기본 꺼짐. 에디터 생성 스크립트와 원본 Mixamo FBX, Blender 소스는 이 배포 패키지에 포함하지 않는다. 편집 가능한 .anim과 사용 메시/FBX는 포함한다.
애니메이션은 Legacy SampleAnimation 방식의 제자리 재생이며 위치 이동 곡선은 제외한다. Humanoid Animator, 입력/이동, 네트워크는 포함하지 않는다.
큰 Flair/Hip Hop 동작은 기본 이동보다 변형이 커서 넥타이/옷 관통과 지면 접촉을 최종 게임 동작 범위에 맞게 추가 조정할 수 있다. 좌표 검사 통과가 관통 없음의 보장은 아니다.
재질은 Built-in Standard 및 Unlit/Color 사용. URP/HDRP 프로젝트에서는 렌더 파이프라인에 맞는 셰이더로 변환해야 한다. 넥타이 색상 코드는 _Color 속성 사용: URP로 바꾸면 _BaseColor 대응도 필요하다.
