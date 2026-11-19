"""SD-01 (#44) BGM / SE generator for team5.

All sounds are synthesized from scratch with numpy (no samples), so they are
original works of the team and can live in the public repository.

Usage:  python3 gen_audio.py OUT_DIR
Writes OUT_DIR/BGM/*.wav (then convert to OGG with ffmpeg) and OUT_DIR/SE/*.wav.
"""
import sys, os, wave
import numpy as np

SR = 44100
rng = np.random.default_rng(44)


# ---------------------------------------------------------------- basics
def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


NOTE = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def nn(name):
    """'C5' -> 72, 'Eb5' -> 75, 'F#4' -> 66"""
    base = NOTE[name[0]]
    i = 1
    while name[i] in "b#":
        base += -1 if name[i] == "b" else 1
        i += 1
    return base + 12 * (int(name[i:]) + 1)


def t_axis(dur):
    return np.arange(int(dur * SR)) / SR


def smooth(x, n):
    if n <= 1:
        return x
    k = np.ones(n) / n
    return np.convolve(x, k, mode="same")


def fade_edges(x, a=0.003, r=0.01):
    na, nr = int(a * SR), int(r * SR)
    x = x.copy()
    if na:
        x[:na] *= np.linspace(0, 1, na)
    if nr:
        x[-nr:] *= np.linspace(1, 0, nr)
    return x


# ---------------------------------------------------------------- instruments
def marimba(f, dur, vel=1.0):
    t = t_axis(max(dur, 0.6) + 0.4)
    y = (np.sin(2 * np.pi * f * t) * np.exp(-t * 5)
         + 0.35 * np.sin(2 * np.pi * f * 4 * t) * np.exp(-t * 22)
         + 0.12 * np.sin(2 * np.pi * f * 9.2 * t) * np.exp(-t * 60))
    return fade_edges(y * vel)


def bell(f, dur, vel=1.0):
    t = t_axis(max(dur, 0.8) + 0.8)
    y = (np.sin(2 * np.pi * f * t) * np.exp(-t * 2.5)
         + 0.4 * np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t * 6)
         + 0.2 * np.sin(2 * np.pi * f * 5.4 * t) * np.exp(-t * 12))
    return fade_edges(y * vel)


def pluck(f, dur, vel=1.0, bright=1.0, decay=4.0):
    t = t_axis(dur + 0.25)
    y = np.zeros_like(t)
    for k in range(1, 9):
        if f * k > 9000:
            break
        y += (bright ** (k - 1)) / k * np.sin(2 * np.pi * f * k * t) * np.exp(-t * (decay + 2.5 * k))
    env = np.ones_like(t)
    rel = int(0.06 * SR)
    end = int(dur * SR)
    if end + rel < len(env):
        env[end:end + rel] = np.linspace(1, 0, rel)
        env[end + rel:] = 0
    return fade_edges(y * env * vel, a=0.002)


def bass(f, dur, vel=1.0):
    t = t_axis(dur + 0.08)
    y = np.sin(2 * np.pi * f * t) + 0.3 * np.sin(2 * np.pi * 2 * f * t) + 0.1 * np.sin(2 * np.pi * 3 * f * t)
    env = np.exp(-t * 1.5)
    end = int(dur * SR)
    env[end:] *= np.linspace(1, 0, len(env) - end)
    return fade_edges(y * env * vel, a=0.006)


def pad(f, dur, vel=1.0):
    t = t_axis(dur + 0.3)
    y = np.zeros_like(t)
    for det in (-0.004, 0.0, 0.004):
        ff = f * (1 + det)
        y += np.sin(2 * np.pi * ff * t) + 0.25 * np.sin(2 * np.pi * 2 * ff * t) + 0.1 * np.sin(2 * np.pi * 3 * ff * t)
    env = np.minimum(1, t / 0.25)
    end = int(dur * SR)
    env[end:] *= np.linspace(1, 0, len(env) - end)
    return y * env * vel / 3


def shaker(vel=1.0):
    t = t_axis(0.07)
    n = rng.standard_normal(len(t))
    n = n - smooth(n, 6)  # crude high-pass
    return n * np.exp(-t * 60) * vel * 0.5


