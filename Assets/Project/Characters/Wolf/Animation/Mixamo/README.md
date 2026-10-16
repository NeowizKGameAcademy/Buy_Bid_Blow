# Wolf Mixamo animations

Source: user-provided Idle.fbx and Joyful Jump.fbx. Both contain Mixamo transform animation matching the existing Wolf_Model skeleton. Standalone clips use Generic/Mecanim transform curves (legacy=false), so this wolf preview does not require a Humanoid avatar. The source import uses Legacy only to preserve explicit bone curves before exporting non-Legacy .anim files.

- Wolf_Mixamo_Idle.anim: 8.33 seconds, 30 fps, loops.
- Wolf_Mixamo_JoyfulJump.anim: 1.87 seconds, 30 fps, one shot.
- Wolf_MixamoPreview.controller: starts in Idle; JoyfulJump trigger transitions to jump, returns to Idle at end.
- PF_Wolf_MixamoPreview.prefab: existing Wolf_Model plus configured Animator, ready to drag into a scene and Play. Root motion disabled so this never moves a network player root. Jump's bone motion remains visible.

Trigger from code:

```csharp
animator.SetTrigger("JoyfulJump");
```

For the existing character controller, place these clips into states on the Animator attached to Wolf_Outfits (matching skeleton root). Existing WolfCharacter.controller and gameplay character prefabs were left unchanged. Do not put these raw clips on an unrelated skeleton without retargeting.

Verification: all 175 transform curves per clip resolve to Wolf_Model transforms. Sampled at 20% and 70%: Idle moves 16 bones, JoyfulJump moves 28. Actual gameplay/network integration is not part of this import.
