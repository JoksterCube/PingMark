| Version | Update Notes |
|----------|---------------|
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