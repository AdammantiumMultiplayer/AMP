﻿using System;
using System.Collections.Concurrent;

namespace AMP.Threading {
	public class Dispatcher {

		private static readonly ConcurrentQueue<Action> executionQueue = new ConcurrentQueue<Action>();

		public static void UpdateTick() {
			int ms = DateTime.UtcNow.Millisecond;
			Action action;
			lock(executionQueue) {
				while(executionQueue.TryDequeue(out action) && DateTime.UtcNow.Millisecond - 20 < ms) {
					if (action != null) {
						action.Invoke();
					}
				}
			}
		}

		public static void Enqueue(Action action) {
            executionQueue.Enqueue(() => {
				action();
			});
        }

	}
}
