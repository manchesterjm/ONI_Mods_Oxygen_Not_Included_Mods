# HANDOFF — "Buildable Geysers" (ORIGINAL mod, planned 2026-07-10)

> **For the next session (plan mode first, then build).** Everything decided so far is here; read this +
> `ONI_Mods_Reference.md` (the mod-building sections) before planning. Game build **740622**,
> `ONI_Mods_Fork/lib/` already refreshed to it.

## Why this mod exists (the motivating incident, diagnosed 2026-07-10)

Josh's years-old main save started with an **Ethanol Geyser** that silently vanished. Diagnosis (verified):
vanilla ONI has **no ethanol geyser** — it was a **Customize Geyser (Steam 1861107947)** custom type. ONI
strips any saved entity whose prefab is missing at load; ONI also blanket-disables all mods after a hard
crash/quit (documented repeatedly — disk `mods.json` routinely shows all-off while the in-game menu, which
is authoritative, shows all-on). One load+save during such a window deleted the geyser permanently.
Re-enabling the mod restores the *type*, not the lost map object.

**Josh rejected the sandbox-spawn fix** — this save has never been sandbox-flagged and he wants achievements
kept alive. Chosen fix (option 2 of the ones offered): a proper reusable mod.

## What the mod must do

1. Add **buildable "geyser kit" buildings** — dupes construct one with materials; on construction complete
   it **replaces itself with the real geyser prefab** at that spot, then removes itself.
2. Cover **every geyser type in the loaded game, including other mods' geysers** (Customize Geyser's
   Ethanol Geyser is the acceptance test) — **enumerate live**, never hardcode a type list.
3. **No sandbox/debug flag** — the save must stay achievement-eligible. Placement costs dupe labor +
   materials by design (feels earned, not cheaty).
4. Buildings live in the **Base menu under a dedicated subcategory** (e.g. "geysers"), like Decor Lights'
   Furniture→"lights" subcat, so ~20+ kits don't spam the root menu.

## Proven in-repo patterns to clone (all in `ONI_Mods_Fork/`, all validated on 737790/740622)

| Need                                   | Steal from                                                                                     |
| -------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Build-complete → spawn entity → delete self | **BuildableNaturalTile** (`BuildingComplete.OnSpawn` postfix name-gated to its ID; cell replace + `Util.KDestroyGameObject`) |
| Many buildings from one catalog + shared config base + menu subcategory | **DecorLights** (`DecorLightCatalog` single source of truth; `DecorLightsPatches` loops it for strings/menu/tech; `ModUtil.AddBuildingToPlanScreen`) |
| Live reads instead of hardcoded game data | **RocketFuelCalculator** (reads TUNING + tank capacities live — the StatsUnlimitedLite lesson) |
| Surgical Harmony patching + prefab-tag gating | **CritterCondoAnywhere**                                                                  |
| Menu/tech runtime lookup robust to renames | **InsulatedDoor** 737790 port (unlock/menu placement derived from a vanilla sibling at runtime) |

