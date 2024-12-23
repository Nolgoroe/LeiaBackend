using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    /// <summary>
    /// Temporary table created for logging
    /// </summary>
    public class BackendLog
    {
        public DateTime Timestamp;
        /// <summary>
        /// Optional player ID, should be empty if not related to player
        /// This exists for research/debugging purposes (for example seeing all logs related to a specific user)
        /// </summary>
        public Guid PlayerId;
        public required string Log;
    }
}
