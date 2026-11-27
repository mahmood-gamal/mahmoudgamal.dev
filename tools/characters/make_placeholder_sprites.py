#!/usr/bin/env python3
"""Draws the placeholder character sprite sheet and its SpriteFrames resource.

Usage: python3 tools/characters/make_placeholder_sprites.py   (needs Pillow)

Writes assets/characters/placeholder/placeholder_sheet.png and
assets/characters/placeholder/placeholder_frames.tres. One row per animation,
64x96 px frames, feet on the bottom pixel row. See docs/characters.md.
"""
from pathlib import Path

from PIL import Image, ImageDraw

FRAME_W, FRAME_H = 64, 96
OUT = Path(__file__).resolve().parents[2] / "assets/characters/placeholder"

# name, frame count, frames per second, loops
ANIMATIONS = [("idle", 4, 4.0, True), ("walk", 6, 10.0, True), ("attack", 4, 10.0, False)]

OUTLINE = (40, 28, 24, 255)
SKIN = (240, 200, 160, 255)
HAIR = (120, 70, 40, 255)
TUNIC = (200, 60, 50, 255)
TUNIC_DARK = (150, 40, 35, 255)
PANTS = (70, 70, 110, 255)
BOOTS = (80, 50, 30, 255)
BLADE = (210, 215, 225, 255)


def rect(d, x0, y0, x1, y1, fill):
    d.rectangle([x0, y0, x1, y1], fill=fill, outline=OUTLINE)


def draw_frame(d, ox, anim, i):
    cx = ox + FRAME_W // 2
    bob = 0
    left_leg = right_leg = 0  # forward (-) / back (+) step offsets in px
    arm = 0  # 0 = rest, 1..3 = swing stages

    if anim == "idle":
        bob = [0, 1, 2, 1][i]
    elif anim == "walk":
        step = [-6, -3, 0, 6, 3, 0][i]
        left_leg, right_leg = step, -step
        bob = [0, 1, 2, 0, 1, 2][i]
    elif anim == "attack":
        arm = i  # wind up, raise, strike, recover

    floor = FRAME_H - 1
    # Legs and boots.
    for leg_x, off in ((cx - 9, left_leg), (cx + 2, right_leg)):
        rect(d, leg_x + off, floor - 22, leg_x + off + 7, floor - 6, PANTS)
        rect(d, leg_x + off - 1, floor - 6, leg_x + off + 8, floor, BOOTS)

    top = floor - 22 - 26 + bob
    # Body.
    rect(d, cx - 13, top, cx + 13, floor - 20 + bob // 2, TUNIC)
    d.line([cx - 13, top + 14, cx + 13, top + 14], fill=TUNIC_DARK, width=2)
    # Head.
    head_top = top - 26
    d.ellipse([cx - 13, head_top, cx + 13, top + 2], fill=SKIN, outline=OUTLINE)
    d.chord([cx - 14, head_top - 2, cx + 14, top - 6], 180, 360, fill=HAIR, outline=OUTLINE)
    d.rectangle([cx - 6, head_top + 15, cx - 3, head_top + 18], fill=OUTLINE)
    d.rectangle([cx + 3, head_top + 15, cx + 6, head_top + 18], fill=OUTLINE)

    # Left arm at rest.
    rect(d, cx - 19, top + 2, cx - 14, top + 20, SKIN)
    # Right arm and sword.
    hx, hy = cx + 17, top + 20
    if arm == 0:
        rect(d, cx + 14, top + 2, cx + 19, top + 20, SKIN)
        tip = (hx + 2, hy - 26)
    elif arm == 1:  # wind up behind the head
        rect(d, cx + 12, top - 10, cx + 18, top + 4, SKIN)
        hx, hy = cx + 15, top - 10
        tip = (hx + 18, hy - 18)
    elif arm == 2:  # overhead
        rect(d, cx + 6, top - 16, cx + 12, top + 2, SKIN)
        hx, hy = cx + 9, top - 16
        tip = (hx - 4, hy - 26)
    else:  # strike forward and down
        rect(d, cx + 14, top + 4, cx + 26, top + 10, SKIN)
        hx, hy = cx + 26, top + 7
        tip = (hx + 4, hy + 24)
    d.line([hx, hy, *tip], fill=OUTLINE, width=5)
    d.line([hx, hy, *tip], fill=BLADE, width=3)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    columns = max(count for _, count, _, _ in ANIMATIONS)
    sheet = Image.new("RGBA", (FRAME_W * columns, FRAME_H * len(ANIMATIONS)), (0, 0, 0, 0))
    for row, (name, count, _, _) in enumerate(ANIMATIONS):
        for i in range(count):
            frame = Image.new("RGBA", (FRAME_W, FRAME_H), (0, 0, 0, 0))
            draw_frame(ImageDraw.Draw(frame), 0, name, i)
            sheet.paste(frame, (i * FRAME_W, row * FRAME_H))
    sheet.save(OUT / "placeholder_sheet.png")

    subs, anims = [], []
    for row, (name, count, fps, loop) in enumerate(ANIMATIONS):
        frames = []
        for i in range(count):
            sid = f"AtlasTexture_{name}_{i}"
            subs.append(
                f'[sub_resource type="AtlasTexture" id="{sid}"]\n'
                f'atlas = ExtResource("1_sheet")\n'
                f"region = Rect2({i * FRAME_W}, {row * FRAME_H}, {FRAME_W}, {FRAME_H})\n"
            )
            frames.append(f'{{\n"duration": 1.0,\n"texture": SubResource("{sid}")\n}}')
        anims.append(
            "{\n"
            f'"frames": [{", ".join(frames)}],\n'
            f'"loop": {"true" if loop else "false"},\n'
            f'"name": &"{name}",\n'
            f'"speed": {fps}\n'
            "}"
        )
    tres = (
        f'[gd_resource type="SpriteFrames" load_steps={len(subs) + 2} format=3]\n\n'
        '[ext_resource type="Texture2D" path="res://assets/characters/placeholder/placeholder_sheet.png" id="1_sheet"]\n\n'
        + "\n".join(subs)
        + "\n[resource]\n"
        + f"animations = [{', '.join(anims)}]\n"
    )
    (OUT / "placeholder_frames.tres").write_text(tres)


if __name__ == "__main__":
    main()
