# Auction first-person interface example

Scene: SC_AuctionSecondFloorInterface.unity. HUD prefab: PF_AuctionHUD_Example.prefab.

The user's level images are framing references only. Existing sandbox environment remains; simple rail and pedestal proxies indicate viewing composition. Camera position (0,5.45,-6.35), looks toward (0,5.3,-12.8), FOV67; eye height 1.45 above second-floor Y4. This is a static preview camera, not a new player controller.

HUD uses editable native Text/Image/RectTransform at 1920×1080 reference resolution:
- TL assets: 460×220, (32,-24), cash150 / Total268 / holdings2,2,0 / reserved35 / spendable115.
- TC public auction: 720×140, (0,-26), highest35 by self / auction countdown00:38.
- TR round: 270×190, (-32,-24), round6of17 / round countdown03:38.
- BC bid controls: 760×230, (0,32), next36 / keys / spendable114 / cannot cancel leading bid.

Four transparent PNG nameplate shapes have clipped shoulders and raised centers, muted brass outline and translucent teal fill. Shape artwork is generated with Unity geometry sampling; no text baked into sprites. Submit/keycaps are visual examples and have no gameplay/input binding. Original HUD preserved inactive.

Central world-space lot label is reserved but inactive; envelope/rotation/product-label production is deferred as requested. No future direction information is shown. Pig and wolves are existing model-only assets. Wolves face the auctioneer.

Verification: Unity compilation, saved scene and prefab, sprite references and editable label count, isolated 1920×1080 rendering; see AuctionSecondFloorPreview.png and AuctionSecondFloorVerification.txt. Original scene backup: Library/SC_AuctionSecondFloorInterface.before-ui.unity.
