# The Quiet Way — free-movement courtyard

Open **Tools → Alley Stealth → Open Playable Level**, press Play, and click the Game view.
The playable scene is `Assets/Scenes/ServiceAlley.unity`. The original sample scene and cat demo remain under `Assets/Scenes` too.

- **WASD / arrows:** move freely across the floor, relative to the fixed camera.
- **Gamepad left stick:** the same movement through the existing input asset.
- Walk around obstacles, watch the red cones, and choose your route to the far gate.
- There are no movement rails, depth permission triggers, or automatic returns to a lane.

## What changed in this pass

The original alley remains, with its sprite art, cat animation, collision system, and raycast-based lookouts. Free movement replaces the old rail controller. The camera now follows both floor axes without rotating.

The repeated hiding pockets and entry stripes are gone. The courtyard has stacked and offset wooden crates, an L-shaped brick wall, staggered stone pillars, a tall locker, and a low freight cart. Their silhouettes, colors, sizes, and positions differ. The lookouts occupy different positions and face different directions.

The front curb was replaced by a solid boundary at the outer floor edge. The perimeter keeps you inside the authored level; it does not force you down a particular path. Guards also have small collision capsules, so you cannot walk through them.

## 1. Free movement still happens on a floor

The world still uses X/Z for the floor and Y for height. But input is now relative to the camera: D means right in the view, and W means toward the upper part of the view. With the isometric camera, each of those directions generally changes both world X and world Z.

`FreeMovementController.Update` reads the Move action, flattens the camera's forward direction onto the floor, and combines forward/right input. It limits input length to one, so W+D does not move faster than W alone.

The resulting velocity is multiplied by `Time.deltaTime` and passed to `CharacterController.Move`. Unity resolves collisions; the controller adds gravity because CharacterController does not apply gravity automatically. Releasing the controls stops horizontal movement at the current location. No code pulls Z back to zero.

The important fields are `movementSpeed`, `movementCamera`, and `inputActions`. `gravity` and `groundStickSpeed` keep the cat grounded. `Awake` gets the capsule and clones Player/Move; `OnEnable` enables input; `OnDisable` stops input and clears movement; `OnDestroy` disposes the action.

The script publishes `ActualMovement` after collisions. It also publishes `ScreenHorizontalMovement`, which measures that displacement toward the camera's right. These describe what actually happened, not merely what key was pressed.

**Try this:** double movementSpeed. The cat travels faster in every horizontal direction, but animation frame rate remains independently adjustable.

## 2. Animation follows movement, not world-axis assumptions

`CatSpriteAnimator.LateUpdate` uses ActualMovement to choose the existing Cat-5 idle or walk frames. Pushing against a wall does not automatically count as walking. ScreenHorizontalMovement controls left/right sprite flipping; this avoids facing the wrong way just because isometric motion also changes world X.

Its useful Inspector fields are `idleFrames`, `walkFrames`, and `framesPerSecond`. The white cat comes from the existing Pet Cats pack. The player prefab is now named `PlayerCat.prefab`.

## 3. The camera follows freely but stays isometric

`IsometricFollowCamera.LateUpdate` reads player X and Z, restricts the focus to the authored camera limits, adds viewOffset, then eases toward that position. Y and rotation remain fixed. LateUpdate runs after player movement so it follows the latest position.

`followSmoothTime` changes how quickly it catches up. `horizontalLimits` and `depthLimits` keep framing near the level ends. These limits affect only the camera, never player movement. Camera Orthographic Size changes the visible area.

The sprite artwork is projected for the fixed camera angle, approximately 35.264 degrees pitch and -30 degrees yaw. Avoid casually changing that angle: the pictures would no longer line up with the collision geometry. Zoom and smoothing are safer experiments.

## 4. Obstacles are physical cover wherever you approach them

The new obstacles live under **02 - Courtyard obstacles**. Each block has a BoxCollider on its root and a **Flat art** sprite child. Moving that root moves the picture and collision together. Compound designs, such as the stack and L-wall, group multiple blocks under a named parent.