def kick(vel=1.0):
    t = t_axis(0.25)
    f = 45 + 80 * np.exp(-t * 30)
    ph = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(ph) * np.exp(-t * 14) * vel


def tick(vel=1.0, f=3200):
    t = t_axis(0.03)
    return np.sin(2 * np.pi * f * t) * np.exp(-t * 220) * vel


def woodblock(f=800, vel=1.0):
    t = t_axis(0.12)
    return (np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 2.3 * t)) * np.exp(-t * 45) * vel


# ---------------------------------------------------------------- mixing
class Mix:
    def __init__(self, length_s, tail_s=3.0):
        self.n = int(length_s * SR)
        self.buf = np.zeros((self.n + int(tail_s * SR), 2))

    def add(self, sig, at_s, gain=1.0, pan=0.0):
        i = int(round(at_s * SR))
        if i >= len(self.buf):
            return
        j = min(len(self.buf), i + len(sig))
        l = np.cos((pan + 1) * np.pi / 4)
        r = np.sin((pan + 1) * np.pi / 4)
        s = sig[: j - i] * gain
        self.buf[i:j, 0] += s * l * 1.414
        self.buf[i:j, 1] += s * r * 1.414

    def render_loop(self):
        """Wrap the tail onto the start so the loop is seamless."""
        out = self.buf[: self.n].copy()
        tail = self.buf[self.n:]
        k = min(len(tail), self.n)
        out[:k] += tail[:k]
        return master(out)

    def render_oneshot(self):
        out = self.buf.copy()
        nz = np.nonzero(np.abs(out).max(axis=1) > 1e-4)[0]
        out = out[: nz[-1] + int(0.05 * SR)] if len(nz) else out
        return master(fade_edges_2(out))


def fade_edges_2(x, r=0.2):
    n = int(r * SR)
    x = x.copy()
    x[-n:] *= np.linspace(1, 0, n)[:, None]
    return x


def master(x, peak_db=-1.0):
    x = np.tanh(x / (np.abs(x).max() + 1e-9) * 1.3)  # gentle saturation
    x = x / np.abs(x).max() * 10 ** (peak_db / 20)
    return x


def write_wav(path, x):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if x.ndim == 1:
        x = x[:, None]
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(x.shape[1])
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


# ---------------------------------------------------------------- the theme
# One village theme in C major. Ura uses the SAME bars/tempo/length in C minor,
# so the game can cross-fade omote <-> ura at the same playback position.
MEL_A = [
    "E5 .5 G5 .5 C6 1 G5 .5 E5 .5 G5 1",
    "A5 1 G5 .5 E5 .5 C5 1 r 1",
    "F5 .5 A5 .5 C6 1 A5 .5 F5 .5 A5 1",
    "G5 1.5 F5 .5 E5 1 D5 1",
    "E5 .5 G5 .5 C6 1 D6 .5 C6 .5 G5 1",
    "A5 .5 C6 .5 A5 1 G5 .5 E5 .5 C5 1",
    "D5 .5 F5 .5 A5 1 G5 .5 B5 .5 D6 1",
    "C6 2 r 2",
]
MEL_B = [
    "A5 1 C6 .5 A5 .5 F5 1 A5 1",
    "B5 1 D6 .5 B5 .5 G5 2",
    "G5 1 B5 .5 G5 .5 E5 1 G5 1",
    "A5 1.5 G5 .5 E5 2",
    "F5 .5 G5 .5 A5 .5 C6 .5 A5 1 F5 1",
    "G5 .5 A5 .5 B5 .5 D6 .5 B5 1 G5 1",
    "F5 1 E5 1 D5 1 F5 1",
    "G5 2 B4 .5 C5 .5 D5 1",
]
# chords per bar: list of (beat, root, quality)
CH_A = [[(0, "C", "")], [(0, "A", "m")], [(0, "F", "")], [(0, "G", "")],
        [(0, "C", "")], [(0, "A", "m")], [(0, "D", "m"), (2, "G", "")], [(0, "C", "")]]
CH_B = [[(0, "F", "")], [(0, "G", "")], [(0, "E", "m")], [(0, "A", "m")],
        [(0, "F", "")], [(0, "G", "")], [(0, "D", "m")], [(0, "G", "")]]

