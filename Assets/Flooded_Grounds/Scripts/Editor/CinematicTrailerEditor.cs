using UnityEngine;
using UnityEditor;
using HorrorGame.Cutscenes;

namespace HorrorGame.Editor
{
    public class CinematicTrailerEditor
    {
        [MenuItem("Trailer/🎬 Bat Dau Quay Trailer Dien Anh (F9)", false, 1)]
        public static void PlayTrailer()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Quay Trailer Điện Ảnh",
                    "Bạn hãy bấm nút [Play] (▶) vào game trước, sau đó bấm phím F9 (hoặc bấm lại menu này) để bắt đầu quay Trailer nhé!",
                    "Đã hiểu"
                );
                return;
            }

            CinematicTrailerDirector director = Object.FindObjectOfType<CinematicTrailerDirector>();
            if (director == null)
            {
                GameObject directorObj = new GameObject("[CinematicTrailerDirector]");
                director = directorObj.AddComponent<CinematicTrailerDirector>();
            }

            director.StartTrailer();
            Debug.Log("🎬 [Trailer Editor] Đã bắt đầu khởi chạy Trailer điện ảnh (~52s)!");
        }

        [MenuItem("Trailer/⏹ Dung Trailer (ESC)", false, 2)]
        public static void StopTrailer()
        {
            if (!Application.isPlaying) return;

            CinematicTrailerDirector director = Object.FindObjectOfType<CinematicTrailerDirector>();
            if (director != null)
            {
                director.StopTrailer();
            }
        }

        [MenuItem("Trailer/🎞️ Xuat File Video MP4 (Python FFmpeg)", false, 20)]
        public static void ExportMp4Manual()
        {
            string scriptPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "Flooded_Grounds/Scripts/Cutscenes/compile_trailer_mp4.py"));
            string pythonExe = @"C:\Users\Admin\AppData\Local\Programs\Python\Python313\python.exe";
            if (!System.IO.File.Exists(pythonExe)) pythonExe = "python";

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = pythonExe,
                    Arguments = string.Format("\"{0}\"", scriptPath),
                    UseShellExecute = true
                });
                Debug.Log("🎬 [Trailer Editor] Đã kích hoạt tiến trình biên dịch MP4!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("❌ Lỗi kích hoạt MP4 compiler: " + ex.Message);
            }
        }

        [MenuItem("Trailer/📂 Mo Thu Muc Video (Windows Explorer)", false, 21)]
        public static void OpenVideoFolder()
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            string mp4File = System.IO.Path.Combine(root, "Trailer_DeadIslandSurvival.mp4");
            if (System.IO.File.Exists(mp4File))
            {
                System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", mp4File));
            }
            else
            {
                System.Diagnostics.Process.Start("explorer.exe", string.Format("\"{0}\"", root));
            }
        }
    }
}
