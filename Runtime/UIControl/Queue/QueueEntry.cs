using System;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

namespace ILib.UI
{
	internal class QueueEntry : IQueueEntry
	{
		public bool IsClosed
		{
			get
			{
				if (m_Cancel) return true;
				if (Instance != null)
				{
					return Instance.Object == null;
				}
				return false;
			}
		}

		public UIInstance Instance
		{
			get
			{
				if (m_Opening == null || !m_Opening.IsCompleted) return null;
				return m_Opening?.Result ?? null;
			}
		}

		IQueueController m_Parent;
		CancellationToken m_Token;

		Func<Task<UIInstance>> m_Open;
		Task<UIInstance> m_Opening;
		bool m_Close;
		bool m_Cancel;
		Action m_OnClosed;

		public QueueEntry(IQueueController parent, CancellationToken token)
		{
			m_Parent = parent;
			token.Register(async () => await Close());
		}

		public void SetOpenAction(Func<Task<UIInstance>> open)
		{
			m_Open = open;
		}

		public Task Open()
		{
			if (m_Open == null) return (m_Opening as Task) ?? Util.Successed;
			m_Opening = m_Open();
			m_Open = null;
			return m_Opening;
		}

		public async Task Close()
		{
			if (m_Close) return;
			m_Close = true;
			if (m_Opening == null)
			{
				m_Cancel = true;
				m_Open = null;
				await m_Parent.Close(this);
			}
			else
			{
				await m_Opening;
				await m_Parent.Close(this);
			}
		}

		public async void Dispose()
		{
			await Close();
		}

		public async Task WaitClose(CancellationToken token)
		{
			if (m_Close) return;
			TaskCompletionSource<bool> task = new TaskCompletionSource<bool>();
			m_OnClosed += () =>
			{
				task.TrySetResult(true);
			};
			token.Register(() =>
			{
				task.TrySetCanceled(token);
			});
			await task.Task;
		}

		public TaskAwaiter GetAwaiter()
		{
			return WaitClose(default).GetAwaiter();
		}

		public void SetClose()
		{
			m_Close = true;
			m_OnClosed?.Invoke();
			m_OnClosed = null;
		}
	}

}
