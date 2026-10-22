# Graph Report - CoffeeTycon  (2026-10-21)

## Corpus Check
- 109 files · ~900,454 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 2139 file(s) not represented in the graph (top: .meta 1190, .prefab 660, .fbx 126)

## Summary
- 1676 nodes · 4854 edges · 61 communities (47 shown, 14 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 549 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `ff3cf423`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- CafeUI
- TouchPad
- manifest.json
- dependencies
- CafeGame
- PreparedDrink
- CafePolish
- .Validate
- CafeTableBlackjack
- CafeHundred
- unityengine
- CafeExpansion
- CafeLifeBuilder
- GameObject
- packages-lock.json
- .Validate
- Vector3
- com.unity.render-pipelines.core
- com.unity.modules.jsonserialize
- com.unity.modules.unitywebrequest
- .Load
- CafeCustomer
- CafeEspressoFlow
- CafeDecorProgression
- CupWork
- Collider
- com.unity.modules.imgui
- CafeAspectChecks
- com.unity.modules.audio
- com.unity.burst
- CafePlayer
- CafeCityLife
- CafeMetricAttachment
- PlayerController
- Coffee Tycoon (Unity)
- CafeHome
- CafeParanormal
- com.unity.modules.physics2d
- .Run
- ReadmeEditor
- com.unity.test-framework
- com.unity.modules.physics
- com.unity.modules.imageconversion
- .OpenOperations
- com.unity.ext.nunit
- CafeGameManager
- com.unity.ai.navigation
- com.unity.modules.ui
- com.unity.ugui
- MonoBehaviour
- com.unity.collab-proxy
- State
- com.unity.modules.umbra
- com.unity.modules.wind
- CustomerAI
- CafeCleaning
- CafeBasketball
- .claude/CLAUDE.md
- .Verify

## God Nodes (most connected - your core abstractions)
1. `CafeGame` - 197 edges
2. `CafeUI` - 103 edges
3. `CafeLife` - 89 edges
4. `CafePolish` - 75 edges
5. `CafeWorld` - 66 edges
6. `CafeParanormal` - 58 edges
7. `CafeHundred` - 55 edges
8. `CafeExpansion` - 52 edges
9. `CafeStory` - 43 edges
10. `CafeFocusedMode` - 39 edges

## Surprising Connections (you probably didn't know these)
- `Yapı` --references--> `CafeWorld`  [INFERRED]
  CLAUDE.md → Assets/CafeLife/Scripts/CafeBoulevard.cs
- `Yapı` --references--> `CafeExpansion`  [INFERRED]
  CLAUDE.md → Assets/CafeLife/Scripts/CafeExpansion.cs
- `Yapı` --references--> `CafeGame`  [INFERRED]
  CLAUDE.md → Assets/CafeLife/Scripts/CafeGame.cs
- `Yapı` --references--> `CafeUI`  [INFERRED]
  CLAUDE.md → Assets/CafeLife/Scripts/CafeDecorUI.cs
- `Yapı` --references--> `CafeDecorProgression`  [INFERRED]
  CLAUDE.md → Assets/CafeLife/Scripts/CafeDecorProgression.cs

## Import Cycles
- None detected.

## Communities (61 total, 14 thin omitted)

### Community 0 - "CafeUI"
Cohesion: 0.07
Nodes (9): CafeStreetActivities, Aim, Game, CafeUI, CafeFontSize, StudioRoot, MenuNotice, CafeHUD (+1 more)

### Community 1 - "TouchPad"
Cohesion: 0.10
Nodes (6): CafeTouchChecks, CafeCounterPress, TouchPad, TouchSprint, MobileJoystick, Input

### Community 2 - "manifest.json"
Cohesion: 0.04
Nodes (46): com.unity.modules.ai, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion, com.unity.modules.imgui, com.unity.modules.jsonserialize (+38 more)

### Community 3 - "dependencies"
Cohesion: 0.04
Nodes (47): dependencies, com.unity.ai.navigation, com.unity.collab-proxy, com.unity.ide.rider, com.unity.ide.visualstudio, com.unity.inputsystem, com.unity.modules.accessibility, com.unity.modules.adaptiveperformance (+39 more)

### Community 4 - "CafeGame"
Cohesion: 0.08
Nodes (8): CafeSupplyChecks, CafeUpgradeCheck, CafeGame, Casino, CupInHand, CupOnMachine, Paused, SavePath

### Community 5 - "PreparedDrink"
Cohesion: 0.19
Nodes (5): CafeDeliveryChecks, CafeTrayManagerChecks, PreparedDrink, Temperature, CafeService

### Community 6 - "CafePolish"
Cohesion: 0.07
Nodes (8): CafeArtDirectionChecks, CafeDecorChecks, CafeNaturalDetailChecks, CafePropAudit, CafeStreetSurfaceChecks, CafePolish, CafePrintedLabel, fitted

### Community 7 - ".Validate"
Cohesion: 0.15
Nodes (3): CafeBoulevardChecks, CafeCasinoAccessChecks, CafeRearBarChecks

### Community 8 - "CafeTableBlackjack"
Cohesion: 0.06
Nodes (13): CafeClubShots, CafeGhostShots, CafeStoryShots, CafeActivityArea, CafeCasino, Active, Game, Result (+5 more)

### Community 9 - "CafeHundred"
Cohesion: 0.06
Nodes (16): CafeLatteSafetyChecks, CafeVenue, ActionLabel, Contract, Cost, Description, Goal, Progress (+8 more)

### Community 10 - "unityengine"
Cohesion: 0.05
Nodes (6): CafeHomeAssets, CafeMaterialImport, CafeMenuReadabilityChecks, CafeOneTimeCoins, CafeReleaseBuild, CafeLife

### Community 11 - "CafeExpansion"
Cohesion: 0.10
Nodes (7): CafeExpansion, Data, DemandMultiplier, Hour, Rainy, Rush, Seated

### Community 12 - "CafeLifeBuilder"
Cohesion: 0.09
Nodes (3): CafeLifeBuilder, CafeVisualUpgrade, CafePrototypeBuilder

### Community 14 - "packages-lock.json"
Cohesion: 0.06
Nodes (33): com.unity.modules.ai, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion, com.unity.modules.imgui, com.unity.modules.jsonserialize (+25 more)

### Community 16 - "Vector3"
Cohesion: 0.16
Nodes (3): CafeWorld, CafeTarget, PickupPoint

### Community 17 - "com.unity.render-pipelines.core"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, depth, source, version, dependencies (+22 more)

### Community 18 - "com.unity.modules.jsonserialize"
Cohesion: 0.08
Nodes (31): dependencies, depth, source, version, dependencies, depth, source, version (+23 more)

### Community 19 - "com.unity.modules.unitywebrequest"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 20 - ".Load"
Cohesion: 0.15
Nodes (8): CafeCasinoNativeProbe, CafeBranchData, Economy, Recipe, SaveData, CafeDayRecord, Profit, CafeExpansionData

### Community 21 - "CafeCustomer"
Cohesion: 0.16
Nodes (3): CafeCustomer, Passing, QueuePosition

### Community 23 - "CafeDecorProgression"
Cohesion: 0.14
Nodes (5): CafeDecorProgression, CurrentSeatColor, Selected, SurfaceCount, Finish

### Community 26 - "com.unity.modules.imgui"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, url, version, dependencies, depth, source (+18 more)

### Community 28 - "com.unity.modules.audio"
Cohesion: 0.09
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 29 - "com.unity.burst"
Cohesion: 0.09
Nodes (23): dependencies, depth, source, version, dependencies, depth, source, version (+15 more)

### Community 30 - "CafePlayer"
Cohesion: 0.10
Nodes (7): CafeModalChecks, CafePointerChecks, CafeReticleChecks, CafePlayer, Hint, PointerMode, WorldInputBlocked

### Community 31 - "CafeCityLife"
Cohesion: 0.24
Nodes (4): CafeCityLife, Game, CafeLitter, CafeWalker

### Community 33 - "PlayerController"
Cohesion: 0.14
Nodes (6): CoffeeMachine, Interactable, PlayerController, CurrentInteractable, MovementInput, UpgradeStation

### Community 34 - "Coffee Tycoon (Unity)"
Cohesion: 0.25
Nodes (7): Coffee Tycoon (Unity), Derleme ve test (Unity editörü açıkken), graphify, Kayıt, Kod stili, Kurallar, Yapı

### Community 36 - "CafeParanormal"
Cohesion: 0.05
Nodes (14): CafeCustomerVisualChecks, CafeDepthLabel, CafeParanormal, FloorY, InBuilding, Level, Upstairs, CafeStory (+6 more)

### Community 37 - "com.unity.modules.physics2d"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 38 - ".Run"
Cohesion: 0.09
Nodes (10): CafeFocusedCheck, CafeFocusedProbe, CafeOrderGuideChecks, CafeTypographyChecks, CafeFocusedMode, ActionKey, ActionLabel, CounterControls (+2 more)

### Community 39 - "ReadmeEditor"
Cohesion: 0.15
Nodes (3): ReadmeEditor, Readme, Section

### Community 40 - "com.unity.test-framework"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, url, version, dependencies, depth, dependencies (+8 more)

### Community 41 - "com.unity.modules.physics"
Cohesion: 0.08
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 42 - "com.unity.modules.imageconversion"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 44 - "com.unity.ext.nunit"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, version, dependencies, depth, source, url (+3 more)

### Community 45 - "CafeGameManager"
Cohesion: 0.27
Nodes (3): CafeGameManager, Instance, Money

### Community 46 - "com.unity.ai.navigation"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, dependencies, depth, source (+3 more)

### Community 47 - "com.unity.modules.ui"
Cohesion: 0.20
Nodes (10): dependencies, depth, source, version, dependencies, depth, source, version (+2 more)

### Community 48 - "com.unity.ugui"
Cohesion: 0.20
Nodes (10): depth, source, version, dependencies, depth, source, url, version (+2 more)

### Community 50 - "MonoBehaviour"
Cohesion: 0.18
Nodes (3): CafeSip, CameraFollow, FirstPersonCamera

### Community 51 - "com.unity.collab-proxy"
Cohesion: 0.11
Nodes (16): dependencies, depth, source, url, version, dependencies, depth, source (+8 more)

### Community 53 - "State"
Cohesion: 0.29
Nodes (7): State, Drinking, Entering, Leaving, ToPickup, ToSeat, Waiting

### Community 54 - "com.unity.modules.umbra"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.umbra

### Community 55 - "com.unity.modules.wind"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.wind

### Community 57 - "CafeCleaning"
Cohesion: 0.12
Nodes (6): CafeCleaning, CafeDirt, Tool, Cloth, Mop, None

### Community 60 - "CafeBasketball"
Cohesion: 0.15
Nodes (7): CafeBasketAimChecks, Passed, CafeBasketball, AssistedLastShot, BallRadius, baskets, Held

## Knowledge Gaps
- **157 isolated node(s):** `None`, `Mop`, `Cloth`, `Profit`, `Data` (+152 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 524 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **14 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `CafeGame` connect `CafeGame` to `CafeUI`, `TouchPad`, `PreparedDrink`, `CafePolish`, `.Validate`, `CafeTableBlackjack`, `CafeHundred`, `unityengine`, `CafeExpansion`, `CafeLifeBuilder`, `GameObject`, `.Validate`, `Vector3`, `.Load`, `CafeCustomer`, `CafeEspressoFlow`, `CafeDecorProgression`, `CupWork`, `Collider`, `CafeAspectChecks`, `CafePlayer`, `CafeMetricAttachment`, `Coffee Tycoon (Unity)`, `CafeHome`, `CafeParanormal`, `.Run`, `.OpenOperations`, `Vector3`, `MonoBehaviour`, `.Start`, `CafeCleaning`, `CafeBasketball`, `.Verify`?**
  _High betweenness centrality (0.177) - this node is a cross-community bridge._
- **What connects `None`, `Mop`, `Cloth` to the rest of the system?**
  _157 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `CafeUI` be split into smaller, more focused modules?**
  _Cohesion score 0.06862288464524142 - nodes in this community are weakly interconnected._
- **Why does `CafeUI` connect `CafeUI` to `Coffee Tycoon (Unity)`, `CafeGame`, `CafeParanormal`, `unityengine`, `GameObject`, `MonoBehaviour`, `.Load`, `CafeCustomer`, `CafeDecorProgression`?**
  _High betweenness centrality (0.049) - this node is a cross-community bridge._
- **Should `TouchPad` be split into smaller, more focused modules?**
  _Cohesion score 0.10483870967741936 - nodes in this community are weakly interconnected._
- **Why does `dependencies` connect `com.unity.modules.jsonserialize` to `com.unity.modules.physics2d`, `com.unity.test-framework`, `com.unity.modules.physics`, `com.unity.modules.imageconversion`, `com.unity.ext.nunit`, `com.unity.ai.navigation`, `packages-lock.json`, `com.unity.modules.ui`, `com.unity.render-pipelines.core`, `com.unity.ugui`, `com.unity.collab-proxy`, `com.unity.modules.unitywebrequest`, `com.unity.modules.umbra`, `com.unity.modules.wind`, `com.unity.modules.imgui`, `com.unity.modules.audio`, `com.unity.burst`?**
  _High betweenness centrality (0.045) - this node is a cross-community bridge._
- **Should `manifest.json` be split into smaller, more focused modules?**
  _Cohesion score 0.0425531914893617 - nodes in this community are weakly interconnected._