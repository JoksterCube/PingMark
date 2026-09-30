# Ping Distance

## Add your distance to world pings

**Ping Distance** appends the distance from your character to the location of each world ping.

---

### 🧭 Features

- Adds the distance in meters to the in-world ping label.
- Lets you configure the in-world ping label color (white by default).
- Runs locally; the server and other players do not need the mod.
- Toggle all mod features with **Right Control + P** by default, or use the **Enabled** setting.
- Press **Z** while aiming at a mob to light up its native nameplate in the same color as its health bar, with your distance above the mob's name. Aiming elsewhere sends a normal location ping with your name and distance; sky pings land at the maximum camera distance.
- Mob highlights last 10 seconds by default; the duration is configurable and synced from the server when it also runs the mod. Other clients running the mod see the same highlight, even on an unmodded server.
- One mob can be highlighted at a time by default. The server-synced maximum is configurable; highlighting another mob replaces the oldest highlight.
- Ordinary world and map pings continue to show the player's name and distance.
- Configure the enabled state, color, and shortcuts in `BepInEx/config/JoksterCube.PingDistance.cfg`.

---

## ⚙️ Manual Installation Instructions
*You must have BepInEx installed.*

1. Locate your Valheim game folder.  
2. Extract the contents of the archive into the `BepInEx\plugins` folder.  
3. Launch the game and ping a location to see its distance.  
4. Adjust settings in `BepInEx\config\JoksterCube.PingDistance.cfg` if needed.

---

### 🧩 Compatibility

- Client-side; an unmodded server forwards highlights to other clients running the mod. Server installation also enables synchronized duration settings.
- Works on unmodded servers; other players do not need to install it.

---

### 💬 Feedback & Support

Report issues or suggest improvements via the mod’s Thunderstore page or GitHub repository.

---

**Author:** JoksterCube  
**Version:** 1.0.0
**License:** MIT  
