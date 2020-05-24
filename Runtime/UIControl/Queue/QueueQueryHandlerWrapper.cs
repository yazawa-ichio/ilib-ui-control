namespace ILib.UI
{
	public class QueueQueryHandlerWrapper<TResult> : IQueueQueryHandler<TResult>
	{
		IQueueQueryHandler m_Inner;
		public QueueQueryHandlerWrapper(IQueueQueryHandler handler)
		{
			m_Inner = handler;
		}
		public TResult GetResult()
		{
			return m_Inner.GetResult<TResult>();
		}
	}

}