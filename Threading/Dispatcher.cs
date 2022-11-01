﻿using System;
using System.Collections.Generic;

namespace AMP.Threading {
	public class Dispatcher {

		private readonly Queue<Action> _executionQueue = new Queue<Action>();

		public static void UpdateTick() {
			lock(Instance()._executionQueue) {
				int ms = DateTime.UtcNow.Millisecond;
				while(Instance()._executionQueue.Count > 0 && DateTime.UtcNow.Millisecond - 20 < ms) {
                    Instance()._executionQueue.Dequeue().Invoke();
				}
			}
		}

		public static void Enqueue(Action action) {
            lock(Instance()._executionQueue) {
                Instance()._executionQueue.Enqueue(() => {
                    action();
                });
            }
        }

        private static Dispatcher current = null;

		internal Dispatcher() {
			if(current == null) { current = this; }
		}

		public static Dispatcher Instance() {
			if(current == null) {
				current = new Dispatcher();
				//throw new Exception("Dispatcher could not find the Dispatcher object. Please ensure you have added initialized the Dispatcher correctly.");
			}
			return current;
		}
	}
}
