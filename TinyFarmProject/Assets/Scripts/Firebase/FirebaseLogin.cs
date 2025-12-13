using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FirebaseLogin : MonoBehaviour
{
    // ================= REGISTER =================
    [Header("Register")]
    public InputField ipRegisterEmail;
    public InputField ipRegisterPassword;
    public InputField ipRegisterConfirmPassword;
    public Button buttonRegister;

    [Header("Register Message UI")]
    public Text registerMessageText; // ⭐ MESSAGE REGISTER

    // ================= LOGIN =================
    [Header("Sign In")]
    public InputField ipLoginEmail;
    public InputField ipLoginPassword;
    public Button buttonLogin;

    [Header("Login Message UI")]
    public Text loginMessageText; // ⭐ MESSAGE LOGIN

    // ================= SWITCH =================
    [Header("Switch form")]
    public Button buttonMoveToSignIn;
    public Button buttonMoveToRegister;
    public GameObject loginForm;
    public GameObject registerForm;

    // ================= SCENE =================
    [Header("Scene Transition")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset nextSceneAfterLogin;
#else
    [SerializeField] private string nextSceneAfterLogin = "mapSummer";
#endif

    private FirebaseAuth auth;

    // =====================================================
    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        buttonRegister.onClick.AddListener(RegisterAccountFirebase);
        buttonLogin.onClick.AddListener(SignInAccountWithFireBase);
        buttonMoveToRegister.onClick.AddListener(SwitchForm);
        buttonMoveToSignIn.onClick.AddListener(SwitchForm);

        ClearAllMessages();
    }

    // ================= REGISTER =================
    public void RegisterAccountFirebase()
    {
        ClearRegisterMessage();

        string email = ipRegisterEmail.text;
        string password = ipRegisterPassword.text;
        string confirm = ipRegisterConfirmPassword.text;

        if (string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(confirm))
        {
            ShowRegisterError("Vui lòng nhập đầy đủ thông tin");
            return;
        }

        if (password != confirm)
        {
            ShowRegisterError("Mật khẩu xác nhận không khớp");
            return;
        }

        ShowRegisterLoading("Đang đăng ký...");

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    ShowRegisterError("Đăng ký bị hủy");
                    return;
                }

                if (task.IsFaulted)
                {
                    ShowRegisterError("Email đã tồn tại hoặc mật khẩu yếu");
                    Debug.LogError(task.Exception);
                    return;
                }

                FirebaseUser user = task.Result.User;
                PlayerSession.SetCurrentUserId(user.UserId);

                ShowRegisterSuccess("Đăng ký thành công!");

                if (FirebaseDatabaseManager.Instance != null)
                {
                    FirebaseDatabaseManager.Instance.InitializeNewUserData(user.UserId);
                }
            });
    }

    // ================= LOGIN =================
    public void SignInAccountWithFireBase()
    {
        ClearLoginMessage();

        string email = ipLoginEmail.text;
        string password = ipLoginPassword.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowLoginError("Vui lòng nhập email và mật khẩu");
            return;
        }

        ShowLoginLoading("Đang đăng nhập...");

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    ShowLoginError("Đăng nhập bị hủy");
                    return;
                }

                if (task.IsFaulted)
                {
                    ShowLoginError("Email hoặc mật khẩu không đúng");
                    Debug.LogError(task.Exception);
                    return;
                }

                FirebaseUser user = task.Result.User;
                PlayerSession.SetCurrentUserId(user.UserId);

                ShowLoginSuccess("Đăng nhập thành công!");

                if (FirebaseDatabaseManager.Instance != null)
                {
                    FirebaseDatabaseManager.Instance.CheckAndInitializeUserData(user.UserId);
                }

#if UNITY_EDITOR
                SceneManager.LoadScene(nextSceneAfterLogin.name);
#else
                SceneManager.LoadScene(nextSceneAfterLogin);
#endif
            });
    }

    // ================= SWITCH FORM =================
    public void SwitchForm()
    {
        loginForm.SetActive(!loginForm.activeSelf);
        registerForm.SetActive(!registerForm.activeSelf);
        ClearAllMessages();
    }

    // ================= MESSAGE HELPERS =================
    void ShowRegisterError(string msg)
    {
        registerMessageText.text = "❌ " + msg;
        registerMessageText.color = Color.red;
    }

    void ShowRegisterSuccess(string msg)
    {
        registerMessageText.text = "✅ " + msg;
        registerMessageText.color = Color.green;
    }

    void ShowRegisterLoading(string msg)
    {
        registerMessageText.text = "⏳ " + msg;
        registerMessageText.color = Color.yellow;
    }

    void ClearRegisterMessage()
    {
        if (registerMessageText != null)
            registerMessageText.text = "";
    }

    void ShowLoginError(string msg)
    {
        loginMessageText.text = "❌ " + msg;
        loginMessageText.color = Color.red;
    }

    void ShowLoginSuccess(string msg)
    {
        loginMessageText.text = "✅ " + msg;
        loginMessageText.color = Color.green;
    }

    void ShowLoginLoading(string msg)
    {
        loginMessageText.text = "⏳ " + msg;
        loginMessageText.color = Color.yellow;
    }

    void ClearLoginMessage()
    {
        if (loginMessageText != null)
            loginMessageText.text = "";
    }

    void ClearAllMessages()
    {
        ClearLoginMessage();
        ClearRegisterMessage();
    }
}
