import os
import math
import wave
import struct
import random
import hashlib

OUT_DIR = r"c:\LTG\Abyssal_Dungeon_3D\Assets\Resources\Audio"
os.makedirs(OUT_DIR, exist_ok=True)

SR = 44100


def ensure_wav_meta(wav_path):
    meta_path = wav_path + ".meta"
    if os.path.exists(meta_path):
        return
    rel = os.path.relpath(wav_path, r"c:\LTG\Abyssal_Dungeon_3D").replace("\\", "/")
    guid = hashlib.md5(rel.encode("utf-8")).hexdigest()
    meta_content = f"""fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 6
  defaultSettings:
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 0
    quality: 1
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8", newline="\n") as mf:
        mf.write(meta_content)


def write_wav(filename, samples):
    path = os.path.join(OUT_DIR, filename)
    peak = max(max(abs(s) for s in samples), 1e-5)
    norm = 0.92 / peak if peak > 0.92 else 1.0
    with wave.open(path, "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(SR)
        buf = bytearray()
        for s in samples:
            v = int(max(-1.0, min(1.0, s * norm)) * 32767.0)
            buf.extend(struct.pack("<h", v))
        wf.writeframes(bytes(buf))
    ensure_wav_meta(path)


def lowpass(samples, cutoff_hz):
    rc = 1.0 / (2.0 * math.pi * cutoff_hz)
    dt = 1.0 / SR
    alpha = dt / (rc + dt)
    out = [0.0] * len(samples)
    prev = 0.0
    for i, x in enumerate(samples):
        prev = prev + alpha * (x - prev)
        out[i] = prev
    return out


def highpass(samples, cutoff_hz):
    lp = lowpass(samples, cutoff_hz)
    return [x - y for x, y in zip(samples, lp)]


def bandpass(samples, low_hz, high_hz):
    return lowpass(highpass(samples, low_hz), high_hz)


def synth_stone_step(variant, is_land=False):
    rng = random.Random(1000 + variant * 97 + (500 if is_land else 0))
    dur = 0.24 if is_land else 0.16
    n = int(SR * dur)
    noise = [rng.uniform(-1.0, 1.0) for _ in range(n)]
    grit = bandpass(noise, 1200.0, 4600.0)
    slap = bandpass(noise, 400.0, 1400.0)

    out = [0.0] * n
    heel_f = 155.0 + variant * 11.0
    toe_delay = int(SR * (0.018 if is_land else (0.028 + variant * 0.004)))

    for i in range(n):
        t = i / SR
        # Heel impact: solid stone thud + sharp mineral click
        env_heel = math.exp(-t * (28.0 if is_land else 38.0))
        click_env = math.exp(-t * 140.0)
        thud = math.sin(2.0 * math.pi * (heel_f * math.exp(-t * 14.0)) * t) * env_heel * 0.65
        click = grit[i] * click_env * 0.85 + slap[i] * math.exp(-t * 55.0) * 0.55

        # Subtle toe scrape / secondary tap on stone slab
        toe = 0.0
        if i >= toe_delay:
            tt = (i - toe_delay) / SR
            toe_env = math.exp(-tt * 65.0)
            toe = (grit[i] * 0.55 + slap[i] * 0.45) * toe_env * (0.85 if is_land else 0.55)

        # Subtle cathedral stone room tail
        tail = slap[i] * math.exp(-t * 14.0) * 0.14
        out[i] = (thud + click + toe + tail) * (1.15 if is_land else 0.95)
    return out


def synth_wood_step(variant, is_land=False):
    rng = random.Random(2000 + variant * 131 + (500 if is_land else 0))
    dur = 0.28 if is_land else 0.20
    n = int(SR * dur)
    noise = [rng.uniform(-1.0, 1.0) for _ in range(n)]
    wood_knock_noise = bandpass(noise, 220.0, 950.0)
    bark_tap = bandpass(noise, 800.0, 2200.0)

    out = [0.0] * n
    # Hollow wooden plank resonance frequencies (fundamental + woody box modes)
    f1 = 128.0 + variant * 9.0
    f2 = f1 * 1.58  # woody hollow fifth/sixth mode
    f3 = f1 * 2.42  # plank knock overtone
    creak_f = 520.0 + variant * 45.0
    toe_delay = int(SR * (0.022 if is_land else (0.032 + variant * 0.004)))

    for i in range(n):
        t = i / SR
        # Hollow timber thump
        env_body = math.exp(-t * (19.0 if is_land else 25.0))
        env_knock = math.exp(-t * 52.0)
        pitch_drop = 1.0 + 0.35 * math.exp(-t * 45.0)

        plank_body = (
            0.72 * math.sin(2.0 * math.pi * f1 * pitch_drop * t)
            + 0.48 * math.sin(2.0 * math.pi * f2 * t)
            + 0.28 * math.sin(2.0 * math.pi * f3 * t) * env_knock
        ) * env_body

        knock = wood_knock_noise[i] * env_knock * 0.70 + bark_tap[i] * math.exp(-t * 95.0) * 0.45

        # Subtle wood plank creak / flex after step
        creak_env = math.sin(min(1.0, t / dur) * math.pi) * math.exp(-t * 14.0)
        creak_mod = math.sin(2.0 * math.pi * 18.0 * t) * 28.0
        creak = math.sin(2.0 * math.pi * (creak_f + creak_mod) * t) * creak_env * 0.16

        # Secondary plank rattle / toe tap
        toe = 0.0
        if i >= toe_delay:
            tt = (i - toe_delay) / SR
            toe = (
                0.45 * math.sin(2.0 * math.pi * (f1 * 1.15) * tt) * math.exp(-tt * 34.0)
                + wood_knock_noise[i] * math.exp(-tt * 65.0) * 0.50
            ) * (0.85 if is_land else 0.52)

        out[i] = (plank_body + knock + creak + toe) * (1.15 if is_land else 0.98)
    return out


def synth_metal_step(variant, is_land=False):
    rng = random.Random(3000 + variant * 157 + (500 if is_land else 0))
    dur = 0.25 if is_land else 0.18
    n = int(SR * dur)
    noise = [rng.uniform(-1.0, 1.0) for _ in range(n)]
    clink_noise = bandpass(noise, 1600.0, 5800.0)

    out = [0.0] * n
    base = 420.0 + variant * 28.0
    # Inharmonic metallic plate modes
    modes = [base, base * 1.47, base * 2.11, base * 2.89, base * 3.76]
    decays = [24.0, 32.0, 42.0, 55.0, 72.0]
    amps = [0.42, 0.35, 0.28, 0.22, 0.16]

    for i in range(n):
        t = i / SR
        ring = 0.0
        for m_f, dec, a in zip(modes, decays, amps):
            ring += a * math.sin(2.0 * math.pi * m_f * t) * math.exp(-t * dec)
        thump = 0.45 * math.sin(2.0 * math.pi * 145.0 * math.exp(-t * 16.0) * t) * math.exp(-t * 32.0)
        transient = clink_noise[i] * math.exp(-t * 110.0) * 0.78
        out[i] = (ring + thump + transient) * (1.15 if is_land else 0.92)
    return out


def synth_dirt_step(variant, is_land=False):
    rng = random.Random(4000 + variant * 173 + (500 if is_land else 0))
    dur = 0.22 if is_land else 0.17
    n = int(SR * dur)
    noise = [rng.uniform(-1.0, 1.0) for _ in range(n)]
    crunch = bandpass(noise, 550.0, 2900.0)
    rustle = bandpass(noise, 1800.0, 5200.0)

    out = [0.0] * n
    for i in range(n):
        t = i / SR
        u = i / n
        # Granular multi-grain envelope for gravel/moss/earth crunch
        grain = 0.55 + 0.45 * math.sin(2.0 * math.pi * (42.0 + variant * 6.0) * t) * math.sin(2.0 * math.pi * 115.0 * t)
        env = math.sin(min(1.0, u * 1.15) * math.pi) * math.exp(-t * (16.0 if is_land else 22.0))
        soft_thump = 0.48 * math.sin(2.0 * math.pi * (105.0 * math.exp(-t * 18.0)) * t) * math.exp(-t * 34.0)
        out[i] = (soft_thump + (crunch[i] * 0.75 + rustle[i] * 0.45) * grain * env) * (1.15 if is_land else 0.95)
    return out


def synth_crystal_step(variant, is_land=False):
    rng = random.Random(5000 + variant * 191 + (500 if is_land else 0))
    dur = 0.24 if is_land else 0.18
    n = int(SR * dur)
    noise = [rng.uniform(-1.0, 1.0) for _ in range(n)]
    splash = bandpass(noise, 900.0, 4800.0)

    out = [0.0] * n
    chime_f = 1180.0 + variant * 95.0
    for i in range(n):
        t = i / SR
        chime = (
            0.42 * math.sin(2.0 * math.pi * chime_f * t) * math.exp(-t * 28.0)
            + 0.26 * math.sin(2.0 * math.pi * (chime_f * 1.62) * t) * math.exp(-t * 42.0)
        )
        tap = splash[i] * math.exp(-t * 58.0) * 0.68
        thud = 0.38 * math.sin(2.0 * math.pi * 170.0 * math.exp(-t * 15.0) * t) * math.exp(-t * 36.0)
        out[i] = (chime + tap + thud) * (1.15 if is_land else 0.92)
    return out


def synth_weapon_sfx(wpn_type, is_skill=False):
    """Synthesizes distinctive weapon attack & skill sound effects for each of the 10 classes."""
    seed_map = {
        "sword": 101, "hammer": 202, "greataxe": 303, "dagger": 404, "bow": 505,
        "staff": 606, "scepter": 707, "gauntlet": 808, "scythe": 909, "mace": 1010
    }
    rng = random.Random(seed_map.get(wpn_type, 100) + (777 if is_skill else 0))
    dur = 0.42 if is_skill else 0.26
    n = int(SR * dur)
    noise = [rng.uniform(-1.0, 1.0) for _ in range(n)]
    out = [0.0] * n

    if wpn_type == "sword":
        # Crisp steel slash + high metallic ring
        air = bandpass(noise, 1100.0, 4800.0)
        edge = bandpass(noise, 3200.0, 7800.0)
        for i in range(n):
            t = i / SR
            u = i / n
            whoosh_env = math.sin(min(1.0, u * 1.4) * math.pi) * math.exp(-t * (6.5 if is_skill else 10.5))
            ring_f = 1480.0 if not is_skill else 1320.0
            ring = (
                0.36 * math.sin(2.0 * math.pi * ring_f * t) * math.exp(-t * 16.0)
                + 0.24 * math.sin(2.0 * math.pi * (ring_f * 1.54) * t) * math.exp(-t * 22.0)
            )
            slash = (air[i] * 0.65 + edge[i] * 0.55) * whoosh_env
            impact = 0.45 * math.sin(2.0 * math.pi * (240.0 * math.exp(-t * 14.0)) * t) * math.exp(-t * 18.0) if is_skill else 0.0
            out[i] = slash + ring + impact

    elif wpn_type == "hammer":
        # Heavy bronze/iron shield-hammer smash + low seismic thud
        crunch = bandpass(noise, 250.0, 2200.0)
        for i in range(n):
            t = i / SR
            sub_f = 135.0 * math.exp(-t * 10.0) + 42.0
            sub = 0.72 * math.sin(2.0 * math.pi * sub_f * t) * math.exp(-t * (7.5 if is_skill else 12.0))
            clang = (
                0.35 * math.sin(2.0 * math.pi * 540.0 * t) * math.exp(-t * 18.0)
                + 0.25 * math.sin(2.0 * math.pi * 830.0 * t) * math.exp(-t * 24.0)
            )
            hit = crunch[i] * math.exp(-t * (11.0 if is_skill else 18.0)) * 0.65
            out[i] = sub + clang + hit

    elif wpn_type == "greataxe":
        # Ferocious heavy cleaver roar + deep air tear
        tear = bandpass(noise, 320.0, 2600.0)
        grit = bandpass(noise, 1400.0, 4400.0)
        for i in range(n):
            t = i / SR
            u = i / n
            env = math.sin(min(1.0, u * 1.25) * math.pi) * math.exp(-t * (5.5 if is_skill else 8.8))
            sweep_f = 220.0 - 95.0 * u
            roar = 0.55 * math.sin(2.0 * math.pi * sweep_f * t + 0.6 * math.sin(2.0 * math.pi * 45.0 * t)) * env
            steel = 0.28 * math.sin(2.0 * math.pi * 920.0 * t) * math.exp(-t * 19.0)
            out[i] = roar + (tear[i] * 0.75 + grit[i] * 0.45) * env + steel

    elif wpn_type == "dagger":
        # Twin razor-fast high-velocity slices
        razor = bandpass(noise, 2600.0, 8600.0)
        for i in range(n):
            t = i / SR
            # First slice at t=0, second twin-slice at t=0.09s
            e1 = math.exp(-((t - 0.035) ** 2) / 0.0006)
            e2 = math.exp(-((t - 0.115) ** 2) / 0.0007) if is_skill else math.exp(-((t - 0.095) ** 2) / 0.0005) * 0.75
            ping1 = 0.35 * math.sin(2.0 * math.pi * 2450.0 * t) * e1
            ping2 = 0.35 * math.sin(2.0 * math.pi * 2880.0 * t) * e2
            out[i] = razor[i] * (e1 + e2) * 0.95 + ping1 + ping2

    elif wpn_type == "bow":
        # Taut bowstring twang + whistling arrow flight
        whistle = bandpass(noise, 1800.0, 6200.0)
        for i in range(n):
            t = i / SR
            # String pluck with pitch bend
            twang_f = 195.0 + 65.0 * math.exp(-t * 28.0)
            twang = (
                0.52 * math.sin(2.0 * math.pi * twang_f * t) * math.exp(-t * 16.0)
                + 0.28 * math.sin(2.0 * math.pi * (twang_f * 2.0) * t) * math.exp(-t * 24.0)
            )
            arrow_env = math.exp(-((t - 0.08) ** 2) / (0.008 if is_skill else 0.004))
            arrow_tone = 0.32 * math.sin(2.0 * math.pi * (1650.0 - 450.0 * (t / dur)) * t) * arrow_env
            out[i] = twang + whistle[i] * arrow_env * 0.75 + arrow_tone

    elif wpn_type == "staff":
        # Arcane elemental staff surge + crystalline crackle
        spark = bandpass(noise, 1400.0, 5600.0)
        for i in range(n):
            t = i / SR
            u = i / n
            env = math.sin(min(1.0, u * 1.3) * math.pi) * math.exp(-t * (5.2 if is_skill else 8.5))
            f_mod = 520.0 + (260.0 if is_skill else 140.0) * math.sin(2.0 * math.pi * 14.0 * t)
            orb = (
                0.48 * math.sin(2.0 * math.pi * f_mod * t)
                + 0.30 * math.sin(2.0 * math.pi * (f_mod * 1.498) * t)
            ) * env
            out[i] = orb + spark[i] * env * 0.52

    elif wpn_type == "scepter":
        # Dark Warlock void scepter pulse + eerie harmonic
        void_n = bandpass(noise, 380.0, 2100.0)
        for i in range(n):
            t = i / SR
            u = i / n
            env = math.sin(min(1.0, u * 1.2) * math.pi) * math.exp(-t * (5.5 if is_skill else 9.0))
            base_f = 260.0 - 70.0 * u
            dark_chord = (
                0.50 * math.sin(2.0 * math.pi * base_f * t)
                + 0.35 * math.sin(2.0 * math.pi * (base_f * 1.189) * t)  # minor third
                + 0.25 * math.sin(2.0 * math.pi * (base_f * 1.414) * t)  # tritone
            ) * env
            out[i] = dark_chord + void_n[i] * env * 0.55

    elif wpn_type == "gauntlet":
        # Monk rapid martial-arts air snap + chi shockwave thump
        snap = bandpass(noise, 900.0, 4200.0)
        for i in range(n):
            t = i / SR
            punch_f = 185.0 * math.exp(-t * 22.0) + 58.0
            punch = 0.75 * math.sin(2.0 * math.pi * punch_f * t) * math.exp(-t * (10.0 if is_skill else 16.0))
            chi = 0.34 * math.sin(2.0 * math.pi * 680.0 * math.exp(-t * 8.0) * t) * math.exp(-t * 14.0) if is_skill else 0.0
            crack = snap[i] * math.exp(-t * 32.0) * 0.82
            out[i] = punch + crack + chi

    elif wpn_type == "scythe":
        # Druid nature gale-wind sweep + resonant wooden/jade chime
        gale = bandpass(noise, 600.0, 3400.0)
        for i in range(n):
            t = i / SR
            u = i / n
            env = math.sin(min(1.0, u * 1.35) * math.pi) * math.exp(-t * (5.8 if is_skill else 9.2))
            chime = (
                0.38 * math.sin(2.0 * math.pi * 740.0 * t) * math.exp(-t * 12.0)
                + 0.26 * math.sin(2.0 * math.pi * 1110.0 * t) * math.exp(-t * 16.0)
            )
            wind = gale[i] * env * 0.78
            out[i] = wind + chime

    else:  # "mace" (Cleric)
        # Sacred flanged mace impact + golden cathedral bell harmonic
        strike = bandpass(noise, 700.0, 3800.0)
        for i in range(n):
            t = i / SR
            thud = 0.55 * math.sin(2.0 * math.pi * (160.0 * math.exp(-t * 16.0) + 50.0) * t) * math.exp(-t * 15.0)
            bell_f = 880.0 if not is_skill else 1046.5
            bell = (
                0.42 * math.sin(2.0 * math.pi * bell_f * t) * math.exp(-t * (7.0 if is_skill else 11.0))
                + 0.25 * math.sin(2.0 * math.pi * (bell_f * 1.5) * t) * math.exp(-t * 13.0)
                + 0.18 * math.sin(2.0 * math.pi * (bell_f * 2.0) * t) * math.exp(-t * 18.0)
            )
            out[i] = thud + bell + strike[i] * math.exp(-t * 28.0) * 0.48

    return out


def main():
    count = 0
    for v in range(1, 5):
        write_wav(f"sfx_step_stone_{v}.wav", synth_stone_step(v, False))
        write_wav(f"sfx_step_wood_{v}.wav", synth_wood_step(v, False))
        write_wav(f"sfx_step_metal_{v}.wav", synth_metal_step(v, False))
        write_wav(f"sfx_step_dirt_{v}.wav", synth_dirt_step(v, False))
        write_wav(f"sfx_step_crystal_{v}.wav", synth_crystal_step(v, False))
        count += 5

    write_wav("sfx_land_stone.wav", synth_stone_step(1, True))
    write_wav("sfx_land_wood.wav", synth_wood_step(1, True))
    write_wav("sfx_land_metal.wav", synth_metal_step(1, True))
    write_wav("sfx_land_dirt.wav", synth_dirt_step(1, True))
    write_wav("sfx_land_crystal.wav", synth_crystal_step(1, True))
    count += 5

    # Also update default sfx_footstep.wav
    write_wav("sfx_footstep.wav", synth_stone_step(1, False))
    count += 1

    # Generate 20 weapon-specific attack & skill SFX across all 10 classes/weapons
    weapon_types = [
        "sword", "hammer", "greataxe", "dagger", "bow",
        "staff", "scepter", "gauntlet", "scythe", "mace"
    ]
    for wpn in weapon_types:
        write_wav(f"sfx_wpn_{wpn}_attack.wav", synth_weapon_sfx(wpn, is_skill=False))
        write_wav(f"sfx_wpn_{wpn}_skill.wav", synth_weapon_sfx(wpn, is_skill=True))
        count += 2

    print(f"Generated {count} footstep & weapon WAV files in {OUT_DIR}")


if __name__ == "__main__":
    main()

