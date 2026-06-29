#!/usr/bin/env python3
"""Generate dedicated hanging/pendant ONI-style light concepts (ceiling-mounted).

Same Z-Image-Turbo + ONI-style recipe as gen_oni_lights.py, but every concept hangs
from a cord/chain at the TOP of the frame with the fixture below it, so the art anchors
to the ceiling cleanly. 16 concepts -> contact sheet for Josh to pick from.
"""
from __future__ import annotations

import json
import time
import urllib.request
import urllib.parse
import uuid
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

SERVER = "127.0.0.1:8188"
OUT_DIR = Path("/mnt/win-d/Pictures/test/oni_lights_hanging") / datetime.now().strftime("%Y%m%d_%H%M%S")

WIDTH, HEIGHT = 1024, 1024
STEPS, CFG = 8, 1.0
SEED_BASE = 880200

PREFIX = ("hand-drawn 2D video game item illustration, a single hanging pendant light "
          "fixture suspended from a cord or chain at the very top of the frame, ")
STYLE = (", the fixture hangs down below the cord, in the art style of Oxygen Not Included "
         "by Klei Entertainment, thick black ink outlines, painterly gouache shading, "
         "hand-painted cartoon game asset, soft warm glowing light, the single object "
         "centered in frame, plain solid off-white background, no text, clean simple "
         "background, sharp focus")

CONCEPTS = {
    "pendant_dome":      "an industrial metal dome pendant lamp on a cord with a warm glowing bulb beneath it",
    "glass_globe":       "a clear round glass globe pendant lamp hanging from a brass cord, glowing softly inside",
    "bare_edison":       "a single bare Edison filament bulb hanging from a long black cord, warm amber glow",
    "festoon_string":    "a drooping string of small round festoon party bulbs hanging in a swag, warm glow",
    "paper_lantern_hang":"a round red paper lantern hanging from a cord, glowing warmly from within",
    "caged_pendant":     "a caged industrial pendant lantern hanging from a chain, glowing bulb inside the cage",
    "crystal_pendant":   "a faceted glowing crystal hanging point-down from a thin chain, casting purple light",
    "jelly_pendant":     "a glowing bioluminescent jellyfish in a hanging glass teardrop on a cord, soft blue light",
    "terrarium_hang":    "a round glass terrarium globe with glowing moss hanging from a leather cord",
    "oil_lamp_hang":     "an ornate brass oil lamp hanging from a hook and chain, warm orange flame glow",
    "moroccan_lantern":  "an ornate pierced-metal Moroccan lantern hanging from a chain, warm patterned glow",
    "firefly_jar_hang":  "a mason jar of glowing fireflies hanging upside-down from a rope, soft yellow light",
    "plasma_pendant":    "a hanging glass orb pendant with purple plasma arcs inside, on a metal cord",
    "vine_bloom":        "a hanging vine with glowing flower-shaped bioluminescent blooms trailing down, soft green light",
    "starlight_orb":     "a frosted glass orb pendant projecting tiny points of starlight, hanging from a chain",
    "spiral_chandelier": "a small spiral tiered chandelier of glowing candle bulbs hanging from a chain",
}


def build_graph(prompt_text: str, seed: int) -> dict:
    g: dict = {
        "unet": {"class_type": "UNETLoader",
                 "inputs": {"unet_name": "z_image_turbo_bf16.safetensors", "weight_dtype": "default"}},
        "clip": {"class_type": "CLIPLoader",
                 "inputs": {"clip_name": "qwen_3_4b.safetensors", "type": "stable_diffusion", "device": "default"}},
        "vae": {"class_type": "VAELoader", "inputs": {"vae_name": "ae.safetensors"}},
        "sampling": {"class_type": "ModelSamplingAuraFlow", "inputs": {"model": ["unet", 0], "shift": 1.0}},
        "pos": {"class_type": "CLIPTextEncode",
                "inputs": {"clip": ["clip", 0], "text": PREFIX + prompt_text + STYLE}},
        "neg": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["clip", 0], "text": ""}},
        "latent": {"class_type": "EmptySD3LatentImage",
                   "inputs": {"width": WIDTH, "height": HEIGHT, "batch_size": 1}},
    }
    g["ksampler"] = {"class_type": "KSampler",
                     "inputs": {"model": ["sampling", 0], "positive": ["pos", 0], "negative": ["neg", 0],
                                "latent_image": ["latent", 0], "seed": seed, "steps": STEPS, "cfg": CFG,
                                "sampler_name": "euler", "scheduler": "simple", "denoise": 1.0}}
    g["decode"] = {"class_type": "VAEDecode", "inputs": {"samples": ["ksampler", 0], "vae": ["vae", 0]}}
    g["save"] = {"class_type": "SaveImage", "inputs": {"images": ["decode", 0], "filename_prefix": "oni_hang"}}
    return g


