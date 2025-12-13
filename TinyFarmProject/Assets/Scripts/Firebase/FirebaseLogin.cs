using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirebaseLogin : MonoBehaviour
{
    [Header("Register")]
    public InputField ipRegisterEmail;
    public InputField ipRegisterPassword;
    public Button buttonRegister;

    [Header("Sign In")]
    public InputField ipLoginEmail;
    public InputField ipLoginPassword;
    public Button buttonLogin;

    [Header("Switch form")]
    public Button buttonMoveToSignIn;
    public Button buttonMoveToRegister;

    public GameObject loginForm;
    public GameObject registerForm;

    private FirebaseAuth auth;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        buttonRegister.onClick.AddListener(RegisterAccountFirebase);
        buttonLogin.onClick.AddListener(SignInAccountWithFireBase);
        buttonMoveToRegister.onClick.AddListener(SwitchForm);
        buttonMoveToSignIn.onClick.AddListener(SwitchForm);
    }

    public void RegisterAccountFirebase()
    {
        string email = ipRegisterEmail.text;
        string password = ipRegisterPassword.text;
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.Log("Dang ki bi huy");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.Log("Dang ki that bai: " + task.Exception);
                return;
            }

            // Chỉ chạy khi thật sự thành công
            Debug.Log("Dang ki thanh cong");
            FirebaseUser user = task.Result.User;
            
            // 🔧 Lưu User ID từ Firebase Authentication
            // (Cache sẽ tự động clear)
            PlayerSession.SetCurrentUserId(user.UserId);
            Debug.Log($"[FirebaseLogin] New user registered with ID: {user.UserId}");
            
            // 🔧 Tạo dữ liệu mặc định cho user mới
            if (FirebaseDatabaseManager.Instance != null)
            {
                FirebaseDatabaseManager.Instance.InitializeNewUserData(user.UserId);
                Debug.Log($"[FirebaseLogin] Initialized default data for new user");
            }
        });
    }

    public void SignInAccountWithFireBase()
    {
        string email = ipLoginEmail.text;
        string password = ipLoginPassword.text;

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Dang nhap bi huy");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.Log("Dang nhap that bai: " + task.Exception);
                return;
            }

            // Chỉ chạy khi thật sự thành công
            Debug.Log("Dang nhap thanh cong");
            FirebaseUser user = task.Result.User;
            
            // 🔧 Lưu User ID từ Firebase Authentication
            // (Cache sẽ tự động clear)
            PlayerSession.SetCurrentUserId(user.UserId);
            Debug.Log($"[FirebaseLogin] Player logged in with ID: {user.UserId}");
            
            // 🔧 Kiểm tra và initialize user data nếu cần
            if (FirebaseDatabaseManager.Instance != null)
            {
                FirebaseDatabaseManager.Instance.CheckAndInitializeUserData(user.UserId);
                Debug.Log($"[FirebaseLogin] Checking user data...");
            }

            SceneManager.LoadScene("mapSummer");
        });
    }

    public void SwitchForm()
    {
        loginForm.SetActive(!loginForm.activeSelf);
        registerForm.SetActive(!registerForm.activeSelf);
    }
}
