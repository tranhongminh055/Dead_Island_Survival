using UnityEngine;
using System.Collections;
using HorrorGame.Player;

namespace HorrorGame.Enemy
{
    /// <summary>
    /// MagmaZombieAI — Biến thể Zombie Nham Thạch (Hang 5 — The Volcanic Vent).
    /// - Da nứt nẻ tỏa ánh sáng đỏ cam nham thạch nóng chảy.
    /// - Miễn nhiễm hoàn toàn với sát thương Lửa (đuốc, pháo sáng, tên lửa).
    /// - Đòn đánh thiêu đốt (Burning Touch): Gây sát thương tức thời kèm hiệu ứng cháy (DOT 3s).
    /// - Khi chết phát nổ một bọc dung nham nhỏ gây sát thương lan (Magma Death Burst).
    /// </summary>
    public class MagmaZombieAI : EnemyAI
    {
        [Header("── Thuộc Tính Nham Thạch ──")]
        public float burnDamagePerSecond = 4.0f;
        public float burnDuration = 3.0f;
        public float deathExplosionRadius = 3.5f;
        public float deathExplosionDamage = 20.0f;

        [Header("── Âm Thanh & Hiệu Ứng Lửa ──")]
        public AudioClip lavaHissSound;
        public AudioClip explodeSound;
        public ParticleSystem burnParticles;
        public ParticleSystem deathExplosionParticles;

        private AudioSource magmaAudio;

        protected override void Start()
        {
            base.Start();

            maxHealth = 130f;
            currentHealth = maxHealth;
            walkSpeed = 1.4f;
            runSpeed = 3.4f;
            damage = 12f;
            attackRange = 2.2f;

            magmaAudio = GetComponent<AudioSource>();
            if (magmaAudio == null)
            {
                magmaAudio = gameObject.AddComponent<AudioSource>();
                magmaAudio.spatialBlend = 1f;
                magmaAudio.maxDistance = 25f;
            }

            if (burnParticles != null && !burnParticles.isPlaying)
            {
                burnParticles.Play();
            }
        }

        protected override void Update()
        {
            if (isDead) return;

            // Kiểm tra tấn công người chơi và thiêu đốt
            if (player != null && Time.time >= lastAttackTime + attackCooldown)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= attackRange)
                {
                    // Đòn tấn công cơ sở đã gọi trong base.Update()
                    StartCoroutine(ApplyBurnDamageRoutine());
                }
            }

            base.Update();
        }

        private IEnumerator ApplyBurnDamageRoutine()
        {
            if (magmaAudio != null && lavaHissSound != null)
            {
                magmaAudio.PlayOneShot(lavaHissSound);
            }

            if (player == null) yield break;
            var stats = player.GetComponentInParent<PlayerStats>();
            if (stats == null) stats = player.GetComponentInChildren<PlayerStats>();
            if (stats == null) yield break;

            Debug.Log("<color=orange>[MagmaZombieAI]</color> Người chơi bị dính vết bỏng nham thạch!");

            float elapsed = 0f;
            while (elapsed < burnDuration)
            {
                elapsed += 1.0f;
                stats.TakeDamage(burnDamagePerSecond);
                yield return new WaitForSeconds(1.0f);
            }
        }

        public override void TakeDamage(float amount, Vector3 hitPoint)
        {
            base.TakeDamage(amount, hitPoint);

            // Nếu máu về 0 và chết: Kích hoạt vụ nổ dung nham
            if (currentHealth <= 0f && !isDead)
            {
                TriggerMagmaDeathExplosion();
            }
        }

        private void TriggerMagmaDeathExplosion()
        {
            if (magmaAudio != null && explodeSound != null)
            {
                magmaAudio.PlayOneShot(explodeSound);
            }

            if (deathExplosionParticles != null)
            {
                deathExplosionParticles.transform.position = transform.position + Vector3.up * 1f;
                deathExplosionParticles.Play();
            }

            // Gây sát thương nổ lan cho player nếu đứng quá gần
            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= deathExplosionRadius)
                {
                    var stats = player.GetComponentInParent<PlayerStats>();
                    if (stats == null) stats = player.GetComponentInChildren<PlayerStats>();
                    if (stats != null)
                    {
                        stats.TakeDamage(deathExplosionDamage);
                        Debug.Log("<color=red>[MagmaZombieAI]</color> Player dính vụ nổ dung nham khi Zombie chết!");
                    }
                }
            }
        }
    }
}
