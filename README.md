# dotnet-logstash-http
.NET Logger to ELK through Logstash http plugin (https://www.elastic.co/guide/en/logstash/current/plugins-inputs-http.html)
```C#
public Startup(IHostingEnvironment env)
{
    ...

    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ApplicationName", "Serilog.Sinks.LogstashHttp.ExampleApp")
        .WriteTo.LogstashHttp("https://elk-host:8443")
        //.MinimumLevel.Warning()
        .CreateLogger();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
{
    ...

    loggerFactory.AddSerilog();

    ...
}
```