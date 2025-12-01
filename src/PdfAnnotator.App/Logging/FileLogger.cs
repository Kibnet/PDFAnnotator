using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PdfAnnotator.App.Logging;

public class FileLogger : ILogger
{
    private readonly string _filePath;
    private static readonly ConcurrentDictionary<string, object> Locks = new();
    private readonly string _categoryName;

    public FileLogger(string filePath, string categoryName)
    {
        _filePath = filePath;
        _categoryName = categoryName;
        Locks.TryAdd(filePath, new object());
    }

    public IDisposable? BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = $"{DateTimeOffset.Now:u} [{logLevel}] {_categoryName}: {formatter(state, exception)}";
        if (exception != null)
        {
            message += Environment.NewLine + exception;
        }

        var padlock = Locks[_filePath];
        lock (padlock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.AppendAllText(_filePath, message + Environment.NewLine);
        }
    }
}
