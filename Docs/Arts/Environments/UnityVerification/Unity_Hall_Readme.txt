Furnished auction hall — Unity 6000.2.8f1

Scene: Assets/Project/Content/Levels/ExchangeFurnished/Scenes/SC_ExchangeFurnished.unity
Level prefab: Assets/Project/Content/Levels/ExchangeFurnished/AuctionHall_Furnished.prefab
Open menu: BuyBidBlow > Hall > Open Furnished Hall
Play: WASD movement, mouse look, E open/close/sit/stand, Esc release cursor.

Source: BBB_Hall_A88_Waiting_Lounge_Red_Sofa.blend, saved source remains unchanged.
The original Exchange scene is preserved. This is a separate furnished level.

Reusable components:
- A78 banner nests the existing Content/Props/ChampionshipBanner/ChampionshipBanner.prefab.
  Its mesh and cloth pin coefficients fit the current 16 m hall placement.
- Six barrier groups use the existing RopeBarrierDemo constraint solver, moved with
  its original script GUID into HallInteraction so it also runs outside the editor.
  The original A/B test scene and spacing controls remain supported.
  HallPhysicsRope supplies anchors and tube mesh visualization for each span.
  Edit Rope Length and anchor positions to adjust each span.
- Six door leaves use HallDoor; edit Open Angle and Degrees Per Second.
- Fifteen seating groups provide 21 places using HallSeat.
  HallSeatActor controls occupancy, safe exit checks, physics suspension and restoration.
  onSeatedChanged can be connected to the character animator.
- HallWalkthrough registers its capsule with ropes and cloth for contact response.
  Production player prefabs can register their capsules through contacts/SetContacts.

Current interaction state is local. Multiplayer replication and seated character
animation are not implemented by this import. Existing player E interaction hooks
are connected, but multiplayer behavior needs separate integration.

Import:
1. Run Docs/Arts/Environments/Tools/export_hall_unity.py in Blender with the A88 file.
2. In Unity choose BuyBidBlow > Hall > Import Furnished Hall.
Mesh staging is written to Logs/HallImport. Saved Unity assets do not need staging
or Blender installed to load. Procedural Blender shading is approximated with URP
materials; node graphs are not transferred. Existing image textures are included.

Checks: Hall_Verification.txt and Hall_Play_Verification.txt.
Preview images: Auction.png, Reception.png.