# omote -> ura (C major -> C minor, G stays major for tension)
URA_NOTE = {"E": "Eb", "A": "Ab", "B": "B"}
URA_CHORD = {("C", ""): ("C", "m"), ("A", "m"): ("Ab", ""), ("F", ""): ("F", "m"),
             ("G", ""): ("G", ""), ("D", "m"): ("D", "dim"), ("E", "m"): ("Eb", "")}
QUAL = {"": [0, 4, 7], "m": [0, 3, 7], "dim": [0, 3, 6]}


def parse_bar(s, minor=False):
    tok = s.split()
    out, beat = [], 0.0
    for i in range(0, len(tok), 2):
        name, d = tok[i], float(tok[i + 1])
        if name != "r":
            if minor and name[0] in URA_NOTE and name[1] not in "b#":
                name = URA_NOTE[name[0]] + name[1:]
            out.append((beat, nn(name), d))
        beat += d
    return out


def chord_notes(root, q, octave=4):
    r = nn(root + str(octave))
    return [r + i for i in QUAL[q]]


def theme(minor, bpm, sections, lead, arrangement):
    spb = 60 / bpm
    bars_mel = []
    bars_ch = []
    for sec in sections:
        bars_mel += MEL_A if sec == "A" else MEL_B
        bars_ch += CH_A if sec == "A" else CH_B
    nbars = len(bars_mel)
    m = Mix(nbars * 4 * spb)
    for b in range(nbars):
        t0 = b * 4 * spb
        # melody
        for beat, note, d in parse_bar(bars_mel[b], minor):
            lead(m, t0 + beat * spb, note, d * spb)
        # chords
        chs = bars_ch[b]
        for ci, (cb, root, q) in enumerate(chs):
            if minor:
                root, q = URA_CHORD[(root, q)]
            cend = chs[ci + 1][0] if ci + 1 < len(chs) else 4
            arrangement(m, t0, spb, cb, cend, root, q, b)
    return m


# ---- arrangements
def omote_lead(m, t, note, d):
    m.add(marimba(midi(note), d), t, 0.55, 0.0)


def omote_acc(m, t0, spb, cb, cend, root, q, bar):
    ns = chord_notes(root, q, 4)
    broot = nn(root + "2")
    # bass: root on chord start, fifth halfway
    m.add(bass(midi(broot), spb * 1.4), t0 + cb * spb, 0.5, -0.1)
    if cend - cb >= 4:
        m.add(bass(midi(broot + 7), spb * 1.4), t0 + (cb + 2) * spb, 0.42, -0.1)
    # ukulele-ish off-beat strums
    for beat in np.arange(cb, cend, 1.0):
        for k, n in enumerate(ns):
            m.add(pluck(midi(n), spb * 0.35, bright=0.55), t0 + (beat + 0.5) * spb + k * 0.008, 0.13, -0.35)
    # drums
    for beat in np.arange(cb, cend, 1.0):
        if beat % 2 == 0:
            m.add(kick(), t0 + beat * spb, 0.35)
        m.add(shaker(), t0 + beat * spb, 0.10, 0.3)
        m.add(shaker(), t0 + (beat + 0.5) * spb, 0.16, 0.3)


def ura_lead(m, t, note, d):
    # pizzicato-ish pluck, an octave lower, short notes = sneaking
    m.add(pluck(midi(note - 12), min(d, 0.5) * 0.6, bright=0.75, decay=7), t, 0.55, 0.0)
    m.add(marimba(midi(note), d, 0.25), t, 0.15, 0.2)


def ura_acc(m, t0, spb, cb, cend, root, q, bar):
    ns = chord_notes(root, q, 3)
    broot = nn(root + "2")
    # staccato walking bass on every beat: root, root, fifth, root
    pat = [0, 0, 7, 0]
    for beat in np.arange(cb, cend, 1.0):
        m.add(bass(midi(broot + pat[int(beat) % 4]), spb * 0.35), t0 + beat * spb, 0.55, -0.1)
    # soft sustained pad
    m.add(pad(midi(ns[0] + 12), (cend - cb) * spb), t0 + cb * spb, 0.05, -0.4)
    m.add(pad(midi(ns[1] + 12), (cend - cb) * spb), t0 + cb * spb, 0.05, 0.4)
    # clock ticks on 8ths, accent on beat
    for beat in np.arange(cb, cend, 0.5):
        m.add(tick(1.0 if beat % 1 == 0 else 0.5), t0 + beat * spb, 0.10, 0.45)
    if cb == 0:
        m.add(kick(), t0, 0.28)


