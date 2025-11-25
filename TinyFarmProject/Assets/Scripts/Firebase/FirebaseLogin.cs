using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;

public class FirebaseLogin : MonoBehaviour
{
    public InputField ipRegisterEmail;
    public InputField ipRegisterPassword;
    public Button buttonRegister;

    private FirebaseAuth auth;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        buttonRegister.onClick.AddListener(RegisterAccountFirebase);

    }

    public void RegisterAccountFirebase()
    {
        string email = ipRegisterEmail.text;
        string password = ipRegisterPassword.text;

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if(task.IsCanceled)
            {
                Debug.Log("Dang ki bi huy");
                return;
            }
            if(task.IsFaulted)
            {
                Debug.Log("Dang ki that bai");
            }
            if (task.IsCompleted) {
                Debug.Log("Dang ki thanh cong");
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
