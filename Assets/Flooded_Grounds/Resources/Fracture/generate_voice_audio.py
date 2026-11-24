"""
Tổng hợp giọng người rên la đau đớn bằng mô hình Source-Filter (giống bộ tổng hợp Klatt):
  - Nguồn: chuỗi xung thanh quản (Rosenberg) có jitter/shimmer, rè khàn (period-doubling), hơi thở.
  - Bộ lọc: 5 formant cộng hưởng khoang miệng thay đổi theo thời gian (AAAH -> UUGH -> MMNGH).
  - Hậu kỳ: méo tiếng khi gào (saturation), lọc dải, phản xạ không gian nhẹ.

Xuất ra:
  fracture_scream_agony.wav  - Tiếng gào thét + rên rỉ khi gãy xương
  limp_pain_grunt.wav        - Tiếng "Hự!/Ngh!" đau khi bước chân què
"""
import os
import wave
import numpy as np
from scipy.signal import lfilter, butter, sosfilt

SR = 44100
OUT_DIR = os.path.dirname(os.path.abspath(__file__))


# ───────────────────────── Tiện ích ─────────────────────────
def curve(points, n):
    t = np.arange(n) / SR
    return np.interp(t, [p[0] for p in points], [p[1] for p in points])


def wander(n, rate_hz, rng):
    """Dao động ngẫu nhiên tần số thấp trong khoảng [-1, 1] (run giọng tự nhiên)."""
    k = max(3, int(n / SR * rate_hz) + 3)
    pts = rng.uniform(-1, 1, k)
    return np.interp(np.linspace(0, k - 1, n), np.arange(k), pts)


def write_wav(name, x):
    x = x / (np.max(np.abs(x)) + 1e-9) * 0.89
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    path = os.path.join(OUT_DIR, name)
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    print("Generated:", path, f"({len(x) / SR:.2f}s)")


# ───────────────────────── Nguồn thanh quản ─────────────────────────
def glottal_source(f0, amp, rough, oq, rng, jitter=0.012, shimmer=0.10):
    n = len(f0)
    out = np.zeros(n)
    i, c = 0, 0
    while i < n:
        r = rough[i]
        alt = (c % 2 == 1)  # Chu kỳ xen kẽ lệch nhau -> tiếng rè khàn (vocal fry / growl)
        f = f0[i] * (1.0 + rng.normal(0, jitter))
        if alt:
            f *= (1.0 - 0.18 * r)
        f = max(f, 50.0)
        T = max(int(SR / f), 8)

        a = amp[i] * max(0.0, 1.0 + rng.normal(0, shimmer))
        if alt:
            a *= (1.0 - 0.55 * r)
        a *= (T / 100.0) ** 0.6  # Cân bằng âm lượng giữa giọng cao và giọng trầm

        if a > 1e-5:
            q = oq[i]
            Tp = max(int(T * q * 0.62), 2)
            Tn = max(int(T * q * 0.38), 1)
            p = np.zeros(T)
            k = np.arange(Tp)
            p[:Tp] = 0.5 * (1 - np.cos(np.pi * k / Tp))
            m = min(Tn, T - Tp)
            k = np.arange(m)
            p[Tp:Tp + m] = np.cos(0.5 * np.pi * k / Tn)
            d = np.diff(p, prepend=0.0)  # Đạo hàm = luồng khí + bức xạ môi
            end = min(i + T, n)
            out[i:end] += a * d[:end - i]
        i += T
        c += 1
    return out


# ───────────────────────── Khoang miệng (formant) ─────────────────────────
def formant_filter(x, formants, block=128):
    y = x.copy()
    n = len(x)
    for F, B in formants:
        zi = np.zeros(2)
        out = np.zeros(n)
        for s in range(0, n, block):
            e = min(s + block, n)
            mid = (s + e) // 2
            r = np.exp(-np.pi * B[mid] / SR)
            th = 2 * np.pi * F[mid] / SR
            a1 = 2 * r * np.cos(th)
            a2 = -r * r
            b0 = 1 - a1 - a2
            out[s:e], zi = lfilter([b0], [1, -a1, -a2], y[s:e], zi=zi)
        y = out
    return y


