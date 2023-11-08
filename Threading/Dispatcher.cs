using AMP.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using UnityEngine;

namespace AMP.Threading {
    public class Dispatcher {

		private static readonly ConcurrentQueue<Action> executionQueue = new ConcurrentQueue<Action>();

		public static void UpdateTick() {
			int ms = DateTime.UtcNow.Millisecond;
			Action action;
			lock(executionQueue) {
				while(/*DateTime.UtcNow.Millisecond - 20 < ms &&*/ executionQueue.TryDequeue(out action)) {
					if (action != null) {
						try {
							action.Invoke();
						}catch(Exception ex) {
							Log.Err(ex);
						}
					}
				}
			}

            #if FULL_DEBUG
			Log.Debug($"Tick took {DateTime.UtcNow.Millisecond - ms}ms");
			#endif
        }

		public static void Enqueue(Action action) {
            #if FULL_DEBUG
			StackTrace stackTrace = new StackTrace();
            string caller = stackTrace.GetFrame(1).GetMethod().ReflectedType.Name + "." + stackTrace.GetFrame(1).GetMethod().Name;
			#endif

            executionQueue.Enqueue(() => {
                #if FULL_DEBUG
				Log.Warn("DEBUG", caller);
				#endif

				action();
			});
        }

	}
}
