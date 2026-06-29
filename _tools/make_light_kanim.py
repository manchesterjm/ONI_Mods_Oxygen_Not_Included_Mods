#!/usr/bin/env python3
"""Build a single-symbol ONI kanim for a static decor-light building from a PNG.

Pipeline per light:
  1. process_fixture(): take a generated 1024^2 concept PNG, key out the near-white
     background to alpha, autocrop to the fixture, downscale to a sane texture size.
  2. build_kanim(): emit <slug>_build.bytes / <slug>_anim.bytes / <slug>.png — one
     symbol ("ui") shown by three anims (place/off/on), all identity transform.

World scale is set by the symbol pivot size, not the texture resolution (natural_tile
renders ~1 cell with pivot_w=248). SCALE/Y_OFFSET below were calibrated once in-game
on a reference light and then reused for the whole batch.

Registration rule (verified against ceilinglight_pretty): the game serves a modded
kanim as ``lower(build.bytes name) + "_kanim"``; the building def's ``anim:`` must match.
"""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import kanim_codec as kc  # noqa: E402

# --- world-placement constants (calibrate on light #1, then reuse) -----------
WORLD_MAX = 248.0      # kanim units for the fixture's longest side (~1 cell, per natural_tile)
Y_OFFSET = -100.0      # element translation to seat the sprite on the cell floor
TEX_MAX = 256          # max texture dimension after downscale (keep detail, stay small)
BG_TOL = 40            # color distance from the sampled corner bg that still counts as background
BG_FEATHER = 18        # transition band (px-distance) feathered from opaque->transparent at the edge


def _sample_bg(px, w: int, h: int) -> tuple[float, float, float]:
    """Median border color = the (warm cream) background to key out."""
    import statistics
    pts = ([(x, 0) for x in range(0, w, 4)] + [(x, h - 1) for x in range(0, w, 4)] +
           [(0, y) for y in range(0, h, 4)] + [(w - 1, y) for y in range(0, h, 4)])
    cols = [px[x, y][:3] for x, y in pts]
    return tuple(statistics.median(c[i] for c in cols) for i in range(3))


def process_fixture(src_png: str, out_png: str, pad: int = 6, trim_stem: bool = False,
                    stem_frac: float = 0.10) -> tuple[int, int]:
    """Key out the cream background, autocrop to the fixture, downscale. Returns (w, h).

    The backgrounds are warm ivory (blue channel ~225), not pure white, so we key on
    color distance from the actual sampled corner color rather than a brightness cut.
    Flooding from the borders avoids punching holes in bright parts of the fixture (a
    glowing bulb can be near-cream too); the boundary alpha is feathered for a clean edge.

    trim_stem crops the long hanging cord down to a short stub (for ceiling pendants).
    """
    im = Image.open(src_png).convert("RGBA")
    px = im.load()
    w, h = im.size
    br, bg_, bb = _sample_bg(px, w, h)

    def near_bg(x: int, y: int) -> float:
        r, g, b, _ = px[x, y]
        d = ((r - br) ** 2 + (g - bg_) ** 2 + (b - bb) ** 2) ** 0.5
        if d <= BG_TOL:
            return 0.0                                  # background
        if d >= BG_TOL + BG_FEATHER:
            return 1.0                                  # solid fixture
        return (d - BG_TOL) / BG_FEATHER                # feathered edge

    visited = bytearray(w * h)
    stack = [(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)]
    stack += [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)]
    while stack:
        x, y = stack.pop()
        if x < 0 or y < 0 or x >= w or y >= h or visited[y * w + x]:
            continue
        visited[y * w + x] = 1
        alpha = near_bg(x, y)
        if alpha >= 1.0:
            continue                                    # hit the fixture; stop flooding here
        r, g, b, a = px[x, y]
        px[x, y] = (r, g, b, int(a * alpha))
        if alpha == 0.0:                                # only keep flooding through pure background
            stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    bbox = im.getbbox()
    if bbox:
        im = im.crop((max(0, bbox[0] - pad), max(0, bbox[1] - pad),
                      min(w, bbox[2] + pad), min(h, bbox[3] + pad)))
    if trim_stem:
        im = trim_cord(im, stem_frac=stem_frac)
    if max(im.size) > TEX_MAX:
        s = TEX_MAX / max(im.size)
        im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
    im.save(out_png)
    return im.size


def trim_cord(im: Image.Image, stem_frac: float = 0.10) -> Image.Image:
    """Crop the long hanging cord above the fixture down to a short stub.

    The generated pendants dangle from a long thin cord; placed in-game that pushes
    the fixture far below the ceiling. Detect the fixture (the wide part) and keep
    only a short stem of cord above it (stem_frac of the fixture's height)."""
    import numpy as np
    a = np.asarray(im.convert("RGBA"))[:, :, 3]
    widths = (a > 40).sum(axis=1)
    if widths.max() == 0:
        return im
    fixture_rows = np.where(widths > 0.33 * widths.max())[0]
    if len(fixture_rows) == 0:
        return im
    f_top, f_bottom = fixture_rows[0], fixture_rows[-1]
    stem = int(stem_frac * (f_bottom - f_top + 1))
    new_top = max(0, f_top - stem)
    if new_top == 0:
        return im
    return im.crop((0, new_top, im.width, im.height))


