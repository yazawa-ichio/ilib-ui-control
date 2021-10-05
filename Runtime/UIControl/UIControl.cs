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
		[System.Obsolete("IsBehindDeactivate関数に変更になりました", true)]
		public virtual bool IsDeactivateInBehind => IsBehindDeactivate(HideTransitionOption.Default);

		/// <summary>
		///　Behind時に非表示アニメーション処理を実行するか？
		///　デフォルトの動作はIsDeactivateInBehindと同じ値になります。
		/// </summary>
		[System.Obsolete("UseHideTransition関数に変更になりました", true)]
		public virtual bool IsHideInBehind => IsDeactivateInBehind;

		/// <summary>
		/// アニメーションによる遷移を行うクラスです。
		/// </summary>
		protected ITransition Transition { get; private set; }

		/// <summary>
		/// 自身を管理する親のコントローラーです。
		/// </summary>
		protected IController Controller { get; private set; }

		protected TParam Param { get; private set; }

		[SerializeField]
		bool m_IsDeactivateInBehind = false;
		bool m_InHide;

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
			//nullは許容する
			if (prm != null)
			{
				Param = (TParam)prm;
			}
			Transition?.OnPreCreated();
			return OnCreated(Param);
		}

		async Task IControl.OnClose()
		{
			UIControlLog.Trace("[ilib-ui] OnClose:{0}", this);
			await OnPreClose();
			await OnCloseTransition();
			await OnClose();
		}

		async Task IControl.OnFront(bool open)
		{
			UIControlLog.Trace("[ilib-ui] OnFront:{0}, open:{1}", this, open);
			await OnPreFront(open);
			await OnFrontTransition(open);
			await OnFront(open);
		}

		async Task<bool> IControl.OnBehind(object frontParam)
		{
			UIControlLog.Trace("[ilib-ui] OnBehind:{0}", this);
			await OnPreBehind();
			var hideOption = HideTransitionOption.Default;
			if (frontParam is IBehindTargetOption option)
			{
				hideOption = option.HideOption;
			}
			m_InHide = await OnBehindTransition(hideOption);
			await OnBehind();
			return m_InHide;
		}

		/// <summary>
		/// UIが作成された直後に実行されます。
		/// </summary>
		protected virtual Task OnCreated(TParam prm) => Util.Successed;

		/// <summary>
		/// UIを削除する直前に実行されます。
		/// 非表示のアニメーションよりも前に実行されます。
		/// </summary>
		protected virtual Task OnPreClose() => Util.Successed;

		/// <summary>
		/// UIを削除する直前に実行されます。
		/// 非表示のアニメーションよりも後に実行されます。
		/// デフォルトでは可能であれば閉じるアニメーションを実行します。
		/// アクティブではない場合は何も行いません。
		/// </summary>
		protected virtual Task OnClose() => Util.Successed;

		/// <summary>
		/// UIを削除する直前に実行されます。
		/// デフォルトでは可能であれば閉じるアニメーションを実行します。
		/// アクティブではない場合は何も行いません。
		/// </summary>
		protected virtual Task OnCloseTransition() => (IsActive && Transition != null) ? Transition.Hide(true) : Util.Successed;

		/// <summary>
		/// UIが最前面に来た際に実行されます。オープン処理かどうかは引数で確認できます。
		/// アニメーションよりも早くに実行されます。
		/// </summary>
		protected virtual Task OnPreFront(bool open)
		{
			return Util.Successed;
		}

		/// <summary>
		/// UIが最前面に来た際に実行されます。
		/// 表示のアニメーションよりも後に実行されます。
		/// デフォルトでは可能であれば開くアニメーションを実行します。
		/// </summary>
		protected virtual Task OnFront(bool open)
		{
			return Util.Successed;
		}

		/// <summary>
		/// UIが最前面に来た際に実行されます。
		/// デフォルトでは可能であれば開くアニメーションを実行します。
		/// </summary>
		protected virtual Task OnFrontTransition(bool open)
		{
			var inHide = m_InHide;
			m_InHide = false;
			if ((open || inHide) && Transition != null)
			{
				var show = Transition.Show(open);
				if (show != null) return show;
			}
			return Util.Successed;
		}

		/// <summary>
		/// UIが最前面から後ろになった際に実行されます。Close時は実行されません。
		/// アニメーションよりも早くに実行されます。
		/// </summary>
		protected virtual Task OnPreBehind() => Util.Successed;

		/// <summary>
		/// UIが最前面から後ろになった際に実行されます。Close時は実行されません。
		/// アニメーションよりも後に実行されます。
		/// </summary>
		protected virtual Task OnBehind() => Util.Successed;

		/// <summary>
		/// UIが最前面から後ろになった際に実行されます。Close時は実行されません。
		/// デフォルトでは可能であれば閉じるアニメーションを実行します。
		/// </summary>
		protected virtual async Task<bool> OnBehindTransition(HideTransitionOption type)
		{
			if (type == HideTransitionOption.Disable)
			{
				return false;
			}
			bool deactivate = IsBehindDeactivate(type);
			bool hideAnim = UseHideTransition(type);
			if (hideAnim && Transition != null)
			{
				var hide = Transition.Hide(false);
				if (hide != null)
				{
					await hide;
				}
			}
			return deactivate;
		}

		/// <summary>
		///　Behind時に自身のゲームオブジェクトを非アクティブにするか？
		/// </summary>
		protected virtual bool IsBehindDeactivate(HideTransitionOption option)
		{
			switch (option)
			{
				case HideTransitionOption.DeactivateOnly:
				case HideTransitionOption.Deactivate:
					return true;
				case HideTransitionOption.TransitionOnly:
				case HideTransitionOption.Disable:
					return false;
			}
			return m_IsDeactivateInBehind;
		}

		/// <summary>
		///　Behind時に非表示アニメーション処理を実行するか？
		///　デフォルトの動作はIsDeactivateInBehindと同じ値になります。
		/// </summary>
		protected virtual bool UseHideTransition(HideTransitionOption option)
		{
			switch (option)
			{
				case HideTransitionOption.DeactivateOnly:
					return false;
				case HideTransitionOption.TransitionOnly:
					return true;
			}
			return IsBehindDeactivate(option);
		}

	}
}