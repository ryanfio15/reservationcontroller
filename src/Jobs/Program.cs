// Entry point for the optional Container Apps scheduled job (B: Cross-cutting concerns,
// "Scheduled work"). It is the same image as the web host, started with a different command,
// and it is not deployed in the prototype.
//
// There are no in-process timers anywhere in this solution: this process runs its actions once
// and exits, so reruns and overlapping runs are harmless. The actions themselves — writing
// NoShow events, topping up slots, purging expired host sessions — arrive in phase 8.

var builder = Host.CreateApplicationBuilder(args);

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Jobs");
logger.LogInformation("Jobs host started with no actions registered; exiting.");

return 0;
