# Rotating auction envelope
PF_AuctionEnvelope.prefab contains a thin cream cuboid, triangular flap, plain octagonal red seal and independent TextMeshPro 3D round label. Meshes and materials are saved editable Unity assets. No detailed textures or company ornaments.

AuctionEnvelopeDisplay rotates only Rotating Envelope in Play mode, 20 degrees/sec (18 seconds/revolution). Adjust degreesPerSecond in Inspector. SetRounds(first, second) changes the label; this does not connect auction/network data. Root remains stationary; child text follows rotation. Existing scene instance: Auction Envelope Display.

Verification.txt records creation and Play-mode check. Edit-mode preview remains stationary.

## Hemisphere glass cloche
Glass Cloche - Stationary is a sibling of Rotating Envelope, so the cover/tray stay fixed while the envelope rotates. Hemisphere radius1.32, base localY-0.57; open bottom with thin circular tray and glass knob. Mesh SM_ClocheHemisphere.asset is saved separately. ClocheGlass.shader uses a transparent tint, Fresnel rim and restrained highlight; M_ClocheGlass exposes Center Opacity and Rim Opacity. This is a stylized clear cover, without optical refraction. Envelope rotating bounds fit inside the hemisphere. Preview render verified in AuctionSecondFloorPreview.png.
