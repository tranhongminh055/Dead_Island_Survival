import sys
import re

file_path = "d:/Project Unity/Survival/Assets/Flooded_Grounds/Scripts/Cutscenes/AirplaneCrashCutscene.cs"

with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# FIX 1: Fire material and size
fire_material_old = """            Shader fireShader = Shader.Find("Particles/Standard Unlit");
            if (fireShader == null) fireShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (fireShader == null) fireShader = Shader.Find("Mobile/Particles/Additive");
            if (fireShader != null) {
                Material fireMat = new Material(fireShader);
                fireMat.color = new Color(1f, 0.4f, 0.1f, 0.8f);
                if (fireMat.HasProperty("_TintColor")) fireMat.SetColor("_TintColor", new Color(1f, 0.4f, 0.1f, 0.8f));
                pFire.GetComponent<ParticleSystemRenderer>().sharedMaterial = fireMat;
            }"""

fire_material_new = """            // Bỏ gán material để Unity tự động dùng Default-Particle (khắc phục lỗi ô vuông màu cam)
            var pRenderer = pFire.GetComponent<ParticleSystemRenderer>();
            if (pRenderer != null) {
                Material defaultMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
                if (defaultMat != null && defaultMat.shader != null) {
                    pRenderer.sharedMaterial = defaultMat;
                }
            }"""

if fire_material_old in content:
    content = content.replace(fire_material_old, fire_material_new)

fire_shape_old = """            fShape.shapeType = ParticleSystemShapeType.Hemisphere;
            fShape.radius = 1.5f;"""

fire_shape_new = """            fShape.shapeType = ParticleSystemShapeType.Hemisphere;
            fShape.radius = 8.0f; // Tăng bán kính bốc lửa bao trùm xác máy bay
            fMain.startSize = 4.0f; // Lửa to hơn"""

if fire_shape_old in content:
    content = content.replace(fire_shape_old, fire_shape_new)

# FIX 2: Cabin Light blinking at crash site
smoke_old = """            // TÃ¡ÂºÂ¡o khÃƒÂ³i Ã„â€˜en
            GameObject smokeObj = new GameObject("Wreck_Smoke");"""

if smoke_old not in content:
    # Try alternate encoding string if needed, or regex
    smoke_old = r'// [^\n]*\n\s*GameObject smokeObj = new GameObject\("Wreck_Smoke"\);'

smoke_new = """            // Bật lại đèn trong khoang và cho chớp tắt liên tục
            Transform intLight = wreckTarget.Find("C400_InteriorLight");
            if (intLight != null) {
                Light lit = intLight.GetComponent<Light>();
                if (lit != null) {
                    lit.enabled = true;
                    lit.color = new Color(0.8f, 0.9f, 1f); // Trắng xanh chập mạch
                    var intFlicker = intLight.gameObject.AddComponent<LightFlicker_Runtime>();
                    intFlicker.lightSource = lit;
                }
            }

            // Tạo khói đen
            GameObject smokeObj = new GameObject("Wreck_Smoke");"""

content = re.sub(smoke_old, smoke_new, content)

# FIX 3: Camera Parent in Start()
start_cam_old = """                Camera cam = playerController.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    // FIX: NAAA,AA,Au game"""

# we need a flexible regex for Start()
import re
start_regex = r'(Camera cam = playerController\.GetComponentInChildren<Camera>\(\);\s*if \(cam != null\)\s*\{)([\s\S]*?)(originalCamLocalPos = cam\.transform\.localPosition;)'

start_new = r"""\1
                    // Đưa camera vào xương Head để không bị lùi lại sau lưng khi có animation di chuyển người
                    Animator anim = playerController.GetComponentInChildren<Animator>();
                    if (anim != null) {
                        Transform headBone = anim.GetBoneTransform(HumanBodyBones.Head);
                        if (headBone != null && cam.transform.parent != headBone) {
                            cam.transform.SetParent(headBone, true);
                        }
                    }

                    \3"""

content = re.sub(start_regex, start_new, content)

# FIX 4: Camera offset in SeatPlayerInAirplane
seat_cam_old = r'(cutsceneCamera = cam\.transform;\s*)cam\.transform\.localPosition = new Vector3\(0f, 1\.6f, 0f\);'
seat_cam_new = r"""\1if (cam.transform.parent != null && cam.transform.parent.name.ToLower().Contains("head")) {
                          cam.transform.localPosition = Vector3.zero;
                      } else {
                          cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                      }"""

content = re.sub(seat_cam_old, seat_cam_new, content)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)

print("Fixes applied successfully via python script.")