def bandpass(x, lo, hi, order=2):
    sos = butter(order, [lo, hi], btype="bandpass", fs=SR, output="sos")
    return sosfilt(sos, x)


def finish(y, drive, room_mix, rng):
    y = y / (np.max(np.abs(y)) + 1e-9)
    y = np.tanh(drive * y) / np.tanh(drive)          # Méo tiếng khi gào thét
    y = sosfilt(butter(2, 75, btype="highpass", fs=SR, output="sos"), y)
    y = sosfilt(butter(4, 7800, btype="lowpass", fs=SR, output="sos"), y)
    # Phản xạ không gian nhẹ (rừng / ngoài trời)
    L = int(SR * 0.35)
    t = np.arange(L) / SR
    ir = rng.normal(0, 1, L) * np.exp(-t / 0.07)
    ir = sosfilt(butter(2, 3500, btype="lowpass", fs=SR, output="sos"), ir)
    ir /= np.sum(np.abs(ir))
    wet = np.convolve(y, ir)[:len(y)]
    wet /= (np.max(np.abs(wet)) + 1e-9)
    return y * (1 - room_mix) + wet * room_mix


def synth_voice(dur, f0_pts, amp_pts, rough_pts, breath_pts, drive_pts,
                F_pts, B_vals, oq_pts, tremor_hz, tremor_depth, seed):
    rng = np.random.default_rng(seed)
    n = int(SR * dur)
    t = np.arange(n) / SR

    f0 = curve(f0_pts, n)
    f0 *= 1 + tremor_depth * np.sin(2 * np.pi * tremor_hz * t + 2.0 * wander(n, 3, rng))
    f0 *= 1 + 0.025 * wander(n, 8, rng)
    amp = curve(amp_pts, n)
    rough = curve(rough_pts, n)
    breath = curve(breath_pts, n)
    oq = curve(oq_pts, n)

    src = glottal_source(f0, amp, rough, oq, rng)
    src /= (np.max(np.abs(src)) + 1e-9)

    asp = sosfilt(butter(2, 600, btype="highpass", fs=SR, output="sos"), rng.normal(0, 1, n))
    asp /= (np.max(np.abs(asp)) + 1e-9)
    excitation = src + asp * breath * amp * 0.9

    formants = [(curve(F_pts[k], n), np.full(n, B_vals[k])) for k in range(len(F_pts))]
    y = formant_filter(excitation, formants)

    drive = float(np.mean(curve(drive_pts, n)[amp > 0.3])) if np.any(amp > 0.3) else 1.2
    return y, drive, rng, n


