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
        Debug.Log($"💧 OnParticleCollision! other={other.name}, tag={other.tag}");
        
        if (other.CompareTag("Rain"))
        {
            if (crop != null)
            {
                crop.Water();
                Debug.Log("🌧️ Mưa rơi trúng cây → tưới");
            }
            else
            {
                Debug.LogWarning("⚠️ RainWaterReceiver: crop = null!");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ OnParticleCollision nhưng tag không phải 'Rain', tag={other.tag}");
        }
    }
}