def _white_outline(colour: Image.Image) -> Image.Image:
    """White line-art derived from the fixture's dark ink: opaque on dark outlines,
    transparent in the light interior. ONI shows this see-through during construction."""
    import numpy as np
    arr = np.asarray(colour.convert("RGBA"), dtype=float)
    r, g, b, a = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2], arr[:, :, 3]
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    ink = np.clip((120.0 - lum) / 90.0, 0.0, 1.0)        # dark pixels -> 1
    out = np.zeros_like(arr)
    out[:, :, 0:3] = 255.0
    out[:, :, 3] = (a / 255.0) * ink * 255.0
    return Image.fromarray(out.astype("uint8"), "RGBA")


def build_kanim(slug: str, tex_png: str, out_dir: str,
                world_max: float = WORLD_MAX, y_offset: float = Y_OFFSET,
                anchor_top: float | None = None) -> None:
    """Write the three kanim files for `slug` into out_dir/<slug>/.

    Mirrors the known-good natural_tile structure: three symbols (base/place/ui)
    all drawing the one fixture texture, and four anims (off/on -> base, place ->
    place, ui -> ui). ONI's placement/construction preview looks up the `place`
    symbol by name; without it the preview falls back to a solid white box.

    anchor_top (ceiling pendants): place the art's TOP edge at this y (the ceiling
    surface, ~-100), so the fixture hangs down from the ceiling regardless of its
    height. When None, y_offset positions the art centre (floor lamps).
    """
    w, h = Image.open(tex_png).size
    longest = max(w, h)
    pivot_w = world_max * w / longest
    pivot_h = world_max * h / longest
    if anchor_top is not None:
        y_offset = anchor_top - pivot_h / 2.0

    # The atlas packs the colour fixture (left half) and a white silhouette of it
    # (right half) side by side. base/ui draw the colour half; the place symbol draws
    # the white half, so ONI's placement + under-construction preview shows a clean
    # see-through white outline (mirroring vanilla kanims' pre-whitened place art).
    aw = 2 * w
    inset_x, inset_y = 0.5 / aw, 0.5 / h
    colour_uv = (inset_x, inset_y, 0.5 - inset_x, 1.0 - inset_y)
    white_uv = (0.5 + inset_x, inset_y, 1.0 - inset_x, 1.0 - inset_y)

    def symbol(name: str, uv: tuple) -> kc.BuildSymbol:
        frame = kc.BuildFrame(
            source_frame=0, duration=1, build_image_idx=0,
            pivot_x=0.0, pivot_y=0.0, pivot_w=pivot_w, pivot_h=pivot_h,
            x1=uv[0], y1=uv[1], x2=uv[2], y2=uv[3])
        h_ = kc.klei_hash(name)
        return kc.BuildSymbol(hash=h_, path=h_, color=0, flags=0, frames=[frame])

    sym_specs = (("base", colour_uv), ("place", white_uv), ("ui", colour_uv))
    build = kc.Build(
        version=10, name=slug,
        symbols=[symbol(n, uv) for n, uv in sym_specs],
        hashes=[(kc.klei_hash(n), n) for n, _ in sym_specs])

    def one_frame_anim(name: str, symbol_name: str) -> kc.Anim:
        el = kc.AnimElement(image_hash=kc.klei_hash(symbol_name), index=0, layer=0, flags=0,
                            a=1.0, bb=1.0, g=1.0, rr=1.0,
                            m=(1.0, 0.0, 0.0, 1.0, 0.0, y_offset), order=0.0)
        fr = kc.AnimFrame(x=0.0, y=y_offset, w=pivot_w, h=pivot_h, elements=[el])
        return kc.Anim(name=name, hash=kc.klei_hash(name), rate=30.303030014038086, frames=[fr])

    anim = kc.AnimFile(
        version=5, max_vis_symbol_frames=1,
        anims=[one_frame_anim("off", "base"), one_frame_anim("on", "base"),
               one_frame_anim("place", "place"), one_frame_anim("ui", "ui")],
        hashes=[(kc.klei_hash(s), s) for s in ("base", "place", "ui", "off", "on")])

    # Atlas = [colour | white-outline], each w x h. The outline (place art) keeps
    # the fixture's dark ink lines as white and drops the interior to transparent,
    # matching ONI's see-through construction-preview look.
    colour = Image.open(tex_png).convert("RGBA")
    atlas = Image.new("RGBA", (aw, h), (0, 0, 0, 0))
    atlas.paste(colour, (0, 0))
    atlas.paste(_white_outline(colour), (w, 0))

    dest = Path(out_dir) / slug
    dest.mkdir(parents=True, exist_ok=True)
    (dest / f"{slug}_build.bytes").write_bytes(kc.write_build(build))
    (dest / f"{slug}_anim.bytes").write_bytes(kc.write_anim(anim))
    atlas.save(dest / f"{slug}.png")
    print(f"  built {slug}: tex {w}x{h}, atlas {aw}x{h}, pivot {pivot_w:.0f}x{pivot_h:.0f} -> {dest}")


if __name__ == "__main__":
    # CLI: make_light_kanim.py <slug> <concept_png> <out_dir>
    slug, concept, out = sys.argv[1], sys.argv[2], sys.argv[3]
    tmp = f"/tmp/claude-1000/-home-josh/481d5ced-63f4-463a-a30d-b05b0047f3c7/scratchpad/{slug}_tex.png"
    process_fixture(concept, tmp)
    build_kanim(slug, tmp, out)
