# Decor Lights Expansion — Handoff (2026-06-28)

Adding new lamps to the **Decor Lights** ONI mod. Started as "add 4 more lights," grew to
**36 new lamps** (20 from a first concept batch + 16 dedicated hanging designs). The mod is
on game build **737790**. This doc is the resume point.

---

## TL;DR status

| Group | Count | Placement | Geometry status |
| ----- | ----- | --------- | --------------- |
| Floor lamps | 14 | OnFloor, 1×2 | ✅ **validated in-game** (Shinebug Jar) |
| Ceiling/hanging | 20 | OnCeiling, 1×1 | ✅ **VALIDATED in-game (2026-06-29): anchor_top=−60, light `(0,0.4)`** — lamp top at ceiling underside, glow on the body |
| Wall lamps | 2 | OnWall, 1×1 | ✅ **VALIDATED in-game (2026-06-29)** — Caged Edison, Neon Coil; first-guess geometry was good |

Also done & validated: background keying, the construction/placement **white-outline** preview
(Josh chose "outline only"), and the build/compile/install loop.

**Not done:** final ceiling confirm; wall verification; spot-check the 14 floor lamps; update the
catalog header comment (says "20", now 36); **sync to Windows D: Local**; **commit + push to the repo**;
update `ONI_Mods_Reference.md`.

---

## Where everything lives

- **Mod source:** `/mnt/win-d/Documents/ONI_Mods_Fork/DecorLights/`
  - C#: `DecorLightSpec.cs`, `DecorLightConfigBase.cs`, `DecorLightCatalog.cs` (single source of truth,
    36 specs + `All[]`), 36 tiny `<Name>Config.cs` subclasses, `DecorLightsPatches.cs` (loops the catalog
    for strings/menu/tech), `ModUtils.cs`. Original 4 lamps: `Ceiling/Lava/Salt/LuminiferousSphereConfig.cs`.
  - Art: `anim/assets/decor_<slug>/` (one folder per lamp: `_build.bytes`, `_anim.bytes`, `.png`).
  - `DecorLights.csproj` — `EnableDefaultItems=false`, globs `*.cs`.
- **Tooling:** `/mnt/win-d/Documents/ONI_Mods_Fork/_tools/`
  - `kanim_codec.py` — read/write BILD+ANIM (round-trip validated; the correctness gate).
  - `make_light_kanim.py` — `process_fixture()` (bg-key + autocrop + optional cord-trim) and
    `build_kanim()` (single-image kanim). **All geometry constants are here.**
  - `build_all_kanims.py` — **canonical rebuild of all 36 kanims** with the calibrated per-placement
    constants and the slug→source-image map. Edit geometry here, run, then install.
  - `gen_oni_lights.py` / `gen_hanging_lights.py` — ComfyUI concept generators (Z-Image-Turbo).
- **Concept images (keep!):**
  - Floor/original 20: `/mnt/win-d/Pictures/test/oni_lights/20260628_093645/` (`NN_<slug>_<seed>.png` + contact sheet)
  - Hanging 16: `/mnt/win-d/Pictures/test/oni_lights_hanging/20260628_105537/`
- **Installed Local mod (CachyOS):** `~/.config/unity3d/Klei/Oxygen Not Included/mods/Local/DecorLights/`
  (DLL + `anim/assets/decor_*`). One DLL backup: `DecorLights.dll.bak_20260628_095626`.
- **Windows D: Local (NOT yet synced):** `/mnt/win-d/Documents/Klei/OxygenNotIncluded/mods/Local/DecorLights/`

---

## Build / install / iterate loop

