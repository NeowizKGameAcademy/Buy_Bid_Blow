# Premium auction sprite production

Destination: Assets/Project/Dev/Sandbox/Interface/PremiumSprites. Built-in image_gen, one call for each asset (button states share one 2×2 sheet).
Reference: approved auction mockup codex-clipboard-89261e2e-58b8-4c63-87f6-a12fb056bd56.png.

Common prompt: individual production Unity UI sprite, restrained aged brass rim with subtle bevel/reflection, smoky teal interior, light satin texture, genuine transparent exterior, centered front-facing asset, no scene/text/content baked into backgrounds. Low-poly company icons readable at small size.

| Asset | Specific prompt / requirement |
| --- | --- |
| BG_Assets_Luxe | Asset panel, shallow arched upper edge, cut shoulders/lower corners. |
| BG_Header_Luxe | Very wide auction status nameplate, subtly raised center, clipped corners. |
| BG_Timer_Luxe | Compact timer escutcheon, crowned upper center, clipped corners. |
| BG_Bid_Luxe | Bid console, raised upper center, sloped shoulders, clipped lower corners. |
| BG_Lot_Luxe | Auction ticket with side notches; prepared but remains hidden for later. |
| BTN_BidStates_Luxe | 2×2 same button: Normal/Focused/Pressed/Disabled, bronze bevel, stronger focus light, recessed pressed state, desaturated disabled state. No labels. |
| BG_Keycap_Luxe | Blank dark keycap with fine brass/bronze bevel and clipped corners. |
| IC_Cash_Luxe | Chunky faceted gold money pouch with tied neck. |
| IC_Straw_Luxe | Chunky gold wheat sheaf. |
| IC_Timber_Luxe | Green log with pale cut end, diagonal view. |
| IC_Brick_Luxe | Three red clay bricks in a faceted stack. |

Labels, amounts, keyboard symbols and separators remain independently editable Unity UI objects. Scene layout and character/world composition unchanged. Sprite sources retained unmodified; importer trims only sprite rectangles and imports alpha. Submit stores four SpriteSwap states, but does not perform auction logic. The center lot background is stored on the inactive reserved object.

Final button refinement prompt: preserve exact four-state layout and clipped silhouettes; replace teal faces with brushed brass. Normal satin golden-brown, Focused bright gold, Pressed recessed bronze, Disabled desaturated brass. No text/icons. This replaces the first teal-button draft.
Panel opacity in scene: Image alpha 0.84; generated PNG centers remain almost opaque, so translucency is explicitly controlled in Unity. Asset/bid labels inset slightly to avoid curved frame edges. Final imported asset count: 11 PNGs / 14 sprites.
