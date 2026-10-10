import sys

def fix_airplane_crash():
    file_path = "d:/Project Unity/Survival/Assets/Flooded_Grounds/Scripts/Cutscenes/AirplaneCrashCutscene.cs"
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()

    helper_method = """        private Material softParticleMat;
        private Material GetSoftParticleMaterial()
        {
            if (softParticleMat != null) return softParticleMat;
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            softParticleMat = new Material(shader);
            Texture2D tex = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            for (int y = 0; y < 32; y++) {
                for (int x = 0; x < 32; x++) {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    float alpha = Mathf.Clamp01(1f - (dist / 15.5f));
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            softParticleMat.mainTexture = tex;
            return softParticleMat;
        }

        private void CreateCrashEffects(Transform wreckTarget)"""

    if "GetSoftParticleMaterial" not in content:
        content = content.replace("private void CreateCrashEffects(Transform wreckTarget)", helper_method)

    # Manual string replacements
    smoke_shader_old = """            Shader smokeShader = Shader.Find("Particles/Standard Unlit");
            if (smokeShader == null) smokeShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (smokeShader == null) smokeShader = Shader.Find("Mobile/Particles/Alpha Blended");
            if (smokeShader != null) {
                Material smokeMat = new Material(smokeShader);
                smokeMat.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
                if (smokeMat.HasProperty("_TintColor")) smokeMat.SetColor("_TintColor", new Color(0.1f, 0.1f, 0.1f, 0.6f));
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = smokeMat;
            }"""
    content = content.replace(smoke_shader_old, "            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();")
    
    # Remove the comment tags
    content = content.replace("/* TẠM KHÓA PARTICLE VÌ THIẾU TEXTURE", "")
    content = content.replace("ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();\n            }\n            */", "ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();")
    content = content.replace("ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();\r\n            }\r\n            */", "ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();")

    fire_shader_old = """            Shader fireShader = Shader.Find("Particles/Standard Unlit");
            if (fireShader == null) fireShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (fireShader == null) fireShader = Shader.Find("Mobile/Particles/Additive");
            if (fireShader != null) {
                Material fireMat = new Material(fireShader);
                fireMat.color = new Color(1f, 0.4f, 0.1f, 0.8f);
                if (fireMat.HasProperty("_TintColor")) fireMat.SetColor("_TintColor", new Color(1f, 0.4f, 0.1f, 0.8f));
                pFire.GetComponent<ParticleSystemRenderer>().sharedMaterial = fireMat;
            }"""
    content = content.replace(fire_shader_old, "            pFire.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();")

    with open(file_path, "w", encoding="utf-8") as f:
        f.write(content)

def fix_airplane_exterior():
    file_path = "d:/Project Unity/Survival/Assets/Flooded_Grounds/Scripts/Cutscenes/AirplaneCabinExterior.cs"
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()

    ext_helper = """        private Material softParticleMat;
        private Material GetSoftParticleMaterial()
        {
            if (softParticleMat != null) return softParticleMat;
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            softParticleMat = new Material(shader);
            Texture2D tex = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            for (int y = 0; y < 32; y++) {
                for (int x = 0; x < 32; x++) {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    float alpha = Mathf.Clamp01(1f - (dist / 15.5f));
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            softParticleMat.mainTexture = tex;
            return softParticleMat;
        }

        private void CreateRainParticles()"""

    if "GetSoftParticleMaterial" not in content:
        content = content.replace("private void CreateRainParticles()", ext_helper)

    fire_shader_old = """            Shader fireShader = Shader.Find("Particles/Standard Unlit");
            if (fireShader == null) fireShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (fireShader == null) fireShader = Shader.Find("Mobile/Particles/Additive");
            if (fireShader != null)
            {
                Material fireMat = new Material(fireShader);
                fireMat.color = new Color(1f, 0.5f, 0.1f, 0.8f);
                var psr = fireObj.GetComponent<ParticleSystemRenderer>();
                if (psr != null) psr.sharedMaterial = fireMat;
            }"""
    content = content.replace(fire_shader_old, "            if (fireObj.GetComponent<ParticleSystemRenderer>() != null) fireObj.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();")

    with open(file_path, "w", encoding="utf-8") as f:
        f.write(content)

fix_airplane_crash()
fix_airplane_exterior()
print("Fixed exact matches!")
