using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;

public class FirebaseDatabaseManager : MonoBehaviour
{
    private DatabaseReference reference;

    private async void Awake()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status == DependencyStatus.Available)
        {
            reference = FirebaseDatabase.DefaultInstance.RootReference;
            Debug.Log("Firebase Ready!");
        }
        else
        {
            Debug.LogError("Firebase lỗi: " + status);
        }
    }

    // GHI: CHỈ LÀ 1 DÒNG – LƯU CHUỖI JSON ĐẸP
    public void WriteAsJsonString(string id, TilemapDetail data)
    {
        if (reference == null) return;

        string json = JsonConvert.SerializeObject(data);
        // → json = {"x":1,"y":1,"tilemapState":0}

        reference.Child("Users").Child(id).SetValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
                Debug.Log("ĐÃ GHI CHUỖI JSON ĐẸP LÊN FIREBASE!");
            else
                Debug.LogError("Lỗi: " + task.Exception);
        });
    }

    // ĐỌC: In ra chuỗi JSON + deserialize nếu cần
    public void ReadJsonString(string id)
    {
        if (reference == null) return;

        reference.Child("Users").Child(id).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Đọc lỗi: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            if (!snapshot.Exists)
            {
                Debug.Log("Không có dữ liệu tại: " + id);
                return;
            }

            // CHÍNH LÀ CHUỖI JSON ĐẸP BẠN MUỐN!
            string json = snapshot.Value.ToString();
            Debug.Log("<color=yellow>Chuỗi JSON từ Firebase:</color> " + json);

            // Nếu cần dùng lại object:
            TilemapDetail tile = JsonConvert.DeserializeObject<TilemapDetail>(json);
            Debug.Log($"<color=lime>Object:</color> ({tile.x}, {tile.y}) - {tile.tilemapState}");
        });
    }

    // TEST TỰ ĐỘNG
    void Start()
    {
        Invoke(nameof(Test), 2f);
    }

    void Test()
    {
        var tile = new TilemapDetail(1, 1, TilemapState.Ground);
        WriteAsJsonString("123", tile);

        Invoke(nameof(ReadBack), 2f);
    }

    void ReadBack()
    {
        ReadJsonString("123");
    }
}