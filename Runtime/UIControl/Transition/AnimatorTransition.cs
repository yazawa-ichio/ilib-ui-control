using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace ILib.UI
{
	/// <summary>
	/// Animatorを利用した遷移処理です。
	/// SHOW・HIDEステートの完了を検知して遷移します。
	/// </summary>
	public class AnimatorTransition : MonoBehaviour, ITransition
	{

		Animator m_Animator;
		string m_WaitState;
		TaskCompletionSource<bool> m_Wait;

		public void OnPreCreated()
		{
			m_Animator = GetComponent<Animator>();
		}

		public Task Hide(bool close)
		{
			m_Wait?.SetCanceled();
			StopAllCoroutines();

			m_Animator.SetBool("OPEN", false);
			m_Animator.SetBool("SHOW", false);
			m_Animator.SetBool("CLOSE", close);
			m_Animator.SetBool("HIDE", true);

			m_WaitState = "HIDE";
			m_Wait = new TaskCompletionSource<bool>();
			StartCoroutine(Wait());
			return m_Wait.Task;
		}

		public Task Show(bool open)
		{
			m_Wait?.SetCanceled();
			StopAllCoroutines();
			m_Animator.SetBool("OPEN", open);
			m_Animator.SetBool("SHOW", true);
			m_Animator.SetBool("CLOSE", false);
			m_Animator.SetBool("HIDE", false);
			m_WaitState = "SHOW";
			m_Wait = new TaskCompletionSource<bool>();
			StartCoroutine(Wait());
			return m_Wait.Task;
		}

		IEnumerator Wait()
		{
			while (true)
			{
				var info = m_Animator.GetCurrentAnimatorStateInfo(0);
				if (info.IsName(m_WaitState) && info.normalizedTime >= 1f)
				{
					m_Wait.SetResult(true);
					m_Wait = null;
					yield break;
				}
				yield return null;
			}
		}

		void OnDisable()
		{
			if (m_Wait != null)
			{
				m_Wait.TrySetException(new System.Exception($"disable in AnimatorTransition. {this}"));
			}
		}

	}

}