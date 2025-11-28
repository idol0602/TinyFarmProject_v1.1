using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoUI : MonoBehaviour
{
    public static ItemInfoUI Instance;

    public GameObject panel;
    public Image icon;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI description;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject); // đảm bảo singleton

        if (panel != null)
            panel.SetActive(false);
    }
    public void Show(ItemClass item)
    {
        if (item == null) return;

        if (panel != null) panel.SetActive(true);
        if (icon != null) icon.sprite = item.icon;
        if (itemName != null) itemName.text = item.itemName;
        if (description != null) description.text = item.description;
    }



    public void Hide()
    {
        panel.SetActive(false);
    }
}
