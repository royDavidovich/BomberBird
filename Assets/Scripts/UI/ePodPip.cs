namespace BomberBird.UI
{
	/// <summary>How one pod hollow on the HUD tree reads.</summary>
	public enum ePodPip
	{
		/// <summary>The bird cannot carry this many pods, so the hollow is not shown at all.</summary>
		Hidden,

		/// <summary>The pod is in hand.</summary>
		Ready,

		/// <summary>The pod is out on the field and comes back when it bursts.</summary>
		Spent,
	}
}
