# PingMark

## Share discoveries and keep your party on target!

See **how far away** every ping is, something vanilla Valheim never shows. Aim, press **Z**, and your friends see exactly what you found: a named marker, a colored outline and a live distance. Stand still and press **Left Shift + Z** to sense loot and creatures hidden in the dark around you.

---

### 📸 Screenshots

**Everyone sees what you found, and how far away it is.**

<p align="center">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/Items.jpg" alt="Dropped weapons named and outlined, with who pinged them and their distance" width="549">
</p>

**Sense the loot hidden in the dark.** One press of Perception outlines every coin, gem and chest around you.

<p align="center">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/PerceptionDark.jpg" alt="Perception outlining coins, gems and chests in a dark forest" width="600">
</p>

**Read creatures and fish from afar.** Taming progress and fish quality, without walking up to them.

<p align="center">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/Taming.jpg" alt="Wolf being tamed, showing Wolf (2%) and its distance" width="360">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/Fish.jpg" alt="Pinged pufferfish showing its quality and distance" width="340">
</p>

---

### 🧭 Features

**📍 Pinging**
- **📏 Distance on every ping:** world and map pings show who pinged and how far away it is, updating as you move. Works on regular map pings too.
- **🎯 Aim and ping:** mark what you are looking at with its name and an outline, even while running.
- **💎 Find the loot:** dropped weapons, armor and materials glow so they're easy to spot in the grass, and so do cut logs and broken ore deposits.
- **⏱️ Longer pings:** set how long pings stay with **Ping Duration** (vanilla 5 s).

**🐺 Creatures & fish**
- **🎯 Track creatures:** highlight a mob with a nameplate and distance that follow it, red for hostile, green for friendly.
- **🐗 Taming from afar:** ping an animal from across the field to see how its taming is going, like **Wolf (15%)**, or how a tamed one feels, like **Boar (Hungry)**.
- **🐟 Fish quality:** pinged fish show their quality before you cast, like **Pufferfish • 5**, in their own color.
- **🎣 Watch your catch:** ping a fish before it bites and it stays outlined until you land it or it gets away. While a fish is hooked, the **Fishsight** slot next to your power appears: press **Left Alt + Z** to switch the outline on or off. **Auto Outline Hooked Fish** outlines every catch.

**🔮 Perception**
- **👁️ Sense what's near:** stand still and press **Left Shift + Z** to outline loot around you for 15 s, handy for coins and gems in dark dungeons. Nearby PingMark players see it too.
- **🐺 Sense creatures:** it also highlights every hostile and friendly creature in range, beyond the usual highlight limit.
- **⏳ Like a Guardian Power:** its own slot shows **Ready** or the time left, and it's ready again 60 s after each use. Prefer a cost that grows with what you sensed? Set **Perception Fatigue Mode** to `Relative`.

**🎨 Your rules**
- **👥 Mark teammates:** optionally highlight players instead of the spot they stood on (off by default).
- **🗂️ Choose what counts:** pick which objects are **Valuables**, **Locations** or **Useful**, and change colors, labels, keys and range.

---

### 🎮 Controls

| Shortcut | Action |
| --- | --- |
| **Z** | Ping the spot or object you aim at, or highlight a creature. |
| **Left Shift + Z** | Perception (standing still): sense loot and creatures near you. |
| **Left Alt + Z** | While a fish is hooked: switch its outline on or off. |
| **Right Ctrl + P** | Turn PingMark on or off. |
| **Right Ctrl + Right Shift + P** | Open the Prefab Collections menu (admins only on servers). |

🎛️ **Ping**, **Perception** and **Fishsight** can be rebound in the game's **Settings > Controls** (the modifier stays, so Perception keeps Left Shift). Every shortcut is also in the config under **9 - Inputs**.

---

### 🛠️ Prefab Collections Menu

Decide which objects get a name and outline when pinged.

- **Search and filter** every object and location in the game, including modded ones.
- **Move** objects between colored sections, or add, rename and remove sections.
- **Auto Sort** rebuilds the default **Valuables**, **Locations** and **Useful** sections, modded objects included. **Undo** reverts, **Apply** saves.
- **Find an object's ID:** turn on **Debug Ping Names** (an Advanced setting) and press **Z** on it.
- **For admins sharing lists:** **Copy YAML** / **Import YAML** move every section through the clipboard, a section's **Import** takes a CSV list, and **Export Report** writes how every object was sorted to `BepInEx/config/JoksterCube.PingMark.AutoSortReport.csv`.

> 🔒 On a server with PingMark, only admins can change collections, and the server checks every change. Collections are saved in `BepInEx/config/JoksterCube.PingMark.Collections.yml` and synced to all players.

---

### 📦 Installation

Install with a mod manager (r2modman, Thunderstore App), or manually:

1. Install **BepInExPack Valheim**.
2. Extract PingMark into `BepInEx/plugins`.
3. Launch the game. Settings are in `BepInEx/config/JoksterCube.PingMark.cfg`.

