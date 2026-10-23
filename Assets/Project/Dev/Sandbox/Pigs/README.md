# Pig Animation Museum

Open SC_Pig_AnimationMuseum.unity and press Play.
Menu shortcut: Tools > BuyBidBlow > Pigs > Open Animation Museum.

Nine independent exhibits:
1. Green / Talking (1)
2. Yellow / Thankful
3. Red / Quick Formal Bow
4. Black / Quick Formal Bow
5. Black / Dwarf Idle
6. Black / Counting
7. Yellow / Standing Idle
8. Green / Standing Idle
9. Red / Standing Idle

Each original FBX has its own generic Animator and looping controller. Root motion is
disabled. Models are normalized to the same exhibit height; source FBX files are unchanged.
Extracted basecolor/normal textures and URP materials live in Materials.

Controls:
- On-screen buttons: overview or close-up of each exhibit.
- Pause/Play all, Restart all, and playback speed 0.1x–2x.
- RMB captures mouse; WASD moves the camera, Q/E moves down/up, Shift moves faster.
- Esc releases the mouse; Tab hides/shows the panel.

This is an editor-only Dev sandbox, not a build scene.
The nine files are copied from the user-provided Downloads paths.
Rebuild Animation Museum regenerates the scene and its generated assets; close the museum
scene first. Original Downloads files are not changed.

Add Standing Idle Exhibits appends the three idle stations while retaining existing exhibits.
