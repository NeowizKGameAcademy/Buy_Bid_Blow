# Shared menus, public bidding, final results (2026-10-22)

## Menu ownership and assets
- `UI/Menu/Options/Resources/BBB_GameMenu.prefab`: shared settings, game menu, controls, leave confirmation and in-game launcher. Standard editable Canvas / Text / Button / Slider objects.
- `GameMenuController`: local UI and local input/cursor ownership. MainMenuActions.Settings, LobbyHud.ToggleOptions and PlayerNet.OnStartLocalPlayer use this prefab. Lobby leave, result main-menu return and game-menu leave share confirmation and disconnection.
- Esc opens the game menu. W/S selects, A/D adjusts settings, E executes. Mouse buttons/sliders also work. Menu never pauses match time.
- `GameSettings`: PlayerPrefs key `BBB.Settings.v1`; master through AudioListener, music/sfx/ui through CueAudio buses; mouse sensitivity applied by PlayerMove; display modes use Screen.fullScreenMode. Default sensitivity 50 preserves prior scale.
- Existing SettingsPanelView/PauseMenuView are retained for Dev showcase compatibility; the live entry points now use BBB_GameMenu.

## Public self-raise
OpenAuction reserves only `newBid - oldBid` when the high bidder raises. Failed requests keep the previous bid and reservation. Outbidding releases the previous bidder's full reservation. Deadline, positive minimum and no cancellation remain enforced by server. Auction UI shows '입찰 올리기' and includes the existing reserved bid in the cash preview.

## Results and privacy
- MatchEngine records starting total and each closing round after auction/bet settlement, valued at that round's closing prices. A delayed Tick still records each round once. Reservation remains included in cash.
- No history / other players' breakdown is added to public MatchNet state. FinalStanding sends full breakdown and histories only from PublishResults after completion.
- Per player: cash, stock value, share count, company shares[3], final prices[3], total history[roundCount+1]. Initial 0 plus 13 closes = 14 samples.
- `UI/Result/Resources/BBB_DetailedResults.prefab`: final breakdown and graph tabs, player selectors, company valuations and return actions. `AssetHistoryGraph` is a native uGUI Graphic; line colors correspond to player names, selected line emphasized; click selects a round.
- Ranking remains total assets descending, then total shares; identical totals and shares keep shared rank. Host-only lobby return remains authoritative. Everyone may disconnect to main menu.

## Compatibility
ProtocolVersion is now 7 because the result payload schema changed. Rebuild both dedicated server and clients from this checkout before deployment. Existing deployed servers/builds are not restarted or replaced by this task.

## Verification
- Logs/MenuResultRulesTests.xml: auction boundaries, cash reservations and round-history accounting.
- Logs/MenuResults/: actual settings/result/graph captures. GraphSixPlayers is explicitly authored presentation sample data, not a six-client network session.
- Actual host PlayMode checks: menu keeps clock running; settings affect real audio buses and persist; results arrive only after finish with 14 samples; detailed UI, lobby return and main-menu disconnect; public high bidder can increase through PlayerNet command.

Verified: 21 EditMode rule tests and 2 real-host PlayMode integration tests passed. Audio buses and persistence tested; public  to  self-raise reserves exactly ; leaving mid-match and after result/lobby returns SC_MainMenu. Cross-process six-client network play remains a separate deployment check. Main scene cleanup removed accidental editor preview audio-host clones; GameAudio.EnsureHost now rejects edit-mode calls.

Final verification: Logs/MenuResultsPlayTests.xml (2/2 PASS), Logs/MenuResultRulesTests.xml (21/21 PASS). No server deployment or build replacement performed. Shared resources are assembled and saved by Tools/Buy Bid Blow/UI/Rebuild Shared Menu And Results.
