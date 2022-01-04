using UnityEngine;

public class CoroutineWrapper
{
	/// <summary>
	/// A class that inherits from MonoBehavior so coroutines can be spawned from the UtpServer class.
	/// </summary>
	protected class CoroutineRunner : MonoBehaviour { }
	private CoroutineRunner m_CoroutineRunnerWorker; // instantiated separately to avoid a stack overflow when accessing _coroutineRunner
	protected CoroutineRunner m_CoroutineRunner
	{
		get
		{
			if (m_CoroutineRunnerWorker != null)
			{
				return m_CoroutineRunnerWorker;
			}
			return InitCoroutineRunner();
		}
		set { }
	}

	/// <summary>
	/// Initialize a CoroutineRunner.
	/// </summary>
	/// <returns>A CoroutineRunner.</returns>
	private CoroutineRunner InitCoroutineRunner()
	{
		GameObject instance = new GameObject();
		instance.isStatic = true;
		m_CoroutineRunnerWorker = instance.AddComponent<CoroutineRunner>();
		return m_CoroutineRunnerWorker;
	}
}
