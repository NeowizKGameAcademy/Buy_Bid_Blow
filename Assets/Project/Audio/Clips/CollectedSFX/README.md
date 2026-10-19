# 수집 효과음 WAV 정리

기존 WAV 10개는 그대로 복사하고 MP3 1개·AIFF 2개만 WAV로 변환했습니다. 원본 Downloads 파일은 변경하지 않았습니다. 샘플레이트·채널·길이·음량을 유지했고 자르기·정규화·모노화는 하지 않았습니다.

AIFF는 원본과 같은 16-bit PCM으로 변환. MP3는 디코딩된 피크가 정수 PCM 범위를 약간 넘어, 클리핑 없이 보존하도록 32-bit float WAV로 변환했습니다. 기존 WAV의 비트 깊이·PCM/float 형식은 유지했습니다. MP3의 손실된 음질이 복구되지는 않습니다.

|원본|프로젝트 파일|처리|
|---|---|---|
|나무 발소리.wav|Movement/SFX_Footsteps_Wood.wav|copied unchanged|
|결정.mp3|UI/SFX_UI_Confirm.wav|converted to pcm_f32le|
|뒤로·닫기.wav|UI/SFX_UI_BackClose.wav|copied unchanged|
|선택 이동.aiff|UI/SFX_UI_Navigate.wav|converted to pcm_s16le|
|페이지 넘김.wav|UI/SFX_UI_PageTurn.wav|copied unchanged|
|수첩 열기·닫기.wav|UI/SFX_UI_Notebook.wav|copied unchanged|
|수량조절.aiff|UI/SFX_UI_Quantity.wav|converted to pcm_s16le|
|매수,매도 완료.wav|Market/SFX_Trade_Complete.wav|copied unchanged|
|베팅 참여.wav|Market/SFX_Bet_Place.wav|copied unchanged|
|마감 임박.wav|Auction/SFX_Auction_ClosingSoon.wav|copied unchanged|
|낙찰.wav|Auction/SFX_Auction_Gavel.wav|copied unchanged|
|경매 시작.wav|Auction/SFX_Auction_Start.wav|copied unchanged|
|새 시세 공개.wav|Market/SFX_Market_PriceReveal.wav|copied unchanged|

검증: 13개 전체 재디코딩 성공, 원본과 샘플 수 일치, 디코딩 샘플 차이 0, 기존 WAV 복사본의 원본 해시 일치. 상세 내용은 ConversionManifest.json 참조.

씬·재생 로직은 연결하지 않았습니다. 발소리·경매 시작 같은 녹음은 실제 연결 전 청음 후 구간 분리가 필요할 수 있습니다. 출처 URL·제작자·라이선스는 미제공 상태이며 원본 Freesound 페이지에서 별도 기록해야 합니다.
