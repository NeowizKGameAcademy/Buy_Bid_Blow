GrandEntrance

Object-owned prefab and assets, following Assets/Project/README.md.
GrandEntrance.prefab: frame and two independent hinge-origin door leaves; simple collision; three URP/Lit materials. No NetworkIdentity, gameplay interaction or scene transition is attached.
Models/GrandEntrance.fbx: 3 meshes, 43,500 triangles, applied bevel geometry and UVs. Original editable source is Docs/Arts/Props/Models/BBB_Grand_Entrance_Textured.blend.
Materials/: wood, teal painted wood, brass.
Textures/: 1024px color and normal maps; normal intensity is already reduced in the maps.

Open the preview at Assets/Project/Dev/Sandbox/GrandEntrance/GrandEntrancePreview.unity. It is not in the build scene list. Model/LeftDoor and Model/RightDoor rotate independently about their local Y hinge axes. Production scene placement is left to the scene author.
Rebuild via Tools > Buy Bid Blow > Build Grand Entrance Prefab. Builder resides in Dev/Editor/GrandEntrance. Rebuilding regenerates the prefab and preview.

BlenderKit source material IDs (Royalty Free):
Wood Smooth Walnut: 43dde512-6578-4084-b9a2-d7751cfbf855
White Painted Wood: 6f2f7909-f892-4afc-b77c-83f4942fc134
Brass: 2df7112c-f41b-4001-8e75-9ab44c341684
Game distribution must not expose the source assets for direct extraction. These development source files are not a redistributable standalone material pack.
https://www.blendkit.com/docs/licenses/licensing-faq/
