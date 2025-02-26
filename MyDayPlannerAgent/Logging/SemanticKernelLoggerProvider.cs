// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;

public class SemanticKernelLoggerProvider : ILoggerProvider, IDisposable
{
    ILogger<AgenticExperiences.MyDayPlannerAgent.MyDayPlannerAgent> _logger;

    public SemanticKernelLoggerProvider(ILogger<AgenticExperiences.MyDayPlannerAgent.MyDayPlannerAgent> logger)
    {
        _logger = logger;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SemanticKernelLogger(_logger);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose managed resources here.
        }

        // Dispose unmanaged resources here.
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }
}