def submit(graph: dict, client_id: str) -> str:
    body = json.dumps({"prompt": graph, "client_id": client_id}).encode()
    req = urllib.request.Request(f"http://{SERVER}/prompt", data=body,
                                 headers={"Content-Type": "application/json"})
    return json.loads(urllib.request.urlopen(req).read())["prompt_id"]


def wait_images(prompt_id: str, timeout: float = 240) -> list[dict]:
    deadline = time.time() + timeout
    while time.time() < deadline:
        with urllib.request.urlopen(f"http://{SERVER}/history/{prompt_id}") as r:
            hist = json.loads(r.read())
        if prompt_id in hist:
            imgs: list[dict] = []
            for node in hist[prompt_id]["outputs"].values():
                imgs.extend(node.get("images", []))
            return imgs
        time.sleep(1.5)
    raise TimeoutError(f"prompt {prompt_id} did not finish in {timeout}s")


def fetch(img: dict) -> bytes:
    q = urllib.parse.urlencode({"filename": img["filename"], "subfolder": img.get("subfolder", ""),
                                "type": img.get("type", "output")})
    with urllib.request.urlopen(f"http://{SERVER}/view?{q}") as r:
        return r.read()


def wait_server(timeout: float = 300) -> None:
    deadline = time.time() + timeout
    while time.time() < deadline:
        try:
            urllib.request.urlopen(f"http://{SERVER}/system_stats", timeout=3).read()
            return
        except Exception:
            time.sleep(2)
    raise TimeoutError("ComfyUI server never came up")


def build_contact_sheet(saved: list[tuple[int, str, Path]], out: Path) -> None:
    cols, cell, pad, label_h = 4, 360, 14, 40
    rows = (len(saved) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cell + (cols + 1) * pad,
                              rows * (cell + label_h) + (rows + 1) * pad), (32, 32, 36))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype("/usr/share/fonts/TTF/DejaVuSans-Bold.ttf", 20)
    for idx, (n, slug, path) in enumerate(saved):
        r, c = divmod(idx, cols)
        x, y = pad + c * (cell + pad), pad + r * (cell + label_h + pad)
        sheet.paste(Image.open(path).convert("RGB").resize((cell, cell), Image.LANCZOS), (x, y))
        draw.text((x + 4, y + cell + 8), f"#{n}  {slug}", fill=(240, 240, 240), font=font)
    sheet.save(out)
    print(f"contact sheet -> {out}")


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    print(f"Output -> {OUT_DIR}\nWaiting for ComfyUI server...")
    wait_server()
    client_id = uuid.uuid4().hex
    saved: list[tuple[int, str, Path]] = []
    for i, (slug, text) in enumerate(CONCEPTS.items(), start=1):
        seed = SEED_BASE + i
        t0 = time.time()
        pid = submit(build_graph(text, seed), client_id)
        img = wait_images(pid)[0]
        dest = OUT_DIR / f"{i:02d}_{slug}_{seed}.png"
        dest.write_bytes(fetch(img))
        saved.append((i, slug, dest))
        print(f"  #{i:02d} {slug:18s} ({time.time() - t0:4.1f}s)")
    build_contact_sheet(saved, OUT_DIR / "_contact_sheet.png")


if __name__ == "__main__":
    main()
