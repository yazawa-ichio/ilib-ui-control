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
	public abstract class UIQueue<TParam, UControl> : UIController<TParam, UControl>, IQueueController where UControl : class, IControl
	{
		bool m_Run = false;
		List<QueueEntry> m_Queue = new List<QueueEntry>();

		public int Count => m_Queue.Count;

		public bool IsEmpty => !m_Run && m_Queue.Count == 0;

		protected override IEnumerable<T> GetActive<T>()
		{
			if (m_Queue.Count == 0) yield break;
			var ui = m_Queue[0]?.Instance?.Control ?? null;
			if (ui != null && ui.IsActive && ui is T target)
			{
				yield return target;
			}
		}

		public IQueueEntry Enqueue(string path, TParam prm, CancellationToken token = default)
		{
			var entry = new QueueEntry(this, token);
			entry.SetOpenAction(() => EnqueueImpl(entry, path, prm));
			m_Queue.Add(entry);
			if (m_Queue.Count == 1)
			{
				entry.Open();
			}
			return entry;
		}

		async Task<UIInstance> EnqueueImpl(QueueEntry entry, string path, TParam prm)
		{
			if (m_Queue.Count == 1)
			{
				return await Open<UControl>(path, prm);
			}
			else
			{
				var prev = m_Queue[0];
				m_Queue.RemoveAt(0);
				var ret = await Change<UControl>(path, prm, null, new UIInstance[] { prev.Instance });
				prev.SetClose();
				return ret;
			}
		}

		protected override Task Close(IControl control)
		{
			if (m_Queue.Count > 0 && m_Queue[0].Instance?.Control == control)
			{
				return Close(m_Queue[0]);
			}
			return Util.Successed;
		}

		public async Task Close(IQueueEntry entry)
		{
			if (m_Run)
			{
				return;
			}
			m_Run = true;

			try
			{
				var _entry = (entry as QueueEntry);
				if (m_Queue.Count == 0 || m_Queue[0] != _entry)
				{
					m_Queue.Remove(_entry);
					return;
				}

				if (m_Queue.Count == 1)
				{
					await Close(new UIInstance[] { _entry.Instance });
					var prev = m_Queue[0];
					m_Queue.RemoveAt(0);
					prev.SetClose();
					if (m_Queue.Count > 0)
					{
						//これはClose中に来たものなので待たなくてもいい
#pragma warning disable CS4014
						m_Queue[0].Open();
#pragma warning restore CS4014
					}
				}
				else
				{
					await m_Queue[1].Open();
				}
			}
			finally
			{
				m_Run = false;
			}
		}


	}

}