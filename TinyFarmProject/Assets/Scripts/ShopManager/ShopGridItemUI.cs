using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopGridItemUI : MonoBehaviour, IPointerClickHandler
{
    public Image icon;

    private SeedData seedData;
    private ShopManager shopManager;

    public void Setup(SeedData data, ShopManager manager)
    {
        seedData = data;
        shopManager = manager;
        icon.sprite = data.seedIcon;
    }

    // ✅ Hàm này tự động chạy khi bạn click vào ô
    public void OnPointerClick(PointerEventData eventData)
    {
        shopManager.ShowDetail(seedData);
    }
}
