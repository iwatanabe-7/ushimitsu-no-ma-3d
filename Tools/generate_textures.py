#!/usr/bin/env python3
"""Generates the tileable surface textures used by SceneBuilder.

Every texture is synthesized from noise so the project keeps its "no external
assets" rule. All noise is built in the frequency domain (or wrapped modulo the
texture size), which makes every output seamlessly tileable.

Albedo maps are near-neutral detail: the hue comes from the material colour set
in SceneBuilder.CreateMaterials(), and each map is normalised to a fixed mean
brightness (see ALBEDO_MEAN) so the tint there can be computed as
`targetColour / mean` and the scene keeps its tuned overall palette.

Usage:  python3 Tools/generate_textures.py
Output: Assets/Textures/T_<Name>_Albedo.png and T_<Name>_Normal.png
"""

import os

import numpy as np
from PIL import Image

N = 512
OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Textures")

# Mean albedo brightness each map is normalised to. SceneBuilder mirrors these
# values when it converts a material colour into a texture tint.
ALBEDO_MEAN = {
    "Plaster": 0.5,
    "Wood": 0.5,
    "Tatami": 0.5,
    "TatamiEdge": 0.5,
    "Paper": 0.75,
    "Stone": 0.5,
}


# ---------------- Noise primitives ----------------

def freq_grid(n=N):
    f = np.fft.fftfreq(n) * n
    return np.meshgrid(f, f)  # (kx, ky): kx varies along columns (u), ky along rows (v)


def spectral(seed, beta=2.0, stretch_u=1.0, stretch_v=1.0, kmin=0.0, kmax=None):
    """Periodic 1/f^beta noise, normalised to roughly [0, 1].

    stretch_u > 1 suppresses variation along u, i.e. features elongate along u.
    """
    rng = np.random.default_rng(seed)
    white = rng.standard_normal((N, N))
    kx, ky = freq_grid()
    k = np.sqrt((kx * stretch_u) ** 2 + (ky * stretch_v) ** 2)
    k[0, 0] = 1.0
    amp = 1.0 / k ** beta
    amp[0, 0] = 0.0
    if kmin > 0:
        amp[k < kmin] = 0.0
    if kmax is not None:
        amp[k > kmax] = 0.0
    out = np.real(np.fft.ifft2(np.fft.fft2(white) * amp))
    return normalise(out)


def normalise(a, lo_pct=1.0, hi_pct=99.0):
    lo, hi = np.percentile(a, [lo_pct, hi_pct])
    return np.clip((a - lo) / (hi - lo + 1e-9), 0.0, 1.0)


def stamp_strands(img, seed, count, length, width, value, curl=0.02, angle=None, angle_jitter=np.pi):
    """Draws short wandering fibres, wrapping at the edges so the result tiles."""
    rng = np.random.default_rng(seed)
    for _ in range(count):
        x, y = rng.uniform(0, N, 2)
        a = rng.uniform(0, 2 * np.pi) if angle is None else angle + rng.uniform(-angle_jitter, angle_jitter)
        steps = int(rng.uniform(*length))
        v = value * rng.uniform(0.4, 1.0)
        w = rng.uniform(*width)
        for _ in range(steps):
            a += rng.normal(0.0, curl)
            x = (x + np.cos(a)) % N
            y = (y + np.sin(a)) % N
            r = int(np.ceil(w))
            for dy in range(-r, r + 1):
                for dx in range(-r, r + 1):
                    d = np.hypot(dx, dy)
                    if d <= w:
                        img[int(y + dy) % N, int(x + dx) % N] += v * (1.0 - d / (w + 1.0))
    return img


def stamp_dots(img, seed, count, radius, value):
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[-6:7, -6:7]
    for _ in range(count):
        cx, cy = rng.integers(0, N, 2)
        r = rng.uniform(*radius)
        v = value * rng.uniform(0.5, 1.0)
        mask = np.clip(1.0 - np.hypot(xx, yy) / r, 0.0, 1.0) ** 1.5
        ys = (cy + yy) % N
        xs = (cx + xx) % N
        img[ys, xs] += v * mask
    return img


# ---------------- Output ----------------

