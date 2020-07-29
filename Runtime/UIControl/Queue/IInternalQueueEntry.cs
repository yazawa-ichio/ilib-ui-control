using System;
using System.Threading.Tasks;

namespace ILib.UI
{
	internal interface IInternalQueueEntry<TParam> : IQueueEntry
	{
		UIInstance Instance { get; set; }
		string Path { get; }
		TParam Param { get; }

		void PreClose();
		void CompleteClose();
		void Fail(Exception ex);
		Task WaitCloseRequest();
		void PreOpen();
		void Abort();
	}
}