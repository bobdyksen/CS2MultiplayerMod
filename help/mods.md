---
title: Mods and compatibility
extra_javascript:
  - assets/mod-table.js
seo_title: "Mod compatibility | ZulutimeMultiplayerMod"
description: "Check supported Cities: Skylines II mods and community multiplayer test results. Filter compatibility reports and learn how the mod compatibility check works."
---

# Mods and compatibility

This list combines **official support and community multiplayer testing**.

**Last updated:** October 12, 2026

## Compatibility list

<div class="mod-table" markdown>

| Status | Mod | Details | Tested by |
| --- | --- | --- | --- |
| 🛠️ Official | [Traffic](https://mods.paradoxplaza.com/mods/80095/Windows) | Fully supported. | CS2 Multiplayer Developers, J. M. S., DaStrobel, Janno |
| 🛠️ Official | [Road Speed Adjuster](https://mods.paradoxplaza.com/mods/125866/Windows) | Fully supported. | CS2 Multiplayer Developers |
| 🟢 Works | Anarchy | Works without known issues. | J. M. S., Janno |
| 🟢 Works | Building Use | No multiplayer issues observed in the tested modset. | Janno |
| 🟢 Works | Custom Chirps | No multiplayer issues observed in the tested modset. | Janno |
| 🟢 Works | [Extended Tooltip](https://mods.paradoxplaza.com/mods/78188/Windows) | Works for host and clients. | Tommy, Janno |
| 🟢 Works | I18n Everywhere | Works without known issues. | J. M. S., Janno |
| 🟢 Works | Find It | Works without known issues. | J. M. S. |
| 🟢 Works | Region Flag Icons | Works without known issues. | J. M. S. |
| 🟢 Works | Asset Icon Library | Works without known issues. | J. M. S. |
| 🟢 Works | Unified Icon Library | Works without known issues. | J. M. S., Janno |
| 🟢 Works | Extra Lib | Works without known issues. | J. M. S. |
| 🟢 Works | Industry Boundary | Works without known issues. | Tommy |
| 🟢 Works | All Transit + Trucks | Works without known issues. | Tommy |
| 🟢 Works | Lumina | Works without known issues. Cosmetic mod. | DaStrobel |
| 🟢 Works | [Stop Jaywalking](https://mods.paradoxplaza.com/mods/150664/Windows) | Works for host and clients as a client-side mod. | Tommy |
| 🟢 Works | [Road Name Remover](https://mods.paradoxplaza.com/mods/77463/Windows) | Works for host and clients as a client-side mod. | Tommy |
| 🟢 Works | [Achievement Fixer](https://mods.paradoxplaza.com/mods/121256/Windows) | Works for host and clients as a client-side mod. | Tommy |
| 🟢 Works | [Specialized Industry Freedom](https://mods.paradoxplaza.com/mods/155500/Windows) | Works for both host and clients. | Tommy |
| 🟢 Works | [Articulated Buses](https://mods.paradoxplaza.com/mods/148570/Windows) | Works for host and clients as a client-side mod. | Tommy |
| 🟢 Works | No Vehicle Despawn | No multiplayer issues observed in the tested modset. | Janno |
| 🟢 Works | Realistic JobSearch | No multiplayer issues observed in the tested modset. | Janno |
| 🟢 Works | Realistic Trips | No multiplayer issues observed in the tested modset. | Janno |
| 🟢 Works | Realistic Workplaces And Households | No multiplayer issues observed in the tested modset. | Janno |
| 🟢 Works | Traffic Tool Essentials | No multiplayer issues observed in the tested modset. | Janno |
| 🟢 Works | Official Region Packs | Official region packs such as the German and French packs were tested without problems. | J. M. S. |
| 🟡 Partially works | Move It | Since 0.1.7.1, moving buildings, props and trees and road edits are synchronized once you let go. Objects with attached sub-objects are not synchronized yet. | J. M. S., Janno |
| 🟡 Partially works | Node Controller | Since 0.1.7.1, road edits are synchronized when the edit ends. If the mod cannot tell which road was edited, it holds the edit instead of changing the wrong road. | DaStrobel, J. M. S., Janno |
| 🟡 Partially works | Traffic Lights Enhancement | Works when used by the host. Changes become effective for clients after a resync. Non-host players cannot change intersection settings themselves. | DaStrobel |
| 🟡 Partially works | CoPaste | Works for the host. Usage by non-host players can cause desyncs and may require a forced resync. | Tommy |
| 🟡 Partially works | [529 Tiles](https://mods.paradoxplaza.com/mods/74328/Windows) | When using `Select initial starting tiles`, only the host can purchase the free starting tiles. Afterwards non-host players can purchase tiles normally. | Tommy |
| 🟡 Partially works | [Change Company](https://mods.paradoxplaza.com/mods/114101/Windows) | Only the host can use and configure the mod and affected buildings. | Tommy |
| 🟡 Partially works | [Event Rush](https://mods.paradoxplaza.com/mods/156682/Windows) | Citizens created by the mod are visible to both host and clients. Very large events can cause significant performance problems. | Tommy |
| 🟡 Partially works | Decals / Props | Placement works, but changes are not automatically synchronized. A manual synchronization makes them visible to other players. | KeKo |
| 🔴 Doesn't work | Better Bulldozer | Bulldozing objects can cause connected players to crash. | KeKo |

</div>

**Mods by Gruny:** J. M. S. reported that mods made by **Gruny** generally appeared to work without
problems. Because individual mods were not listed separately in the report, they are not
individually marked as verified above.

## Report a mod

Please only report mods you have actually tested in multiplayer, in this format:

```text
🟢 / 🟡 / 🔴 Mod Name
🔗 Paradox Mods Link
Status: Works / Partially Works / Doesn't Work
Details: What works, what doesn't, and whether there are host/client limitations.
```

## Other mods are warned about

Other mods do not block hosting or joining. The active playset is still read, including
asset-only mods such as maps, prop packs and prefab packs, and the names are written to
the log and shown in a warning banner. Nothing in the synchronization layer accounts for
an unverified third party changing prefabs, tools or the simulation, so one extra mod on
one machine can still desync the session or crash the other player. Mods in your other
playsets are not enabled for this run and are ignored.

When a player joins, the host compares both players' other active mods, including their
versions, and both sides see a warning that names the difference. The join continues.
This applies to the supported mods above too: Traffic, Anarchy or Road Speed Adjuster on
only one computer changes what the game builds there. Find It and Asset Icon Library only
change what one player sees and are not compared.

A different ZulutimeMultiplayerMod build is still refused, unless the host turns on Allow
a Different Mod Build (Own Risk). The network protocol, the Cities: Skylines II version
and the DLC set are always checked.

| Banner | Meaning |
| --- | --- |
| Other mods enabled | The listed mods are active. Host and Join stay available. Desyncs are possible. |
| Other mods enabled, still loaded | Already disabled in the playset, but still in memory. Restart the game once to unload them. Host and Join stay available. |
| Different mod builds allowed | Other mods are active, and the host will also admit a different build of this mod. |

Using the same playset on every computer is still the safe setup. A playset that contains
only ZulutimeMultiplayerMod plus Traffic, Road Speed Adjuster, Anarchy, Find It and Asset
Icon Library is the one this project maintains.

---

## Allowing a different build of this mod

Options ▸ ZulutimeMultiplayerMod ▸ Advanced ▸ Compatibility ▸ Allow a Different Mod Build
(Own Risk). It can only be changed while offline, before hosting or joining.

With it on, a host admits players on a different ZulutimeMultiplayerMod build, as long as
the network protocol matches. Other mods are warned about whether or not this is on.

It does not bypass:

- the network protocol check, because different builds can encode network data differently,
- the Cities: Skylines II version check, or
- the DLC check.

It also does not make another mod multiplayer-aware. Expect desyncs, missing prefabs,
broken cities or crashes when the mod lists or the builds differ.

---

## Other mod compatibility

Mods not listed under official support are unverified. They no longer block a session,
and they are not guaranteed to leave synchronized state alone. This includes mods that
may appear to be display-only or UI-only.

---