Installing on the server is **optional**. It syncs settings, filters ping range on the server and protects the collections.

---

### 🧩 Compatibility

- ✅ Works in single-player and on servers **without** PingMark.
- ✅ Players **without** the mod can join. They see ordinary pings (no names, outlines or highlights).
- 📏 Z pings reach players within **200 m of you** by default. Map pings stay global.
- ⚠️ On a PingMark server, every player with PingMark needs the **same version** as the server. Players without the mod can still join.

---

### 🔧 Configuration

Most players never need to change anything. These are the settings worth knowing about. **Synced** settings come from the server when it runs PingMark.

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Z Ping Broadcast Distance | 200 m | ✔ | How far your Z pings reach. `0` = unlimited. |
| Send Z Pings To Unmodded Players | On | ✔ | Players without the mod still get an ordinary ping. |
| Ping Cooldown | 0 s | ✔ | Minimum time between Z pings. |
| Enable Other Players | Off | ✔ | Let Z highlight players. |
| Show Pinger Name / Show Distance | On | | What ping labels display. |
| Outline Width | 2 px | | `0` hides all outlines. |
| Lock Configuration | On | ✔ | Only admins can change synced settings. |

> 💡 Some settings are hidden as **Advanced** in [Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Official_BepInEx_ConfigurationManager/). Turn on *Advanced settings* there to see them. They are marked with ⚙️ in the full reference below.

<details>
<summary><b>Full settings reference</b></summary>

#### 1 - General

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Enabled | On | | Turn all PingMark features on or off. |
| Lock Configuration | On | ✔ | Only admins can change synced settings. |
| Refresh Interval ⚙️ | 0.25 s | ✔ | How often distances update (0.05–5 s). |
| Ping Cooldown | 0 s | ✔ | Minimum time between Z pings and highlights (0–60 s). |
| Z Ping Broadcast Distance | 200 m | ✔ | Sender-to-recipient range (0–10,000 m, `0` = unlimited). Without a modded server, the shorter of both players' ranges applies. |
| Send Z Pings To Unmodded Players | On | ✔ | Send ordinary pings to players without PingMark, at any distance. |
| Debug Ping Names ⚙️ | Off | | Show the detected object/location IDs when pinging. Useful for editing collections. |
| Show Ability Names | On | | Show the names above the Guardian Power and the ability slots beside it. Turn off to hide them and bring the slots closer. |

#### 2 - Ping Label

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Show Pinger Name | On | | Show who pinged. |
| Show Distance | On | | Show your distance to pings and highlights. |
| Show Target Name | On | | Show the pinged object's name. |
| Use Kilometers | On | | Show 1,000 m and above as km. |
| Empty Label Text ⚙️ | PING | | Text shown when everything above is hidden. |
| Ping Color | White | | Label color. |
| Match Ping Text To Outline | Off | | Use the outline color for the label instead. |
| Ping Duration | 5 s | ✔ | How long a location or object ping stays: label, outline and map marker (1–120 s). |

#### 3 - Camera Ping

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Max Distance | 500 m | | How far you can aim (10–2,000 m). |
| Max Underwater Depth ⚙️ | 10 m | | Deeper targets ping the water surface instead (0–100 m). |

#### 4 - Ping Targets

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Enable Drops | On | ✔ | Name and outline dropped items. |
| Enable Fish | On | ✔ | Name and outline fish, with their quality. |
| Enable Gravestones | On | ✔ | Name and outline gravestones. |
| Enable Other Players | Off | ✔ | Highlight players instead of pinging their position (Perception highlights them too). |
| Enable All Other | Off | ✔ | Name and outline anything not in a collection (Perception outlines it too). |

#### 5 - Outlines

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Enable Ping Outline | On | | Outline pinged objects and players. |
| Outline Width | 2 px | | 0–10 px, visible through walls. `0` hides outlines. |
| Ping Drop Color | Purple | | Dropped items. |
| Ping Fish Color | Pink | | Fish, and the fish on your line. |
| Ping Gravestone/Player Color | Orange | | Gravestones and players. |
| Ping Anything Color | White | | Other objects. |
| Hostile / Ally Outline Color | Red / Green | | Highlighted creatures. |

#### 6 - Mob Highlight

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Enable Mob Highlight | On | ✔ | Allow creature highlights. |
| Enable Mob Outline | On | | Outline highlighted creatures. |
| Mob Highlight Duration | 10 s | ✔ | How long a highlight lasts (1–120 s). |
| Max Highlighted Mobs | 1 | ✔ | Highlights per player at once (1–20). A new one replaces your oldest. |

#### 7 - Prefab Collections

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Color 1 – Color 12 ⚙️ | Gray, White, Red, Orange, Gold, Lime, Green, Teal, Cyan, Blue, Violet, Pink | | Palette that collection sections pick from. |
| Unassigned Section Color ID ⚙️ | 1 | | Palette slot for the menu's Unassigned column. |

#### 8 - Player Highlight

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Player Highlight Duration | 5 s | ✔ | How long a player highlight lasts (1–120 s). |
| Hide Own Player Outline | On | ✔ | Don't outline yourself when someone marks you. |

