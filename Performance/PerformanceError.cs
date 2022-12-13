using AMP.Logging;
using System;

namespace AMP.Performance {
    public class PerformanceError {

        private long initial_millis = 0;
        private long last_millis = 0;

        private long errorSpacing;
        private string errorMessage;
        private bool outputToConsole;
        public PerformanceError(long errorSpacing = 5000, string errorPrefix = "Performance", string errorMessage = "Performance issue, loop running for {0}ms.", bool outputToConsole = true) {
            this.errorSpacing    = errorSpacing;
            this.errorMessage    = errorMessage;
            this.outputToConsole = outputToConsole;

            Reset();
        }

        public void Reset() {
            initial_millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            last_millis = initial_millis;
        }

        public bool HasPerformanceIssue() {
            long millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if(millis >= last_millis + errorSpacing) {
                last_millis += errorSpacing;

                if(outputToConsole) {
                    Log.Err(string.Format(errorMessage, (millis - initial_millis)));
                }

                return true;
            }
            return false;
        }

    }
}
