using AMP.Logging;
using System;
using System.Collections.Concurrent;

namespace AMP.Threading {
    public class Dispatcher {

		private static readonly ConcurrentQueue<Action> executionQueue = new ConcurrentQueue<Action>();

		public static void UpdateTick() {
			int ms = DateTime.UtcNow.Millisecond;
			Action action;
			lock(executionQueue) {
				while(DateTime.UtcNow.Millisecond - 20 < ms && executionQueue.TryDequeue(out action)) {
					if (action != null) {
						try {
							action.Invoke();
						}catch(Exception ex) {
							Log.Err(ex);
						}
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
