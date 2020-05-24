using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ILib.UI
{
	/// <summary>
	/// UIQueueを用いた結果受け取り手続きを行うためのクラスです。
	/// UIControlにIQueueQueryHandler/IHasQueryResultのいずれかを実装する必要があります。
	/// </summary>
	public interface IQueueQuery<TResult> : IQueueEntry
	{
		Task<TResult> WaitResult(CancellationToken token);
		new TaskAwaiter<TResult> GetAwaiter();
	}

}