```bash
# 1. (re)build kanims after any geometry change
/opt/miniconda3/bin/python /mnt/win-d/Documents/ONI_Mods_Fork/_tools/build_all_kanims.py

# 2. compile the DLL — MUST target the csproj explicitly (bare `dotnet build` wanders into
#    sibling mod projects like BiggerStorage/FullMinerYield and errors on .NET4.7.1)
cd /mnt/win-d/Documents/ONI_Mods_Fork/DecorLights
SCR=/tmp/oni_dl_build; rm -rf $SCR
/usr/bin/dotnet build DecorLights.csproj -c Release -o $SCR/out \
  --property:BaseIntermediateOutputPath=$SCR/obj/ -p:IntermediateOutputPath=$SCR/obj/
# expect: Build succeeded, 0 Error(s), 2 benign MSB3277 warnings

# 3. install DLL + kanims to Local
LOCAL="$HOME/.config/unity3d/Klei/Oxygen Not Included/mods/Local/DecorLights"
cp $SCR/out/DecorLights.dll "$LOCAL/DecorLights.dll"
for d in /mnt/win-d/Documents/ONI_Mods_Fork/DecorLights/anim/assets/decor_*; do
  n=$(basename "$d"); mkdir -p "$LOCAL/anim/assets/$n"; cp "$d"/* "$LOCAL/anim/assets/$n/"; done

# 4. FULLY restart ONI (it builds the kanim atlas once at startup; reinstalling under a
#    running game shows the OLD art). In-game Mods menu is authoritative.
```

DLL-only changes (catalog values, configs) need step 2-3. Art-only changes need step 1 + copy + restart.

---

## The kanim pipeline (how PNG → in-game art)

ONI uses Klei's binary kanim. No Unity/Klei tool needed — `kanim_codec.py` writes it directly,
validated by **byte-exact round-trip** against the known-good `ceilinglight_pretty` + `natural_tile`.

Per lamp, `build_kanim()` emits a **single-image** kanim:
- **3 symbols** `base` / `place` / `ui` (mirrors natural_tile). Atlas packs `[colour | white-outline]`
  side-by-side; `base`/`ui` UV → colour half, `place` UV → white-outline half.
- **4 anims** `off`/`on`→base, `place`→place, `ui`→ui. ONI plays `place` for the drag preview AND the
  "Awaiting Delivery" object (`BuildingLoader.CreateBuildingUnderConstruction` → `Add2DComponents(...,"place",...)`),
  so the white-outline `place` art is the see-through construction preview.

