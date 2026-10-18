using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using HorrorGame.Player;
using HorrorGame.Cave;

namespace HorrorGame.Enemy
{
    /// <summary>
    /// ParasiteQueenBoss — Trùm Cuối Hang Động (Hang 6 — The Parasite Core).
    /// Sinh vật đột biến ký sinh khổng lồ phong cách The Forest Boss Fight:
    /// - Máu tối đa: 2000 HP.
    /// - 3 Giai Đoạn (Phases):
    ///   + Phase 1 (100% - 60% HP): Vung tua vòi cào đất, phun bọc bào tử axit, gọi bầy Parasite Zombie.
    ///   + Phase 2 (60% - 30% HP): Tiếng thét xung chấn tâm linh làm giảm mạnh Sanity, ảo giác toàn màn hình.
    ///   + Phase 3 (30% - 0% HP): Cuồng nộ (Enrage), tốc độ cực nhanh, lộ lõi phát sáng (Điểm yếu x3 sát thương).
    /// - Khi bị tiêu diệt: Rơi "Parasite Cure", mở lối thoát tắt lên mặt đất và mở khóa bí mật hòn đảo.
    /// </summary>
    public class ParasiteQueenBoss : EnemyAI
    {
        public enum BossPhase
        {
            Phase1_Awakening,
            Phase2_PsychicFrenzy,
            Phase3_EnragedCore
        }

        [Header("── Thông Số Boss Cuối ──")]
        public BossPhase currentPhase = BossPhase.Phase1_Awakening;
        public string bossTitle = "NỮ HOÀNG KÝ SINH (PARASITE QUEEN)";
        public Transform weakpointCoreTransform; // Điểm yếu trên đầu/ngực

        [Header("── Kỹ Năng Đòn Đánh Boss ──")]
        public float heavySlamDamage = 35.0f;
        public float acidSpitCooldown = 7.0f;
        public float summonMinionsCooldown = 25.0f;
        public GameObject acidProjectilePrefab;
        public Transform spitLaunchPoint;
        public GameObject minionZombiePrefab;
        public Transform[] minionSpawnPoints;

        [Header("── Âm Thanh & Bầu Không Khí Boss ──")]
        public AudioClip bossIntroRoar;
        public AudioClip bossPhaseChangeSound;
        public AudioClip bossAcidSpitClip;
        public AudioClip bossDeathRoar;

        [Header("── Phần Thưởng Hạ Boss ──")]
        public GameObject parasiteCureDropPrefab;
        public GameObject ancientRelicDropPrefab;
        public GameObject exitLadderShortcut; // Thang bí mật mở ra khi trùm chết

        private float lastAcidSpitTime = -999f;
        private float lastSummonTime = -999f;
        private AudioSource bossAudio;
        private bool isEnraged = false;
        private bool hasTriggeredDeath = false;

        protected override void Start()
        {
            base.Start();

            maxHealth = 2000f;
            currentHealth = maxHealth;
            walkSpeed = 2.2f;
            runSpeed = 4.5f;
            damage = 25f;
            attackRange = 4.5f; // Sải tay/tua vòi rất dài
            chaseRange = 60f;

            bossAudio = GetComponent<AudioSource>();
            if (bossAudio == null)
            {
                bossAudio = gameObject.AddComponent<AudioSource>();
                bossAudio.spatialBlend = 1f;
                bossAudio.maxDistance = 50f;
            }

            if (bossIntroRoar != null && bossAudio != null)
            {
                bossAudio.PlayOneShot(bossIntroRoar);
            }
        }

        protected override void Update()
        {
            if (isDead) return;

            // 1. Kiểm tra và chuyển đổi các Phase theo lượng máu
            UpdateBossPhases();

            // 2. Kỹ năng Phun Bào Tử Axit tầm xa
            if (player != null && Time.time >= lastAcidSpitTime + acidSpitCooldown)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist > attackRange && dist <= 28f)
                {
                    PerformAcidSpitAttack();
                }
            }

            // 3. Kỹ năng Triệu Hồi Minion định kỳ
            if (Time.time >= lastSummonTime + summonMinionsCooldown)
            {
                PerformSummonMinions();
            }

