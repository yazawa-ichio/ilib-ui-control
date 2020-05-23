using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace ILib.UI
{

	/// <summary>
	/// UIの表示制御の基底クラスです。
	/// Open・Change・Closeの操作を行えます。
	/// 実行中に非アクティブにするとコルーチンが停止するため正常に動かなくなります。
	/// </summary>
	public abstract class UIController<TParam, UControl> : MonoBehaviour, IController where UControl : class, IControl
	{

		Task IController.Close(IControl control) => Close(control);

		protected abstract Task Close(IControl control);

		/// <summary>
		/// アセットバンドル等から読む時は継承先で書き換えてください。
		/// </summary>
		protected virtual Task<GameObject> Load<T>(string path, TParam prm)
		{
			var task = new TaskCompletionSource<GameObject>();
			var loading = Resources.LoadAsync<GameObject>(path);
			loading.completed += (_) =>
			{
				var ret = loading.asset as GameObject;
				if (ret != null)
				{
					task.SetResult(ret);
				}
				else
				{
					task.SetException(new Exception($"not found {path}, {prm}, {typeof(T)}"));
				}
			};
			return task.Task;
		}

		protected abstract IEnumerable<T> GetActive<T>();

		/// <summary>
		/// 現在アクティブな指定型のUIに対して処理を行います。
		/// </summary>
		public bool Execute<T>(Action<T> action)
		{
			bool ret = false;
			foreach (var ui in GetActive<T>())
			{
				ret = true;
				action.Invoke(ui);
			}
			UIControlLog.Debug("[ilib-ui] Execute<{0}>(), ret:{1}", typeof(T), ret);
			return ret;
		}

		/// <summary>
		/// 現在アクティブな指定型のUIに対して一つだけ処理を行います。
		/// </summary>
		public bool ExecuteAnyOne<T>(Action<T> action)
		{
			UIControlLog.Debug("[ilib-ui] ExecuteAnyOne<{0}>(Action<T>)", typeof(T));
			return ExecuteAnyOne<T>(x =>
			{
				action(x);
				return true;
			});
		}

		/// <summary>
		/// 現在アクティブな指定型のUIに対して一つだけ処理を行います。
		/// </summary>
		public bool ExecuteAnyOne<T>(Func<T, bool> action)
		{
			UIControlLog.Debug("[ilib-ui] ExecuteAnyOne<{0}>(Func<{0}, bool>)", typeof(T));
			foreach (var ui in GetActive<T>())
			{
				var ret = action.Invoke(ui);
				if (ret) return true;
			}
			return false;
		}

		/// <summary>
		/// IExecuteBackを実装したUIに対してバック処理を行います。
		/// </summary>
		public bool ExecuteBack()
		{
			UIControlLog.Trace("[ilib-ui] Do ExecuteBack()");
			return ExecuteAnyOne<IExecuteBack>(x =>
			{
				var ret = x.TryBack();
				if (ret) UIControlLog.Debug("[ilib-ui] ExecuteBack:{0}", x);
				return ret;
			});
		}

		Queue<Action> m_ProcessRequest = new Queue<Action>();

		/// <summary>
		/// 実行中のプロセスがある場合にtrueが返ります。
		/// UIQueueの場合、表示待ちのリクエストはプロセスに含まれないことに注意してください。
		/// </summary>
		public bool HasProcess => m_ProcessCount > 0;

		protected void EnqueueProcess(Action action)
		{
			if (m_ProcessCount > 0)
			{
				m_ProcessRequest.Enqueue(action);
			}
			else
			{
				action?.Invoke();
			}
		}

		int m_ProcessCount = 0;
		protected void StartProcess()
		{
			if (m_ProcessCount == 0)
			{
				UIControlLog.Trace("[ilib-ui] OnStartProcess");
				OnStartProcess();
			}
			m_ProcessCount++;
		}

		protected void EndProcess()
		{
			m_ProcessCount--;
			if (m_ProcessCount == 0)
			{
				UIControlLog.Trace("[ilib-ui] OnEndProcess");
				OnEndProcess();
				while (m_ProcessRequest.Count > 0 && !HasProcess)
				{
					m_ProcessRequest.Dequeue().Invoke();
				}
			}
		}

		/// <summary>
		/// プロセスが開始された際に実行されます。
		/// ダブルタップが出来ないように入力制限を行う等に利用します。
		/// </summary>
		protected virtual void OnStartProcess() { }

		/// <summary>
		/// 全てのプロセスが終了した際に実行されます。
		/// ダブルタップが出来ないように入力制限を行う等に利用します。
		/// </summary>
		protected virtual void OnEndProcess() { }

		/// <summary>
		/// オープン処理が開始された際に実行されます。
		/// </summary>
		protected virtual Task OnBeginOpen() => Util.Successed;

		/// <summary>
		/// UIが生成された直後に実行されます。
		/// </summary>
		protected virtual void OnOpen(UControl ui) { }

		/// <summary>
		/// オープン処理が完了した際に実行されます。
		/// </summary>
		protected virtual Task OnEndOpen() => Util.Successed;

		/// <summary>
		/// 親のUIのBehind処理の実行が完了するまでOnFrontの処理を実行しません
		/// 標準は無効です。
		/// </summary>
		protected virtual bool IsWaitBehindBeforeOnFront<T>(string path, TParam prm) where T : UControl
		{
			return false;
		}

		protected async Task<UIInstance> Open<T>(string path, TParam prm, UIInstance parent = null) where T : UControl
		{
			UIControlLog.Debug("[ilib-ui] Open:{0}, prm:{1}, parent:{2}", path, prm, parent);
			StartProcess();
			try
			{

				await OnBeginOpen();

				var prefab = await Load<T>(path, prm);

				var obj = Instantiate(prefab, transform);
				var ui = obj.GetComponent<T>();

				ui.SetController(this);

				var behind = parent?.Control?.OnBehind() ?? Util.Successed;

				await ui.OnCreated(prm);

				OnOpen(ui);

				if (behind != null && IsWaitBehindBeforeOnFront<T>(path, prm))
				{
					await behind;
					behind = null;
					if (parent != null && parent.Control != null)
					{
						if (parent.Control.IsDeactivateInBehind) parent.Object.SetActive(false);
					}
				}

				await ui.OnFront(open: true);

				if (behind != null)
				{
					await behind;
					if (parent != null && parent.Control != null)
					{
						if (parent.Control.IsDeactivateInBehind) parent.Object.SetActive(false);
					}
				}

				await OnEndOpen();

				var ret = new UIInstance();
				ret.Control = ui;
				ret.Object = obj;
				ret.Parent = parent;
				return ret;

			}
			finally
			{
				UIControlLog.Trace("[ilib-ui] Complete Open:{0}", path);
				EndProcess();
			}
		}


		protected Task CloseControls(UIInstance[] controls)
		{
			if (controls == null || controls.Length == 0)
			{
				return Util.Successed;
			}
			return Task.WhenAll(controls.Select(async x =>
			{
				UIControlLog.Debug("[ilib-ui] Close:{0}", x);
				await x.Control.OnClose();
				OnClose(x.Control as UControl);
				Destroy(x.Object);
			}));
		}

		/// <summary>
		/// 遷移処理が開始された際に実行されます。
		/// </summary>
		protected virtual Task OnBeginChange() => Util.Successed;

		/// <summary>
		/// 遷移処理が完了した際に実行されます。
		/// </summary>
		protected virtual Task OnEndChange() => Util.Successed;

		/// <summary>
		/// 親のUIのClose処理の実行が完了するまでOnOpenの処理を実行しません
		/// 標準は無効です。
		/// </summary>
		protected virtual bool IsWaitCloseBeforeOnOpen<T>(string path, TParam prm) where T : UControl
		{
			return false;
		}

		protected async Task<UIInstance> Change<T>(string path, TParam prm, UIInstance parent, UIInstance[] releases) where T : UControl
		{
			UIControlLog.Debug("[ilib-ui] Change:{0}, prm:{1}, parent:{2}", path, prm, parent);

			StartProcess();
			try
			{
				await OnBeginChange();

				var loading = Load<T>(path, prm);

				var close = CloseControls(releases);

				var prefab = await loading;

				if (IsWaitCloseBeforeOnOpen<T>(path, prm))
				{
					await close;
				}

				var obj = Instantiate(prefab, transform);
				var ui = obj.GetComponent<T>();

				ui.SetController(this);

				await ui.OnCreated(prm);

				OnOpen(ui);

				await ui.OnFront(open: true);

				if (!close.IsCompleted)
				{
					await close;
				}

				await OnEndChange();

				var ret = new UIInstance();
				ret.Control = ui;
				ret.Object = obj;
				ret.Parent = parent;
				return ret;
			}
			finally
			{
				UIControlLog.Trace("[ilib-ui] Complete Change:{0}", path);
				EndProcess();
			}
		}

		/// <summary>
		/// 削除処理が開始された際に実行されます。
		/// </summary>
		protected virtual Task OnBeginClose() => Util.Successed;

		/// <summary>
		/// インスタンスを削除する直前に実行されます。
		/// </summary>
		protected virtual void OnClose(UControl ui) { }

		/// <summary>
		/// 削除処理が完了した際に実行されます。
		/// </summary>
		protected virtual Task OnEndClose() => Util.Successed;

		/// <summary>
		/// Close処理の実行が完了するまでOnFrontの処理を実行しません
		/// 標準は無効です。
		/// </summary>
		protected virtual bool IsWaitCloseBeforeOnFront(UControl font)
		{
			return false;
		}

		protected async Task Close(UIInstance[] releases, UIInstance front = null)
		{
			UIControlLog.Debug("[ilib-ui] Close, next front:{0}", front);
			StartProcess();
			try
			{
				await OnBeginClose();

				var close = CloseControls(releases);

				if (front != null)
				{
					if (!front.Object.activeSelf) front.Object.SetActive(true);
					if (IsWaitCloseBeforeOnFront(front.Control as UControl))
					{
						await close;
					}
					await front.Control.OnFront(open: false);
				}

				if (!close.IsCompleted)
				{
					await close;
				}

				await OnEndClose();
			}
			finally
			{
				UIControlLog.Trace("[ilib-ui] Complete Close");
				EndProcess();
			}
		}


	}

}