#### 9 - Inputs

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Ping Shortcut | Z | | Ping what you aim at, or highlight the creature you aim at. Also in Settings > Controls. |
| Toggle Mod Shortcut | Right Ctrl + P | | Turn PingMark on or off. |
| Prefab Editor Shortcut | Right Ctrl + Right Shift + P | | Open the Prefab Collections menu. |
| Perception Shortcut | Left Shift + Z | | Use Perception. Only while standing still (no other key held). Its main key can also be set in Settings > Controls. |
| Fishsight Shortcut | Left Alt + Z | | While a fish is hooked, switch its outline. Empty hides the Fishsight slot. Also in Settings > Controls. |
| Enable Prefab Editor Shortcut ⚙️ | On | | Turn the menu shortcut on or off. |

#### 10 - Perception

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Enable Perception | On | ✔ | Allow Perception and show its slot next to the Guardian Power. It senses what Ping Targets allows. |
| Perception Range | 10 m | ✔ | How far around you Perception looks. |
| Perception Line Of Sight | On | ✔ | Only sense what you can see: things behind walls, rock or buildings, or buried underground, are left out. |
| Perception Duration | 15 s | ✔ | How long Perception keeps things outlined (the Perception status). |
| End Perception Early | On | ✔ | End Perception and start fatigue once every drop, pickable, chest, deposit and enemy it found is gone. |
| Perception Max Outlines | 64 | ✔ | Most objects outlined per use, nearest first. |
| Perception Fatigue Duration | 60 s | ✔ | Fixed mode: cooldown from each use, like a Guardian Power. Relative mode: the base fatigue once Perception ends. `0` = none. |
| Perception Fatigue Mode | Fixed | ✔ | `Fixed`: ready again the duration above after each use. `Relative`: fatigue starts once Perception ends, adds a cost for everything sensed, and starts early once it's all looted or slain. |
| Fatigue Per Object | 1.5 s | ✔ | Relative mode: added for each listed object outlined. |
| Fatigue Per Drop | 0.25 s | ✔ | Relative mode: added for each dropped item on the ground outlined. |
| Fatigue Per Ally | 0.5 s | ✔ | Relative mode: added for each tamed or non-hostile creature. |
| Fatigue Per Enemy | 2.5 s | ✔ | Relative mode: added for each hostile creature. |
| Perception Max Fatigue | 90 s | ✔ | Relative mode: the longest the fatigue can get. |
| Perceive Creatures | On | ✔ | Perception also highlights every hostile and allied creature in range, ignoring Max Highlighted Mobs. Players follow Enable Other Players. |
| Perception Effect | On | | Play a Guardian Power style burst around a player who uses Perception (nearby PingMark players see it too). |
| Perception Effect Color | Light blue | | Color of that burst, for your Perception and other players'. |
| Perception Sound | sfx_WishbonePing_near | | Sound played with the burst (any sound prefab, e.g. `sfx_secretfound`). Empty = silent. |

#### 11 - Fishing

| Setting | Default | Synced | What it does |
| --- | --- | :---: | --- |
| Auto Outline Hooked Fish | Off | | Outline every fish you hook until it's caught or gets away. Off: only a fish you pinged before it bit; the rest wait for Left Alt + Z. |

</details>

---

### 🖼️ Gallery

**Outlines in their own colors:** treasure and a hostile skeleton, a runestone, a Greydwarf nest.

<p align="center">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/Treasure.jpg" alt="Treasure chest and hostile skeleton highlighted with distinct colors" width="300">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/RuneStone.jpg" alt="Runestone marked with a colored outline and distance label" width="200">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/Spawner.jpg" alt="Greydwarf nest marked with a colored outline and distance label" width="170">
</p>

**Creature nameplates and distances that follow them.**

<p align="center">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/Deer.jpg" alt="Wildlife highlight with a moving nameplate" width="215">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/Boar.jpg" alt="Friendly creature highlights with names and distances" width="195">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/Distance.jpg" alt="World ping with a live distance label" width="150">
</p>

**Fishing:** the fish you pinged stays outlined on your line, and the Perception and Fishsight slots sit beside your power.

<p align="center">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/Fishing.jpg" alt="Hooked fish outlined in pink while fishing from a boat" width="340">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/Perception.jpg" alt="Guardian Power, Perception and Fishsight slots on the HUD" width="393">
</p>

**Prefab Collections menu:** searchable, color-coded sections of everything that can be pinged.

<p align="center">
	<img src="https://raw.githubusercontent.com/JoksterCube/PingMark/refs/heads/main/Screenshots/Menu.jpg" alt="Prefab Collections admin menu" width="600">
</p>

---

### 💬 Feedback & Support

Report bugs or suggest improvements on [GitHub](https://github.com/JoksterCube/PingMark/issues).

---

**Author:** JoksterCube<br>
**Version:** 1.6.2<br>
**License:** GNU GPL v3 or later