            base.Update();
        }

        private void UpdateBossPhases()
        {
            float healthRatio = currentHealth / maxHealth;

            if (healthRatio <= 0.30f && currentPhase != BossPhase.Phase3_EnragedCore)
            {
                EnterPhase3();
            }
            else if (healthRatio <= 0.60f && healthRatio > 0.30f && currentPhase == BossPhase.Phase1_Awakening)
            {
                EnterPhase2();
            }
        }

        private void EnterPhase2()
        {
            currentPhase = BossPhase.Phase2_PsychicFrenzy;
            runSpeed = 5.2f;
            acidSpitCooldown = 5.0f;

            if (bossAudio != null && bossPhaseChangeSound != null)
            {
                bossAudio.PlayOneShot(bossPhaseChangeSound);
            }

            // Giảm 20 Sanity của người chơi khi Boss phát ra xung chấn tâm linh
            if (player != null)
            {
                var stats = player.GetComponentInParent<PlayerStats>();
                if (stats == null) stats = player.GetComponentInChildren<PlayerStats>();
                if (stats != null)
                {
                    stats.DrainSanity(20f);
                }
            }

            Debug.Log("<color=red>[ParasiteQueenBoss]</color> Phase 2: Nữ hoàng Ký Sinh gầm thét phát ra sóng tâm linh!");
        }

        private void EnterPhase3()
        {
            currentPhase = BossPhase.Phase3_EnragedCore;
            isEnraged = true;
            runSpeed = 6.2f; // Cực nhanh cuồng nộ
            damage = heavySlamDamage;

            if (bossAudio != null && bossPhaseChangeSound != null)
            {
                bossAudio.PlayOneShot(bossPhaseChangeSound);
            }

            Debug.Log("<color=red>[ParasiteQueenBoss]</color> Phase 3 CUỒNG NỘ: Lõi điểm yếu đã lộ ra! Đánh vào đầu!");
        }

        private void PerformAcidSpitAttack()
        {
            lastAcidSpitTime = Time.time;

            if (bossAudio != null && bossAcidSpitClip != null)
            {
                bossAudio.PlayOneShot(bossAcidSpitClip);
            }

            Vector3 launchPos = spitLaunchPoint != null ? spitLaunchPoint.position : transform.position + Vector3.up * 2.5f;
            Vector3 targetPos = player.position + Vector3.up * 1f;
            Vector3 spitDir = (targetPos - launchPos).normalized;

            if (acidProjectilePrefab != null)
            {
                GameObject proj = Instantiate(acidProjectilePrefab, launchPos, Quaternion.LookRotation(spitDir));
                Rigidbody rb = proj.GetComponent<Rigidbody>();
                if (rb == null) rb = proj.AddComponent<Rigidbody>();
                rb.velocity = spitDir * 20f;
                Destroy(proj, 5f);
            }
            else
            {
                // Fallback raycast
                RaycastHit hit;
                if (Physics.Raycast(launchPos, spitDir, out hit, 30f))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        var stats = hit.collider.GetComponentInParent<PlayerStats>();
                        if (stats != null) stats.TakeDamage(15f);
                    }
                }
            }
        }

        private void PerformSummonMinions()
        {
            lastSummonTime = Time.time;

            if (minionZombiePrefab == null || minionSpawnPoints == null) return;

            int count = (currentPhase == BossPhase.Phase3_EnragedCore) ? 3 : 2;
            for (int i = 0; i < count && i < minionSpawnPoints.Length; i++)
            {
                if (minionSpawnPoints[i] != null)
                {
                    Instantiate(minionZombiePrefab, minionSpawnPoints[i].position, minionSpawnPoints[i].rotation);
                }
            }

            Debug.Log(string.Format("<color=magenta>[ParasiteQueenBoss]</color> Nữ hoàng triệu hồi {0} Parasite Zombies bảo vệ!", count));
        }

        public override void TakeDamage(float amount, Vector3 hitPoint)
        {
            // Nếu người chơi bắn/chém trúng Điểm yếu (Weakpoint) ở Phase 3: Sát thương nhân 3!
            if (currentPhase == BossPhase.Phase3_EnragedCore && weakpointCoreTransform != null)
            {
                float distToWeakpoint = Vector3.Distance(hitPoint, weakpointCoreTransform.position);
                if (distToWeakpoint < 1.5f)
                {
                    amount *= 3.0f;
                    Debug.Log("<color=yellow>[ParasiteQueenBoss] CRITICAL HIT! Trúng lõi điểm yếu x3 Sát thương!</color>");
                }
            }

            base.TakeDamage(amount, hitPoint);

            if (currentHealth <= 0f && !hasTriggeredDeath)
            {
                HandleBossDefeated();
            }
        }

        private void HandleBossDefeated()
        {
            hasTriggeredDeath = true;

            if (bossAudio != null && bossDeathRoar != null)
            {
                bossAudio.PlayOneShot(bossDeathRoar);
            }

            Debug.Log("<color=green>[ParasiteQueenBoss] 🏆 CHIẾN THẮNG! NỮ HOÀNG KÝ SINH ĐÃ BỊ TIÊU DIỆT!</color>");

            // Mở thang lối thoát tắt lên mặt đất
            if (exitLadderShortcut != null)
            {
                exitLadderShortcut.SetActive(true);
            }

            // Rơi phần thưởng tối thượng
            Vector3 dropPos = transform.position + Vector3.up * 1f;
            if (parasiteCureDropPrefab != null)
            {
                Instantiate(parasiteCureDropPrefab, dropPos + transform.right * 1.5f, Quaternion.identity);
            }

            if (ancientRelicDropPrefab != null)
            {
                Instantiate(ancientRelicDropPrefab, dropPos - transform.right * 1.5f, Quaternion.identity);
            }

            // Chữa sạch bệnh ký sinh trùng cho player khi boss chết
            if (player != null)
            {
                var parasiteSys = player.GetComponentInParent<ZombieParasiteSystem>();
                if (parasiteSys == null) parasiteSys = player.GetComponentInChildren<ZombieParasiteSystem>();
                if (parasiteSys != null)
                {
                    parasiteSys.CureAll();
                }
            }
        }

        private void OnGUI()
        {
            if (isDead) return;

            // Thanh Máu Boss (Boss Health Bar) trên cùng màn hình
            float barWidth = 460f;
            float barHeight = 22f;
            float x = (Screen.width - barWidth) / 2;
            float y = 35f;

            float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);

            // Nền đen viền
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(x - 3, y - 3, barWidth + 6, barHeight + 6), Texture2D.whiteTexture);

            // Thanh máu theo Phase: Đỏ (Phase 1), Tím (Phase 2), Cam Rực (Phase 3)
            Color barColor = Color.red;
            if (currentPhase == BossPhase.Phase2_PsychicFrenzy) barColor = new Color(0.7f, 0.2f, 0.9f);
            else if (currentPhase == BossPhase.Phase3_EnragedCore) barColor = new Color(1f, 0.4f, 0.1f);

            GUI.color = barColor;
            GUI.DrawTexture(new Rect(x, y, barWidth * healthRatio, barHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Tên Boss
            GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
            nameStyle.fontSize = 15;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.alignment = TextAnchor.MiddleCenter;
            nameStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(x, y - 26, barWidth, 24), bossTitle, nameStyle);
        }
    }
}
