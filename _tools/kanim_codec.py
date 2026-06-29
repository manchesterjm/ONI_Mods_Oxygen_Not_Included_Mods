#!/usr/bin/env python3
"""Minimal reader/writer for Klei (ONI) .kanim binary files.

Two files make a kanim build: ``*_build.bytes`` (BILD: texture atlas symbols +
frames + UV/pivot, plus a hashed-string table) and ``*_anim.bytes`` (ANIM: named
animations -> frames -> elements that reference build symbols by hash).

This codec round-trips both known-good fork kanims byte-for-byte (see
``_selftest`` / ``roundtrip_ok``). That equality is the correctness gate: if the
re-encode matches the original, the field layout is right.

Format (version BILD=10, ANIM=5), all little-endian:

  BILD:
    'BILD', i32 version, i32 symbolCount, i32 frameCount, kstr name
    per symbol: i32 hash, i32 path, u32 color, i32 flags, i32 numFrames
      per frame: i32 sourceFrame, i32 duration, i32 buildImageIdx,
                 f32 pivotX, pivotY, pivotW, pivotH, f32 x1, y1, x2, y2
    i32 hashCount; per: i32 hash, kstr text

  ANIM:
    'ANIM', i32 version, i32 elementCount, i32 frameCount, i32 animCount
    per anim: kstr name, i32 hash, f32 rate, i32 numFrames
      per frame: f32 x, y, w, h; i32 elementCount
        per element: i32 imageHash, i32 index, i32 layer, i32 flags,
                     f32 a, b, g, r, f32 m1..m6 (2x3 transform), f32 order
    i32 maxVisSymbolFrames    (after the anims, before the hash table)
    i32 hashCount; per: i32 hash, kstr text

  kstr = i32 length + UTF-8 bytes (no terminator).
"""
from __future__ import annotations

import struct
from dataclasses import dataclass, field


# ---- Klei string hash (matches Hash.SDBMLower used by the game) -------------
def klei_hash(text: str) -> int:
    """SDBM-lower hash; result fits a signed int32 as stored in the files."""
    h = 0
    for ch in text.lower():
        h = (ord(ch) + (h << 6) + (h << 16) - h) & 0xFFFFFFFF
    # store as signed 32-bit
    return h - 0x100000000 if h >= 0x80000000 else h


class _Reader:
    def __init__(self, data: bytes):
        self.d = data
        self.o = 0

    def i32(self) -> int:
        v = struct.unpack_from("<i", self.d, self.o)[0]
        self.o += 4
        return v

    def u32(self) -> int:
        v = struct.unpack_from("<I", self.d, self.o)[0]
        self.o += 4
        return v

    def f32(self) -> float:
        v = struct.unpack_from("<f", self.d, self.o)[0]
        self.o += 4
        return v

    def kstr(self) -> str:
        n = self.i32()
        s = self.d[self.o:self.o + n].decode("utf-8")
        self.o += n
        return s

    def magic(self) -> str:
        s = self.d[self.o:self.o + 4].decode("ascii")
        self.o += 4
        return s


class _Writer:
    def __init__(self):
        self.parts: list[bytes] = []

    def i32(self, v: int):
        self.parts.append(struct.pack("<i", v))

    def u32(self, v: int):
        self.parts.append(struct.pack("<I", v))

    def f32(self, v: float):
        self.parts.append(struct.pack("<f", v))

    def kstr(self, s: str):
        b = s.encode("utf-8")
        self.i32(len(b))
        self.parts.append(b)

    def magic(self, s: str):
        self.parts.append(s.encode("ascii"))

    def out(self) -> bytes:
        return b"".join(self.parts)


# ---------------------------------------------------------------- BILD model --
@dataclass
class BuildFrame:
    source_frame: int
    duration: int
    build_image_idx: int
    pivot_x: float
    pivot_y: float
    pivot_w: float
    pivot_h: float
    x1: float
    y1: float
    x2: float
    y2: float


@dataclass
class BuildSymbol:
    hash: int
    path: int
    color: int
    flags: int
    frames: list[BuildFrame] = field(default_factory=list)


@dataclass
class Build:
    version: int
    name: str
    symbols: list[BuildSymbol]
    hashes: list[tuple[int, str]]

    @property
    def frame_count(self) -> int:
        return sum(len(s.frames) for s in self.symbols)


def parse_build(data: bytes) -> Build:
    r = _Reader(data)
    assert r.magic() == "BILD", "not a BILD file"
    version = r.i32()
    sym_count = r.i32()
    r.i32()  # frame_count (recomputed on write)
    name = r.kstr()
    symbols: list[BuildSymbol] = []
    for _ in range(sym_count):
        sym = BuildSymbol(hash=r.i32(), path=r.i32(), color=r.u32(), flags=r.i32())
        nframes = r.i32()
        for _ in range(nframes):
            sym.frames.append(BuildFrame(
                source_frame=r.i32(), duration=r.i32(), build_image_idx=r.i32(),
                pivot_x=r.f32(), pivot_y=r.f32(), pivot_w=r.f32(), pivot_h=r.f32(),
                x1=r.f32(), y1=r.f32(), x2=r.f32(), y2=r.f32()))
        symbols.append(sym)
    hashes = [(r.i32(), r.kstr()) for _ in range(r.i32())]
    return Build(version=version, name=name, symbols=symbols, hashes=hashes)


