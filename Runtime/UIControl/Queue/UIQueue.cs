using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ILib.UI
{
	/// <summary>
	/// UIの表示をキュー制御で行います。現在のUIが閉じられるまで、次の表示リクエストは実行されません。
	/// 表示前に不要になったリクエストはキャンセルできます。
	/// </summary>
	public class UIQueue : UIQueue<object, IControl> { }

	/// <summary>
	/// UIの表示をキュー制御で行います。現在のUIが閉じられるまで、次の表示リクエストは実行されません。
	/// 表示前に不要になったリクエストはキャンセルできます。
	/// </summary>
	public abstract class UIQueue<TParam, UControl> : UIController<TParam, UControl> where UControl : class, IControl
	{
		bool m_Run = false;
		List<IInternalQueueEntry<TParam>> m_Queue = new List<IInternalQueueEntry<TParam>>();

		public int Count => m_Queue.Count;

		public bool IsEmpty => !m_Run && m_Queue.Count == 0;

		bool m_HasError;
		IInternalQueueEntry<TParam> m_Current;

		protected override IEnumerable<T> GetActive<T>()
		{
			var ui = m_Current?.Instance?.Control ?? null;
			if (ui != null && ui.IsActive && ui is T target)
			{
				yield return target;
			}
		}

		public IQueueEntry Enqueue(string path, TParam prm, CancellationToken token = default)
		{
			var entry = new QueueEntry<TParam>(token, path, prm);
			m_Queue.Add(entry);
			TryRun();
			return entry;
		}

		public IQueueQuery<TResult> Query<TResult>(string path, TParam prm, CancellationToken token = default)
		{
			var entry = new QueueEntry<TParam>(token, path, prm);
			var query = new QueueQuery<TParam, TResult>(entry);
			m_Queue.Add(query);
			TryRun();
			return query;
		}

		public void RepairError(bool clear = false)
		{
			if (!m_HasError) return;
			m_HasError = false;
			m_Run = false;
			if (clear)
			{
				while (m_Queue.Count > 0)
				{
					m_Queue[0].Abort();
					m_Queue.RemoveAt(0);
				}
			}
			else
			{
				Run();
			}
		}

		protected override Task Close(IControl control)
		{
			if (m_Current.Instance?.Control == control)
			{
				return m_Current.Close();
			}
			return Util.Successed;
		}

		protected virtual void HandleError(Exception ex)
		{
		}

		void TryRun()
		{
			if (!m_Run && !m_HasError)
			{
				Run();
			}
		}

		async void Run()
		{
			try
			{
				m_Run = true;
				await RunImpl();
				m_Run = false;
			}
			catch (Exception ex)
			{
				m_HasError = true;
				HandleError(ex);
			}
		}

		async Task RunImpl()
		{
			while (m_Queue.Count > 0)
			{
				await Open();

				await m_Current.WaitCloseRequest();

				await Close();
			}
		}

		async Task Open()
		{
			var prev = m_Current;
			m_Current = m_Queue[0];
			m_Queue.RemoveAt(0);
			m_Current.PreOpen();
			try
			{
				if (prev == null)
				{
					m_Current.Instance = await Open<UControl>(m_Current.Path, m_Current.Param);
				}
				else
				{
					prev.PreClose();
					m_Current.Instance = await Change<UControl>(m_Current.Path, m_Current.Param, null, new UIInstance[] { prev.Instance });
					prev.CompleteClose();
				}
			}
			catch (Exception ex)
			{
				UIControlLog.Warning("[ilib-ui] {0}", ex);
				prev?.Fail(ex);
				m_Current.Fail(ex);
				m_Current = null;
				throw;
			}
		}

		async Task Close()
		{

			//キャンセル済を削除
			while (m_Queue.Count > 0 && m_Queue[0].IsClosed)
			{
				m_Queue.RemoveAt(0);
				continue;
			}

			//残りのリクエストがなければClose実行
			if (m_Queue.Count == 0)
			{
				try
				{
					m_Current.PreClose();
					await Close(new UIInstance[] { m_Current.Instance });
					m_Current.CompleteClose();
					m_Current = null;
				}
				catch (Exception ex)
				{
					UIControlLog.Warning("[ilib-ui] {0}", ex);
					m_Current.Fail(ex);
					m_Current = null;
					throw;
				}
			}
		}
	}

}