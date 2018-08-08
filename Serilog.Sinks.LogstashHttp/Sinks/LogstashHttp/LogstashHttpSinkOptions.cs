// Copyright 2014 Serilog Contributors
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
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.LogstashHttp
{
    /// <summary>
    ///     Provides ElasticsearchSink with configurable options
    /// </summary>
    public class LogstashHttpSinkOptions
    {
        /// <summary>
        ///     Configures the sink defaults
        /// </summary>
        public LogstashHttpSinkOptions()
        {
            Period = TimeSpan.FromSeconds(2);
            BatchPostingLimit = 50;
            ContentType = "application/json";
        }

        /// <summary>
        ///     The maximum number of events to post in a single batch.
        /// </summary>
        public int BatchPostingLimit { get; set; }

        /// <summary>
        ///     The time to wait between checking for event batches. Defaults to 2 seconds.
        /// </summary>
        public TimeSpan Period { get; set; }

        /// <summary>
        ///     Supplies culture-specific formatting information, or null.
        /// </summary>
        public IFormatProvider FormatProvider { get; set; }

        /// <summary>
        ///     When true fields will be written at the root of the json document
        /// </summary>
        public bool InlineFields { get; set; }

        /// <summary>
        ///     The minimum log event level required in order to write an event to the sink.
        /// </summary>
        public LogEventLevel? MinimumLogEventLevel { get; set; }

        /// <summary>
        ///     Customizes the formatter used when converting log events into ElasticSearch documents. Please note that the
        ///     formatter output must be valid JSON
        /// </summary>
        public ITextFormatter CustomFormatter { get; set; }

        /// <summary>
        ///     Customizes the formatter used when converting log events into the durable sink. Please note that the formatter
        ///     output must be valid JSON
        /// </summary>
        public ITextFormatter CustomDurableFormatter { get; set; }

        /// <summary>
        ///     Logstash Uri
        /// </summary>
        public string LogstashUri { get; set; }

        /// <summary>
        ///     Enables sending the batch of events at once as json lines to logstash. Defaults to false.
        /// </summary>
        public bool JsonLinesBatch { get; set; }

        /// <summary>
        ///     Content-Type request header used when posting logs to logstash. Defaults to "application/json"
        /// </summary>
        public string ContentType { get; set; }
  }
}