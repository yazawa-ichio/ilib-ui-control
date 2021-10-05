namespace ILib.UI
{
	/// <summary>
	/// Behind対象のUIに対するオプションです。
	/// </summary>
	public interface IBehindTargetOption
	{
		/// <summary>
		/// 非表示処理を上書きするオプションです。
		/// </summary>
		HideTransitionOption HideOption { get; }
	}
}