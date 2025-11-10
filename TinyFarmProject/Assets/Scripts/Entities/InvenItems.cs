using Newtonsoft.Json;
using UnityEngine;

public class InvenItems
{
    public string name { get; set; }

    public string description { get; set; }

    public InvenItems()
    {

    }

    public InvenItems(string name, string description)
    {
        this.name = name;
        this.description = description;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