# ───────────────────────── 1. TIẾNG GÀO THÉT KHI GÃY XƯƠNG ─────────────────────────
def make_agony_scream():
    dur = 3.0
    y, drive, rng, n = synth_voice(
        dur,
        # Cao độ: gào "AAAARGH!" ~420Hz -> vỡ giọng rên "UUGH" -> rên trầm "mmnngh"
        f0_pts=[(0, 320), (0.06, 410), (0.25, 445), (0.55, 425), (0.85, 375), (1.05, 300),
                (1.35, 190), (1.6, 150), (1.7, 140), (2.05, 128), (2.4, 118), (3.0, 100)],
        amp_pts=[(0, 0), (0.035, 1.0), (0.7, 0.95), (0.95, 0.8), (1.3, 0.45), (1.6, 0.15),
                 (1.68, 0.0), (2.08, 0.0), (2.16, 0.38), (2.45, 0.32), (2.8, 0.12), (3.0, 0)],
        rough_pts=[(0, 0.65), (0.3, 0.35), (0.8, 0.5), (1.1, 0.75), (1.6, 0.9), (2.1, 0.8), (3.0, 0.95)],
        breath_pts=[(0, 0.18), (0.9, 0.22), (1.4, 0.38), (1.6, 0.55), (2.1, 0.3), (3.0, 0.55)],
        drive_pts=[(0, 2.6), (1.0, 2.2), (3.0, 1.4)],
        F_pts=[
            [(0, 750), (0.1, 950), (0.8, 900), (1.1, 700), (1.5, 520), (2.08, 340), (2.5, 320), (3.0, 310)],
            [(0, 1250), (0.1, 1450), (0.8, 1350), (1.1, 1150), (1.5, 900), (2.08, 1050), (3.0, 980)],
            [(0, 2500), (0.1, 2700), (1.5, 2450), (3.0, 2350)],
            [(0, 3500), (3.0, 3400)],
            [(0, 4500), (3.0, 4400)],
        ],
        B_vals=[110, 120, 160, 250, 350],
        oq_pts=[(0, 0.5), (1.0, 0.55), (1.6, 0.7), (3.0, 0.72)],
        tremor_hz=6.0, tremor_depth=0.03, seed=7,
    )

    # Tiếng hít rít qua kẽ răng "sssshh" giữa hai cơn rên (1.70s - 2.05s)
    t = np.arange(n) / SR
    hiss_env = np.interp(t, [1.68, 1.75, 1.98, 2.06], [0, 1, 0.8, 0], left=0, right=0)
    hiss = bandpass(rng.normal(0, 1, n), 2200, 6500) * hiss_env
    hiss *= 0.22 * np.max(np.abs(y)) / (np.max(np.abs(hiss)) + 1e-9)
    y = y + hiss

    return finish(y, drive, room_mix=0.12, rng=rng)


# ───────────────────────── 2. TIẾNG "HỰ!" KHI BƯỚC CHÂN QUÈ ─────────────────────────
def make_limp_grunt():
    dur = 0.65
    y, drive, rng, n = synth_voice(
        dur,
        f0_pts=[(0, 135), (0.06, 175), (0.22, 155), (0.45, 118), (0.65, 105)],
        amp_pts=[(0, 0), (0.025, 1.0), (0.18, 0.85), (0.42, 0.25), (0.6, 0)],
        rough_pts=[(0, 0.7), (0.3, 0.6), (0.65, 0.9)],
        breath_pts=[(0, 0.35), (0.3, 0.3), (0.65, 0.6)],
        drive_pts=[(0, 1.8), (0.65, 1.5)],
        F_pts=[
            [(0, 420), (0.08, 640), (0.4, 520), (0.65, 450)],
            [(0, 1000), (0.08, 1180), (0.4, 1050), (0.65, 1000)],
            [(0, 2450), (0.65, 2400)],
            [(0, 3400), (0.65, 3400)],
            [(0, 4300), (0.65, 4300)],
        ],
        B_vals=[110, 120, 160, 250, 350],
        oq_pts=[(0, 0.55), (0.65, 0.65)],
        tremor_hz=5.0, tremor_depth=0.015, seed=21,
    )
    return finish(y, drive, room_mix=0.08, rng=rng)


if __name__ == "__main__":
    scream = make_agony_scream()
    grunt = make_limp_grunt()
    # Tiếng la hét dùng file thu âm thật từ Pixabay (fracture_scream_agony.mp3), không tạo bằng code nữa
    write_wav("limp_pain_grunt.wav", grunt)

    # Xuất ảnh phổ (spectrogram) để kiểm tra cấu trúc giọng nói (nếu có matplotlib)
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
        fig, axes = plt.subplots(2, 1, figsize=(10, 6))
        for ax, (title, x) in zip(axes, [("Agony scream", scream), ("Limp grunt", grunt)]):
            ax.specgram(x, NFFT=1024, Fs=SR, noverlap=768, cmap="magma")
            ax.set_ylim(0, 6000)
            ax.set_title(title)
        plt.tight_layout()
        spec_path = os.environ.get("SPEC_OUT")
        if spec_path:
            plt.savefig(spec_path, dpi=90)
            print("Spectrogram:", spec_path)
    except ImportError:
        pass
