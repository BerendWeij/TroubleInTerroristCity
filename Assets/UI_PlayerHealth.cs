using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using UnityEngine;

public class UI_PlayerHealth : MonoBehaviour
{
    [SerializeField] private ProgressBar _progressBar; 
    
    public void BindPlayer(Player player)
    {
        if (player != null)
        {
            player.Health.AddChangeListener((float health) => _progressBar.SetValue(health));
        }
    }
}
