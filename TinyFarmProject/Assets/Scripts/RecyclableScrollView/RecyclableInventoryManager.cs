using NUnit.Framework;
using PolyAndCode.UI;
using System.Collections.Generic;
using UnityEngine;

public class RecyclableInventoryManager : MonoBehaviour,IRecyclableScrollRectDataSource
{
    [SerializeField]
    RecyclableScrollRect _recycableScrollRect;
    [SerializeField]
    private int _dataLength;

    private List<InvenItems> _invenItems = new List<InvenItems>();

    private void Awake()
    {
        _recycableScrollRect.DataSource = this;
    }

    public int GetItemCount()
    {
        return _invenItems.Count;
    }

    public void SetCell(ICell cell, int index)
    {
        var item = cell as CellItemData;
        item.ConfigureCell(_invenItems[index], index);
    }

    private void Start()
    {
        List<InvenItems> lstItem = new List<InvenItems>();
        for (int i = 0; i < 50; i++)
        {
            InvenItems invenItem = new InvenItems();
            
            invenItem.name = "Name_" + i.ToString();
            invenItem.description = "Des_" + i.ToString();

            lstItem.Add(invenItem);
        }

        SetLstItem(lstItem);

        if (_recycableScrollRect!= null)
        {
            _recycableScrollRect.ReloadData(); 
            Debug.Log("Đã gọi ReloadData() (Giả lập).");
        }
    }

    public void SetLstItem(List<InvenItems> lst)
    {
        _invenItems = lst;
        Debug.Log($"Danh sách đã được gán. Tổng số vật phẩm: {_invenItems.Count}");
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            InvenItems invenItemDemo = new InvenItems("ca", "ca");
            _invenItems.Add(invenItemDemo);
            _recycableScrollRect.ReloadData();
        }
    }
}
