using System.Threading.Tasks;

namespace ILib.UI
{
	internal interface IQueueController
	{
		Task Close(IQueueEntry entry);
	}
}
