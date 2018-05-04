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
using Serilog.Formatting;

namespace Serilog.Sinks.LogstashHttp
{
    internal class LogstashHttpSinkState
    {
        private LogstashHttpSinkState(LogstashHttpSinkOptions options)
        {
            Options = options;

            Formatter = options.CustomFormatter ?? new LogstashHttpJsonFormatter(
                            formatProvider: options.FormatProvider,
                            renderMessage: true,
                            closingDelimiter: string.Empty,
                            inlineFields: options.InlineFields,
                            indexName: options.IndexName
                        );
            DurableFormatter = options.CustomDurableFormatter ?? new LogstashHttpJsonFormatter(
                                   formatProvider: options.FormatProvider,
                                   renderMessage: true,
                                   closingDelimiter: Environment.NewLine,
                                   inlineFields: options.InlineFields,
                                   indexName: options.IndexName
                               );
        }

        public LogstashHttpSinkOptions Options { get; }
        public ITextFormatter Formatter { get; }
        public ITextFormatter DurableFormatter { get; }

        public static LogstashHttpSinkState Create(LogstashHttpSinkOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return new LogstashHttpSinkState(options);
        }
    }
}