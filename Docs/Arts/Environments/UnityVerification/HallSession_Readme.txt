Shared lobby / game hall

The normal client route is now:
SC_MainMenu -> SC_ExchangeFurnished (lobby) -> same scene (match).
SC_Lobby is the retained historical backdrop scene, excluded from Build Settings
and from all current menu, bot, server-setup and lobby-tool routes.

The furnished hall contains the existing Lobby HUD, fixed camera, connection
controller and network-manager settings. Its approved lighting, posters,
furniture layout and A94 collision remain in place. Gameplay Presentation holds
the existing local Player prefab, inactive while customizing/connecting.

MatchStarting switches HallSession presentation and enables the local player
at that player's waiting position. It never calls LoadScene or LoadSceneAsync.
The synchronized round countdown is retained; it is not a loading screen.
Only one camera/audio listener is active in either mode. Leaving a match still
returns to the menu, and re-entry creates a fresh lobby session.

The six waiting positions are centered at (0, 0.04, 9.6), on the clear carpet
under the mezzanine. Seat order runs left to right in the lobby camera.
The camera is at (0, 3.3, -0.1), facing the carpet; avatars face the camera.
Gameplay starts at the same seat, looking out toward the main hall.
HallSessionSetup.AlignWaitingArea updates the saved scene and is also used by
the shared-hall migration. The lobby builders share the same camera layout.

Protocol version is 2 because MatchStarting no longer carries a scene name.
Use the newly built client and server together. The server scene is refreshed
from the network settings in the shared hall by GameBuild.UpdateServerScene.

Editor migration: HallSessionSetup.Apply (idempotent; keeps existing hall).
Runtime flow verification: HallSessionVerification.RunBatch.
Verification covered host admission plus five independent UDP peers, the real
start command/message, round 1, movement, leaving, and returning to the lobby.
All six slots were checked for furniture clearance and camera visibility.
No sceneLoaded event occurred between lobby and match. Existing hall, collider,
and baked-lightmap object identities remained unchanged; runtime errors: zero.

Original hall backup: Logs/HallSession/SC_ExchangeFurnished_before_session.unity
Test result: HallSession_Verification.txt
