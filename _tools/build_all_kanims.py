#!/usr/bin/env python3
"""Canonical rebuild of every Decor Lights expansion kanim, with the calibrated
placement constants in one place. Run this, then install to the Local mod folder.

    /opt/miniconda3/bin/python _tools/build_all_kanims.py

Edits to the placement geometry (size / vertical anchor) happen HERE, not scattered
across shell one-liners. After running, copy DecorLights/anim/assets/decor_* into the
Local mod and restart ONI (it caches the kanim atlas at startup).

Calibration status (2026-06-28):
  FLOOR  : world 248, y_offset -100           -> VALIDATED in-game (Shinebug Jar)
  CEILING: world 140, anchor_top -104, trim   -> R12 (2026-06-29). anchor_top = art TOP-edge
                                                  position in kanim units, y-DOWN (larger=lower
                                                  on screen). Cell bottom = origin (0), so the
                                                  ceiling/top-of-cell = -104. GOAL (per Josh):
                                                  lamp TOP edge touches the underside of the
                                                  ceiling -> anchor_top = -104 exactly. (History:
                                                  +104/+208 put the top 1-2 cells BELOW the cell.)
  WALL   : world 248-ish, y_offset -100        -> UNVERIFIED (edison/neon brackets)

Cell size ~= 104 units/cell. ⚠️ anchor_top sign is INVERTED vs intuition: INCREASING
CEILING_ANCHOR_TOP moves the fixture DOWN, DECREASING it moves the fixture UP. To nudge
ceiling fixtures, change CEILING_ANCHOR_TOP by fractions of 104 (lower value = higher art).
"""
from __future__ import annotations

import glob
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_light_kanim as m  # noqa: E402

FORK = "/mnt/win-d/Documents/ONI_Mods_Fork/DecorLights"
OUT = os.path.join(FORK, "anim", "assets")
FLOOR_SRC = "/mnt/win-d/Pictures/test/oni_lights/20260628_093645"
HANG_SRC = "/mnt/win-d/Pictures/test/oni_lights_hanging/20260628_105537"
TMP = "/tmp/oni_light_tex"

# --- calibrated geometry ----------------------------------------------------
FLOOR_WORLD, FLOOR_Y = 248.0, -100.0
CEILING_WORLD, CEILING_ANCHOR_TOP, CEILING_STEM = 140.0, -60.0, 0.05
WALL_WORLD, WALL_Y = 248.0, -100.0

# slug -> (source_dir, image_number_prefix). slug == kanim build name; the building
# def's anim = slug + "_kanim".
FLOOR = {
    "decor_shinebug": (FLOOR_SRC, "01"), "decor_mushroom_cluster": (FLOOR_SRC, "04"),
    "decor_amethyst_geode": (FLOOR_SRC, "05"), "decor_plasma_globe": (FLOOR_SRC, "06"),
    "decor_hurricane_lantern": (FLOOR_SRC, "07"), "decor_bubble_tube": (FLOOR_SRC, "10"),
    "decor_candelabra": (FLOOR_SRC, "12"), "decor_terrarium_orb": (FLOOR_SRC, "13"),
    "decor_wisp_orb": (FLOOR_SRC, "14"), "decor_sputnik_atomic": (FLOOR_SRC, "15"),
    "decor_algae_column": (FLOOR_SRC, "17"), "decor_salt_ring_tower": (FLOOR_SRC, "18"),
    "decor_star_projector": (FLOOR_SRC, "19"), "decor_lightning_bottle": (FLOOR_SRC, "20"),
}
CEILING = {
    # reconfigured from the original 20 (hanging-style art)
    "decor_crystal_chandelier": (FLOOR_SRC, "11"), "decor_paper_lantern": (FLOOR_SRC, "02"),
    "decor_jellyfish_jar": (FLOOR_SRC, "08"), "decor_anglerfish": (FLOOR_SRC, "16"),
    # 16 dedicated hanging designs
    "decor_pendant_dome": (HANG_SRC, "01"), "decor_glass_globe": (HANG_SRC, "02"),
    "decor_bare_edison": (HANG_SRC, "03"), "decor_festoon_string": (HANG_SRC, "04"),
    "decor_paper_lantern_hang": (HANG_SRC, "05"), "decor_caged_pendant": (HANG_SRC, "06"),
    "decor_crystal_pendant": (HANG_SRC, "07"), "decor_jelly_pendant": (HANG_SRC, "08"),
    "decor_terrarium_hang": (HANG_SRC, "09"), "decor_oil_lamp_hang": (HANG_SRC, "10"),
    "decor_moroccan_lantern": (HANG_SRC, "11"), "decor_firefly_jar_hang": (HANG_SRC, "12"),
    "decor_plasma_pendant": (HANG_SRC, "13"), "decor_vine_bloom": (HANG_SRC, "14"),
    "decor_starlight_orb": (HANG_SRC, "15"), "decor_spiral_chandelier": (HANG_SRC, "16"),
}
WALL = {
    "decor_edison_cage": (FLOOR_SRC, "03"), "decor_neon_coil": (FLOOR_SRC, "09"),
}


def src_image(folder: str, num: str) -> str:
    return glob.glob(f"{folder}/{num}_*.png")[0]


def main() -> None:
    os.makedirs(TMP, exist_ok=True)
    for slug, (folder, num) in FLOOR.items():
        tex = f"{TMP}/{slug}.png"
        m.process_fixture(src_image(folder, num), tex)
        m.build_kanim(slug, tex, OUT, world_max=FLOOR_WORLD, y_offset=FLOOR_Y)
    for slug, (folder, num) in CEILING.items():
        tex = f"{TMP}/{slug}.png"
        m.process_fixture(src_image(folder, num), tex, trim_stem=True, stem_frac=CEILING_STEM)
        m.build_kanim(slug, tex, OUT, world_max=CEILING_WORLD, anchor_top=CEILING_ANCHOR_TOP)
    for slug, (folder, num) in WALL.items():
        tex = f"{TMP}/{slug}.png"
        m.process_fixture(src_image(folder, num), tex)
        m.build_kanim(slug, tex, OUT, world_max=WALL_WORLD, y_offset=WALL_Y)
    print(f"built {len(FLOOR)} floor + {len(CEILING)} ceiling + {len(WALL)} wall kanims -> {OUT}")


if __name__ == "__main__":
    main()
