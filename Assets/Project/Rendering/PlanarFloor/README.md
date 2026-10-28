# Free planar floor reflection study

Self-contained URP 17 / Unity 6 prototype. No paid assets, imported packages, or changes to the shared renderer asset.

## Review

Use **BuyBidBlow > Lighting > Build Free Floor Reflection Study** to copy the saved furnished exchange into `Assets/Project/Dev/Sandbox/Rendering/PlanarFloor/SC_FreeFloorReflection.unity` and capture matched before/after views. This is a material/reflection experiment, not the newly proposed entrance-lobby layout.

The builder keeps the original lightmaps, exposure, lights and base floor materials. It extracts only horizontal ground-floor wood triangles into an upward-facing, backface-culled coating mesh, leaving carpets occluded by the depth buffer. It validates winding/normals and renders a solid grey check before applying the coating. Source scene bytes are checked after the build.

Outputs: `Docs/Arts/Environments/Lighting/FreeFloorReflection/`. `Validation.txt` records lifecycle checks. CPU submission time is not GPU time or a gameplay FPS benchmark.

The saved study has a dedicated review camera and edit-mode reflection updates. Open its Game view to inspect the hall; toggle `PlanarFloorReflection` on `Free_Floor_Reflection_Study` for comparison. Rebuilding replaces this generated study. No original scene camera is changed. DX11 was rendered and checked in Unity 6000.2.8f1; other graphics APIs and gameplay performance are not yet validated.

## Runtime controls

`PlanarFloorReflection` uses an explicitly assigned source camera, falling back to `Camera.main`. It supports a single monoscopic camera and one horizontal floor height. Default render scale is 0.5, maximum texture dimension 1024, update rate 30 Hz, and reflected distance 45 m. The main camera still draws normally when the component is disabled. Lower resolution/distance and disable reflected shadows if profiling shows excessive cost.

Render requests run in LateUpdate (after normal camera movement), not inside SRP rendering callbacks. The reflection camera has post-processing disabled; the final camera grades the image once. Resources and inverted culling state are restored on disable and after exceptions. XR cameras, below-floor cameras, and floors outside the source frustum are skipped.

The coating is a deliberately art-directed Fresnel blend over the existing Lit floor; it is not a replacement for URP's physically based BRDF. Mip-based blur approximates rough reflection. Single-camera rendering, fast motion at a reduced update rate, nonstandard/jittered camera projections, and transparent surfaces require further testing before production integration. Additional floors/mirrors require separate capture planes and their own rendering budgets.

## Next lighting step

Reflection does not repair the existing green wall-light spill, diffuse lighting balance, geometry normals, or lightmap density. Test those separately with existing URP lighting before considering an external solution. Keep this scene as an A/B reference until the result is approved for the production level.

Study tools are in `Assets/Project/Dev/Editor/Rendering`; lighting studies and their generated assets are in `Assets/Project/Dev/Sandbox/Rendering`. This folder contains the runtime reflection component and shader.
