# 게임 오디오 연결

기존 Assets/CueAudio를 재사용한다. 실제 클립: Clips/CollectedSFX, 임시 클립: Clips/Temp(TEMP_ 접두사, 합성 톤).

## 설정
설정은 Cues/MatchAudio.json 하나로 한다. 큐마다 `clips`에 클립 파일의 에셋 경로를 직접 적고, 버스 음량은 `buses`에 둔다. Cues/MatchAudioCatalog.asset은 빌드에 클립을 포함시키려고 JSON에서 자동 생성하는 파일이다. JSON을 저장하거나 Clips 아래 파일을 가져오면 MatchAudioSetup.Sync가 다시 만들며, 손으로 고치지 않는다. 버스의 믹서 그룹만 카탈로그에 남는다. Cue Browser의 Save JSON은 `buses`를 지우므로 쓰지 않는다. music 버스 큐의 클립은 Sync가 Streaming으로 맞춘다.

## 효과음
- ui.navigate: 선택 이동. 키보드 포커스·마우스 버튼 진입·거래 탭 변경.
- ui.confirm: 결정. 버튼 실행·거래·입찰 요청. 성공 확인과 구분한다.
- ui.close: 뒤로·닫기. 거래창·팝업·HowToPlay 닫기.
- ui.quantity: 수량 조절. 거래 수량·입찰금 변경.
- ui.notebook: 수첩 열기·닫기.
- ui.page: HowToPlay 페이지 전환·수첩 스크롤이 실제 변경될 때.
- market.trade: 매수·매도 완료. 본인 서버 성공 결과 수신 때만.
- market.bet: 베팅 참여. 본인 서버 참여 성공 결과 수신 때만.
- match.auction_start: 경매 시작. 공개·비공개 각 슬롯 시작. MatchNet.AuctionStageChanged.
- match.closing_soon: 마감 임박. 남은 10초 이하 진입 시 슬롯당 한 번.
- match.auction_end: 낙찰 망치. 슬롯 종료 한 번. 낙찰창은 중복 재생하지 않는다. MatchNet.AuctionStageChanged.
- match.start: 경기 시작. MatchNet.RoundTurned 중 1라운드가 열릴 때. 임시 클립.
- match.round_turn: 새 시세 공개. MatchNet.RoundTurned 중 라운드가 넘어갈 때.
- match.end: 경기 종료. MatchNet.RoundTurned 중 마지막 라운드가 닫힐 때(이때는 round_turn 대신). 임시 클립.

HallSpeaker(장내 스피커)는 SC_ExchangeFurnished의 가운데 전광판(TickerBoard_Center) 위에 놓인 물체다. 일어난 일은 화면과 같은 MatchNet static 이벤트(RoundTurned·AuctionStageChanged)를 듣고 CueAudioHost.PlayAt으로 자기 위치에서 재생을 요청한다. MatchNet은 접속 뒤에 본 변화만 내므로 재활성화·늦은 접속에도 지난 알림을 재생하지 않는다. 미리 알리는 마감 임박만 MatchTimeline을 보고 경매 구간 끝 10초 전(구간이 더 짧으면 시작 시점)에 슬롯당 한 번 낸다. 현재 큐는 spatialBlend 0(2D)이라 위치와 무관하게 들리며, MatchAudio.json에서 값을 올리면 스피커 쪽에서 들린다.
GameAudio는 같은 프레임 동일 UI 큐를 합친다. UIAudioFeedback은 버튼 포커스/마우스 진입과 클릭을 처리하고 거래·팝업의 전용 처리와 구분한다.

## BGM
- Cozy Swing Groove(Main).mp3 → Clips/BGM/BGM_MainMenu.mp3 → bgm.menu → SC_MainMenu
- Auction House Swing(InGame).mp3 → Clips/BGM/BGM_InGame.mp3 → bgm.game → SC_ExchangeFurnished (대기실 포함)

사용자 제공 파일과 Assets 내 음원의 SHA256 비교로 중복 없음을 확인했다. 원본 변경 없이 복사했다. Streaming, music 버스, 음량 .25, 루프, .8초 크로스페이드. Resources/GameAudioHost.prefab을 GameAudio가 생성하고 SceneAudioDriver가 전환한다. 기존 데모 BGM은 사용하지 않는다. 음량은 MatchAudio.json에서 조정한다.

## ④ 캐릭터 음원 호출 지점
PlayerActionAudio.PlayFootstep(), PlayJump(), PlayEmotion(). 점프는 PlayerMove.Jumped → PlayerAnimation → 기존 PlayerNet 애니메이션 RPC → PlayJump 경로에 연결했다. 소유자와 관찰자 모두 RPC 수신 시 한 번 재생하며 공중의 추가 입력에는 재생하지 않는다. 발소리·감정표현은 호출 지점만 준비했다. 3D 거리 1.5~18m. 연속 녹음 발소리는 보폭별 재생 전에 단발 편집/청음이 필요하다. 감정표현 전용 음원이 없어 해당 필드는 비워 두었다.

## 검증
Logs/FullAudioTests.xml: 2개 Passed. 실제 Mirror 호스트에서 시세 공개 5회, 경매 시작·종료·마감 임박 각 2회, 중복 방지, 행동 호출 지점, 메뉴→게임 BGM 전환 후 이전 곡 종료, UI 선택·페이지·수첩·수량·닫기·확인, 서버 확인 거래·베팅 성공음을 검사했다. AudioSource 및 CueAudio Playing 기록을 검증하며 스피커 청음·음량 평가는 별도다.
전용 서버 UNITY_SERVER에서는 오디오가 꺼진다. 청음/오디오 테스트는 Player subtarget으로 한다.
조립: Tools/Buy Bid Blow/Audio/Connect Match Sounds (MatchAudioSetup.Apply, 카탈로그 동기화 후 호스트·플레이어·스피커 연결). 스피커만 다시 놓기: Tools/Buy Bid Blow/Audio/Install Hall Speaker.

## 추가 효과음 (2026-10-21)
- ui.notebook: 수첩 열기·닫기 수정본으로 교체. 기존 파일 경로와 GUID 유지.
- auction.won: 본인 낙찰 결과창이 표시될 때 벨. 공개/비공개 공통이며 패찰/유찰에는 재생하지 않음.
- information.reveal: 낙찰 결과창의 정보 보기로 수첩을 열 때 종이 바스락. 일반 수첩 열기음을 대체하며 이후 닫기는 ui.notebook.
- ui.error: 서버가 보낸 MatchNoticeKind.Rejected 수신 시 한 번. 거래·입찰·베팅 거절에 공통 사용하며 일반 패찰에는 재생하지 않음.
추가 음원 원본 위치: C:/훈련생폴더/팀 프로젝트/Buy,Bid,Blow/사운드. 원본을 수정하지 않고 CollectedSFX로 복사. MatchAudio.json 저장 시 기존 SyncOnImport가 카탈로그를 갱신.
검증: Logs/NewAudioTests.xml Passed. 호스트에서 일반 수첩 열기/닫기, 본인 낙찰음 1회, 패찰 무음, 정보 열람 시 일반 수첩음 중복 방지, 거래·입찰·베팅 서버 거절음, 카탈로그 유효성과 Playing 기록을 확인. 음량에 대한 청음 평가는 별도.
