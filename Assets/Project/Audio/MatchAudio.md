# 게임 오디오 연결

기존 Assets/CueAudio를 재사용한다. 실제 클립: Clips/CollectedSFX, 큐: Cues/MatchAudio.json, 바인딩: Cues/MatchAudioCatalog.asset.

## 효과음
- ui.navigate: 선택 이동. 키보드 포커스·마우스 버튼 진입·거래 탭 변경.
- ui.confirm: 결정. 버튼 실행·거래·입찰 요청. 성공 확인과 구분한다.
- ui.close: 뒤로·닫기. 거래창·팝업·HowToPlay 닫기.
- ui.quantity: 수량 조절. 거래 수량·입찰금 변경.
- ui.notebook: 수첩 열기·닫기.
- ui.page: HowToPlay 페이지 전환·수첩 스크롤이 실제 변경될 때.
- market.trade: 매수·매도 완료. 본인 서버 성공 결과 수신 때만.
- market.bet: 베팅 참여. 본인 서버 참여 성공 결과 수신 때만.
- match.auction_start: 경매 시작. 공개·비공개 각 슬롯 시작.
- match.closing_soon: 마감 임박. 남은 10초 이하 진입 시 슬롯당 한 번.
- match.auction_end: 낙찰 망치. 슬롯 종료 한 번. 낙찰창은 중복 재생하지 않는다.
- match.closing_bell: 새 시세 공개. 라운드 구간 종료 시. 임시 라운드 시작 시세음은 제거했다.

HallSpeaker(장내 스피커)는 SC_ExchangeFurnished의 가운데 전광판(TickerBoard_Center) 위에 놓인 물체다. MatchTimeline의 구간 시작·종료 static 이벤트를 듣고 CueAudioHost.PlayAt으로 자기 위치에서 재생을 요청한다. 타임라인은 지난 시각을 다시 내지 않으므로 재활성화·늦은 접속에도 지난 알림을 재생하지 않는다. 마감 임박은 경매 구간 끝 10초 전(구간이 더 짧으면 시작 시점)에 슬롯당 한 번. 현재 큐는 spatialBlend 0(2D)이라 위치와 무관하게 들리며, MatchAudio.json에서 값을 올리면 스피커 쪽에서 들린다.
GameAudio는 같은 프레임 동일 UI 큐를 합친다. UIAudioFeedback은 버튼 포커스/마우스 진입과 클릭을 처리하고 거래·팝업의 전용 처리와 구분한다.

## BGM
- Cozy Swing Groove(Main).mp3 → Clips/BGM/BGM_MainMenu.mp3 → bgm.menu → SC_MainMenu
- Auction House Swing(InGame).mp3 → Clips/BGM/BGM_InGame.mp3 → bgm.game → SC_ExchangeFurnished (대기실 포함)

사용자 제공 파일과 Assets 내 음원의 SHA256 비교로 중복 없음을 확인했다. 원본 변경 없이 복사했다. Streaming, music 버스, 음량 .25, 루프, .8초 크로스페이드. Resources/GameAudioHost.prefab을 GameAudio가 생성하고 SceneAudioDriver가 전환한다. 기존 데모 BGM은 사용하지 않는다. MatchAudioCatalog에서 음량을 조정할 수 있다.

## ④ 캐릭터 음원 호출 지점
PlayerActionAudio.PlayFootstep(), PlayJump(), PlayEmotion(). 점프는 PlayerMove.Jumped → PlayerController → 기존 PlayerNet 애니메이션 RPC → PlayJump 경로에 연결했다. 소유자와 관찰자 모두 RPC 수신 시 한 번 재생하며 공중의 추가 입력에는 재생하지 않는다. 발소리·감정표현은 호출 지점만 준비했다. 3D 거리 1.5~18m. 연속 녹음 발소리는 보폭별 재생 전에 단발 편집/청음이 필요하다. 감정표현 전용 음원이 없어 해당 필드는 비워 두었다.

## 검증
Logs/FullAudioTests.xml: 2개 Passed. 실제 Mirror 호스트에서 시세 공개 5회, 경매 시작·종료·마감 임박 각 2회, 중복 방지, 행동 호출 지점, 메뉴→게임 BGM 전환 후 이전 곡 종료, UI 선택·페이지·수첩·수량·닫기·확인, 서버 확인 거래·베팅 성공음을 검사했다. AudioSource 및 CueAudio Playing 기록을 검증하며 스피커 청음·음량 평가는 별도다.
전용 서버 UNITY_SERVER에서는 오디오가 꺼진다. 청음/오디오 테스트는 Player subtarget으로 한다.
조립: Tools/Buy Bid Blow/Audio/Connect Match Sounds (MatchAudioSetup.Apply). 스피커만 다시 놓기: Tools/Buy Bid Blow/Audio/Install Hall Speaker.
