// Copyright 2017 Aleksandr Sukhodko
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.LogstashHttp
{
    /// <summary>
    ///     Writes log events as documents to ElasticSearch.
    /// </summary>
    public class LogstashHttpSink : PeriodicBatchingSink
    {
        private readonly LogstashHttpSinkState _state;

        /// <summary>
        ///     Creates a new LogstashHttpSink instance with the provided options
        /// </summary>
        /// <param name="options">Options configuring how the sink behaves, may NOT be null</param>
        public LogstashHttpSink(LogstashHttpSinkOptions options)
            : base(options.BatchPostingLimit, options.Period)
        {
            _state = LogstashHttpSinkState.Create(options);
        }

        /// <summary>
        ///     Emit a batch of log events, running to completion synchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <remarks>
        ///     Override either
        ///     <see
        ///         cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatch(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" />
        ///     or
        ///     <see
        ///         cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatchAsync(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" />
        ///     , not both.
        /// </remarks>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            // ReSharper disable PossibleMultipleEnumeration
            if (events == null || !events.Any())
                return;

            using (var c = new HttpClient())
            {
                foreach (var e in events)
                {
                    var sw = new StringWriter();
                    _state.Formatter.Format(e, sw);
                    var logData = sw.ToString();
                    await
                        c.PostAsync(_state.Options.LogstashUri,
                            new StringContent(logData, Encoding.UTF8, "application/json"));
                }
            }
        }
    }
}