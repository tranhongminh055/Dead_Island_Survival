import sys

file_path = "d:/Project Unity/Survival/Assets/Flooded_Grounds/Scripts/Cutscenes/AirplaneCrashCutscene.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

update_method_old = """        private void Update()
        {
            if (isCabinFreeLookActive && cutsceneCamera != null)
            {
                HandleCabinMouseLook();
            }
        }"""

update_method_new = """        private bool isSkipped = false;

        private void Update()
        {
            // Bỏ qua cutscene khi bấm Space, Enter hoặc Escape
            if (!isSkipped && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape)))
            {
                SkipCutscene();
            }

            if (isCabinFreeLookActive && cutsceneCamera != null)
            {
                HandleCabinMouseLook();
            }
        }

        private void SkipCutscene()
        {
            isSkipped = true;
            StopAllCoroutines();

            if (audioSource != null) audioSource.Stop();
            if (voiceAudioSource != null) voiceAudioSource.Stop();
            
            if (whiteFlashUI != null) whiteFlashUI.gameObject.SetActive(false);
            if (blackScreenUI != null) blackScreenUI.gameObject.SetActive(false);
            if (uiParent != null) uiParent.gameObject.SetActive(false);
            
            ClearSubtitle();

            if (CinematicPostProcessing.Instance != null)
            {
                CinematicPostProcessing.Instance.SetColorTint(Color.clear, 0f);
                CinematicPostProcessing.Instance.AnimateLetterbox(0f, 0f);
                CinematicPostProcessing.Instance.FadeScreenDamage(0f, 0f);
                CinematicPostProcessing.Instance.FadeVignette(0f, Color.clear, 0f);
            }

            TeleportToForestLyingDown();

            Camera cam = playerController != null ? playerController.GetComponentInChildren<Camera>() : null;
            if (cam != null && hasSavedCamTransform)
            {
                cam.transform.localPosition = originalCamLocalPos;
                cam.transform.localRotation = originalCamLocalRot;
            }

            SetPlayerRenderersVisible(true);
            SetGameplayUIVisible(true);

            if (playerController != null)
            {
                CharacterController cc = playerController.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;
                playerController.enabled = true;
            }

            // Cleanup objects
            Transform alignPivot = airplaneCabin != null ? airplaneCabin.transform.Find("CabinAlignmentPivot") : null;
            if (alignPivot != null) Destroy(alignPivot.gameObject);

            if (gameTitleGroup != null) gameTitleGroup.alpha = 0f;

            // Xóa script để không tốn tài nguyên
            Destroy(gameObject, 0.5f);
        }"""

if "SkipCutscene()" not in content:
    if update_method_old in content:
        content = content.replace(update_method_old, update_method_new)
        with open(file_path, "w", encoding="utf-8") as f:
            f.write(content)
        print("Skip trailer feature restored successfully.")
    else:
        print("Could not find exact Update method to replace.")
else:
    print("SkipCutscene already exists.")
