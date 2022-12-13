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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Nito.AsyncEx;

using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.LogstashHttp
{
    /// <summary>
    ///     Writes log events as documents to ElasticSearch.
    /// </summary>
    public class LogstashHttpSink : IBatchedLogEventSink
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly AsyncLock Mutex = new AsyncLock();

        private readonly LogstashHttpSinkState _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogstashHttpSink"/> class with the provided options
        /// </summary>
        /// <param name="options">
        /// Options configuring how the sink behaves, may NOT be null
        /// </param>
        public LogstashHttpSink(LogstashHttpSinkOptions options)
        {
            _state = LogstashHttpSinkState.Create(options);
        }

        public Task OnEmptyBatchAsync() => Task.CompletedTask;

        /// <summary>
        /// Emit a batch of log events, running to completion synchronously.
        /// </summary>
        /// <param name="events">
        /// The events to emit.
        /// </param>
        /// <remarks>
        /// Override either
        ///     <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatch(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})"/>
        ///     or
        ///     <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatchAsync(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})"/>
        ///     , not both.
        /// </remarks>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            // ReSharper disable PossibleMultipleEnumeration
            if (events == null || !events.Any()) return;

            if (_state.Options.JsonLinesBatch)
            {
                try
                {
                    var sw = new StringWriter();
                    foreach (var e in events)
                    {
                        _state.Formatter.Format(e, sw);

                        sw.WriteLine();
                    }
                    var logData = sw.ToString();

                    await PostLogDataAsync(logData);
                }
                catch (Exception ex)
                {
                    // Debug me
                    throw ex;
                }
            }
            else
            {
                foreach (var e in events)
                {
                    try
                    {
                        var sw = new StringWriter();
                        _state.Formatter.Format(e, sw);
                        var logData = sw.ToString();

                        await PostLogDataAsync(logData);
                    }
                    catch (Exception ex)
                    {
                        // Debug me
                        throw ex;
                    }
                }
            }
        }

        /// <summary>
        /// Posts the serialized events log data to logstash uri
        /// </summary>
        /// <param name="logData"></param>
        /// <returns></returns>
        private async Task PostLogDataAsync(string logData)
        {
            var stringContent = new StringContent(logData);
            stringContent.Headers.Remove("Content-Type");
            stringContent.Headers.Add("Content-Type", _state.Options.ContentType);

            // Using singleton of HttpClient so we need ensure of thread safety. Just use LockAsync.
            using (await Mutex.LockAsync().ConfigureAwait(false))
            {
                await HttpClient.PostAsync(_state.Options.LogstashUri, stringContent).ConfigureAwait(false);
            }
        }
    }
}