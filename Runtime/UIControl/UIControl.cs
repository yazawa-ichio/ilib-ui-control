using UnityEngine;

namespace ILib.UI
{
	using System.Threading.Tasks;

	/// <summary>
	/// UIの表示制御を行うクラスです。
	/// </summary>
	public abstract class UIControl<TParam> : MonoBehaviour, IControl<TParam>
	{
		/// <summary>
		/// UIが有効かどうかです。
		/// デフォルトの動作は自身のゲームオブジェクトのアクティブと同じ値になります。
		/// </summary>
		public virtual bool IsActive { get => gameObject != null ? gameObject.activeInHierarchy : false; }

		/// <summary>
		///　Behind時に自身のゲームオブジェクトを非アクティブにするか？
		/// </summary>
		public virtual bool IsDeactivateInBehind { get => m_IsDeactivateInBehind; }

		/// <summary>
		///　Behind時に非表示アニメーション処理を実行するか？
		///　デフォルトの動作はIsDeactivateInBehindと同じ値になります。
		/// </summary>
		public virtual bool IsHideInBehind { get => IsDeactivateInBehind; }

		/// <summary>
		/// アニメーションによる遷移を行うクラスです。
		/// </summary>
		protected ITransition Transition { get; private set; }

		/// <summary>
		/// 自身を管理する親のコントローラーです。
		/// </summary>
		protected IController Controller { get; private set; }

		[SerializeField]
		bool m_IsDeactivateInBehind = false;

		void IControl.SetController(IController controller)
		{
			Controller = controller;
			Transition = GetTransition();
		}

		public void Close()
		{
			Controller.Close(this);
		}

		/// <summary>
		/// アニメーションによる遷移を行うクラスを返します。
		/// 返さない場合も正常に動きます。
		/// </summary>
		protected virtual ITransition GetTransition()
		{
			return GetComponent<ITransition>();
		}

		Task IControl.OnCreated(object prm)
		{
			Transition?.OnPreCreated();
			return OnCreated((TParam)prm);
		}

		Task IControl.OnClose() => OnClose();

		Task IControl.OnFront(bool open) => OnFront(open);

		Task IControl.OnBehind() => OnBehind();

		/// <summary>
		/// UIが作成された直後に実行されます。
		/// </summary>
		protected virtual Task OnCreated(TParam prm) => Util.Successed;

		/// <summary>
		/// UIを削除する直前に実行されます。
		/// デフォルトでは可能であれば閉じるアニメーションを実行します。
		/// アクティブではない場合は何も行いません。
		/// </summary>
		protected virtual Task OnClose() => (IsActive && Transition != null) ? Transition.Hide(true) : Util.Successed;

		/// <summary>
		/// UIが最前面に来た際に実行されます。オープン処理かどうかは引数で確認できます。
		/// デフォルトでは可能であれば開くアニメーションを実行します。
		/// </summary>
		protected virtual Task OnFront(bool open)
		{
			UIControlLog.Trace("[ilib-ui] OnFront:{0}, open:{1}", this, open);
			if ((open || IsHideInBehind) && Transition != null)
			{
				var show = Transition.Show(open);
				if (show != null) return show;
			}
			return Util.Successed;
		}

		/// <summary>
		/// UIが最前面から後ろになった際に実行されます。Close時は実行されません。
		/// デフォルトでは可能であれば開くアニメーションを実行します。
		/// </summary>
		protected virtual Task OnBehind()
		{
			UIControlLog.Trace("[ilib-ui] OnBehind:{0}", this);
			if (IsHideInBehind && Transition != null)
			{
				var hide = Transition.Hide(false);
				if (hide != null) return hide;
			}
			return Util.Successed;
		}

	}
}
