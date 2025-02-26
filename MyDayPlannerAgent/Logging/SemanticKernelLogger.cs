// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;

public class SemanticKernelLogger : ILogger
{
    ILogger<AgenticExperiences.MyDayPlannerAgent.MyDayPlannerAgent> _logger;

    public SemanticKernelLogger(ILogger<AgenticExperiences.MyDayPlannerAgent.MyDayPlannerAgent> logger)
    {
        _logger = logger;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!this.IsEnabled(logLevel))
        {
            return;
        }

        // You can reformat the message here
        var message = formatter(state, exception);
         _logger.LogInformation(message);
    }
}