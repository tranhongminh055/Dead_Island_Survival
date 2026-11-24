import sys
import re

def fix_airplane_crash():
    file_path = "d:/Project Unity/Survival/Assets/Flooded_Grounds/Scripts/Cutscenes/AirplaneCrashCutscene.cs"
    with open(file_path, "r", encoding="utf-8", errors="surrogateescape") as f:
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
    
    # 1. Remove the comment around smoke
    content = re.sub(r'/\*\s*T.*?KHA"A PARTICLE VAO THI.*?_U TEXTURE', '', content)
    
    # 2. Fix the smoke material part
    content = re.sub(r'Shader smokeShader = Shader\.Find\("Particles/Standard Unlit"\);[\s\S]*?ps\.GetComponent<ParticleSystemRenderer>\(\)\.sharedMaterial = smokeMat;\s*\}',
                     'ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();', content)
    
    # 2b. Also remove the closing comment if it is trailing
    content = re.sub(r'ps\.GetComponent<ParticleSystemRenderer>\(\)\.sharedMaterial = GetSoftParticleMaterial\(\);\s*\*/',
                     'ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();', content)

    # 3. Fix Wreck_Fire block 
    content = re.sub(r'Shader fireShader = Shader\.Find\("Particles/Standard Unlit"\);[\s\S]*?pFire\.GetComponent<ParticleSystemRenderer>\(\)\.sharedMaterial = fireMat;\s*\}',
                     'pFire.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();', content)
    
    # Handle the previous fix
    content = re.sub(r'// B[^\n]*\s*var pRenderer = pFire\.GetComponent<ParticleSystemRenderer>\(\);[\s\S]*?pRenderer\.sharedMaterial = defaultMat;\s*\}\s*\}',
                     'pFire.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();', content)

    with open(file_path, "w", encoding="utf-8", errors="surrogateescape") as f:
        f.write(content)

def fix_airplane_exterior():
    file_path = "d:/Project Unity/Survival/Assets/Flooded_Grounds/Scripts/Cutscenes/AirplaneCabinExterior.cs"
    with open(file_path, "r", encoding="utf-8", errors="surrogateescape") as f:
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

        private void CreateRainParticles()"""

    if "GetSoftParticleMaterial" not in content:
        content = content.replace("private void CreateRainParticles()", helper_method)
    
    content = re.sub(r'Shader fireShader = Shader\.Find\("Particles/Standard Unlit"\);[\s\S]*?if \(psr != null\) psr\.sharedMaterial = fireMat;\s*\}',
                     'if (fireObj.GetComponent<ParticleSystemRenderer>() != null) fireObj.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();', content)

    with open(file_path, "w", encoding="utf-8", errors="surrogateescape") as f:
        f.write(content)

fix_airplane_crash()
fix_airplane_exterior()
print("Fixed successfully!")

