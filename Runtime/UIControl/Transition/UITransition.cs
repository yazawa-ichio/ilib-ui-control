using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ILib.UI
{
	/// <summary>
	/// uGUIの機能を使った簡単なアニメーションの遷移の実装です。
	/// </summary>
	public class UITransition : MonoBehaviour, ITransition
	{
		public interface IAnim
		{
			void Init();
			Task Run(MonoBehaviour owner, bool show);
		}

		[Serializable]
		public class Fade : IAnim
		{
			[SerializeField]
			float m_Time = 0.3f;
			[SerializeField]
			CanvasGroup[] m_Target = null;
			[SerializeField]
			float m_Show = 1f;
			[SerializeField]
			float m_Hide = 0f;
			[SerializeField]
			AnimationCurve m_Curve = AnimationCurve.Linear(0, 0, 1f, 1f);
			[SerializeField]
			bool m_Realtime = false;

			public void Init()
			{
				foreach (var target in m_Target)
				{
					target.alpha = m_Hide;
				}
			}

			public Task Run(MonoBehaviour owner, bool show)
			{
				var task = new TaskCompletionSource<bool>();
				owner.StartCoroutine(Run(show, task));
				return task.Task;
			}

			IEnumerator Run(bool show, TaskCompletionSource<bool> ret)
			{
				float cur = 0f;
				while (cur < m_Time)
				{
					try
					{
						float rate = (cur / m_Time);
						if (show)
						{
							rate = m_Curve.Evaluate(cur / m_Time);
						}
						else
						{
							rate = m_Curve.Evaluate(1f - cur / m_Time);
						}
						foreach (var target in m_Target)
						{
							target.alpha = m_Hide + (m_Show - m_Hide) * rate;
						}
					}
					catch (Exception ex)
					{
						ret.SetException(ex);
						yield break;
					}
					yield return null;
					cur += m_Realtime ? Time.unscaledDeltaTime : Time.deltaTime;
				}
				try
				{
					foreach (var target in m_Target)
					{
						target.alpha = show ? m_Show : m_Hide;
					}
					ret.SetResult(true);
				}
				catch (Exception ex)
				{
					ret.SetException(ex);
				}
			}

		}

		[Serializable]
		public class Move : IAnim
		{
			[SerializeField]
			float m_Time = 0.3f;
			[SerializeField]
			Transform m_Target = null;
			[SerializeField]
			Vector3 m_ShowPos = default;
			[SerializeField]
			Vector3 m_HidePos = default;
			[SerializeField]
			AnimationCurve m_Curve = AnimationCurve.Linear(0, 0, 1f, 1f);
			[SerializeField]
			bool m_Realtime = false;

			public void Init()
			{
				if (m_Target is RectTransform rect)
				{
					rect.anchoredPosition3D = m_HidePos;
				}
				else
				{
					m_Target.localPosition = m_HidePos;
				}
			}

			public Task Run(MonoBehaviour owner, bool show)
			{
				var task = new TaskCompletionSource<bool>();
				owner.StartCoroutine(Run(show, task));
				return task.Task;
			}

			IEnumerator Run(bool show, TaskCompletionSource<bool> ret)
			{
				float cur = 0f;
				while (cur < m_Time)
				{
					try
					{
						float rate = (cur / m_Time);
						if (show)
						{
							rate = m_Curve.Evaluate(cur / m_Time);
						}
						else
						{
							rate = m_Curve.Evaluate(1f - cur / m_Time);
						}
						var pos = m_HidePos + (m_ShowPos - m_HidePos) * rate;
						if (m_Target is RectTransform rect)
						{
							rect.anchoredPosition3D = pos;
						}
						else
						{
							m_Target.localPosition = pos;
						}
					}
					catch (Exception ex)
					{
						ret.SetException(ex);
						yield break;
					}
					yield return null;
					cur += m_Realtime ? Time.unscaledDeltaTime : Time.deltaTime;
				}
				try
				{
					if (m_Target is RectTransform rect)
					{
						rect.anchoredPosition3D = show ? m_ShowPos : m_HidePos;
					}
					else
					{
						m_Target.localPosition = show ? m_ShowPos : m_HidePos;
					}
					ret.SetResult(true);
				}
				catch (Exception ex)
				{
					ret.SetException(ex);
				}
			}

		}

		[SerializeField]
		Fade m_Fade = default;
		[SerializeField]
		Move m_Move = default;

		IAnim[] m_Anim;
		CancellationTokenSource m_Cancellation;

		void OnDisable()
		{
			var tmp = m_Cancellation;
			m_Cancellation = null;
			tmp?.Cancel(true);
		}

		public void OnPreCreated()
		{
			m_Anim = new IAnim[] { m_Fade, m_Move };
			foreach (var anim in m_Anim)
			{
				anim.Init();
			}
		}

		public Task Hide(bool close)
		{
			if (m_Cancellation == null) m_Cancellation = new CancellationTokenSource();

			var hide = m_Anim.Select(x => x.Run(this, false));
			return Task.WhenAll(hide.ToArray()).ContinueWith(_ => { }, m_Cancellation.Token);
		}

		public Task Show(bool open)
		{
			if (m_Cancellation == null) m_Cancellation = new CancellationTokenSource();

			var show = m_Anim.Select(x => x.Run(this, true));
			return Task.WhenAll(show.ToArray()).ContinueWith(_ => { }, m_Cancellation.Token);
		}

	}

}