def height_to_normal(h, strength):
    # Image row 0 is the top of the PNG, which Unity maps to v = 1, so the v
    # gradient is the negated row gradient.
    du = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5 * strength
    dv = -(np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * 0.5 * strength
    n = np.stack([-du, -dv, np.ones_like(h)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n * 0.5 + 0.5


def save(name, albedo_rgb, height, normal_strength):
    os.makedirs(OUT_DIR, exist_ok=True)
    target = ALBEDO_MEAN[name]
    lum = albedo_rgb @ np.array([0.299, 0.587, 0.114])
    albedo_rgb = np.clip(albedo_rgb * (target / lum.mean()), 0.0, 1.0)
    Image.fromarray((albedo_rgb * 255 + 0.5).astype(np.uint8), "RGB").save(
        os.path.join(OUT_DIR, f"T_{name}_Albedo.png"), optimize=True)
    nrm = height_to_normal(height, normal_strength)
    Image.fromarray((nrm * 255 + 0.5).astype(np.uint8), "RGB").save(
        os.path.join(OUT_DIR, f"T_{name}_Normal.png"), optimize=True)
    final_mean = (albedo_rgb @ np.array([0.299, 0.587, 0.114])).mean()
    print(f"T_{name}: albedo mean {final_mean:.3f} (target {target})")


def tone(lum, warm=0.0):
    """Lift a luminance map into RGB with a slight per-channel shift."""
    return np.stack([lum * (1 + warm), lum, lum * (1 - warm)], axis=-1)


# ---------------- Surfaces ----------------

def plaster():
    # 土壁: cloudy trowelled clay with chopped straw (すさ) and grit.
    cloud = spectral(11, beta=1.6)
    fine = spectral(12, beta=0.6)
    fibres = stamp_strands(np.zeros((N, N)), 13, 650, (6, 22), (0.5, 0.9), 1.0, curl=0.15)
    fibres = np.clip(fibres, 0, 1)
    grit = np.clip(stamp_dots(np.zeros((N, N)), 14, 2500, (0.8, 1.8), 1.0), 0, 1)

    lum = 0.5 + 0.16 * (cloud - 0.5) + 0.07 * (fine - 0.5) + 0.22 * fibres - 0.12 * grit
    height = 0.6 * cloud + 0.25 * fine + 0.5 * fibres + 0.35 * grit
    save("Plaster", tone(lum, 0.03), height, 3.0)


def wood():
    # Plain-sawn grain running along u: growth rings that drift and wander
    # (板目), long pore streaks, and broad tonal variation between boards.
    warp = spectral(21, beta=2.8, stretch_u=3.0)
    drift = spectral(25, beta=3.2, stretch_u=1.5)
    rings_cycles = 12  # whole number so the pattern tiles in v
    v = np.mgrid[0:N, 0:N][0] / N
    phase = 2 * np.pi * (v * rings_cycles + warp * 1.3 + drift * 0.9)
    rings = 0.5 + 0.5 * np.sin(phase)
    latewood = rings ** 3
    edge = np.clip(1.0 - np.abs(rings - 0.85) * 8.0, 0, 1)  # crisp boundary on one side of each band
    streaks = spectral(22, beta=1.1, stretch_u=14.0)
    pores = spectral(23, beta=0.3, stretch_u=6.0)
    variation = spectral(24, beta=2.2)

    lum = (0.52 + 0.16 * (streaks - 0.5) + 0.10 * (variation - 0.5)
           - 0.22 * latewood - 0.06 * edge - 0.05 * (pores > 0.82))
    height = 0.5 * streaks - 0.4 * latewood - 0.2 * edge - 0.25 * (pores > 0.82)
    save("Wood", tone(lum, 0.06), height, 2.5)


def tatami():
    # 畳表: rush strands run across the mat (u = the mat's long side, so each
    # strand is a column), bound by warp threads that leave faint grooves.
    u, v = np.mgrid[0:N, 0:N][1] / N, np.mgrid[0:N, 0:N][0] / N
    strands = 192
    profile = 0.5 + 0.5 * np.cos(2 * np.pi * u * strands)  # rounded strand cross-section
    rng = np.random.default_rng(31)
    per_strand = rng.uniform(-1, 1, strands)[(u * strands).astype(int) % strands]
    along = spectral(32, beta=1.2, stretch_v=0.15, stretch_u=4.0)  # varies along each strand (v)
    threads = 26
    groove = np.clip(1.0 - np.abs(((v * threads) % 1.0) - 0.5) * 14.0, 0, 1)
    sheen = spectral(33, beta=2.4)

    lum = (0.5 + 0.07 * per_strand + 0.08 * (along - 0.5) + 0.10 * (profile - 0.5)
           - 0.045 * groove + 0.06 * (sheen - 0.5))
    height = 0.7 * profile - 0.45 * groove + 0.1 * along
    rgb = tone(lum, 0.0)
    rgb[..., 1] *= 1.0 + 0.04 * per_strand  # a few greener / yellower strands
    save("Tatami", rgb, height, 4.0)


def tatami_edge():
    # 畳縁: tight cotton/linen weave.
    u, v = np.mgrid[0:N, 0:N][1] / N, np.mgrid[0:N, 0:N][0] / N
    f = 96
    warp = 0.5 + 0.5 * np.sin(2 * np.pi * u * f)
    weft = 0.5 + 0.5 * np.sin(2 * np.pi * v * f)
    checker = np.sign(np.sin(np.pi * u * f)) * np.sign(np.sin(np.pi * v * f))
    weave = np.where(checker > 0, warp, weft)
    wear = spectral(41, beta=1.8)
    lum = 0.5 + 0.12 * (weave - 0.5) + 0.12 * (wear - 0.5)
    save("TatamiEdge", tone(lum, 0.0), weave + 0.3 * wear, 2.0)


def paper():
    # 和紙: soft cloudy pulp with long kozo fibres.
    cloud = spectral(51, beta=1.4)
    pulp = spectral(52, beta=0.7)
    fibres = stamp_strands(np.zeros((N, N)), 53, 260, (30, 120), (0.4, 0.8), 1.0, curl=0.06)
    fibres = np.clip(fibres, 0, 1)
    lum = 0.75 + 0.06 * (cloud - 0.5) + 0.03 * (pulp - 0.5) + 0.10 * fibres
    height = 0.4 * cloud + 0.3 * pulp + 0.6 * fibres
    save("Paper", tone(lum, 0.015), height, 1.2)


def stone():
    # 三和土: tamped earth with embedded pebbles and grit.
    base = spectral(61, beta=1.5)
    fine = spectral(62, beta=0.5)
    pebbles_dark = np.clip(stamp_dots(np.zeros((N, N)), 63, 900, (1.5, 4.5), 1.0), 0, 1)
    pebbles_light = np.clip(stamp_dots(np.zeros((N, N)), 64, 700, (1.0, 3.5), 1.0), 0, 1)
    lum = 0.5 + 0.14 * (base - 0.5) + 0.10 * (fine - 0.5) - 0.22 * pebbles_dark + 0.22 * pebbles_light
    height = 0.4 * base + 0.3 * fine + 0.7 * pebbles_dark + 0.7 * pebbles_light
    save("Stone", tone(lum, 0.02), height, 3.0)


if __name__ == "__main__":
    plaster()
    wood()
    tatami()
    tatami_edge()
    paper()
    stone()