Build mechanics: SDK-style csproj, `netstandard2.1`, `<Reference>` HintPaths into `ONI_Mods_Fork/lib/`
(740622 set), build in a **local scratch dir** (NTFS obj/bin lock issue), **build the csproj explicitly**
(bare `dotnet build` wanders into sibling projects), install DLL+yamls to `mods/Local/BuildableGeysers/` on
**both** OSes, enable via the **in-game Mods menu** (authoritative). staticID `BuildableGeysers`,
`minimumSupportedBuild: 740622`, version `2026.7.x.0`. Lint 10/10 (`/opt/miniconda3/bin/python3 -m pylint`
for any tooling scripts; C# clean build 0 errors — MSB3277 ×2 is the known benign warning).

## THE key technical unknown — resolve in plan mode BEFORE writing code

**Dynamic building generation vs. load order.** Building configs normally load via reflection over
`IBuildingConfig` classes in `GeneratedBuildings.LoadGeneratedBuildings`; geyser *entity* prefabs (including
modded ones) may register **after** buildings do. If we generate one kit-building per discovered geyser
prefab, the discovery point must run late enough to see other mods' geysers (mod load order also matters —
Customize Geyser must load before us, or discovery must be later than any mod's prefab registration).

Investigation steps (ilspycmd against the game's `Assembly-CSharp.dll`, and the installed Customize Geyser
DLL under `mods/Steam/1861107947/`):

```bash
~/.dotnet/tools/ilspycmd -t GeneratedBuildings <Managed>/Assembly-CSharp.dll   # building load sequence
~/.dotnet/tools/ilspycmd -t GeyserGenericConfig <Managed>/Assembly-CSharp.dll  # how vanilla makes GeyserGeneric_* variants
~/.dotnet/tools/ilspycmd -t GeyserConfigurator <Managed>/Assembly-CSharp.dll   # the type/settings registry
~/.dotnet/tools/ilspycmd <mods/Steam/1861107947>/*.dll | grep -i -A5 "GeyserConfig\|CreatePrefab\|AddGeyser"  # where CG registers its types
```

Candidate designs to weigh in plan mode (pick after the decompiles):
- **A. Enumerate `GeyserConfigurator` settings / `GeyserGenericConfig` variant IDs** (likely available
  earlier than prefabs) and generate kit buildings from that list. Vanilla geysers are all
  `GeyserGeneric_<id>`; check whether Customize Geyser feeds the same registry (if yes, this is clean).
- **B. One single "Geyser Kit" building + a type selector** (side-screen or PLib Options) — dodges dynamic
  building generation entirely; on build complete, spawn the selected type. Less pretty UX, far less
  load-order risk. Fallback if A is gnarly.
- **C. Generate kit buildings in a `Db.Initialize` postfix** (runs after all `LoadGeneratedBuildings`) —
  verify buildings can still be registered that late (plan-screen + tech insertion timing).

## Design decisions already made / constraints

- Spawned geysers **roll their outputs randomly** within type ranges (same as sandbox spawns) — accepted;
  do NOT try to let the player tune emission (that's Customize Geyser's job).
- Decide construction cost in plan mode (something meaningful — e.g. refined metal + plastic-tier — but not
  grindy; Josh's call). Also decide: is the kit deconstructable before completion (yes, normal build rules)
  and is the *spawned geyser* just a normal geyser afterward (yes — no special handling, it IS the prefab).
- Neutronium base? Real worldgen geysers sit on neutronium. Spawning the bare geyser prefab (like sandbox
  does) is acceptable — do what sandbox spawn produces; don't fabricate neutronium tiles.
- Save-compat honesty: a placed **modded** geyser (e.g. Ethanol) still depends on ITS mod staying enabled —
  our mod can't protect it from the exact incident that motivated this project. Note that in the reference
  when documenting.
- Unlock: pick an existing mid-game tech via **runtime lookup off a vanilla sibling** (InsulatedDoor
  pattern), not a hardcoded tech ID.

## Acceptance / validation checklist (in-game, Josh's save COPY first)

1. Fresh load with mod: Base menu shows the "geysers" subcategory listing vanilla types **+ Ethanol Geyser
   (Customize Geyser)** — proves live enumeration across mods.
2. Build an Ethanol Geyser kit in a test spot → dupes build it → real Ethanol Geyser appears, kit gone,
   geyser erupts/analyzes/dormants normally, 0 errors in `Player.log`
   (`~/.config/unity3d/Klei/Oxygen Not Included/Player.log`, UTC timestamps).
3. Save/reload → geyser persists. Colony summary still shows **achievements enabled** (no sandbox flag).
4. Disable our mod → reload → placed VANILLA-type geyser persists (it's a real prefab, not ours);
   kits vanish from menu. Re-enable → menu back. (Ethanol persists iff Customize Geyser stays on.)
5. Then do it for real: **one Ethanol Geyser at the old location in the main save** (get placement from
   Josh in-game).

## Session-log / docs duties for the build session

- Log as-you-go to `Session_Logs/session_<date>.md`; on completion add the mod's section to
  `ONI_Mods_Reference.md` (follow the existing per-mod section format: what/where/patches/gotchas/validated),
  push to the mods repo (`manchesterjm/ONI_Mods_Oxygen_Not_Included_Mods`), sync `mods/Local/` to BOTH OSes
  (byte-identical), bump nothing else.
- Josh runs **Fable 5** for the build (his note 2026-07-10). ONI must be CLOSED during install steps
  (`mods.json` gotchas) and RESTARTED to pick up the mod.
