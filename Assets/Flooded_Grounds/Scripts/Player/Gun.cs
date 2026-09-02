using UnityEngine;

namespace HorrorGame.Player
{
    public class Gun : MonoBehaviour
    {
        [Header("Thông số súng (Gun Stats)")]
        public float damage = 25f;       // Sát thương mỗi viên đạn
        public float range = 100f;       // Tầm bắn tối đa
        public float fireRate = 10f;     // Tốc độ bắn (viên/giây)
        public float impactForce = 60f;  // Lực đẩy khi bắn trúng vật thể vật lý

        [Header("Hộp đạn (Ammo)")]
        public int maxAmmo = 30;         // Băng đạn tối đa
        private int currentAmmo;
        public float reloadTime = 2f;    // Thời gian thay đạn
        private bool isReloading = false;

        [Header("Hiệu ứng (Effects)")]
        public Camera fpsCam;                    // Camera góc nhìn thứ nhất (để ngắm bắn)
        public ParticleSystem muzzleFlash;       // Tia lửa đầu nòng
        public GameObject hitEffectPrefab;       // Hiệu ứng máu/tia lửa khi trúng đạn
        public AudioSource shootSound;           // Âm thanh tiếng súng nổ

        private float nextTimeToFire = 0f;

        void Start()
        {
            currentAmmo = maxAmmo;
            
            // Tìm Camera tự động nếu quên kéo thả
            if (fpsCam == null)
            {
                fpsCam = Camera.main;
            }
        }

        void OnEnable()
        {
            isReloading = false; // Hủy thay đạn nếu người chơi đổi súng giữa chừng
        }

        void Update()
        {
            if (isReloading) return;

            // Hết đạn hoặc bấm R -> Thay đạn
            if (currentAmmo <= 0 || (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo))
            {
                StartCoroutine(Reload());
                return;
            }

            // Bấm chuột trái để bắn
            // Dùng GetButton thay vì GetButtonDown để có thể sấy (bắn liên thanh)
            if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + 1f / fireRate;
                Shoot();
            }
        }

        System.Collections.IEnumerator Reload()
        {
            isReloading = true;
            Debug.Log("Đang thay đạn (Reloading)...");

            // Có thể thêm âm thanh thay đạn ở đây (reloadSound.Play())
            // Có thể thêm Animation thay đạn ở đây (animator.SetTrigger("Reload"))

            yield return new WaitForSeconds(reloadTime);

            currentAmmo = maxAmmo;
            isReloading = false;
            Debug.Log("Thay đạn xong!");
        }

        void Shoot()
        {
            currentAmmo--;

            // 1. Phát tia lửa đầu nòng
            if (muzzleFlash != null)
                muzzleFlash.Play();

            // 2. Phát âm thanh tiếng súng
            if (shootSound != null)
                shootSound.Play();

            // 3. Tính toán đường đạn (Raycast)
            RaycastHit hit;
            if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
            {
                Debug.Log("Bắn trúng: " + hit.transform.name);

                // Xử lý Zombie bị trúng đạn
                HorrorGame.Enemy.EnemyAI enemy = hit.transform.GetComponent<HorrorGame.Enemy.EnemyAI>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }

                // Nếu bắn trúng vật lý (thùng phuy, xác chết) -> đẩy lùi nó
                if (hit.rigidbody != null)
                {
                    hit.rigidbody.AddForce(-hit.normal * impactForce);
                }

                // 4. Tạo hiệu ứng tia lửa/máu tại điểm trúng đạn
                if (hitEffectPrefab != null)
                {
                    GameObject impactGO = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impactGO, 2f); // Xóa hiệu ứng sau 2 giây cho nhẹ máy
                }
            }
        }
    }
}
