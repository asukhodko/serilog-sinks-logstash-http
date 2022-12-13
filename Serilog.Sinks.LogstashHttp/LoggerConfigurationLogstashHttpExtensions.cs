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
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.LogstashHttp;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog
{
    /// <summary>
    ///     Adds the WriteTo.LogstashHttp() extension method to <see cref="LoggerConfiguration" />.
    /// </summary>
    public static class LoggerConfigurationLogstashHttpExtensions
    {
        /// <summary>
        ///     Adds a sink that writes log events as documents to Logstash http plugin.
        /// </summary>
        /// <param name="loggerSinkConfiguration">Options for the sink.</param>
        /// <param name="options">Provides options specific to the LogstashHttp sink</param>
        /// <returns>LoggerConfiguration object</returns>
        public static LoggerConfiguration LogstashHttp(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            LogstashHttpSinkOptions options)
        {
            var sink = new LogstashHttpSink(options);

            var periodicBatchingSink = new PeriodicBatchingSink(sink, options);

            return loggerSinkConfiguration.Sink(periodicBatchingSink, options.MinimumLogEventLevel ?? LevelAlias.Minimum);
        }

        /// <summary>
        ///     Overload to allow basic configuration through AppSettings.
        /// </summary>
        /// <param name="loggerSinkConfiguration">Options for the sink.</param>
        /// <param name="logstashUri">URI for Logstash.</param>
        /// <returns>LoggerConfiguration object</returns>
        /// <exception cref="ArgumentNullException"><paramref name="logstashUri" /> is <see langword="null" />.</exception>
        public static LoggerConfiguration LogstashHttp(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string logstashUri)
        {
            if (string.IsNullOrEmpty(logstashUri))
                throw new ArgumentNullException(nameof(logstashUri), "No Logstash uri specified.");

            var options = new LogstashHttpSinkOptions
            {
                LogstashUri = logstashUri
            };

            return LogstashHttp(loggerSinkConfiguration, options);
        }
    }
}