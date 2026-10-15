# Validation checks

Source-level checks that compile selected production files together with lightweight stubs of the
Unity, BepInEx and Valheim types they use, then run assertions without starting the game.

| File | Covers |
| --- | --- |
| `ZPingNetworkChecks.cs` | Server-side ping routing (`ZPingServerRoutingPatch`) and range filtering |
| `PingTargetResolverChecks.cs` | Target resolution and naming (`PingTargetResolver`) |
| `PrefabFeatureChecks.cs` | Prefab classification and auto sort (`PrefabFeatureClassifier`, `PrefabAutoSort`) |
| `*Stubs.cs`, `MobHighlightStub.cs` | Minimal stand-ins for game and engine types |

These files previously lived only in the ignored `bin/Validation` folder and were compiled by hand.
They are not part of `PingMark.csproj`. Before relying on them, they need a small test project
and updated stubs: the routing checks predate 1.0.7, which added `ZPackage.Size`, `Time` and
`Mathf` usage to `ZPingServerRoutingPatch`.
