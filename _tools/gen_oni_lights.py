#!/usr/bin/env python3
"""Generate 20 ONI-style decorative light-fixture prototypes for the Decor Lights mod.

Concept art for visual selection (Josh picks 4 to build into the mod). Style is
matched to Oxygen Not Included's hand-drawn 2D look: thick ink outlines, painterly
gouache shading, soft warm glow, single centered object on a plain background.

Z-Image-Turbo, no style LoRA, 8 steps, CFG-free (cfg=1). Harness mirrors
~/image_gen/gen_fit_anime.py (known-working CachyOS pattern).
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
OUT_DIR = Path("/mnt/win-d/Pictures/test/oni_lights") / datetime.now().strftime("%Y%m%d_%H%M%S")

WIDTH, HEIGHT = 1024, 1024
STEPS, CFG = 8, 1.0
SEED_BASE = 770100

PREFIX = ("hand-drawn 2D video game item illustration, a single decorative light fixture, ")
STYLE = (", in the art style of Oxygen Not Included by Klei Entertainment, thick black ink "
         "outlines, painterly gouache shading, hand-painted cartoon game asset, soft warm "
         "glowing light radiating from the fixture, the single object centered in frame, "
         "plain solid off-white background, no text, clean simple background, sharp focus")

# 20 distinct light-fixture concepts; key = short slug used in filename + contact-sheet label.
CONCEPTS = {
    "shinebug_jar":      "a glass mason jar full of glowing yellow shine bug critters on a small metal base",
    "paper_lantern":     "a round red paper lantern glowing warmly from within, hanging from a cord",
    "edison_cage":       "an industrial metal cage lamp around a warm amber Edison filament bulb on a bracket",
    "mushroom_cluster":  "a cluster of glowing bioluminescent blue mushrooms sprouting from a clay pot",
    "amethyst_geode":    "a glowing purple amethyst crystal geode lamp resting on a carved stone base",
    "plasma_globe":      "a tesla plasma globe, a glass sphere with purple electric arcs inside, on a round base",
    "hurricane_lantern": "an old brass hurricane oil lantern with a glowing orange flame behind glass",
    "jellyfish_jar":     "a glowing bioluminescent blue jellyfish floating inside a tall glass jar of water",
    "neon_coil":         "a coiled glowing pink neon tube sign lamp mounted on a small metal plate",
    "bubble_tube":       "a tall clear bubble tube lamp with rising bubbles glowing in shifting colors on a base",
    "crystal_chandelier":"a small ornate hanging crystal chandelier with warm glowing candle bulbs",
    "candelabra":        "a brass three-arm candelabra with lit dripping candles and warm flame glow",
    "terrarium_orb":     "a round glass terrarium orb full of moss and tiny glowing plants on a wooden stand",
    "wisp_orb":          "a floating glowing orb of soft white light, a will-o-wisp, hovering over a metal pedestal",
    "sputnik_atomic":    "a mid-century atomic sputnik lamp with radiating metal arms ending in glowing bulbs",
    "anglerfish":        "a deep-sea anglerfish-shaped lamp with a glowing dangling lure bulb above its head",
    "algae_column":      "a tall glass column tank filled with glowing green bioluminescent algae on a metal base",
    "salt_ring_tower":   "a stacked tower of carved Himalayan pink salt rings glowing warm orange from within",
    "star_projector":    "a domed star-projector lamp casting tiny points of starlight, glowing softly on a base",
    "lightning_bottle":  "a corked glass bottle containing crackling captured lightning, electric blue glow",
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
    g["save"] = {"class_type": "SaveImage", "inputs": {"images": ["decode", 0], "filename_prefix": "oni_light"}}
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
    sheet_w = cols * cell + (cols + 1) * pad
    sheet_h = rows * (cell + label_h) + (rows + 1) * pad
    sheet = Image.new("RGB", (sheet_w, sheet_h), (32, 32, 36))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("/usr/share/fonts/TTF/DejaVuSans-Bold.ttf", 20)
    except Exception:
        font = ImageFont.load_default()
    for idx, (n, slug, path) in enumerate(saved):
        r, c = divmod(idx, cols)
        x = pad + c * (cell + pad)
        y = pad + r * (cell + label_h + pad)
        im = Image.open(path).convert("RGB").resize((cell, cell), Image.LANCZOS)
        sheet.paste(im, (x, y))
        draw.text((x + 4, y + cell + 8), f"#{n}  {slug}", fill=(240, 240, 240), font=font)
    sheet.save(out)
    print(f"contact sheet -> {out}")


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    print(f"Output -> {OUT_DIR}")
    print("Waiting for ComfyUI server...")
    wait_server()
    client_id = uuid.uuid4().hex
    saved: list[tuple[int, str, Path]] = []
    for i, (slug, text) in enumerate(CONCEPTS.items(), start=1):
        seed = SEED_BASE + i
        t0 = time.time()
        pid = submit(build_graph(text, seed), client_id)
        imgs = wait_images(pid)
        img = imgs[0]
        dest = OUT_DIR / f"{i:02d}_{slug}_{seed}.png"
        dest.write_bytes(fetch(img))
        saved.append((i, slug, dest))
        print(f"  #{i:02d} {slug:18s} saved ({time.time() - t0:4.1f}s, seed {seed})")
    build_contact_sheet(saved, OUT_DIR / "_contact_sheet.png")


if __name__ == "__main__":
    main()
