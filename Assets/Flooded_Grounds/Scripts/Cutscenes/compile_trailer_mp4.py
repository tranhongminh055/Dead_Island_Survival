"""
Script tự động biên dịch toàn bộ khung hình đã quay thành file video MP4 hoàn chỉnh.
Hỗ trợ cả FFmpeg H.264 Encoder và OpenCV VideoWriter dự phòng.
"""

import glob
import importlib
import io
import os
import shutil
import struct
import subprocess
import sys

# Dynamic imports để tránh cảnh báo missing-import từ Pyrefly / Pyright
try:
    cv2 = importlib.import_module("cv2")
except (ImportError, ModuleNotFoundError):
    cv2 = None

try:
    imageio_ffmpeg = importlib.import_module("imageio_ffmpeg")
except (ImportError, ModuleNotFoundError):
    imageio_ffmpeg = None

if sys.stdout and hasattr(sys.stdout, "reconfigure"):
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, io.UnsupportedOperation, ValueError):
        pass  # noqa: S110

if sys.stderr and hasattr(sys.stderr, "reconfigure"):
    try:
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, io.UnsupportedOperation, ValueError):
        pass  # noqa: S110


def safe_print(*args: object, **kwargs: object) -> None:
    try:
        print(*args, **kwargs)
    except (UnicodeEncodeError, OSError):
        clean_args = [
            str(a).encode("ascii", errors="backslashreplace").decode("ascii")
            for a in args
        ]
        print(*clean_args, **kwargs)


def get_png_dimensions(file_path: str) -> tuple[int, int]:
    """Đọc kích thước khung hình PNG trực tiếp từ binary header (0 dependencies)."""
    try:
        with open(file_path, "rb") as f:
            header = f.read(24)
            if len(header) >= 24 and header.startswith(b"\x89PNG\r\n\x1a\n"):
                w, h = struct.unpack(">II", header[16:24])
                return w, h
    except (OSError, struct.error):
        pass  # noqa: S110
    return 1920, 1080


def find_ffmpeg_executable() -> str | None:
    """Tìm đường dẫn binary FFmpeg trên hệ thống."""
    if imageio_ffmpeg is not None and hasattr(imageio_ffmpeg, "get_ffmpeg_exe"):
        try:
            exe = imageio_ffmpeg.get_ffmpeg_exe()
            if exe and os.path.isfile(exe):
                return exe
        except (AttributeError, OSError):
            pass  # noqa: S110

    system_ffmpeg = shutil.which("ffmpeg")
    if system_ffmpeg and os.path.isfile(system_ffmpeg):
        return system_ffmpeg

    common_paths = [
        r"C:\ffmpeg\bin\ffmpeg.exe",
        r"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
    ]
    for p in common_paths:
        if os.path.isfile(p):
            return p

    return None


