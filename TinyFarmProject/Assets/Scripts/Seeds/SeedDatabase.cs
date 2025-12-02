using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SeedDatabase", menuName = "Game Data/Seed Database")]
public class SeedDatabase : ScriptableObject
{
    public List<SeedData> seeds = new List<SeedData>();
}
