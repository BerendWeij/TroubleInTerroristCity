using System;
using UnityEngine;
using UnityEngine.Events;

public class InteractiveObject : MonoBehaviour
{
	#region Internal
	[Serializable]
	protected struct InteractionAudio
	{
		public SoundPlayer RaycastStartAudio;
		public SoundPlayer RaycastEndAudio;
		public SoundPlayer InteractionStartAudio;
		public SoundPlayer InteractionEndAudio;
	}
	#endregion

	public readonly Value<float> InteractionProgress = new();
	public readonly Value<string> InteractionText = new();

	public bool InteractionEnabled { get { return m_InteractionEnabled; } set { m_InteractionEnabled = value; } }

	[BHeader("Interaction", true)]

	[SerializeField]
	private bool m_InteractionEnabled = true;

	[SerializeField]
	[Multiline]
	private string m_InteractionText = string.Empty;

	[SerializeField]
	private InteractionAudio m_InteractionAudio;

	[Space(3f)]

	[SerializeField]
	private UnityEvent m_InteractionEvent;

	private float m_InteractStart;


	/// <summary>
	/// Called when a player starts looking at the object.
	/// </summary>
	public virtual void OnRaycastStart(Player player)
	{
		m_InteractionAudio.RaycastStartAudio.Play2D();
	}

	/// <summary>
	/// Called while a player is looking at the object.
	/// </summary>
	public virtual void OnRaycastUpdate(Player player) { }

	/// <summary>
	/// Called when a player stops looking at the object.
	/// </summary>
	public virtual void OnRaycastEnd(Player player)
	{
		m_InteractionAudio.RaycastEndAudio.Play2D();
	}

	/// <summary>
	/// Called when a player starts interacting with the object.
	/// </summary>
	public virtual void OnInteractionStart(Player player)
	{
		m_InteractionEvent.Invoke();

		m_InteractionAudio.InteractionStartAudio.Play2D();

		m_InteractStart = Time.time;
	}

	/// <summary>
	/// Called while a player is interacting with the object.
	/// </summary>
	public virtual void OnInteractionUpdate(Player player)
	{
		InteractionProgress.Set(Time.time - m_InteractStart);
	}

	/// <summary>
	/// Called when a player stops interacting with the object.
	/// </summary>
	public virtual void OnInteractionEnd(Player player)
	{
		InteractionProgress.Set(0f);

		m_InteractionAudio.InteractionEndAudio.Play2D();
	}

	protected virtual void Awake()
	{
		InteractionText.Set(m_InteractionText);
	}
}