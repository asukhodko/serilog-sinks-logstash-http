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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.LogstashHttp
{
    /// <summary>
    ///     Custom Json formatter that respects the configured property name handling and forces 'Timestamp' to @timestamp
    /// </summary>
    public class LogstashHttpJsonFormatter : DefaultJsonFormatter
    {
        private readonly bool _inlineFields;

        /// <param name="omitEnclosingObject">
        ///     If true, the properties of the event will be written to
        ///     the output without enclosing braces. Otherwise, if false, each event will be written as a well-formed
        ///     JSON object.
        /// </param>
        /// <param name="closingDelimiter">
        ///     A string that will be written after each log event is formatted.
        ///     If null, <see cref="Environment.NewLine" /> will be used. Ignored if <paramref name="omitEnclosingObject" />
        ///     is true.
        /// </param>
        /// <param name="renderMessage">
        ///     If true, the message will be rendered and written to the output as a
        ///     property named RenderedMessage.
        /// </param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="inlineFields">When set to true values will be written at the root of the json document</param>
        public LogstashHttpJsonFormatter(bool omitEnclosingObject = false,
            string closingDelimiter = null,
            bool renderMessage = false,
            IFormatProvider formatProvider = null,
            bool inlineFields = false)
            : base(omitEnclosingObject, closingDelimiter, renderMessage, formatProvider)
        {
            _inlineFields = inlineFields;
        }

        /// <summary>
        ///     Writes out individual renderings of attached properties
        /// </summary>
        protected override void WriteRenderings(IGrouping<string, PropertyToken>[] tokensWithFormat,
            IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
        {
            output.Write(",\"{0}\":{{", "renderings");
            WriteRenderingsValues(tokensWithFormat, properties, output);
            output.Write("}");
        }

        /// <summary>
        ///     Writes out the attached properties
        /// </summary>
        protected override void WriteProperties(IReadOnlyDictionary<string, LogEventPropertyValue> properties,
            TextWriter output)
        {
            if (!_inlineFields)
                output.Write(",\"{0}\":{{", "fields");
            else
                output.Write(",");

            WritePropertiesValues(properties, output);

            if (!_inlineFields)
                output.Write("}");
        }

        /// <summary>
        ///     Escape the name of the Property before calling ElasticSearch
        /// </summary>
        protected override void WriteDictionary(IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> elements,
            TextWriter output)
        {
            var escaped = elements.ToDictionary(e => DotEscapeFieldName(e.Key), e => e.Value);

            base.WriteDictionary(escaped, output);
        }

        /// <summary>
        ///     Escape the name of the Property before calling ElasticSearch
        /// </summary>
        protected override void WriteJsonProperty(string name, object value, ref string precedingDelimiter,
            TextWriter output)
        {
            name = DotEscapeFieldName(name);

            base.WriteJsonProperty(name, value, ref precedingDelimiter, output);
        }

        /// <summary>
        ///     Escapes Dots in Strings and does nothing to objects
        /// </summary>
        protected virtual ScalarValue DotEscapeFieldName(ScalarValue value)
        {
            var s = value.Value as string;
            return s != null ? new ScalarValue(DotEscapeFieldName(s)) : value;
        }

        /// <summary>
        ///     Dots are not allowed in Field Names, replaces '.' with '/'
        ///     https://github.com/elastic/elasticsearch/issues/14594
        /// </summary>
        protected virtual string DotEscapeFieldName(string value)
        {
            return value?.Replace('.', '/');
        }

        /// <summary>
        ///     Writes out the attached exception
        /// </summary>
        protected override void WriteException(Exception exception, ref string delim, TextWriter output)
        {
            output.Write(delim);
            output.Write("\"");
            output.Write("exception");
            output.Write("\":{");
            WriteExceptionTree(exception, ref delim, output, 0);
            output.Write("}");
        }

        private void WriteExceptionTree(Exception exception, ref string delim, TextWriter output, int depth)
        {
            delim = "";
            WriteSingleException(exception, ref delim, output, depth);
            exception = exception.InnerException;
            if (exception != null)
            {
                output.Write(",");
                output.Write("\"innerException\":{");
                WriteExceptionTree(exception, ref delim, output, depth + 1);
                output.Write("}");
            }
        }

        /// <summary>
        ///     Writes the properties of a single exception, without inner exceptions
        ///     Callers are expected to open and close the json object themselves.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="delim"></param>
        /// <param name="output"></param>
        /// <param name="depth"></param>
        protected void WriteSingleException(Exception exception, ref string delim, TextWriter output, int depth)
        {
            var helpUrl = exception.HelpLink;
            var stackTrace = exception.StackTrace;
            var remoteStackTrace = string.Empty;
            var remoteStackIndex = -1;
            var exceptionMethod = string.Empty;
            var hresult = exception.HResult;
            var source = exception.Source;
            var className = string.Empty;

            WriteJsonProperty("Depth", depth, ref delim, output);
            WriteJsonProperty("ClassName", className, ref delim, output);
            WriteJsonProperty("Message", exception.Message, ref delim, output);
            WriteJsonProperty("Source", source, ref delim, output);
            WriteJsonProperty("StackTraceString", stackTrace, ref delim, output);
            WriteJsonProperty("RemoteStackTraceString", remoteStackTrace, ref delim, output);
            WriteJsonProperty("RemoteStackIndex", remoteStackIndex, ref delim, output);
            WriteStructuredExceptionMethod(exceptionMethod, ref delim, output);
            WriteJsonProperty("HResult", hresult, ref delim, output);
            WriteJsonProperty("HelpURL", helpUrl, ref delim, output);

            //writing byte[] will fall back to serializer and they differ in output 
            //JsonNET assumes string, simplejson writes array of numerics.
            //Skip for now
            //this.WriteJsonProperty("WatsonBuckets", watsonBuckets, ref delim, output);
        }

        private void WriteStructuredExceptionMethod(string exceptionMethodString, ref string delim, TextWriter output)
        {
            if (string.IsNullOrWhiteSpace(exceptionMethodString)) return;

            var args = exceptionMethodString.Split('\0', '\n');

            if (args.Length != 5) return;

            var memberType = int.Parse(args[0], CultureInfo.InvariantCulture);
            var name = args[1];
            var assemblyName = args[2];
            var className = args[3];
            var signature = args[4];
            var an = new AssemblyName(assemblyName);
            output.Write(delim);
            output.Write("\"");
            output.Write("ExceptionMethod");
            output.Write("\":{");
            delim = "";
            WriteJsonProperty("Name", name, ref delim, output);
            WriteJsonProperty("AssemblyName", an.Name, ref delim, output);
            WriteJsonProperty("AssemblyVersion", an.Version.ToString(), ref delim, output);
            WriteJsonProperty("AssemblyCulture", an.CultureName, ref delim, output);
            WriteJsonProperty("ClassName", className, ref delim, output);
            WriteJsonProperty("Signature", signature, ref delim, output);
            WriteJsonProperty("MemberType", memberType, ref delim, output);

            output.Write("}");
            delim = ",";
        }


        /// <summary>
        ///     (Optionally) writes out the rendered message
        /// </summary>
        protected override void WriteRenderedMessage(string message, ref string delim, TextWriter output)
        {
            WriteJsonProperty("message", message, ref delim, output);
        }

        /// <summary>
        ///     Writes out the message template for the logevent.
        /// </summary>
        protected override void WriteMessageTemplate(string template, ref string delim, TextWriter output)
        {
            WriteJsonProperty("messageTemplate", template, ref delim, output);
        }

        /// <summary>
        ///     Writes out the log level
        /// </summary>
        protected override void WriteLevel(LogEventLevel level, ref string delim, TextWriter output)
        {
            var stringLevel = Enum.GetName(typeof(LogEventLevel), level);
            WriteJsonProperty("level", stringLevel, ref delim, output);
        }

        /// <summary>
        ///     Writes out the log timestamp
        /// </summary>
        protected override void WriteTimestamp(DateTimeOffset timestamp, ref string delim, TextWriter output)
        {
            WriteJsonProperty("@timestamp", timestamp, ref delim, output);
        }
    }
}