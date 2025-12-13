# 🎮 Multiplayer Firebase Authentication - Hướng dẫn cài đặt

## 📋 Tóm tắt thay đổi

Dự án TinyFarm đã được cập nhật để hỗ trợ **multiplayer** bằng cách thay thế tất cả hardcoded `"Player1"` bằng **User ID động từ Firebase Authentication**.

### ✅ Các thay đổi chính:

1. **Tạo PlayerSession.cs** - Quản lý session người chơi hiện tại
2. **Cập nhật FirebaseLogin.cs** - Lưu User ID sau khi login thành công
3. **Thay thế tất cả "Player1"** bằng `PlayerSession.GetCurrentUserId()` trong 18+ file

---

## 🔧 Cài đặt

### Bước 1: Đảm bảo PlayerSession.cs tồn tại
- ✅ File đã được tạo tại: `Assets/Scripts/Firebase/PlayerSession.cs`
- Nó là Singleton, tự động khởi tạo nếu chưa tồn tại

### Bước 2: Cấu hình FirebaseLogin
- ✅ Đã cập nhật `Assets/Scripts/Firebase/FirebaseLogin.cs`
- Lưu User ID tự động sau khi login thành công:
```csharp
FirebaseUser user = task.Result.User;
PlayerSession.SetCurrentUserId(user.UserId);
```

### Bước 3: Đảm bảo các file đã được cập nhật
Các file sau đã được cập nhật để sử dụng `PlayerSession.GetCurrentUserId()`:

**Firebase & Loading:**
- FirebaseDatabaseManager.cs
- FarmLoadingManager.cs
- FirebaseLogin.cs

**Player & Action:**
- PlayerHandler.cs (save farm khi ngủ, trồng, thu hoạch)
- PlayerMoney.cs
- DayAndNightManager.cs

**Inventory & Item:**
- InventoryManager.cs
- DraggableItem.cs

**Farm & Crop:**
- FarmLoader.cs (CropManager & MapSumer)
- Crop.cs
- MoneyLoader.cs

**Shop & Order:**
- ShopDetailPanel.cs
- OrderDetailUI.cs

**Door/Scene:**
- openDoor.cs

**Loaders:**
- FarmLoader.cs (CropManager)
- MoneyLoader.cs
- TestMoney.cs

---

## 🎯 Cách sử dụng

### Lấy User ID hiện tại:
```csharp
string currentUserId = PlayerSession.GetCurrentUserId();
```

### Kiểm tra người dùng đã login?
```csharp
if (PlayerSession.IsUserLoggedIn())
{
    // User đã login
}
```

### Clear session (khi logout):
```csharp
PlayerSession.ClearSession();
```

### Set User ID thủ công:
```csharp
PlayerSession.SetCurrentUserId("user123");
```

---

## 🔄 Workflow Login → Game

```
1. User đăng nhập (FirebaseLogin.cs)
   ↓
2. Firebase Authentication xác thực
   ↓
3. SignInAccountWithFireBase() thành công
   ↓
4. PlayerSession.SetCurrentUserId(user.UserId) ← Lưu ID
   ↓
5. Load Scene "mapSummer"
   ↓
6. FarmLoader → FarmLoadingManager → LoadDayAndTimeFromFirebase(userId)
   ↓
7. Tất cả Save/Load sử dụng PlayerSession.GetCurrentUserId()
```

---

## ✨ Lợi ích

### ✅ Multiplayer Support
- Mỗi player có ID riêng từ Firebase
- Dữ liệu của mỗi player được lưu riêng biệt
- Hỗ trợ nhiều tài khoản trên cùng device

### ✅ Tự động hóa
- Không cần hardcode ID
- UserID tự động được lấy từ đối tượng logged in
- Dễ dàng mở rộng cho nhiều player khác

### ✅ Bảo mật
- Sử dụng Firebase Authentication ID chính thức
- Không có ID cố định hoặc mặc định
- Mỗi player có dữ liệu riêng an toàn

---

## 🛠️ Files được sửa đổi (18+ files)

```
✅ Assets/Scripts/Firebase/
   - PlayerSession.cs (NEW)
   - FirebaseLogin.cs (UPDATED)
   - FirebaseDatabaseManager.cs (UPDATED)
   - FarmLoadingManager.cs (UPDATED)

✅ Assets/Scripts/Player/
   - PlayerHandler.cs (UPDATED)

✅ Assets/Scripts/MoneyManager/
   - PlayerMoney.cs (UPDATED)
   - MoneyLoader.cs (UPDATED)
   - TestMoney.cs (UPDATED)

✅ Assets/Scripts/InventoryManagement/
   - InventoryManager.cs (UPDATED)
   - DraggableItem.cs (UPDATED)

✅ Assets/Scripts/CropManager/
   - FarmLoader.cs (UPDATED)
   - Crop.cs (UPDATED)

✅ Assets/Scripts/MapSumer/
   - FarmLoader.cs (UPDATED)

✅ Assets/Scripts/DayTimeManager/
   - DayAndNightManager.cs (UPDATED)

✅ Assets/Scripts/ShopManager/
   - ShopDetailPanel.cs (UPDATED)

✅ Assets/Scripts/Door/
   - openDoor.cs (UPDATED)

✅ Assets/Scripts/OrderManager/
   - OrderDetailUI.cs (UPDATED)
```

---

## 📝 Notes quan trọng

### ⚠️ Chú ý
1. **Phải login trước**: Player bắt buộc phải login trước khi vào game
   - PlayerSession sẽ trả về "" nếu chưa login
   - Thêm kiểm tra `PlayerSession.IsUserLoggedIn()` nếu cần bắt buộc

2. **DontDestroyOnLoad**: PlayerSession được đánh dấu `DontDestroyOnLoad`
   - Nó sẽ tồn tại qua các scene load
   - Dữ liệu session được giữ lại

3. **Firebase Must Be Ready**: 
   - FirebaseDatabaseManager phải được khởi tạo trước
   - Các loader sẽ tự động retry nếu Firebase chưa ready

### 💡 Cải tiến tương lai
- [ ] Thêm fallback nếu Player chưa login
- [ ] Thêm logout functionality
- [ ] Thêm player profile (avatar, tên, vv)
- [ ] Thêm multi-device sync
- [ ] Thêm cache local cho offline mode

---

## 🐛 Troubleshooting

### Vấn đề: Save/Load không work
**Giải pháp:**
1. Kiểm tra `PlayerSession.GetCurrentUserId()` có return "" không
2. Xác nhận user đã login: `PlayerSession.IsUserLoggedIn()`
3. Kiểm tra Firebase có ready: `FirebaseDatabaseManager.FirebaseReady`

### Vấn đề: Multiple players bị overwrite
**Giải pháp:**
1. Xác nhận mỗi account có User ID khác nhau
2. Kiểm tra `SignInAccountWithFireBase()` lưu đúng userId
3. Xem logs: `"Player logged in with ID: ..."`

### Vấn đề: Forgot to login
**Giải pháp:**
1. Thêm check trong Loaders:
```csharp
if (!PlayerSession.IsUserLoggedIn())
{
    Debug.LogError("Player must login first!");
    return;
}
```

---

## 📞 Hỗ trợ
- Check Firebase Authentication: https://console.firebase.google.com
- Xem Logs trong Unity Console
- Verify User ID format (thường là string dài ~ 28 ký tự)

**Update Date:** December 13, 2025  
**Status:** ✅ Multiplayer Support Implemented
