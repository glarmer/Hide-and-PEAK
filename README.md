# Hide and PEAK

Do you love playing Hide and Seek but wish the game had more chaos and excitement?

Introducing **Hide and PEAK**, a mod for PEAK that adds custom taunt sounds, team mechanics, and enhanced gameplay features — only the host needs to have this mod installed!

This mod was commissioned by [Hershey926](https://www.youtube.com/@hershy926), credit for the idea goes entirely to them.

---

## Features

* Custom taunt sounds for Hiders.
* Host-controlled timing and intervals for sounds.
* Team selection UI and in-game scoreboard enhancements.
* Grace period for Hiders at the start of each round.
* Player stats tracking including catches and deaths.
* Easy-to-use configuration menu (F3) for timing, intervals, and more.
* Fully synchronized multiplayer experience: only the host needs the mod installed.

---

## Configuration

* Open the configuration menu with **F3**.
* Options include:

  * Taunt start time
  * Taunt interval
  * Hider grace period
  * Seeker voice toggle
  * Name color customization
* All settings take effect in real-time without restarting the game.

---

## Important Notes

* **Sounds folder:** The `sounds` folder in your PEAK directory **must contain the exact set of WAV files** used by the host. Missing or mismatched files will result in missing taunts or errors.
* Only the host needs this mod installed; clients will automatically synchronize sounds and settings.
* Do **not** report bugs to the game developers while using mods. Test without mods first to ensure issues are truly game-related.
* Only download the mod from official sources to avoid malware.

---

## Manual Installation

Only the host needs to install this mod.

1. Download BepInEx from [here](https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.3/BepInEx_win_x64_5.4.23.3.zip)
2. Extract the contents of the zip into your game directory (default: `C:\Program Files (x86)\Steam\steamapps\common\PEAK`). You should end up with folders like `BepInEx`, `doorstop_config.ini`, etc.
3. Start the game once and then close it to complete the BepInEx setup.

   * **Linux users:** Set the launch option `WINEDLLOVERRIDES="winhttp=n,b" %command%` before running the game.
4. Navigate to `...\PEAK\BepInEx\plugins` and copy the `HideAndPEAK.dll` from releases into this folder.
5. Ensure a `sounds` folder exists in the PEAK directory containing **the same WAV files** as the host.
6. Run the game.

---

## Help

* Find your PEAK folder via Steam: **Right-click PEAK → Manage → Browse Local Files**.
* If issues occur, ensure your `sounds` folder is complete and matches the host.
* Report problems or request features via the [GitHub repository](https://github.com/glarmer/HideAndPEAK).