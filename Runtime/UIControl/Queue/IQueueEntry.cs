using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

namespace ILib.UI
{
	/// <summary>
	/// UIQueueのリクエストです。
	/// awaitした場合はCloseを待ちます
	/// </summary>
	public interface IQueueEntry : System.IDisposable
	{
		bool IsClosed { get; }
		Task Close();
		Task WaitClose(CancellationToken token);
		TaskAwaiter GetAwaiter();
	}
}