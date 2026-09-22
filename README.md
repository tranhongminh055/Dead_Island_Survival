# 🌲 Dead Island Survival (Horror Survival Game)

![Unity Version](https://img.shields.io/badge/Unity-2021%2B-black?style=flat-square&logo=unity)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Mac-blue?style=flat-square)
![Genre](https://img.shields.io/badge/Genre-Survival%20Horror-red?style=flat-square)

**Dead Island Survival** là một dự án game sinh tồn kinh dị góc nhìn thứ nhất (FPS) được phát triển trên nền tảng Unity 3D. Lấy cảm hứng từ những tựa game sinh tồn đình đám như *The Forest* hay *Stranded Deep*, người chơi sẽ vào vai một hành khách sống sót sau một vụ tai nạn máy bay thảm khốc và rơi xuống một hòn đảo/khu rừng hoang vắng, nơi ẩn chứa vô vàn nguy hiểm và những sinh vật khát máu (Zombies/Mutants).

---

## 🌟 Tính năng nổi bật

### ✈️ Cutscene Mở Đầu Ấn Tượng (Cinematic Intro)
- Trải nghiệm cảm giác chân thực trên khoang máy bay C400 trước khi thảm họa xảy ra.
- Hệ thống rung lắc camera (Camera Shake), hiệu ứng đèn chớp tắt, mặt nạ oxy tự động rơi xuống.
- Góc nhìn thứ nhất tự do (Free Look) bên trong khoang cabin, kết hợp với âm thanh sống động (tiếng động cơ, thông báo của cơ trưởng, tiếng hú báo động, tiếng nổ chói tai).

### 🏕️ Hệ thống Sinh Tồn (Survival Mechanics)
Người chơi phải liên tục theo dõi và duy trì các chỉ số sinh tồn cốt lõi để sống sót:
- 🩸 **Health (Máu):** Giảm khi bị tấn công, đói khát hoặc ngã từ trên cao.
- ⚡ **Stamina (Thể Lực):** Tiêu hao khi chạy nước rút, tấn công hoặc thu thập tài nguyên.
- 🍗 **Hunger (Độ Đói):** Cần săn bắt, hái lượm thức ăn để duy trì.
- 💧 **Thirst (Độ Khát):** Cần tìm nguồn nước sạch để uống.
- 🧠 **Sanity (Tinh Thần):** Giảm khi chứng kiến các hiện tượng kinh dị hoặc ở trong bóng tối quá lâu.

### 🧟 Kẻ Địch AI (Zombie / Mutant)
- Kẻ thù thông minh (Enemy AI) có khả năng nghe tiếng động, đuổi theo và tấn công người chơi.
- Hệ thống Zombie Spawner động: Zombie tự động sinh ra xung quanh các khu vực trọng yếu (giống hệ thống Patrol / Horde).
- Tự động dọn dẹp Zombie xung quanh khu vực tai nạn ban đầu để đảm bảo người chơi có thời gian an toàn lúc mới tỉnh dậy.

### 🛠️ Tương Tác & Chế Tạo (Crafting & Interaction)
- Thu thập tài nguyên thiên nhiên (gỗ, đá, lá cây).
- Sử dụng vũ khí cận chiến và súng để sinh tồn.
- Các bộ script Editor hỗ trợ mạnh mẽ việc tự động gắn va chạm (Auto Collider) cho các mô hình phức tạp như mảnh vỡ máy bay, đảm bảo người chơi tương tác vật lý chuẩn xác.

---

## 📂 Cấu trúc thư mục (Highlights)

- `Assets/Flooded_Grounds/Scripts/Cutscenes`: Chứa các kịch bản đoạn phim cắt cảnh (như `AirplaneCrashCutscene.cs`).
- `Assets/Flooded_Grounds/Scripts/Player`: Chứa logic điều khiển nhân vật, chỉ số sinh tồn (`PlayerController`, `PlayerStats`,...).
- `Assets/Flooded_Grounds/Scripts/Effects/Enemy`: AI kẻ địch và trình tạo quái vật (Spawner).
- `Assets/Flooded_Grounds/Scripts/Editor`: Các bộ công cụ (Tools) tự động hóa trong Unity Editor như sửa lỗi hở mô hình máy bay, tạo cabin, sinh vách tường cong,...

---

## 🚀 Hướng dẫn Cài đặt & Chạy Game

1. **Yêu cầu phần mềm:** 
   - [Unity Hub](https://unity.com/download) và Unity Editor phiên bản tương thích (Khuyên dùng bản 2021.3 LTS hoặc mới hơn).
   - Git để clone mã nguồn.
2. **Clone mã nguồn:**
   ```bash
   git clone https://github.com/tranhongminh055/Dead_Island_Survival.git
   ```
3. **Mở Project:**
   - Mở Unity Hub -> Chọn `Add` / `Open` -> Trỏ đến thư mục `Survival` vừa tải về.
   - Chờ Unity import các tài nguyên (textures, models, âm thanh). Quá trình này có thể mất vài phút ở lần đầu tiên.
4. **Trải nghiệm:**
   - Vào thư mục `Assets/Flooded_Grounds/Scenes/` và mở Scene chính (VD: `Scene_A.unity`).
   - Nhấn nút **Play** ở góc trên giữa màn hình để bắt đầu trải nghiệm intro tai nạn máy bay và tiến vào thế giới sinh tồn!

---

## 🛠️ Công nghệ / Plugin sử dụng
- **Engine:** Unity 3D
- **Ngôn ngữ:** C# (C-Sharp)
- Môi trường và ánh sáng động (Dynamic GI, Day-Night Cycle).

---

## 📝 Tác giả
- Phát triển bởi: [tranhongminh055](https://github.com/tranhongminh055)
- Repository: [Dead_Island_Survival](https://github.com/tranhongminh055/Dead_Island_Survival)

*Chúc bạn sống sót thành công trên hòn đảo tử thần!* 🧟‍♂️🔪
