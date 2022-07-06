using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AMP.Threading {
	public class UnityMainThreadDispatcher : MonoBehaviour {

		private static readonly Queue<Action> _executionQueue = new Queue<Action>();

		public void Update() {
			lock(_executionQueue) {
				int ms = DateTime.UtcNow.Millisecond;
				while(_executionQueue.Count > 0 && DateTime.UtcNow.Millisecond - 20 < ms) {
					_executionQueue.Dequeue().Invoke();
				}
			}
		}

		public void Enqueue(IEnumerator action) {
			lock(_executionQueue) {
				_executionQueue.Enqueue(() => {
					StartCoroutine(action);
				});
			}
		}

		public void Enqueue(Action action) {
			Enqueue(ActionWrapper(action));
		}
		IEnumerator ActionWrapper(Action action) {
			action();
			yield return null;
		}


		private static UnityMainThreadDispatcher current = null;

		public static UnityMainThreadDispatcher Instance() {
			if(current == null) {
				throw new Exception("UnityMainThreadDispatcher could not find the UnityMainThreadDispatcher object. Please ensure you have added the MainThreadExecutor Prefab to your scene.");
			}
			return current;
		}


		void Awake() {
			if(current == null) {
				current = this;
				DontDestroyOnLoad(this.gameObject);
			} else {
				Destroy(this.gameObject);
			}
		}

		void OnDestroy() {
			if(current == this)
				current = null;
		}
	}
}
