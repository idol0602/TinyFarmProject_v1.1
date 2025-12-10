using UnityEngine;
using System.Collections;

public class FarmLoader : MonoBehaviour
{
    public string userId = "Player1";

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        FirebaseDatabaseManager firebase = FirebaseDatabaseManager.Instance;

        if (firebase == null)
        {
            Debug.LogError("Firebase manager missing!");
            yield break;
        }

        firebase.LoadFarmFromFirebase(userId);
    }
}
