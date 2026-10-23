A94 furniture collision repair

Scene: Assets/Project/Scenes/SC_ExchangeFurnished.unity

Cause: the original mesh exporter selected collision by the mesh object's own
name. Children such as WOOD_FrontCore and RB_Green_Marble_Top did not contain
Desk or Table, even though their parent was a desk/table. Static batching then
merged those render meshes and discarded the furniture hierarchy.

Repair: 14 static MeshCollider groups, 9,552 collision triangles total:
3 curved reception counters (fronts, ledges, worktops, cabinets, plinths),
2 counter end assemblies, 2 staff gates in their existing poses,
1 auction rostrum, 1 lot pedestal, 2 upper-floor round tables,
1 direction sign and 2 large floor planters.
The 3 lounge planter instances now inherit collision fitted to the brass pot
mesh, instead of the original undersized capsule. Foliage remains non-solid.

Collision-only meshes use the source modeling cages without render bevels.
Tables and pots use per-part convex hulls; concave counter workspaces remain
open. Static collision never needs Rigidbody or a runtime update script.
No extra visible meshes, textures, materials, or draw calls are introduced.

The collision prefab is included in both the saved hall prefab and scene.
HallLevelBuilder attaches it on future imports. HallLoungeClusters fits planter
collision from the pot mesh on future rebuilds. To update for changed Blender
geometry, run Tools/export_hall_collision.py in the approved Blender hall,
then BuyBidBlow/Hall/Repair Furniture Collision in Unity.

Before.txt records the original reproduction (17/18 desk pushes passed through;
one staff-side sample was already obstructed by nearby geometry). After.txt
records coverage after repair. Verification.txt records 648 independent
surface raycasts and the 18 physics pushes after repair, plus all 31 seat
slots interacting/standing and the unobstructed matching rectangle.

The saved scene comparison changed only the hall parent's child list and the
new collision prefab instance. Existing scene objects, lighting, materials,
transforms, seat/door components and gameplay settings were preserved.
Backup: Logs/A94Collision/SC_ExchangeFurnished_before_collision.unity.
