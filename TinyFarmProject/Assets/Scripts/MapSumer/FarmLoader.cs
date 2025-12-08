using UnityEngine;
using System.Collections;

public class FarmLoader : MonoBehaviour
{
    public string userId = "Player1";

    private void Start()
    {
        StartCoroutine(LoadFarmRoutine());
    }

    IEnumerator LoadFarmRoutine()
    {
        while (!FirebaseDatabaseManager.FirebaseReady)
            yield return null;

        FindObjectOfType<FirebaseDatabaseManager>()
            .LoadFarmFromFirebase(userId);
    }
}
