| Version | Update Notes |
|----------|---------------|
| **1.1.6** | - The mod's menu drawing now runs only while the Prefab Collections menu is open, removing per-frame UI overhead during normal play. |
| **1.1.5** | - Faster object ID lookups when pinging and when loading the Prefab Collections menu. |
| **1.1.4** | - Prefab Collections menu: searching and filtering are faster because pending changes are serialized only when they change, and row widths are measured once. |
| **1.1.3** | - Minimap ping labels are updated without per-frame reflection. |
| **1.1.2** | - Shortcuts now match modifiers exactly (Ctrl+Z no longer triggers Z) and no longer allocate memory every frame. |
| **1.1.1** | - Improved outline performance: very large targets are capped at 128 renderers and object hierarchies are checked for changes 4 times per second instead of every frame. |
| **1.1.0** | - Prefab collections moved from the .cfg into JoksterCube.PingMark.Collections.yml, and existing values are migrated automatically. On a PingMark server, collection changes are now checked by the server and accepted from admins only, even when Lock Configuration is off. Edits to the server's collections file are sent to clients live. |
| **1.0.14** | - Removing a section in the Prefab Collections menu can now be undone, and Undo no longer discards a removal after an auto sort. |
| **1.0.13** | - Fixed digits being removed from translated target names. Only raw prefab IDs are reformatted. |
| **1.0.12** | - Ping target names from other players are now shown in your own game language when the target is loaded nearby. |
| **1.0.11** | - Fixed the Unassigned section color not being saved for admins editing collections on a remote server. |
| **1.0.10** | - Fixed an old version-mismatch message appearing on later, unrelated connection failures. |
| **1.0.9** | - Removed an unused admin-sync request that every connection sent to the server. |
| **1.0.8** | - Without a modded server, recipients now also apply their own Z Ping Broadcast Distance, so a sender cannot force unlimited range. |
| **1.0.7** | - Hardened server routing: malformed or oversized ping packets are dropped safely and each player is rate limited. Ping names are capped at 64 characters. Ping Cooldown now also applies to creature and player highlights. |
| **1.0.6** | - Fixed creature nameplate labels reading their style from an expired world ping label. Labels now always use the minimap label style. |
| **1.0.5** | - Fixed an invalid Prefab Sections config value overwriting all collections with defaults. The last valid sections are kept and the config is no longer rewritten while reading. |
| **1.0.4** | - Added runestone, Greydwarf nest, and treasure screenshots to the README. Updated the admin menu screenshot with a higher-quality source image. |
| **1.0.3** | - Doubled screenshot preview dimensions for better readability: gameplay images are 440 px tall and the admin menu image is 1520 px wide. |
| **1.0.2** | - Resized screenshot files for consistent Thunderstore display without relying on HTML sizing attributes. Preserved full-resolution originals. |
| **1.0.1** | - Fixed oversized gameplay screenshots on Thunderstore by using proportional widths instead of height attributes. |
| **1.0.0** | - Initial release with configurable distance labels for world pings and a toggle shortcut. |