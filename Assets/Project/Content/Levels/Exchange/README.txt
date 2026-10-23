Auction Hall - A10 material adaptation

Open Assets/Project/Dev/Sandbox/AuctionHall/AuctionHallPreview.unity and enter Play mode.
Right mouse button captures the cursor; WASD moves; Shift runs; Escape releases it.
AuctionHall.prefab contains the hall geometry, URP materials and static collision.
Preview lighting and walkthrough controller belong only to the preview scene.
Doors are static. Restroom interiors and furniture are not included.

Five texture-based URP/Lit materials use 1024px maps with subtle normal detail.
Blender procedural node graphs are not required by the Unity materials.
Visual stair steps use separate smooth ramp collision.
Performance must still be profiled on the target game hardware.

Material sources: BlenderKit; metadata returned royalty_free and canDownload=true
at retrieval on 2026-10-12. Adapted colors and reduced texture resolution.
Wood Parquet: 891ac40a-07b5-4159-9b65-b9e0b3c5ac97
Wood Smooth Walnut: 43dde512-6578-4084-b9a2-d7751cfbf855
Painted Plaster Wall: d7de4c6d-ecdd-408e-8998-bcb8dbe4ce71
Brass: 2df7112c-f41b-4001-8e75-9ab44c341684

Rebuild generated assets via Tools > Buy Bid Blow > Build Hall Preview.
Geometry-only source: Docs/Arts/Environments/Models/BBB_Hall_A10.blend
Textured source: Docs/Arts/Environments/Models/BBB_Hall_Textured.blend

Source textures: Docs/Arts/Environments/MaterialSources
Production scripts: Docs/Arts/Environments/Tools
Preview validation: Assets/Project/Dev/Sandbox/AuctionHall/Validation.txt
