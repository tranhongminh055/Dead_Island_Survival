using UnityEngine;
using UnityEditor;
using System.IO;

public class FixParticles
{
    [MenuItem("Tools/Fix Particles")]
    public static void Fix()
    {
        string fileCrash = "Assets/Flooded_Grounds/Scripts/Cutscenes/AirplaneCrashCutscene.cs";
        string content = File.ReadAllText(fileCrash);
        
        string helperMethod = @"        private Material softParticleMat;
        private Material GetSoftParticleMaterial()
        {
            if (softParticleMat != null) return softParticleMat;
            Shader shader = Shader.Find(""Legacy Shaders/Particles/Additive"");
            if (shader == null) shader = Shader.Find(""Particles/Standard Unlit"");
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

        private void CreateCrashEffects(Transform wreckTarget)";

        if (!content.Contains("GetSoftParticleMaterial"))
        {
            content = content.Replace("private void CreateCrashEffects(Transform wreckTarget)", helperMethod);
        }

        // Fix Wreck_Smoke section
        // We know there's a comment `/* TẠM KHÓA PARTICLE VÌ THIẾU TEXTURE`
        // We will just find `Shader smokeShader = Shader.Find("Particles/Standard Unlit");` and replace the whole block manually
        
        int idxSmoke = content.IndexOf("Shader smokeShader = Shader.Find(\"Particles/Standard Unlit\");");
        if (idxSmoke != -1)
        {
            int endIdx = content.IndexOf("}", idxSmoke) + 1;
            string before = content.Substring(0, idxSmoke);
            string after = content.Substring(endIdx);
            content = before + "ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();" + after;
        }

        content = content.Replace("/* TẠM KHÓA PARTICLE VÌ THIẾU TEXTURE", "");
        content = content.Replace("ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();\r\n            }\r\n            */", "ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();");
        content = content.Replace("ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();\n            }\n            */", "ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();");


        // Fix Wreck_Fire block
        int idxFire = content.IndexOf("Shader fireShader = Shader.Find(\"Particles/Standard Unlit\");");
        if (idxFire != -1)
        {
            int endIdx = content.IndexOf("}", idxFire) + 1;
            string before = content.Substring(0, idxFire);
            string after = content.Substring(endIdx);
            content = before + "pFire.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();" + after;
        }
        
        // Remove old 'var pRenderer' fix block if it exists
        int pRendererIdx = content.IndexOf("var pRenderer = pFire.GetComponent<ParticleSystemRenderer>();");
        if (pRendererIdx != -1)
        {
            // find the end of the block '}' twice
            int firstClose = content.IndexOf("}", pRendererIdx);
            int secondClose = content.IndexOf("}", firstClose + 1) + 1;
            
            // find start of the comment above it
            int commentIdx = content.LastIndexOf("// B", pRendererIdx);
            if (commentIdx != -1 && (pRendererIdx - commentIdx) < 150)
            {
                pRendererIdx = commentIdx;
            }
            
            string before = content.Substring(0, pRendererIdx);
            string after = content.Substring(secondClose);
            content = before + "pFire.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();" + after;
        }

        File.WriteAllText(fileCrash, content);
        
        // --- AirplaneCabinExterior ---
        string fileExt = "Assets/Flooded_Grounds/Scripts/Cutscenes/AirplaneCabinExterior.cs";
        string extContent = File.ReadAllText(fileExt);

        string extHelper = @"        private Material softParticleMat;
        private Material GetSoftParticleMaterial()
        {
            if (softParticleMat != null) return softParticleMat;
            Shader shader = Shader.Find(""Legacy Shaders/Particles/Additive"");
            if (shader == null) shader = Shader.Find(""Particles/Standard Unlit"");
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

        private void CreateRainParticles()";

        if (!extContent.Contains("GetSoftParticleMaterial"))
        {
            extContent = extContent.Replace("private void CreateRainParticles()", extHelper);
        }

        int extFireIdx = extContent.IndexOf("Shader fireShader = Shader.Find(\"Particles/Standard Unlit\");");
        if (extFireIdx != -1)
        {
            int extEndIdx = extContent.IndexOf("}", extFireIdx) + 1;
            string before = extContent.Substring(0, extFireIdx);
            string after = extContent.Substring(extEndIdx);
            extContent = before + "if (fireObj.GetComponent<ParticleSystemRenderer>() != null) fireObj.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetSoftParticleMaterial();" + after;
        }

        File.WriteAllText(fileExt, extContent);
        
        Debug.Log("Particle materials fixed!");
    }
}
