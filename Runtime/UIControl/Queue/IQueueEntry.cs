using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ILib.UI
{
	/// <summary>
	/// UIQueueのリクエストです。
	/// awaitした場合はCloseを待ちます
	/// </summary>
	public interface IQueueEntry : IDisposable
	{
		/// <summary>
		/// UIが閉じたか？
		/// </summary>
		bool IsClosed { get; }
		/// <summary>
		/// CloseのタスクをUIの完全なCloseまで待ちます。
		/// </summary>
		bool IsWaitCloseCompleted { get; set; }
		/// <summary>
		/// UIをCloseします。
		/// 表示待ちの場合はキャンセルされます。
		/// </summary>
		Task Close();
		/// <summary>
		/// UIが閉じるのを待ちます
		/// キャンセル可能なのは待ち処理です。
		/// </summary>
		Task WaitClose(CancellationToken token);
		TaskAwaiter GetAwaiter();
	}
}