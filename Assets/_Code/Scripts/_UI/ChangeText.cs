using TMPro;
using UnityEngine;

public class ChangeText : MonoBehaviour
{
    private TMP_Text text;
    private void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    public void ChangeTextTo(string newText)
    {
        text.text = newText;
    }
}
