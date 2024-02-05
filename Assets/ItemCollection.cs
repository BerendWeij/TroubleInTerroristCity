using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemCollection", menuName = "ItemCollection")]
public class ItemCollection : ScriptableObject
{
    [DatabaseItem]
    public List<string> items = new();
}
