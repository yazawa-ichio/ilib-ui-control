namespace ILib.UI
{
	public interface IQueueQueryHandler
	{
		TResult GetResult<TResult>();
	}

	public interface IQueueQueryHandler<TResult>
	{
		TResult GetResult();
	}

}