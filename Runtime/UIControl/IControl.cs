using System.Threading.Tasks;

namespace ILib.UI
{
	/// <summary>
	/// UIの表示制御
	/// </summary>
	public interface IControl
	{
		/// <summary>
		/// 有効化？
		/// </summary>
		bool IsActive { get; }
		/// <summary>
		/// Behind時に非アクティブにするか？
		/// </summary>
		[System.Obsolete("OnBehindで結果を返す形に変更になりました。", true)]
		bool IsDeactivateInBehind { get; }

		void SetController(IController controller);
		/// <summary>
		/// UI作成直後に実行されます
		/// </summary>
		Task OnCreated(object prm);
		/// <summary>
		/// UIが最前面に来た際に実行されます。
		/// </summary>
		Task OnFront(bool open);
		/// <summary>
		/// UIが最前面から後ろに回った際に実行されます。
		/// trueが返る場合はオブジェクト自体を非アクティブにします。
		/// </summary>
		Task<bool> OnBehind(object prm);
		/// <summary>
		/// UIを閉じる際に実行されます。
		/// </summary>
		Task OnClose();
	}

	public interface IControl<TParam> : IControl
	{
	}
}