Key facts (hard-won):
- **Registration name** = `lower(build.bytes internal name) + "_kanim"`. So slug `decor_x` → def `anim: "decor_x_kanim"`.
- **klei_hash** = SDBM-lower (in codec; verified against the files' own hash tables).
- **Background key:** the concept backgrounds are warm ivory (blue ch ~225), NOT white. `process_fixture`
  samples the border color and floods by color-distance (`BG_TOL=40`, `BG_FEATHER=18`). A brightness cut fails.
- **Place art** must be pre-whitened (vanilla ceilinglight `place` pixels = RGB 237). `_white_outline()`
  derives white linework from the dark ink (Josh picked **outline-only** over solid-fill / faint-fill).
- **Cord trim:** generated pendants have long cords. `trim_cord(stem_frac)` crops the thin cord above the
  wider fixture to a short nub (ceiling lamps only, `trim_stem=True`).

---

## Geometry / placement calibration (the long pole)

World size is set by the symbol pivot, decoupled from texture resolution. `build_kanim` params:
`world_max` (longest side in kanim units), `y_offset` (art-centre y) OR `anchor_top` (art-TOP y, used for ceiling).

- **Cell ≈ 104 kanim units** (ONI standard ~100; derived from two ceiling data points).
- **OnFloor**: origin at the cell **bottom**. `world_max=248, y_offset=-100` → fixture sits on the floor.
  ✅ validated (Shinebug Jar: good scale, sits on platform, glows).
- **OnCeiling** (model nailed down 2026-06-29 from `make_light_kanim.py:143-152`): `anchor_top` **= the art's
  TOP-edge position** in kanim units, and the kanim Y axis is **DOWN-positive** (larger `anchor_top` ⇒ art lower
  on screen — that's why +104/+208 hung low). Cell bottom = origin (y=0); **cell top / ceiling underside = −104.**
  Cell ≈ 104 units. **GOAL (Josh, corrected): the lamp's TOP edge touches the underside of the ceiling, hanging
  DOWN from it — NOT centered in the cell.** ⇒ **`anchor_top = −104` is the deterministic answer** (art top edge
  at cell top). `world_max=140`, `stem_frac=0.05`, lamps 1×1. To nudge: lower `CEILING_ANCHOR_TOP` = higher art.
  Calibration trail: R8 `+104` / R9 `+208` (way low) → R10 `+10` / R11 `−16` (close, judged against the wrong
  "centered" target) → **R12 `−104` (top-at-ceiling).** **Light:** ceiling `LightOffset` now `(0,1.6)` in
  `DecorLightConfigBase.cs` (+y = up, cell units; tracked up with the art each round: −1→0→0.5→0.75→1.6).
- **OnWall**: `world_max=248, y_offset=-100` — **first guess, never tested.** Edison/Neon art has the bracket
  on the LEFT; likely needs horizontal anchoring (pivot_x offset) so the bracket meets the wall. Unknown which
  way ONI flips wall buildings. Expect a calibration round like the ceiling one.

Light offset per placement is in `DecorLightConfigBase.LightOffset()` (floor up, ceiling down, wall out) —
also first-guess for ceiling/wall, refine if the glow looks off-centre.

---

## C# architecture

`IBuildingConfig` discovery needs one type per building, so: `DecorLightConfigBase : IBuildingConfig`
(shared SaltLamp-style wiring, branches on `DecorPlacement`), each lamp = a `DecorLightSpec` in
`DecorLightCatalog` + a ~5-line subclass returning that spec. `DecorLightsPatches` loops
`DecorLightCatalog.All` to register strings / Furniture→lights menu / GlassFurnishings tech.
To add a lamp: spec in catalog → add to `All[]` → one subclass → its kanim folder.

The 4 original lamps (Ceiling Lamp, Luminiferous Sphere, Lava, Salt) are hand-written and register
themselves separately — don't touch. (Lava/Salt have **no kanim** → they render as ONI's white
missing-art placeholder; that's pre-existing, not our bug.)

---

## Concept generation (if more art is wanted)

ComfyUI native install at `~/ComfyUI` (CachyOS). Launch `~/ComfyUI/run_comfyui.sh`; **kill it after**
(frees the GPU). Z-Image-Turbo, 8 steps, cfg 1, ONI-style prompt (thick ink outlines, painterly gouache,
soft glow, single object on off-white bg). Hanging variant adds "suspended from a cord at the top." Edit
the `CONCEPTS` dict in `gen_oni_lights.py` / `gen_hanging_lights.py`, run, review the contact sheet.

---

## Immediate next steps (resume order)

1. **Get Josh's R8 ceiling result.** If fixture top is at the ceiling → ceiling done. Else nudge
   `CEILING_ANCHOR_TOP` (cell≈104) and rebuild ceiling.
2. **Calibrate wall lamps** (Caged Edison, Neon Coil) in-game — expect a round on horizontal/bracket anchoring.
3. **Spot-check the 14 floor lamps** (should match Shinebug Jar).
4. Fix `DecorLightCatalog.cs` header comment ("20 lamps" → 36; tall-vs-compact note).
5. **Sync to Windows D: Local** (`/mnt/win-d/Documents/Klei/OxygenNotIncluded/mods/Local/DecorLights/`):
   copy the DLL + all `anim/assets/decor_*`.
6. **Commit + push** the fork (`DecorLights/` + `_tools/`) to `github.com/manchesterjm/ONI_Mods_Oxygen_Not_Included_Mods`.
7. Update `Claude_References/Archive/... ONI_Mods_Reference.md` (now in main refs) with the 36 new lamps +
   the `_tools` kanim toolchain.

## Gotchas
- ONI caches the kanim atlas at startup → **always full-restart** to see art changes.
- Build `DecorLights.csproj` **explicitly** (sibling csprojs break a bare `dotnet build`).
- Never overwrite the Local DLL without the backup already in place (one exists).
- 36 + 4 = 40 lamps in the Furniture→lights submenu (chunky but intended).
