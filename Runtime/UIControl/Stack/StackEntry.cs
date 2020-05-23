using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ILib.UI
{
	internal class StackEntry : IStackEntry
	{
		public UIInstance Instance { get; private set; }

		IStackController m_Parent;
		Task m_Pop;
		TaskCompletionSource<IStackEntry> m_Task = new TaskCompletionSource<IStackEntry>();

		public StackEntry(IStackController parent)
		{
			m_Parent = parent;
		}

		public bool IsActive => Instance?.Control.IsActive ?? false;

		public bool IsFornt => m_Parent.IsFornt(this);

		public Task Pop()
		{
			if (m_Pop != null) return m_Pop;
			return m_Pop = m_Parent.Pop(this);
		}

		public void Execute<T>(Action<T> action, bool immediate = false)
		{
			if (Instance != null)
			{
				if (Instance.Control is T target)
				{
					action(target);
				}
			}
		}

		public void SetInstance(UIInstance instance)
		{
			Instance = instance;
			m_Task.SetResult(this);
		}

		public void SetException(Exception ex)
		{
			m_Task.SetException(ex);
		}

		public TaskAwaiter<IStackEntry> GetAwaiter()
		{
			return m_Task.Task.GetAwaiter();
		}
	}

}