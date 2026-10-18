using UnityEngine;
using System.Collections.Generic;
using HorrorGame.Environment;
using HorrorGame.Inventory;

namespace HorrorGame.Cave
{
    /// <summary>
    /// CaveLootChest — Rương Kho Báu Độc Quyền trong Hang Động.
    /// Kế thừa LootChest với các tính năng bổ sung:
    /// - Lưu trạng thái đã mở vào CaveManager / CaveZone.
    /// - Kích hoạt bẫy nước (Flood Trap) nếu là rương kho báu trong Hang 3.
    /// - Hiệu ứng mở rương cổ xưa: Âm thanh cọt kẹt, bụi đá tung bay.
    /// - Hiện biểu ngữ nhận bảo vật độc nhất (Artifact/Weapon Banner).
    /// </summary>
    public class CaveLootChest : LootChest
    {
        [Header("── Thuộc Tính Kho Báu Hang Động ──")]
        [Tooltip("ID của hang chứa rương này (1-6)")]
        public int caveID = 1;

        [Tooltip("Tên bảo vật độc nhất (VD: 'Rìu Đá Nâng Cấp', 'Dao Lặn Quân Đội', 'Katana Obsidian')")]
        public string uniqueTreasureName = "Bảo Vật Hang Động";

        [Tooltip("Có kích hoạt bẫy Flood Trap khi mở không? (Dành riêng cho Hang 3)")]
        public bool triggerFloodTrapOnOpen = false;

        [Header("── Âm Thanh & Bụi Mở Rương ──")]
        public AudioClip creakOpenSound;
        public AudioClip treasureChimeSound;
        public ParticleSystem openDustParticles;

        private AudioSource audioSource;
        private bool hasShownBanner = false;
        private float bannerTimer = 0f;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.maxDistance = 20f;
            }
        }

        private void Update()
        {
            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.deltaTime;
            }

            // Kiểm tra khi rương vừa được mở
            if (isOpened && !hasShownBanner)
            {
                OnCaveChestOpened();
            }
        }

        private void OnCaveChestOpened()
        {
            hasShownBanner = true;
            bannerTimer = 4.5f;

            // 1. Phát âm thanh rương cổ mở và tiếng chuông bảo vật
            if (audioSource != null)
            {
                if (creakOpenSound != null) audioSource.PlayOneShot(creakOpenSound);
                if (treasureChimeSound != null) audioSource.PlayOneShot(treasureChimeSound);
            }

            // 2. Bụi bốc lên
            if (openDustParticles != null)
            {
                openDustParticles.Play();
            }

            // 3. Đánh dấu đã loot kho báu vào CaveManager
            if (CaveManager.Instance != null)
            {
                CaveManager.Instance.MarkCaveLootCollected(caveID);
            }

            // 4. Kích hoạt Flood Trap nếu được thiết lập (Hang 3)
            if (triggerFloodTrapOnOpen)
            {
                if (CaveWaterLevel.Instance != null)
                {
                    CaveWaterLevel.Instance.TriggerFloodTrap();
                }
            }

            Debug.Log(string.Format("<color=yellow>[CaveLootChest] 🏆 Người chơi đã mở kho báu hang {0}: {1}!</color>", caveID, uniqueTreasureName));
        }

        private void OnGUI()
        {
            // Hiển thị biểu ngữ bảo vật khi vừa mở
            if (bannerTimer > 0f)
            {
                float bannerW = 540f;
                float bannerH = 75f;
                float bx = (Screen.width - bannerW) / 2;
                float by = 110f;

                GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
                GUI.DrawTexture(new Rect(bx, by, bannerW, bannerH), Texture2D.whiteTexture);
                GUI.color = Color.white;

                GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
                headerStyle.alignment = TextAnchor.UpperCenter;
                headerStyle.fontSize = 14;
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.normal.textColor = new Color(1f, 0.8f, 0.2f);
                GUI.Label(new Rect(bx, by + 8, bannerW, 20), "⭐ ĐÃ MỞ KHÓA BẢO VẬT HANG ĐỘNG ⭐", headerStyle);

                GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                nameStyle.alignment = TextAnchor.MiddleCenter;
                nameStyle.fontSize = 20;
                nameStyle.fontStyle = FontStyle.Bold;
                nameStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(bx, by + 30, bannerW, 35), uniqueTreasureName, nameStyle);
            }
        }
    }
}
