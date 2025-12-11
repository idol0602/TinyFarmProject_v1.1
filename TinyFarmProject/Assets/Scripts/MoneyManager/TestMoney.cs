#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class TestMoney : MonoBehaviour
{
    private const string PLAYER_ID = "Player1"; // Đổi nếu cần test nhiều người chơi

    // ---------------------------------------------------------
    // TEST: Thêm tiền
    // ---------------------------------------------------------
#if UNITY_EDITOR
    [ContextMenu("TEST: Add +10,000 Money")]
    private void TestAddMoney()
    {
        if (PlayerMoney.Instance != null)
        {
            PlayerMoney.Instance.Add(10000);
            Debug.Log("<color=lime><b>+10,000đ</b> (Test Add Money)</color>");
        }
    }
#endif

    // ---------------------------------------------------------
    // TEST: Trừ tiền
    // ---------------------------------------------------------
#if UNITY_EDITOR
    [ContextMenu("TEST: Subtract -5,000 Money")]
    private void TestSubtractMoney()
    {
        if (PlayerMoney.Instance != null)
        {
            bool success = PlayerMoney.Instance.Subtract(5000);
            if (success)
                Debug.Log("<color=orange><b>-5,000đ</b> (Test Subtract Money)</color>");
            else
                Debug.Log("<color=red>Không đủ tiền để trừ!</color>");
        }
    }
#endif

    // ---------------------------------------------------------
    // TEST: Lưu tiền lên Firebase (gọi thủ công để test nhanh)
    // ---------------------------------------------------------
#if UNITY_EDITOR
    [ContextMenu("TEST: Save Money To Firebase (Manual)")]
    private void TestSaveMoneyManual()
    {
        if (FirebaseDatabaseManager.Instance == null || !FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.LogError("Firebase chưa sẵn sàng!");
            return;
        }

        FirebaseDatabaseManager.Instance.SaveMoneyToFirebase(PLAYER_ID);
        Debug.Log("<color=cyan><b>ĐÃ GỌI SAVE TIỀN LÊN FIREBASE!</b></color>");
    }
#endif

    // ---------------------------------------------------------
    // TEST: Load tiền từ Firebase (gọi thủ công)
    // ---------------------------------------------------------
#if UNITY_EDITOR
    [ContextMenu("TEST: Load Money From Firebase (Manual)")]
    private void TestLoadMoneyManual()
    {
        if (FirebaseDatabaseManager.Instance == null || !FirebaseDatabaseManager.FirebaseReady)
        {
            Debug.LogError("Firebase chưa sẵn sàng!");
            return;
        }

        FirebaseDatabaseManager.Instance.LoadMoneyFromFirebase(PLAYER_ID, (money) =>
        {
            Debug.Log($"<color=magenta><b>LOAD XONG TỪ FIREBASE: {money:N0}đ</b></color>");
        });
    }
#endif

    // ---------------------------------------------------------
    // TEST: Reset tiền về mặc định (1000đ)
    // ---------------------------------------------------------
#if UNITY_EDITOR
    [ContextMenu("TEST: Reset Money về 1000đ")]
    private void TestResetMoney()
    {
        PlayerMoney.Instance?.ResetMoney();
        Debug.Log("<color=white><b>Reset tiền về 1000đ</b></color>");
    }
#endif

    // ---------------------------------------------------------
    // TEST: Xem tiền hiện tại trong Console (không thay đổi gì)
    // ---------------------------------------------------------
#if UNITY_EDITOR
    [ContextMenu("TEST: Log Current Money")]
    private void LogCurrentMoney()
    {
        if (PlayerMoney.Instance != null)
        {
            Debug.Log($"<color=yellow><b>Số tiền hiện tại: {PlayerMoney.Instance.GetCurrentMoney():N0}đ</b></color>");
        }
    }
#endif
}