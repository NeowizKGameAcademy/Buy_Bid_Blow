# Main menu v2 — approved A logo + simplified B buttons

Built-in image_gen used for six new transparent PNG assets. Approved hall artwork retained. Previous v1 sprites retained for comparison.

| Asset | Requirements |
| --- | --- |
| Logo_Main_v2 | Two-line custom ivory/gold extruded serif BUY, BID, / BLOW; diagonal gavel; close-fit teal silhouette; small flourish; transparent exterior. Logo lettering is part of illustration. |
| BTN_MainStates_v2 | Four states in 2×2: Normal, Focused, Pressed, Disabled. Matching clipped-corner walnut frame, teal inset, one brass line. No text/icons baked in. |
| IC_Play_v2 | Separate gold triangular play icon. Also reused as smaller selection indicator. |
| IC_Book_v2 | Separate gold open book. |
| IC_Gear_v2 | Separate gold gear with transparent center. |
| IC_Exit_v2 | Separate gold door/exit arrow. |

## Prompt set
Reference for all assets: approved combined concept exec-c2b43610-bdf8-4355-a8f1-e0536154d42e.png. Production Unity UI sprites on genuinely transparent background.
- Logo: recreate only the exact central custom logo, BUY, BID, first line / BLOW second, gold ivory extrusion, teal silhouette, diagonal gavel, small gold flourish/diamond. No scene/buttons/plaque.
- Buttons: create only 2×2 sheet, identical centered wide button silhouettes, TL Normal / TR Focused / BL Pressed / BR Disabled. Simple clipped walnut frame, teal inset, one brass inlay; focused bright line, pressed recessed dark, disabled desaturated. No words/icons/bolts/medallions.
- Play: standalone cream gold right-pointing triangular icon matching approved reference, no frame/coin/text.
- Book: standalone cream gold open book matching reference, bold readable silhouette, slight bevel, no frame/coin/text.
- Gear: standalone cream gold eight-tooth gear matching reference, transparent center, no frame/coin/text.
- Exit: standalone cream gold door and exit arrow matching reference, no frame/coin/text.

## Scene / prefab
SC_MainMenu.unity and PF_MainMenu.prefab: center anchored, 1920×1080 CanvasScaler reference. Logo bounds 660×460 at (0,245). Menu buttons 470×104 at Y -60/-174/-280/-386. Independent Image icons, editable Text labels. SpriteSwap for keyboard focus/hover/press/disabled; separate selection indicator toggled by selection handler. Existing GameStart/lobby, HowToPlay, Settings volume and Quit actions retained. Build Settings startup scene retained.

## Verification
All six PNG corner alpha values are 0. Imported sprite rects trim transparent margins without altering source pixels. Unity render and Play verification saved alongside scene. Physical keyboard and standalone executable Quit still require manual testing.

## Readability refinement
- Full-screen black dim Image: alpha 0.16, stretch anchors. It sits above background, below logo/buttons.
- BG_MenuSoftShade: deterministic radial alpha gradient, smooth falloff from center to transparent edges, separate editable Image (1080×1380, alpha 0.38), behind menu.
- BTN_MainStates_v3: built-in image_gen edit of v2. Normal/Pressed use dark aged brass; Focused alone uses bright gold. Disabled remains desaturated. Previous artwork retained.
- Logo dimensions remain 660×460 at (0,245).
- Preview shows GameStart selected; labels and icons stay bright because dim layers are behind UI.

## 2026-10-16 image recovery
MainScene PNGs were Git LFS pointer text (130–132 bytes) although current .gitattributes uses ordinary PNG binary storage without an LFS filter. Restored all 13 MainScene PNGs from the local LFS object store, verifying SHA-256 against each pointer before copying. Reimported in the running editor without replacing the user's dirty scene. Scene image references resolve again; prefab has 17 sprite-backed Images. Confirmed restored Game view visually. Keep the restored binary files in the next commit under the existing non-LFS PNG policy.