def title_lead(m, t, note, d):
    m.add(bell(midi(note), d), t, 0.40, 0.1)
    m.add(marimba(midi(note - 12), d, 0.6), t, 0.22, -0.1)


def title_acc(m, t0, spb, cb, cend, root, q, bar):
    ns = chord_notes(root, q, 4)
    broot = nn(root + "2")
    m.add(bass(midi(broot), (cend - cb) * spb * 0.9), t0 + cb * spb, 0.35)
    m.add(pad(midi(ns[0]), (cend - cb) * spb), t0 + cb * spb, 0.07, -0.4)
    m.add(pad(midi(ns[1]), (cend - cb) * spb), t0 + cb * spb, 0.07, 0.4)
    m.add(pad(midi(ns[2]), (cend - cb) * spb), t0 + cb * spb, 0.07, 0.0)
    # gentle arpeggio
    for i, beat in enumerate(np.arange(cb, cend, 0.5)):
        n = ns[[0, 1, 2, 1][i % 4]]
        m.add(pluck(midi(n), spb * 0.4, bright=0.4), t0 + beat * spb, 0.10, -0.3)


def jingle(notes, chord, bpm, lead_fn, minor=False):
    spb = 60 / bpm
    m = Mix(8, tail_s=4)
    t = 0.0
    for name, d in notes:
        if name != "r":
            lead_fn(m, t, nn(name), d * spb)
        t += d * spb
    end = t
    for k, n in enumerate(chord):
        m.add(pad(midi(nn(n)), 2.5), end - spb, 0.12, (-0.4, 0.0, 0.4)[k % 3])
        m.add(pluck(midi(nn(n)), 1.5, bright=0.6, decay=2), end - spb + k * 0.02, 0.18, (-0.4, 0.0, 0.4)[k % 3])
    m.add(bass(midi(nn(chord[0]) - 12), 2.0), end - spb, 0.5)
    return m.render_oneshot()


# ---------------------------------------------------------------- SE
def se_out(sig):
    sig = fade_edges(sig, a=0.001, r=0.02)
    return sig / np.abs(sig).max() * 10 ** (-1 / 20)


def seq(parts, length):
    out = np.zeros(int(length * SR) + SR)
    for at, sig, g in parts:
        i = int(at * SR)
        out[i:i + len(sig)] += sig[: len(out) - i] * g
    nz = np.nonzero(np.abs(out) > 1e-4)[0]
    return out[: nz[-1] + 1]


def glide(f0, f1, dur, shape="sine", decay=6):
    t = t_axis(dur)
    f = f0 * (f1 / f0) ** (t / dur)
    ph = 2 * np.pi * np.cumsum(f) / SR
    y = np.sin(ph) + (0.3 * np.sin(2 * ph) if shape == "rich" else 0)
    return y * np.exp(-t * decay) * np.minimum(1, t / 0.005)


def noise_sweep(dur, lo=4, hi=40):
    t = t_axis(dur)
    n = rng.standard_normal(len(t))
    out = np.zeros_like(n)
    # crude band sweep: mix of smoothed noise with changing window
    for k, w in enumerate(np.linspace(hi, lo, 8).astype(int)):
        seg = slice(int(k * len(t) / 8), int((k + 1) * len(t) / 8))
        out[seg] = (n - smooth(n, max(w, 2)))[seg]
    env = np.sin(np.pi * t / dur) ** 2
    return out * env


