using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ILib.UI
{
	internal class QueueEntry<TParam> : IInternalQueueEntry<TParam>
	{

		public bool IsClosed { get; private set; }

		public string Path { get; private set; }

		public TParam Param { get; private set; }

		public Exception Error { get; private set; }

		public UIInstance Instance { get; set; }

		public bool IsWaitCloseCompleted { get; set; }

		bool m_WaitOpen = true;
		Action<Exception> m_OnComplete;
		TaskCompletionSource<bool> m_CloseRequest = new TaskCompletionSource<bool>();

		public QueueEntry(CancellationToken token, string path, TParam param)
		{
			if (token != default)
			{
				token.Register(() => Dispose());
			}
			Path = path;
			Param = param;
		}

		public void PreOpen()
		{
			m_WaitOpen = false;
		}

		public Task Close()
		{
			m_CloseRequest.TrySetResult(true);
			if (m_WaitOpen)
			{
				CompleteClose();
			}
			return WaitClose(default);
		}

		public async void Dispose()
		{
			try
			{
				await Close();
			}
			catch (Exception ex)
			{
				UIControlLog.Exception(ex);
			}
		}

		public void Abort()
		{
			m_CloseRequest.TrySetException(new Exception("abort UIQueue Request"));
			CompleteClose();
		}

		public Task WaitClose(CancellationToken token)
		{
			if (Error != null) return Task.FromException(Error);

			if (IsClosed) return Util.Successed;

			TaskCompletionSource<bool> task = new TaskCompletionSource<bool>();
			m_OnComplete += (ex) =>
			{
				if (ex != null)
				{
					task.TrySetException(ex);
				}
				else
				{
					task.TrySetResult(true);
				}
			};
			if (default != token)
			{
				token.Register(() =>
				{
					task.TrySetCanceled(token);
				});
			}
			return task.Task;
		}

		public TaskAwaiter GetAwaiter()
		{
			return WaitClose(default).GetAwaiter();
		}


		public void PreClose()
		{
			if (!IsWaitCloseCompleted)
			{
				CloseImpl();
			}
		}

		public void CompleteClose()
		{
			if (IsWaitCloseCompleted)
			{
				CloseImpl();
			}
		}

		void CloseImpl()
		{
			IsClosed = true;
			m_OnComplete?.Invoke(null);
			m_OnComplete = null;
			Param = default;
			Path = null;
		}

		public Task WaitCloseRequest()
		{
			return m_CloseRequest.Task;
		}

		public void Fail(Exception ex)
		{
			Error = ex;
			IsClosed = true;
			m_OnComplete?.Invoke(ex);
			m_OnComplete = null;
			Param = default;
			Path = null;

			if (Instance != null && Instance.Object != null)
			{
				UnityEngine.GameObject.Destroy(Instance.Object);
			}

		}
	}

}