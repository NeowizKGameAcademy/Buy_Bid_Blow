# Shared settings appearance

The approved dark teal, wood and brass concept is assembled in the existing
`BBB_GameMenu` resource prefab. Main menu, lobby and game entry points use this
same prefab; no scene-specific duplicate settings logic is required.

## Editable assets

- `Assets/Project/UI/Menu/Options/Sprites`: 20 transparent PNG sprites: panel,
  wooden title, gear medallion, divider, four secondary button states, three
  primary button states, slider handle, four gear-button states, and four
  menu/controls/home/results emblems.
- `Docs/Arts/UI/Options`: source art retained for future changes.
- `SettingsSkin.cs`: editor assembly, texture import settings, nine-slice borders,
  positions and dimensions. Reference canvas is 1920 × 1080.
- Text, values, sliders and button hit regions remain ordinary uGUI components.
  Art contains no baked labels.

The full-screen dim blocks clicks into gameplay. Slider roots have 66-pixel
hit regions. Buttons use SpriteSwap for normal, highlighted/selected, pressed
and disabled states. Primary disabled uses the muted secondary disabled sprite.
W/S selection highlights one setting row; mouse press selects that setting too.
Screen mode supports both left/right buttons and the existing central button.
Settings persistence and actual sound/sensitivity/display application still use
`GameSettings` through `GameMenuController`.

The same sliced frame, wooden header and button states now also style the game
menu, controls, leave confirmation and detailed results. Existing designed
trading, notebook, auction and notification panels are retained.

The in-game launcher is a 64 × 64 gear button, anchored to top right at
(-26,-35), with no text. The network HUD timer's top-right offset is (-110,-24)
so the gear has a 20-pixel horizontal gap beside the 350 × 104 timer. The HUD
prefab and its editor generator both retain this placement; the player inherits
the prefab change. The existing Esc shortcut still opens the menu.

Rebuild using **Tools → Buy Bid Blow → UI → Rebuild Shared Menu And Results**.
This replaces generated resource prefabs; edit the assembler for persistent
layout changes. Regeneration is not needed for ordinary runtime use.

## Verification

The final PlayMode run passed both `GameMenuResultsTests` tests. The real host
session checks menu input blocking, audio bus application and persistence,
network timer continuing while options are open, result summary/graph and
lobby/main return. Actual captures of settings, game menu, controls, leave
confirmation, gear placement and results are in `Logs/MenuResults`.
`Logs/MenuResultsPlayTests.xml` contains the latest report.

The original result table is hidden when the detailed view exists, so its text
cannot show through the new translucent frame. Its callbacks remain connected.
