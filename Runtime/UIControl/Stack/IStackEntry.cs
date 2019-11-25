using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ILib.UI
{
	/// <summary>
	/// UIStackのリクエストです。
	/// Pushの完了を受け取れます。
	/// </summary>
	public interface IStackEntry
	{
		bool IsActive { get; }
		bool IsFornt { get; }
		Task Pop();
		void Execute<T>(System.Action<T> action, bool immediate = false);
		TaskAwaiter<IStackEntry> GetAwaiter();
	}
}