def make_se():
    se = {}
    se["SE_Pickup"] = seq([(0, bell(midi(nn("E6")), 0.1), 0.6), (0.07, bell(midi(nn("B6")), 0.2), 0.7)], 0.5)[: int(0.45 * SR)]
    se["SE_Craft"] = seq([(i * 0.14, woodblock(520 + 60 * i), 1.0) for i in range(3)] + [(i * 0.14, shaker(), 0.4) for i in range(3)], 0.6)
    se["SE_Crafted"] = seq([(i * 0.06, bell(midi(nn(n)), 0.3), 0.6) for i, n in enumerate(["C6", "E6", "G6", "C7", "E7"])]
                           + [(0.3, noise_sweep(0.5, 2, 6), 0.08)], 1.4)[: int(1.2 * SR)]
    caught_t = t_axis(0.5)
    vib = 1 + 0.03 * np.sin(2 * np.pi * 14 * caught_t)
    f = np.where(caught_t < 0.16, midi(nn("A5")), midi(nn("D5"))) * vib
    ph = 2 * np.pi * np.cumsum(f) / SR
    caught = (np.sin(ph) + 0.35 * np.sin(3 * ph) + 0.15 * np.sin(5 * ph)) * np.exp(-np.maximum(caught_t - 0.16, 0) * 5)
    se["SE_Caught"] = seq([(0, caught, 1.0), (0, woodblock(300), 0.5)], 0.6)
    se["SE_TaskComplete"] = seq([(0.00, marimba(midi(nn("G5")), 0.1), 0.6), (0.10, marimba(midi(nn("C6")), 0.1), 0.6),
                                 (0.20, marimba(midi(nn("E6")), 0.3), 0.7)]
                                + [(0.20 + k * 0.01, pluck(midi(nn(n)), 0.6, bright=0.6, decay=3), 0.25) for k, n in enumerate(["C5", "E5", "G5"])], 1.2)[: int(1.0 * SR)]
    se["SE_TaskFail"] = seq([(0.0, pluck(midi(nn("E4")), 0.18, bright=0.9, decay=3), 0.8),
                             (0.22, pluck(midi(nn("Eb4")) * 0.985, 0.35, bright=0.9, decay=3), 0.8)], 0.8)
    se["SE_Equip"] = seq([(0, noise_sweep(0.25), 0.25), (0.15, bell(midi(nn("G6")), 0.2), 0.45),
                          (0.21, bell(midi(nn("D7")), 0.2), 0.35)], 0.7)[: int(0.6 * SR)]
    se["SE_Button"] = glide(1400, 900, 0.06, decay=50)
    se["SE_ParameterUp"] = seq([(0, glide(600, 1300, 0.25, "rich", 5), 0.8), (0.18, bell(midi(nn("E7")), 0.1), 0.25)], 0.5)[: int(0.45 * SR)]
    se["SE_ParameterDown"] = glide(700, 330, 0.35, "rich", 4)
    return {k: se_out(v) for k, v in se.items()}


# ---------------------------------------------------------------- main
def main(out):
    bgm = {}
    bgm["BGM_Omote"] = theme(False, 104, "ABA", omote_lead, omote_acc).render_loop()
    bgm["BGM_Ura"] = theme(True, 104, "ABA", ura_lead, ura_acc).render_loop()
    bgm["BGM_Title"] = theme(False, 88, "AB", title_lead, title_acc).render_loop()
    bgm["BGM_Clear"] = jingle([("C5", .5), ("E5", .5), ("G5", .5), ("C6", 1), ("A5", .5), ("B5", .5), ("C6", 2)],
                              ["C4", "E4", "G4", "C5"], 120, lambda m, t, n, d: m.add(marimba(midi(n), d), t, 0.6))
    bgm["BGM_GameOver"] = jingle([("G5", 1), ("Eb5", 1), ("C5", 1), ("B4", 1), ("C5", 2)],
                                 ["C3", "Eb4", "G4"], 84, lambda m, t, n, d: m.add(bell(midi(n - 12), d), t, 0.55))
    for k, v in bgm.items():
        write_wav(os.path.join(out, "BGM", k + ".wav"), v)
        print(k, f"{len(v) / SR:.2f}s")
    for k, v in make_se().items():
        write_wav(os.path.join(out, "SE", k + ".wav"), v)
        print(k, f"{len(v) / SR:.2f}s")


if __name__ == "__main__":
    main(sys.argv[1])
