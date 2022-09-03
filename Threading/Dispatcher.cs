using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace AMP.Threading {
	public class Dispatcher : ThunderBehaviour {

		private static readonly Queue<Action> _executionQueue = new Queue<Action>();

		protected override ManagedLoops ManagedLoops => ManagedLoops.Update;

		public void ServerUpdateTick() {
			ManagedUpdate();
		}

		protected override void ManagedUpdate() {
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


		public static Dispatcher current = null;

		public static Dispatcher Instance() {
			if(current == null) {
				throw new Exception("Dispatcher could not find the Dispatcher object. Please ensure you have added initialized the Dispatcher correctly.");
			}
			return current;
		}


		public void Awake() {
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
