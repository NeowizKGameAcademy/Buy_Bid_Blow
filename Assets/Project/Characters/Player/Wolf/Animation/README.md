# Wolf character animation

## Components

- `BBBWolfCharacter`: Animator body actions, movement speed, expression, paddle.
- `BBBWolfAppearance`: outfit selection and URP tie color; call directly from lobby/game/network hooks.
- `BBBWolfCharacterReview`: sandbox GUI in Dev, excluded from player builds.

Both `PF_Wolf_Character` and `SC_Wolf_Character` are wired. Animator is on the model
(`Wolf_Outfits`), so the existing generic skeleton bindings stay valid.
Gameplay should move the outer character root; root motion is disabled.

## Gameplay API

~~~csharp
character.SetMovementSpeed(normalizedSpeed); // 0 = idle, 1 = walk
character.PlayAction(BBBWolfCharacter.Action.Bid);
character.SetExpression(BBBWolfCharacter.Face.Suspicious);

character.appearance.SetOutfit(2, 1); // vest, shorts
character.appearance.SetTieColor(Color.blue);
~~~

SetMovementSpeed updates the locomotion blend tree; it does not move the character.
One-shot actions return to locomotion automatically. Walk, Sit, MixamoWalking and
HipHopDancing loop until another action is requested. StopAction returns to locomotion.
Playing the same action again restarts it. State entry consumes the Action request
by resetting its parameter to -1.

## Animator

`WolfCharacter.controller` has two independent override layers:

- Body: Idle/Walk blend tree (`MoveSpeed`, 0..1), plus ten action states (`Action`).
- Expression: Happy, Suspicious, Shocked, Blank (`Expression`, 0..3).

The original body clips are now non-Legacy generic clips; no Humanoid Avatar is needed
for these matching skeleton paths. Idle is a static pose derived from the first frame
of Mixamo Walking, not a dedicated breathing animation.

Expression clips animate the four existing face groups' active flags. They switch
discretely and work alongside any body action. Continuous facial deformation will
require BlendShapes or a facial rig. Clothing is not keyed by these clips.

Networking is not implemented here. A future Mirror component should synchronize
appearance/action/expression requests and invoke these local APIs.

## Editor tools and verification

- Tools > Buy Bid Blow > Wolf > Build Animator and Wire Character:
  explicitly regenerates the controller, idle and expression clips, and wires the
  prefab and sandbox. Existing manual controller edits are replaced; save the sandbox first.
- Tools > Buy Bid Blow > Wolf > Validate Animator Assets:
  verifies controller layers, references, non-Legacy clips and transform paths.
- Tools > Buy Bid Blow > Wolf > Run Playback Checks:
  enters Play mode, checks sandbox and a prefab instance, and returns to Edit mode.
  Open SC_Wolf_Character first. Result: `Library/WolfAnimatorPlayback.result`.

Playback verification passed 132 assertions: body state entry and advancement,
skeleton movement, face visibility and independent layers, locomotion blending,
paddle lifetime and automatic return, outfit persistence and tie property block.

