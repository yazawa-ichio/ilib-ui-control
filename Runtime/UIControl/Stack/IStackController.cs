using System.Threading.Tasks;

namespace ILib.UI
{
	internal interface IStackController
	{
		bool IsFornt(StackEntry entry);
		Task Pop(StackEntry entry);
	}
}
