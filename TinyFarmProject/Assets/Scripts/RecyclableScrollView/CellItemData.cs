using PolyAndCode.UI;
using UnityEngine;
using UnityEngine.UI;

public class CellItemData : MonoBehaviour, ICell
{
    public Text nameLabel;
    public Text desLabel;

    private InvenItems _contactInfo;
    private int _cellIndex;

    public void ConfigureCell(InvenItems invenItems, int cellIndex)
    {
        _cellIndex = cellIndex;
        _contactInfo = invenItems;
        nameLabel.text = invenItems.name;
        desLabel.text = invenItems.description;
    }
}
