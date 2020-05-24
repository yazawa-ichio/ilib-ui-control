using System;
using System.Runtime.ExceptionServices;

namespace ILib.UI
{
	public class QueryResult<TResult> : IQueueQueryHandler<TResult>
	{
		IQueueEntry m_Entry;
		TResult m_Result;
		Exception m_Exception;
		bool m_Set;

		internal void Init(IQueueEntry entry)
		{
			m_Entry = entry;
		}

		public void Set(TResult result)
		{
			if (m_Set) throw new InvalidOperationException("already set result");
			m_Set = true;
			m_Result = result;
			m_Entry.Close();
		}

		public void Set(Exception error)
		{
			if (m_Set) throw new InvalidOperationException("already set result");
			m_Set = true;
			m_Exception = error;
			m_Entry.Close();
		}

		public TResult GetResult()
		{
			if (!m_Set) throw new InvalidOperationException("unset result");
			if (m_Exception != null)
			{
				ExceptionDispatchInfo.Capture(m_Exception).Throw();
			}
			return m_Result;
		}

	}

}