Each solid block uses the SightBlocker layer. CharacterController collisions stop the cat from walking through it. Enemy sight rays stop at it too. The art itself has no collider and does not decide visibility.

Changing collider Size alone does not resize the authored sprite. Likewise, scaling a sprite alone does not resize the collider. Keep their dimensions aligned. For a new design, the editor's IsometricSpriteArt helper can author an ordinary PNG for a specified box size; runtime gameplay does not depend on that helper.

`SpriteDepthOrder.LateUpdate` sorts upright pictures using their ground positions. A nearer obstacle draws over a cat behind it. The large ground/background and front boundary have fixed draw orders because they span the whole level. This presentation ordering does not affect enemy raycasts.

The upper crate shares its base's ground anchor and has `orderOffset = 1`, so the stack sorts as one position with the upper piece drawn last. This avoids the floor-depth rule accidentally painting the base over the upper crate.

## 5. Sight still depends on rays, not a hidden flag

`EnemyVision` checks distance and viewing angle, then casts toward the player's floor position at sightHeight. A SightBlocker hit before the player means that guard cannot see them. Hiding zones are no longer part of the project.

The red cone uses a fan of calls to the same VisibleDistance method. Each endpoint stops at a blocker or maximum range, and adjacent endpoints form triangles. That mesh is displayed just above the floor as a map of the guard's sight. Detection and drawing share range, angle, direction, layer mask, and trigger policy.

`LateUpdate` updates the sweep, visibility, and cone together after player movement. `viewDistance`, `viewAngle`, `scanHalfAngle`, `scanPeriod`, and `coneSegments` are the useful tuning fields. Zero scanHalfAngle makes a stationary lookout. Awake creates the mesh; OnDisable clears it and sight state; OnDestroy releases it.

An actual event now looks like this:

**WASD moves the capsule anywhere on the floor → an obstacle blocks the sight ray → that guard's CanSeePlayer becomes false → the HUD reports cover blocking sight.**

Another guard with a different viewpoint may still see you. There is no global zone-based immunity.

This pass uses one horizontal sight slice and the player's center position, not separate head/tail samples. The cone is sampled with 160 segments, so thin posts or tiny gaps can require more samples. The level is still flat; multi-height sight and combat remain outside this slice.

## 6. Reading the state display

`StealthStatusDisplay.OnGUI` reads each active lookout. SPOTTED takes priority if any lookout sees the player. OCCLUDED means none sees the player but at least one has an otherwise valid angle/range blocked by geometry. UNSEEN means the player is outside their current sight sectors.

The display never changes movement or detection. `player`, `lookouts`, and `finishX` are saved Inspector references/values. Change wording and colors here without changing stealth rules.

## Ownership and verification

The controller and camera scripts were renamed with their Unity metadata preserved, keeping scene and prefab references intact. The old HidingZone script, trigger objects, rail state, and pocket registration code were removed.

`FreeMovementAlleyUpgrade` is a one-time editor migration. It modifies the saved scene and refuses to reapply to an already upgraded layout. The older builders are authoring/history tools; use Open Playable Level for normal work and edit the saved scene directly.

The Play Mode tests load the actual scene and simulate keyboard input. They check unrestricted movement outside the former pockets, stopping without snapping back, camera-relative input, diagonal speed, cover/perimeter collision, sight blocking, cone/raycast agreement, enemy turning, sprite facing, and camera following. Previews and results are under `Logs/Stealth`.

## Small experiments

1. Move the entire Loading crates group half a unit deeper. Observe how the routes and red cone change together.
2. Move one pillar sideways to widen a passage. Keep enough clearance for the player's 0.6-unit-wide capsule.
3. Set the rear guard's scanHalfAngle to zero and plan a route around its fixed view.
4. Change followSmoothTime without changing movementSpeed; notice how camera lag differs from input responsiveness.
5. In Play Mode, disable a crate collider. Its picture remains, but both movement and sight pass through. Stop Play to discard that experiment.
