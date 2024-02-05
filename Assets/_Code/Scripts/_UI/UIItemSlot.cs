using TMPro;
using UnityEngine;

public class UIItemSlot : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    private void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    public void ChangeText(ItemSlot item)
    {
        textComponent.text = item.Item != null ? item.Item.Name : "Empty";
    }
}
