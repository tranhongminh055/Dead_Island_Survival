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

        public static System.Action OnBossDefeatedEvent;

        private float lastAcidSpitTime = -999f;
        private float lastSummonTime = -999f;
        private AudioSource bossAudio;
        private bool isEnraged = false;
        private bool hasTriggeredDeath = false;

        // Các cụm ký sinh trùng & xúc tu sống bám trên cơ thể Boss (đập phập phồng & uốn éo liên tục)
        private List<Transform> parasiteSacs = new List<Transform>();
        private List<Vector3> parasiteSacBaseScales = new List<Vector3>();
        private List<Transform> parasiteTentacles = new List<Transform>();
        private List<Quaternion> tentacleBaseRots = new List<Quaternion>();
        private Light bossAuraLight;
        private Material parasiteGlowMat;

        protected override void Start()
        {
            base.Start();

            maxHealth = 1200f;
            currentHealth = maxHealth;
            walkSpeed = 2.6f;
            runSpeed = 4.8f;
            damage = 24f;
            attackRange = 4.8f; // Sải tay & xúc tu khổng lồ
            chaseRange = 75f;

            // Đảm bảo kích thước khổng lồ như Boss Sons of the Forest (nếu chưa scale sẵn)
            if (transform.localScale.x < 2.8f)
            {
                transform.localScale = Vector3.one * 3.2f;
            }

            // Mở rộng Collider cho khớp thân hình khổng lồ để bắn/chém trúng dễ dàng
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.center = new Vector3(0f, 0.95f, 0f);
                col.height = 1.95f;
                col.radius = 0.48f;
            }

            // Dựng ngoại hình Đột Biến Ký Sinh Trùng (da biến dị + bọc ký sinh phát sáng + 6 xúc tu uốn éo + bào tử độc)
            BuildMutantParasiteQueenVisuals();

            bossAudio = GetComponent<AudioSource>();
            if (bossAudio == null)
            {
                bossAudio = gameObject.AddComponent<AudioSource>();
                bossAudio.spatialBlend = 1f;
                bossAudio.maxDistance = 60f;
            }

            if (bossIntroRoar != null && bossAudio != null)
            {
                bossAudio.PlayOneShot(bossIntroRoar);
            }
        }

        private void BuildMutantParasiteQueenVisuals()
        {
            // 1. Nhuộm da cơ thể thành sắc thịt đột biến thẫm rùng rợn
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null || rends[i] is ParticleSystemRenderer) continue;
                Material m = rends[i].material;
                if (m != null)
                {
                    m.color = new Color(0.32f, 0.16f, 0.28f);
                }
            }

            // Gắn ký sinh trùng lên xương Spine/Chest nếu có Animator Humanoid để bám theo chuyển động chạy/vung tay!
            Transform attachBone = transform;
            if (animator != null && animator.isHuman)
            {
                Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);
                if (chest != null) attachBone = chest;
            }

            GameObject parasiteRoot = new GameObject("ParasiteQueen_MutantGrowths");
            parasiteRoot.transform.SetParent(attachBone, false);
            parasiteRoot.transform.localPosition = (attachBone == transform) ? new Vector3(0f, 1.15f, 0f) : Vector3.zero;

            // Vật liệu phát sáng ký sinh trùng (đổi màu theo Phase: Xanh độc -> Tím -> Cam rực)
            Shader glowShader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            parasiteGlowMat = new Material(glowShader);
            parasiteGlowMat.color = new Color(0.45f, 1.0f, 0.28f);

            Material tentacleMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            tentacleMat.color = new Color(0.22f, 0.06f, 0.16f);

            // 2. LÕI KÝ SINH PHÁT SÁNG LỚN TRƯỚC NGỰC (ĐIỂM YẾU x3 SÁT THƯƠNG Ở PHASE 3)
            GameObject coreObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreObj.name = "Parasite_WeakpointCore";
            coreObj.transform.SetParent(parasiteRoot.transform, false);
            coreObj.transform.localPosition = new Vector3(0f, 0.10f, 0.22f);
            coreObj.transform.localScale = new Vector3(0.32f, 0.38f, 0.26f);
            Collider coreCol = coreObj.GetComponent<Collider>();
            if (coreCol != null) coreCol.enabled = false;
            Renderer coreRen = coreObj.GetComponent<Renderer>();
            if (coreRen != null) coreRen.sharedMaterial = parasiteGlowMat;
            weakpointCoreTransform = coreObj.transform;
            parasiteSacs.Add(coreObj.transform);
            parasiteSacBaseScales.Add(coreObj.transform.localScale);

            // 3. 10 BỌC KÝ SINH TRÙNG PHẬP PHỒNG BÁM KHẮP VAI, LƯNG, NGỰC VÀ ĐẦU
            Vector3[] sacOffsets = new Vector3[]
            {
                new Vector3(-0.24f, 0.28f, 0.08f),
                new Vector3( 0.24f, 0.26f, 0.06f),
                new Vector3(-0.18f, 0.22f, -0.20f),
                new Vector3( 0.20f, 0.18f, -0.22f),
                new Vector3( 0.00f, 0.34f, -0.18f),
                new Vector3(-0.28f, 0.05f, -0.10f),
                new Vector3( 0.26f, 0.02f, -0.12f),
                new Vector3( 0.12f, 0.48f, 0.06f),
                new Vector3(-0.14f, -0.08f, 0.18f),
                new Vector3( 0.16f, -0.06f, 0.16f)
            };

            for (int i = 0; i < sacOffsets.Length; i++)
            {
                GameObject sac = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sac.name = "Parasite_Pustule_" + i;
                sac.transform.SetParent(parasiteRoot.transform, false);
                sac.transform.localPosition = sacOffsets[i];
                float sz = 0.12f + (i % 3) * 0.045f;
                Vector3 sc = new Vector3(sz * 1.1f, sz * 1.3f, sz);
                sac.transform.localScale = sc;
                Collider c = sac.GetComponent<Collider>();
                if (c != null) c.enabled = false;
                Renderer r = sac.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = parasiteGlowMat;

                parasiteSacs.Add(sac.transform);
                parasiteSacBaseScales.Add(sc);
            }

            // 4. 6 XÚC TU KÝ SINH TRÙNG DÀI MỌC TỪ LƯNG & VAI UỐN ÉO LIÊN TỤC (PHONG CÁCH THE FOREST 2)
            Vector3[] tentacleDirs = new Vector3[]
            {
                new Vector3(-35f, -45f,  40f),
                new Vector3(-35f,  45f, -40f),
                new Vector3(-55f, -20f,  60f),
                new Vector3(-55f,  20f, -60f),
                new Vector3(-20f, -70f,  25f),
                new Vector3(-20f,  70f, -25f)
            };

            for (int t = 0; t < tentacleDirs.Length; t++)
            {
                GameObject pivot = new GameObject("Parasite_Tentacle_" + t);
                pivot.transform.SetParent(parasiteRoot.transform, false);
                float side = (t % 2 == 0) ? -0.18f : 0.18f;
                pivot.transform.localPosition = new Vector3(side, 0.18f + (t / 2) * 0.08f, -0.18f);
                Quaternion baseRot = Quaternion.Euler(tentacleDirs[t]);
                pivot.transform.localRotation = baseRot;

                // 2 đốt xúc tu + đầu nhọn phát sáng
                GameObject seg1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                seg1.transform.SetParent(pivot.transform, false);
                seg1.transform.localPosition = new Vector3(0f, 0.32f, 0f);
                seg1.transform.localScale = new Vector3(0.09f, 0.32f, 0.09f);
                Collider c1 = seg1.GetComponent<Collider>(); if (c1 != null) c1.enabled = false;
                Renderer r1 = seg1.GetComponent<Renderer>(); if (r1 != null) r1.sharedMaterial = tentacleMat;

                GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tip.transform.SetParent(pivot.transform, false);
                tip.transform.localPosition = new Vector3(0f, 0.66f, 0f);
                tip.transform.localScale = new Vector3(0.11f, 0.16f, 0.11f);
                Collider c2 = tip.GetComponent<Collider>(); if (c2 != null) c2.enabled = false;
                Renderer r2 = tip.GetComponent<Renderer>(); if (r2 != null) r2.sharedMaterial = parasiteGlowMat;

                parasiteTentacles.Add(pivot.transform);
                tentacleBaseRots.Add(baseRot);
            }

            // 5. ĐÈN QUẦNG SÁNG KÝ SINH & ĐÁM MÂY BÀO TỬ ĐỘC BỐC QUANH NGƯỜI BOSS
            GameObject auraObj = new GameObject("Boss_ParasiteAuraLight");
            auraObj.transform.SetParent(parasiteRoot.transform, false);
            auraObj.transform.localPosition = Vector3.zero;
            bossAuraLight = auraObj.AddComponent<Light>();
            bossAuraLight.type = LightType.Point;
            bossAuraLight.color = new Color(0.50f, 1.0f, 0.32f);
            bossAuraLight.range = 18f;
            bossAuraLight.intensity = 3.0f;
            bossAuraLight.shadows = LightShadows.None;

            ParticleSystem sporePS = auraObj.AddComponent<ParticleSystem>();
            var main = sporePS.main;
            main.loop = true;
            main.startLifetime = 2.4f;
            main.startSpeed = 0.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.32f);
            main.startColor = new Color(0.55f, 1.0f, 0.30f, 0.65f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            var em = sporePS.emission;
            em.rateOverTime = 18f;
            var sh = sporePS.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.8f;

            ParticleSystemRenderer psr = auraObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                Texture2D softDot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                for (int py = 0; py < 32; py++)
                {
                    for (int px = 0; px < 32; px++)
                    {
                        float dx = (px - 15.5f) / 15.5f;
                        float dy = (py - 15.5f) / 15.5f;
                        float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                        softDot.SetPixel(px, py, new Color(1f, 1f, 1f, d * d));
                    }
                }
                softDot.Apply();
                Shader pShader = Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Alpha Blended") ?? Shader.Find("Standard");
                Material pMat = new Material(pShader);
                pMat.mainTexture = softDot;
                psr.sharedMaterial = pMat;
            }
        }

        private void AnimateBodyParasites()
        {
            float t = Time.time;
            float pulseSpeed = (currentPhase == BossPhase.Phase3_EnragedCore) ? 9.0f
                             : (currentPhase == BossPhase.Phase2_PsychicFrenzy) ? 6.0f : 3.8f;

            // Đổi màu phát sáng của ký sinh trùng theo Phase
            Color phaseColor = (currentPhase == BossPhase.Phase3_EnragedCore) ? new Color(1.0f, 0.32f, 0.08f)
                             : (currentPhase == BossPhase.Phase2_PsychicFrenzy) ? new Color(0.78f, 0.22f, 1.0f)
                             : new Color(0.45f, 1.0f, 0.28f);

            if (parasiteGlowMat != null) parasiteGlowMat.color = phaseColor;
            if (bossAuraLight != null) bossAuraLight.color = phaseColor;

            // Nhịp phập phồng sống động cho từng bọc ký sinh trùng
            for (int i = 0; i < parasiteSacs.Count; i++)
            {
                if (parasiteSacs[i] == null) continue;
                float beat = 1.0f + Mathf.Sin(t * pulseSpeed + i * 0.9f) * 0.22f;
                parasiteSacs[i].localScale = parasiteSacBaseScales[i] * beat;
            }

            // Xúc tu uốn éo quằn quại liên tục
            for (int i = 0; i < parasiteTentacles.Count; i++)
            {
                if (parasiteTentacles[i] == null) continue;
                float swayX = Mathf.Sin(t * (pulseSpeed * 0.65f) + i * 1.4f) * 18f;
                float swayZ = Mathf.Cos(t * (pulseSpeed * 0.55f) + i * 1.1f) * 15f;
                parasiteTentacles[i].localRotation = tentacleBaseRots[i] * Quaternion.Euler(swayX, 0f, swayZ);
            }
        }

        protected override void Update()
        {
            if (isDead) return;

            // Hiệu ứng ký sinh trùng phập phồng & 6 xúc tu uốn éo liên tục trên cơ thể Boss
            AnimateBodyParasites();

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

            // Thông báo cho hệ thống Kết Game (GameEndingManager) biết Trùm Cuối đã bị hạ!
            if (OnBossDefeatedEvent != null)
            {
                OnBossDefeatedEvent.Invoke();
            }
        }

        private void OnGUI()
        {
            if (isDead) return;

            // Thanh Máu Boss (Boss Health Bar) đặt gọn bên dưới Đồng Hồ Ngày/Đêm (tránh đè chữ!)
            float barWidth = 460f;
            float barHeight = 20f;
            float x = (Screen.width - barWidth) / 2;
            float y = 96f;

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
