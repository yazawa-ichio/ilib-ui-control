using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ILib.UI
{

	/// <summary>
	/// UIの表示をスタック制御で行います。
	/// </summary>
	public class UIStack : UIStack<object, IControl> { }

	/// <summary>
	/// UIの表示をスタック制御で行います。
	/// </summary>
	public abstract class UIStack<TParam, UControl> : UIController<TParam, UControl>, IStackController where UControl : class, IControl
	{

		List<UIInstance> m_Stack = new List<UIInstance>();

		public int Count => m_Stack.Count;

		public bool IsEmpty => m_Stack.Count == 0;

		protected override IEnumerable<T> GetActive<T>()
		{
			for (int i = m_Stack.Count - 1; i >= 0; i--)
			{
				var ui = m_Stack[i].Control;
				if (ui.IsActive && ui is T target)
				{
					yield return target;
				}
			}
		}

		bool IStackController.IsFornt(StackEntry entry)
		{
			if (m_Stack.Count == 0 || entry.Instance == null) return false;
			var first = m_Stack[m_Stack.Count - 1];
			return entry.Instance == first;
		}

		public IStackEntry Push(string path, TParam prm)
		{
			var entry = new StackEntry(this);
			EnqueueProcess(() => PushImpl(path, prm, entry));
			return entry;
		}

		public IStackEntry Switch(string path, TParam prm)
		{
			var entry = new StackEntry(this);
			EnqueueProcess(() => ChangeImpl(path, prm, entry));
			return entry;
		}

		protected override Task Close(IControl control)
		{
			var task = new TaskCompletionSource<bool>();
			EnqueueProcess(() =>
			{
				var index = m_Stack.FindIndex(x => control == x.Control);
				if (index >= 0)
				{
					PopImpl(null, m_Stack.Count - index, task);
				}
			});
			return task.Task;
		}

		public Task Pop(int count = 1)
		{
			var task = new TaskCompletionSource<bool>();
			EnqueueProcess(() => PopImpl(null, count, task));
			return task.Task;
		}

		Task IStackController.Pop(StackEntry entry)
		{
			var task = new TaskCompletionSource<bool>();
			EnqueueProcess(() => PopImpl(entry.Instance, 0, task));
			return task.Task;
		}

		async void PushImpl(string path, TParam prm, StackEntry entry)
		{
			var parent = m_Stack.Count > 0 ? m_Stack[m_Stack.Count - 1] : null;
			try
			{
				var x = await Open<UControl>(path, prm, parent);
				m_Stack.Add(x);
				entry.SetInstance(x);
			}
			catch (Exception ex)
			{
				entry.SetException(ex);
			}
		}

		async void ChangeImpl(string path, TParam prm, StackEntry entry)
		{
			var release = m_Stack.Count > 0 ? m_Stack[m_Stack.Count - 1] : null;
			var parent = default(UIInstance);
			UIInstance[] releases = Array.Empty<UIInstance>();
			if (release != null)
			{
				m_Stack.RemoveAt(m_Stack.Count - 1);
				releases = new UIInstance[] { release };
				parent = release.Parent;
			}
			try
			{
				var x = await Change<UControl>(path, prm, parent, releases);
				m_Stack.Add(x);
				entry.SetInstance(x);
			}
			catch (Exception ex)
			{
				entry.SetException(ex);
			}
		}

		async void PopImpl(UIInstance instance, int count, TaskCompletionSource<bool> trigger)
		{
			if (m_Stack.Count == 0)
			{
				Debug.Assert(false, "スタックが空です");
				trigger.SetResult(false);
				return;
			}
			UIInstance front = null;
			UIInstance[] releases = null;
			int index = 0;
			if (instance != null)
			{
				index = m_Stack.IndexOf(instance);
				if (index < 0)
				{
					trigger.SetException(new Exception("指定のUIが見つかりませんでした"));
					return;
				}
			}
			else
			{
				index = m_Stack.Count - count;
				if (index < 0) index = 0;
				instance = m_Stack[index];
			}
			releases = m_Stack.Skip(index).ToArray();
			m_Stack.RemoveRange(index, m_Stack.Count - index);

			front = instance.Parent;

			try
			{
				await Close(releases, front);
				trigger.SetResult(true);
			}
			catch (Exception ex)
			{
				trigger.SetException(ex);
			}
		}

	}

}
