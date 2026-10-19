# Lobby sandbox

`SC_Lobby.unity` is an independent copy of AuctionHall-Deco. The original scene is unchanged.

## Try it

1. Open this scene and enter Play mode.
2. Enter your nickname and port (default 7777), then select **방 만들기 (Host)**. Your wolf appears in a fixed slot.
3. Change the shirt, trousers and tie; select **준비 완료**.
4. Additional standalone players can select **접속 (Join)** using the host's address (KCP UDP 7777). Use `localhost` on the same machine.

The server assigns six fixed slots and validates customization commands. Only the owning player can customize or ready their character. Changing appearance clears readiness. Vacant slots display join prompts; they are not simulated players. Everyone-ready requires at least three participants. Leaving as host closes the sandbox room.

This is a lobby prototype: room discovery/codes, persistent profiles, host migration and transition into a gameplay match are not implemented. UI uses the generated sprites in a saved uGUI Canvas. See `UI/HudLayout.md` for exact placement and keyboard controls. It does not replace the project's eventual room service.

Editor-only display wolves preview four participants in the saved scene; they are removed on Play. Runtime participant counts always come from actual network players.

Verified in Unity: compilation, host spawn, ownership, appearance commands, readiness and host disconnect. Separate remote clients have not been tested.

## Nickname entry

The connection panel now contains **닉네임** and **접속 주소** fields. Enter a nickname before creating or joining a room. Leading/trailing whitespace is trimmed; 1–12 text elements are accepted, excluding line breaks/control characters and angle brackets. The last accepted local nickname is remembered using PlayerPrefs. Duplicate display names are currently allowed; ownership and seat identity remain Mirror connection-based.

Keyboard: N focuses nickname, J focuses address, Tab / Shift+Tab cycles nickname → address → Host → Join, Enter selects a focused button or advances nickname → address → Join. Esc exits text editing so H can create a host directly. Letter shortcuts do not fire while typing a nickname/address. Korean IME composition is allowed to finish before Enter is treated as submission.

LobbyRoom sends the local nickname in the admission request. The server validates and reserves a seat before spawning a player with the nickname SyncVar. LobbyHud reads that synchronized value for each player's nameplate. Names are rendered as plain text with automatic font sizing. A fallback WOLF name is used until a valid name arrives.

LobbyTextInputBridge supplies IME composition/cursor data to uGUI InputField through the current InputSystemUIInputModule's BaseInput override, without changing the project's active Input System setting. UI field layouts are saved directly in SC_Lobby.unity.

UI/LobbyNicknamePreview.png is the actual connection-panel render. UI/LobbyNicknameVerification.txt records automated validation and Play mode host checks. A second remote client and physical Korean IME typing still need device testing.

LAN direct connection now uses a separate ConnectionController, server admission/roster policy, and explicit address/port settings. See [DirectConnection.md](DirectConnection.md) for architecture, usage, and the actual UDP verification scope.
