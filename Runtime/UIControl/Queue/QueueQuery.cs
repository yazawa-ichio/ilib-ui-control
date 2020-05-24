using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ILib.UI
{

	internal class QueueQuery<TParam, TResult> : IInternalQueueEntry<TParam>, IQueueQuery<TResult>
	{
		public bool IsClosed => m_Entry.IsClosed;

		public UIInstance Instance
		{
			get => m_Entry.Instance;
			set
			{
				m_Entry.Instance = value;
				SetHandler(value.Control);
			}
		}

		public string Path => m_Entry.Path;

		public TParam Param => m_Entry.Param;

		public bool IsWaitCloseCompleted { get => m_Entry.IsWaitCloseCompleted; set => m_Entry.IsWaitCloseCompleted = value; }

		IQueueQueryHandler<TResult> m_Handler;

		QueueEntry<TParam> m_Entry;

		public QueueQuery(QueueEntry<TParam> entry)
		{
			m_Entry = entry;
		}

		public Task Close()
		{
			return m_Entry.Close();
		}

		public void Dispose()
		{
			m_Entry.Dispose();
		}

		TaskAwaiter IQueueEntry.GetAwaiter()
		{
			return m_Entry.GetAwaiter();
		}

		public Task WaitClose(CancellationToken token)
		{
			return m_Entry.WaitClose(token);
		}

		public async Task<TResult> WaitResult(CancellationToken token)
		{
			await WaitClose(token);

			token.ThrowIfCancellationRequested();

			if (m_Handler == null) throw new System.Exception("not found handler");

			return m_Handler.GetResult();
		}

		public TaskAwaiter<TResult> GetAwaiter()
		{
			return WaitResult(default).GetAwaiter();
		}

		public void PreClose()
		{
			m_Entry.PreClose();
		}

		public void CompleteClose()
		{
			m_Entry.CompleteClose();
		}

		public void Fail(Exception ex)
		{
			m_Entry.Fail(ex);
		}

		public Task WaitCloseRequest()
		{
			return m_Entry.WaitCloseRequest();
		}

		public void PreOpen()
		{
			m_Entry.PreOpen();
		}

		public void Abort()
		{
			m_Entry.Abort();
		}

		private void SetHandler(IControl control)
		{
			if (control is IQueueQueryHandler<TResult>)
			{
				m_Handler = control as IQueueQueryHandler<TResult>;
			}
			if (control is IQueueQueryHandler)
			{
				m_Handler = new QueueQueryHandlerWrapper<TResult>(control as IQueueQueryHandler);
			}
			if (control is IHasQueryResult<TResult> hasResult)
			{
				hasResult.QueryResult.Init(m_Entry);
				m_Handler = hasResult.QueryResult;
			}
		}

	}

}