using UnityEngine;

namespace ILib.UI
{
	/// <summary>
	/// UIのインスタンスを保持します
	/// </summary>
	public class UIInstance
	{
		public GameObject Object;
		public IControl Control;
		public UIInstance Parent;
	}

}
