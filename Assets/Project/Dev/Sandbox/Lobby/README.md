# Lobby sandbox

`SC_Lobby.unity` is an independent copy of AuctionHall-Deco. The original scene is unchanged.

## Try it

1. Open this scene and enter Play mode.
2. Select **방 만들기 (Host)**. Your wolf appears in a fixed slot.
3. Change the shirt, trousers and tie; select **준비 완료**.
4. Additional standalone players can select **접속 (Join)** using the host's address (KCP UDP 7777). Use `localhost` on the same machine.

The server assigns six fixed slots and validates customization commands. Only the owning player can customize or ready their character. Changing appearance clears readiness. Vacant slots display join prompts; they are not simulated players. Everyone-ready requires at least three participants. Leaving as host closes the sandbox room.

This is a lobby prototype: room discovery/codes, persistent profiles, host migration and transition into a gameplay match are not implemented. UI uses the generated sprites in a saved uGUI Canvas. See `UI/HudLayout.md` for exact placement and keyboard controls. It does not replace the project's eventual room service.

Editor-only display wolves preview four participants in the saved scene; they are removed on Play. Runtime participant counts always come from actual network players.

Verified in Unity: compilation, host spawn, ownership, appearance commands, readiness and host disconnect. Separate remote clients have not been tested.
