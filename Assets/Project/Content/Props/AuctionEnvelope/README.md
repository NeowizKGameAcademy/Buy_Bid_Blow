# Rotating auction envelope
PF_AuctionEnvelope.prefab contains a thin cream cuboid, triangular flap, plain octagonal red seal and independent TextMeshPro 3D round label. Meshes and materials are saved editable Unity assets. No detailed textures or company ornaments.

AuctionEnvelopeDisplay rotates only Rotating Envelope in Play mode, 20 degrees/sec (18 seconds/revolution). Adjust degreesPerSecond in Inspector. SetRounds(first, second) changes the label; this does not connect auction/network data. Root remains stationary; child text follows rotation. Existing scene instance: Auction Envelope Display.

Verification.txt records creation and Play-mode check. Edit-mode preview remains stationary.

## Hemisphere glass cloche
Glass Cloche - Stationary is a sibling of Rotating Envelope, so the cover/tray stay fixed while the envelope rotates. Hemisphere radius1.32, base localY-0.57; open bottom with thin circular tray and glass knob. Mesh SM_ClocheHemisphere.asset is saved separately. ClocheGlass.shader uses a transparent tint, Fresnel rim and restrained highlight; M_ClocheGlass exposes Center Opacity and Rim Opacity. This is a stylized clear cover, without optical refraction. Envelope rotating bounds fit inside the hemisphere. Preview render verified in AuctionSecondFloorPreview.png.

## Game scene integration

`SC_ExchangeFurnished` contains `02_Lot_Pedestal_ROOT/Auction Lot Display/Auction Envelope`. The last object is an instance of this prefab. Its tray sits 0.015m above the pedestal surface; the whole envelope/cloche assembly is scaled to 80% of the smaller pedestal footprint dimension.

`AuctionLotDisplayClient` is a plain MonoBehaviour on the active `Auction Lot Display` parent. It only reads `MatchNet.Current`. While phase is Playing and auctionStage is Open or Secret, it passes auctionLot.firstRound/secondRound to AuctionEnvelopeDisplay.SetRounds and enables the envelope child. None, preparation, finished, invalid lot, or missing MatchNet hides that child. The active parent can still detect the next auction. OnEnable immediately refreshes the current state, so enabling it during an ongoing auction also works.

AuctionEnvelopeDisplay remains responsible for rotation and TextMeshPro text. There are no added RPCs, SyncVars, NetworkIdentities, server modifications, or future direction data on this display. Hiding the child also hides its stationary cloche and tray and stops its rotation Update.

Authoring tool: `Tools/Buy Bid Blow/Hall/Install Auction Lot Display` (`AuctionLotDisplaySetup.Apply`). It changes this placement in the game scene only; the source prefab and sandbox scenes are preserved. Re-running rebuilds that holder using the existing pedestal bounds. Position and scale can then be edited on the scene instance.

PlayMode integration test: `AuctionLotDisplayTests.PedestalEnvelopeFollowsExistingPublicAuctionState`. Covers hidden idle state, public/secret lot labels, existing rotation, enabling during an active auction, None and finished state, and no network components on the display. Results: `Logs/AuctionLotDisplayTests.xml`.