def compile_trailer(
    frame_dir: str | None = None,
    output_path: str | None = None,
    fps: int = 30,
    audio_path: str | None = None,
) -> bool:
    project_root = r"d:\Project Unity\Survival"
    if frame_dir is None:
        frame_dir = os.path.join(project_root, "Trailer_Frames")
    if output_path is None:
        output_path = os.path.join(project_root, "Trailer_DeadIslandSurvival.mp4")
    if audio_path is None:
        default_soundtrack = os.path.join(
            project_root,
            "Assets",
            "Flooded_Grounds",
            "sound main menu",
            "lemonmusiclab-dark-ambient-soundscape-505384.mp3",
        )
        if os.path.isfile(default_soundtrack):
            audio_path = default_soundtrack

    safe_print(f"[Trailer Compiler] Processing frames in: {frame_dir}")
    safe_print(f"[Trailer Compiler] Target MP4 file: {output_path}")
    if audio_path and os.path.isfile(audio_path):
        safe_print(f"[Trailer Compiler] Soundtrack: {audio_path}")

    # Tìm danh sách khung hình
    images = sorted(glob.glob(os.path.join(frame_dir, "frame_*.png")))
    if not images:
        images = sorted(glob.glob(os.path.join(frame_dir, "frame_*.jpg")))

    if not images:
        safe_print("[ERROR] No frames found in directory:", frame_dir)
        return False

    total_frames = len(images)
    safe_print(f"[INFO] Found {total_frames} frames. Compiling MP4 video...")

    # Lấy kích thước hình ảnh
    w, h = get_png_dimensions(images[0])
    if cv2 is not None:
        try:
            first_frame = cv2.imread(images[0])
            if first_frame is not None:
                h, w, _ = first_frame.shape
        except (AttributeError, OSError):
            pass  # noqa: S110

    # 1. Thử xuất bằng FFmpeg (chuẩn H.264 tương thích mọi thiết bị + âm thanh)
    ffmpeg_exe = find_ffmpeg_executable()
    compiled_success = False

    if ffmpeg_exe and os.path.isfile(ffmpeg_exe):
        try:
            safe_print("[INFO] Using FFmpeg H.264 Encoder (High Quality)...")
            input_pattern = os.path.join(frame_dir, "frame_%06d.png")
            if not glob.glob(os.path.join(frame_dir, "frame_*.png")):
                input_pattern = os.path.join(frame_dir, "frame_%06d.jpg")

            cmd = [
                ffmpeg_exe,
                "-y",
                "-framerate",
                str(fps),
                "-i",
                input_pattern,
            ]

            # Mux âm thanh nếu có
            if audio_path and os.path.isfile(audio_path):
                cmd.extend(
                    ["-i", audio_path, "-c:a", "aac", "-b:a", "192k", "-shortest"]
                )

            # Tự động làm tròn chẵn độ phân giải (pad to even numbers) để tránh lỗi libx264 height not divisible by 2
            cmd.extend(
                [
                    "-vf",
                    "pad=ceil(iw/2)*2:ceil(ih/2)*2",
                    "-c:v",
                    "libx264",
                    "-pix_fmt",
                    "yuv420p",
                    "-preset",
                    "fast",
                    "-crf",
                    "18",
                    output_path,
                ]
            )

            subprocess.run(cmd, check=True)
            compiled_success = True
            safe_print("[SUCCESS] Exported MP4 successfully with FFmpeg!")
        except (subprocess.SubprocessError, OSError) as err:
            safe_print("[WARNING] FFmpeg error, falling back to OpenCV:", str(err))

    # 2. Xuất dự phòng bằng OpenCV VideoWriter nếu có cv2
    if not compiled_success and cv2 is not None:
        try:
            safe_print("[INFO] Using OpenCV VideoWriter...")
            # Đảm bảo w, h là số chẵn
            w_even = (w // 2) * 2
            h_even = (h // 2) * 2
            fourcc = cv2.VideoWriter_fourcc(*"mp4v")
            out = cv2.VideoWriter(output_path, fourcc, float(fps), (w_even, h_even))

            for idx, img_path in enumerate(images):
                frame = cv2.imread(img_path)
                if frame is not None:
                    if frame.shape[1] != w_even or frame.shape[0] != h_even:
                        frame = cv2.resize(frame, (w_even, h_even))
                    out.write(frame)
                if idx % 150 == 0:
                    safe_print(
                        f"  Progress: {idx}/{total_frames} frames ({(idx/total_frames)*100:.1f}%)"
                    )

            out.release()
            compiled_success = True
            safe_print("[SUCCESS] Exported MP4 successfully with OpenCV!")
        except (AttributeError, OSError) as err:
            safe_print("[ERROR] OpenCV error:", str(err))

    # Mở thư mục Explorer hiển thị file video vừa tạo
    if os.path.isfile(output_path):
        size_mb = os.path.getsize(output_path) / (1024 * 1024)
        safe_print(f"[READY] VIDEO READY: {output_path} ({size_mb:.2f} MB)")
        try:
            subprocess.Popen(f'explorer.exe /select,"{output_path}"')
        except (subprocess.SubprocessError, OSError):
            pass  # noqa: S110

        # Dọn dẹp các frame tạm để giải phóng dung lượng ổ cứng
        try:
            for img in images:
                os.remove(img)
            os.rmdir(frame_dir)
            safe_print("[CLEANUP] Deleted temporary frames folder.")
        except (OSError, FileNotFoundError):
            pass  # noqa: S110

        return True

    return False


if __name__ == "__main__":
    f_dir = sys.argv[1] if len(sys.argv) > 1 else None
    o_path = sys.argv[2] if len(sys.argv) > 2 else None
    fps_val = int(sys.argv[3]) if len(sys.argv) > 3 else 30
    a_path = sys.argv[4] if len(sys.argv) > 4 else None
    compile_trailer(f_dir, o_path, fps_val, a_path)
