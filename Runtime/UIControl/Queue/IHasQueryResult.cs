namespace ILib.UI
{
	/// <summary>
	/// リザルトを保持する事を示すクラスです。
	/// 値をセットすると自動で自身をCloseします。
	/// </summary>
	public interface IHasQueryResult<TResult>
	{
		QueryResult<TResult> QueryResult { get; }
	}
}