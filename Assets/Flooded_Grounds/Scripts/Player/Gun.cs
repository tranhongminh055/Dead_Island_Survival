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
        private Light flashLight; // Đèn chớp lửa khi bắn

        void Start()
        {
            currentAmmo = maxAmmo;
            
            // Tìm Camera tự động nếu quên kéo thả
            if (fpsCam == null)
            {
                fpsCam = Camera.main;
            }

            // Tạo đèn chớp lửa (Muzzle Flash Light) tự động
            GameObject lightObj = new GameObject("MuzzleFlashLight");
            lightObj.transform.parent = transform; 
            // Vị trí tương đối so với súng, đặt nhô lên và ra trước để sáng rõ thân súng
            lightObj.transform.localPosition = new Vector3(0, 0.15f, 0.6f); 
            flashLight = lightObj.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.7f, 0.1f); // Màu cam vàng của lửa
            flashLight.range = 25f; // Tăng tầm chiếu xa
            flashLight.intensity = 0f; // Ban đầu tắt đèn
        }

        void OnEnable()
        {
            isReloading = false; // Hủy thay đạn nếu người chơi đổi súng giữa chừng
        }

        void Update()
        {
            // Làm mờ dần ánh sáng chớp lửa súng nếu đang bật
            if (flashLight != null && flashLight.intensity > 0)
            {
                flashLight.intensity = Mathf.Lerp(flashLight.intensity, 0f, Time.deltaTime * 30f);
            }

            if (isReloading) return;

            // Hết đạn trong băng -> Dừng tiếng súng nếu đang phát
            if (currentAmmo <= 0)
            {
                if (shootSound != null && shootSound.isPlaying) shootSound.Stop();
            }

            // Bấm R -> Thay đạn
            if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
            {
                if (shootSound != null && shootSound.isPlaying) shootSound.Stop();
                StartCoroutine(Reload());
                return;
            }

            // Bấm chuột trái để bắn
            if (Input.GetButton("Fire1"))
            {
                if (currentAmmo <= 0)
                {
                    if (shootSound != null && shootSound.isPlaying) shootSound.Stop();
                    return; // Hết đạn tuyệt đối không cho bắn
                }
                else if (Time.time >= nextTimeToFire)
                {
                    nextTimeToFire = Time.time + 1f / fireRate;
                    Shoot();
                }
            }

            if (Input.GetButtonUp("Fire1"))
            {
                if (shootSound != null && shootSound.isPlaying) shootSound.Stop();
            }
        }

        System.Collections.IEnumerator Reload()
        {
            isReloading = true;
            if (shootSound != null && shootSound.isPlaying) shootSound.Stop();
            Debug.Log("Đang thay đạn (Reloading)...");

            yield return new WaitForSeconds(reloadTime);

            currentAmmo = maxAmmo;
            isReloading = false;
            Debug.Log("Thay đạn xong!");
        }

        void Shoot()
        {
            if (currentAmmo <= 0) return;
            currentAmmo--;

            // 1. Phát tia lửa đầu nòng
            if (muzzleFlash != null)
                muzzleFlash.Play();
            
            // Bật ánh sáng chớp lửa với cường độ mạnh
            if (flashLight != null)
                flashLight.intensity = 8f;

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
                if (enemy == null) enemy = hit.transform.GetComponentInParent<HorrorGame.Enemy.EnemyAI>();
                if (enemy == null) enemy = hit.transform.GetComponentInChildren<HorrorGame.Enemy.EnemyAI>();
                if (enemy != null)
                {
                    float currentDamage = damage;
                    if ((hit.point.y - enemy.transform.position.y) > 1.35f)
                    {
                        currentDamage *= 2f;
                    }
                    enemy.TakeDamage(currentDamage, hit.point);
                    HorrorGame.Enemy.EnemyAI.SpawnBloodImpact(hit.point, hit.normal);
                }

                // Xử lý thú rừng bị bắn trúng (Săn bắt)
                HorrorGame.Survival.AnimalAI animal = hit.transform.GetComponent<HorrorGame.Survival.AnimalAI>();
                if (animal == null) animal = hit.transform.GetComponentInParent<HorrorGame.Survival.AnimalAI>();
                if (animal != null)
                {
                    animal.TakeDamage(damage);
                    Debug.Log("🎯 Bắn trúng " + animal.animalType + "! Damage: " + damage);
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