def write_build(b: Build) -> bytes:
    w = _Writer()
    w.magic("BILD")
    w.i32(b.version)
    w.i32(len(b.symbols))
    w.i32(b.frame_count)
    w.kstr(b.name)
    for sym in b.symbols:
        w.i32(sym.hash)
        w.i32(sym.path)
        w.u32(sym.color)
        w.i32(sym.flags)
        w.i32(len(sym.frames))
        for f in sym.frames:
            w.i32(f.source_frame)
            w.i32(f.duration)
            w.i32(f.build_image_idx)
            for v in (f.pivot_x, f.pivot_y, f.pivot_w, f.pivot_h, f.x1, f.y1, f.x2, f.y2):
                w.f32(v)
    w.i32(len(b.hashes))
    for h, s in b.hashes:
        w.i32(h)
        w.kstr(s)
    return w.out()


# ---------------------------------------------------------------- ANIM model --
@dataclass
class AnimElement:
    image_hash: int
    index: int
    layer: int
    flags: int
    a: float
    bb: float
    g: float
    rr: float
    m: tuple[float, float, float, float, float, float]
    order: float


@dataclass
class AnimFrame:
    x: float
    y: float
    w: float
    h: float
    elements: list[AnimElement] = field(default_factory=list)


@dataclass
class Anim:
    name: str
    hash: int
    rate: float
    frames: list[AnimFrame] = field(default_factory=list)


@dataclass
class AnimFile:
    version: int
    max_vis_symbol_frames: int
    anims: list[Anim]
    hashes: list[tuple[int, str]]


def parse_anim(data: bytes) -> AnimFile:
    r = _Reader(data)
    assert r.magic() == "ANIM", "not an ANIM file"
    version = r.i32()
    r.i32()  # total elements (recomputed on write)
    r.i32()  # total frames (recomputed on write)
    anim_count = r.i32()
    anims: list[Anim] = []
    for _ in range(anim_count):
        anim = Anim(name=r.kstr(), hash=r.i32(), rate=r.f32())
        nframes = r.i32()
        for _ in range(nframes):
            frame = AnimFrame(x=r.f32(), y=r.f32(), w=r.f32(), h=r.f32())
            nelem = r.i32()
            for _ in range(nelem):
                frame.elements.append(AnimElement(
                    image_hash=r.i32(), index=r.i32(), layer=r.i32(), flags=r.i32(),
                    a=r.f32(), bb=r.f32(), g=r.f32(), rr=r.f32(),
                    m=(r.f32(), r.f32(), r.f32(), r.f32(), r.f32(), r.f32()),
                    order=r.f32()))
            anim.frames.append(frame)
        anims.append(anim)
    max_vis = r.i32()
    hashes = [(r.i32(), r.kstr()) for _ in range(r.i32())]
    return AnimFile(version=version, max_vis_symbol_frames=max_vis, anims=anims, hashes=hashes)


def write_anim(a: AnimFile) -> bytes:
    w = _Writer()
    w.magic("ANIM")
    w.i32(a.version)
    w.i32(sum(len(fr.elements) for an in a.anims for fr in an.frames))
    w.i32(sum(len(an.frames) for an in a.anims))
    w.i32(len(a.anims))
    for anim in a.anims:
        w.kstr(anim.name)
        w.i32(anim.hash)
        w.f32(anim.rate)
        w.i32(len(anim.frames))
        for fr in anim.frames:
            for v in (fr.x, fr.y, fr.w, fr.h):
                w.f32(v)
            w.i32(len(fr.elements))
            for el in fr.elements:
                w.i32(el.image_hash)
                w.i32(el.index)
                w.i32(el.layer)
                w.i32(el.flags)
                for v in (el.a, el.bb, el.g, el.rr, *el.m, el.order):
                    w.f32(v)
    w.i32(a.max_vis_symbol_frames)
    w.i32(len(a.hashes))
    for h, s in a.hashes:
        w.i32(h)
        w.kstr(s)
    return w.out()


def roundtrip_ok(build_path: str, anim_path: str) -> bool:
    bd = open(build_path, "rb").read()
    ad = open(anim_path, "rb").read()
    return write_build(parse_build(bd)) == bd and write_anim(parse_anim(ad)) == ad


if __name__ == "__main__":
    import sys
    base = sys.argv[1] if len(sys.argv) > 1 else (
        "/mnt/win-d/Documents/ONI_Mods_Fork/BuildableNaturalTile/anim/assets/"
        "natural_tile/natural_tile")
    bp, ap = base + "_build.bytes", base + "_anim.bytes"
    b, a = parse_build(open(bp, "rb").read()), parse_anim(open(ap, "rb").read())
    print(f"BILD name={b.name!r} symbols={[s.hash for s in b.symbols]} "
          f"frames={b.frame_count} hashes={b.hashes}")
    print(f"ANIM v{a.version} anims={[(an.name, len(an.frames)) for an in a.anims]} "
          f"hashes={[s for _, s in a.hashes]}")
    print("build roundtrip:", write_build(b) == open(bp, "rb").read())
    print("anim  roundtrip:", write_anim(a) == open(ap, "rb").read())
    # verify hash function against the file's own table
    for h, s in b.hashes:
        assert klei_hash(s) == h, f"hash mismatch for {s!r}: {klei_hash(s)} != {h}"
    print("klei_hash matches all", len(b.hashes), "build strings")
