using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class ItemGenerator
{
    public enum Method
    {
        Specific,
        RandomFromCategory,
        Random
    }

    [SerializeField] public Method GenerateMethod = Method.Specific;

    [DatabaseCategory] [SerializeField] public string Category;

    [DatabaseItem] [SerializeField] public string Name;

    [SerializeField] [MinMax(1, 100, false)]
    private Vector2Int CountRange = new(1, 100);


    public int GetRandomCount()
    {
        return Mathf.Clamp(Random.Range(CountRange.x, CountRange.y + 1), 1, 100);
    }
}

[RequireComponent(typeof(Inventory))]
public class StartupInventory : EntityComponent
{
    [Serializable]
    public class ItemContainerStartupItems
    {
        public string Name;

        [Space] public ItemGeneratorList StartupItems;
    }

    [SerializeField] private ItemContainerStartupItems[] m_ItemContainersStartupItems;


    public override void OnEntityStart() => AddItemsToInventory();

    private void AddItemsToInventory()
    {
        Inventory inventory = GetComponent<Inventory>();

        if (inventory != null)
        {
            foreach (var container in m_ItemContainersStartupItems)
            {
                ItemContainer itemContainer = inventory.GetContainerWithName(container.Name);

                if (itemContainer != null)
                {
                    foreach (var item in container.StartupItems)
                    {
                        if (item.GenerateMethod == ItemGenerator.Method.Specific)
                            itemContainer.AddItem(item.Name, item.GetRandomCount());
                        else if (item.GenerateMethod == ItemGenerator.Method.RandomFromCategory)
                        {
                            ItemInfo itemInfo = ItemDatabase.GetRandomItemFromCategory(item.Category);

                            if (itemInfo != null)
                                itemContainer.AddItem(itemInfo.Id, item.GetRandomCount());
                        }
                        else if (item.GenerateMethod == ItemGenerator.Method.Random)
                        {
                            var category = ItemDatabase.GetRandomCategory();

                            if (category != null)
                            {
                                ItemInfo itemInfo = ItemDatabase.GetRandomItemFromCategory(category.Name);

                                if (itemInfo != null)
                                    itemContainer.AddItem(itemInfo.Id, item.GetRandomCount());
                            }
                        }
                    }
                }
            }
        }
    }
}



[Serializable]
public class ItemGeneratorList : ReorderableArray<ItemGenerator> { }

public class DatabaseProperty : PropertyAttribute 
{
		
}