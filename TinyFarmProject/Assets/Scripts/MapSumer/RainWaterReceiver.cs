using UnityEngine;
using MapSummer;

public class RainWaterReceiver : MonoBehaviour
{
    private Crop crop;

    private void Awake()
    {
        crop = GetComponentInParent<Crop>();
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Rain"))
        {
            if (crop != null)
            {
                crop.Water();
                Debug.Log("🌧️ Mưa rơi trúng cây → tưới");
            }
        }
    }
}
