using AMP.Logging;
using System;
using System.Collections.Concurrent;
#if FULL_DEBUG
using System.Diagnostics;
#endif

namespace AMP.Threading {
    public class Dispatcher {

		private static readonly ConcurrentQueue<Action> executionQueue = new ConcurrentQueue<Action>();
		private static readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

		// Time per frame the queued actions may take, the rest waits for the next frame
		private const double FRAME_BUDGET_MS = 2.0;
		// If more than this is queued, the overflow gets worked off regardless of the budget, so the queue can't keep growing over time
		private const int MAX_BACKLOG = 20;

		public static void UpdateTick() {
			stopwatch.Restart();

			// Everything above the backlog limit has to run this frame, then work on as long as there is time left
			int mustRun = executionQueue.Count - MAX_BACKLOG;
			Action action;
			while((mustRun > 0 || stopwatch.Elapsed.TotalMilliseconds < FRAME_BUDGET_MS) && executionQueue.TryDequeue(out action)) {
				mustRun--;
				if (action == null) continue;
				try {
					action.Invoke();
				}catch(Exception ex) {
					Log.Err(ex);
				}
			}

            #if FULL_DEBUG
			Log.Debug($"Tick took {stopwatch.Elapsed.TotalMilliseconds:0.00}ms, {executionQueue.Count} left");
			#endif
        }

		public static void Enqueue(Action action) {
            #if FULL_DEBUG
			StackTrace stackTrace = new StackTrace();
            string caller = stackTrace.GetFrame(1).GetMethod().ReflectedType.Name + "." + stackTrace.GetFrame(1).GetMethod().Name;
			#endif

            #if FULL_DEBUG
            executionQueue.Enqueue(() => {
				Log.Warn("DEBUG", caller);
				action();
			});
			#else
            executionQueue.Enqueue(action);
			#endif
        }

	}
}
