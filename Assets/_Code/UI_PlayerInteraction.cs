using UnityEngine;

public class UI_PlayerInteraction : MonoBehaviour
{
    #region Anim Hashing
    //Hashed animator strings (Improves performance)
    private readonly int animHash_Show = Animator.StringToHash("Show");
    #endregion

    [BHeader("Generic Interaction", true)]

    [SerializeField]
    private ChangeText m_GenericText = null;

    [BHeader("Equipment Specific Interaction", true)]
		
    private RaycastInfo m_RaycastData;
    private bool m_SwapUIEnabled;
		
    public void BindPlayer(Player player)
    {
        if (player != null)
        {
            player.RaycastInfo.AddChangeListener(OnPlayerRaycastChanged);
            player.EquippedItem.AddChangeListener((Item item) =>
            {
                OnPlayerRaycastChanged(player.RaycastInfo.Val);
            });
        }
    }

    private void OnPlayerRaycastChanged(RaycastInfo raycastData)
    {
        bool show = raycastData != null && raycastData.IsInteractive;
			
        if(m_RaycastData != null)
            m_RaycastData.InteractiveObject.InteractionText.RemoveChangeListener(UpdateInteractText);

        m_RaycastData = raycastData;

        if (show)
        {
            UpdateInteractText(m_RaycastData.InteractiveObject.InteractionText.Val);
				
            m_RaycastData.InteractiveObject.InteractionText.AddChangeListener(UpdateInteractText);
        }
        else
        {
            UpdateInteractText("");
        }
    }

    private void UpdateInteractText(string text)
    {
        m_GenericText.ChangeTextTo(text);
    }
}