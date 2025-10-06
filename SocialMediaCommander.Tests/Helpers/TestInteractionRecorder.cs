using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SocialMediaCommander.Tests.Helpers;

/// <summary>
/// Records test interactions for debugging and documentation purposes.
/// Provides a textual log of all actions performed during a test.
/// </summary>
public class TestInteractionRecorder
{
    private readonly List<string> _interactions = new();
    private readonly DateTime _startTime;
    private const string RecordingsDir = "test-recordings";

    public TestInteractionRecorder()
    {
        _startTime = DateTime.UtcNow;
        Directory.CreateDirectory(RecordingsDir);
    }

    /// <summary>
    /// Records an interaction or action during the test.
    /// </summary>
    /// <param name="action">Description of the action performed</param>
    /// <param name="details">Optional additional details</param>
    public void Record(string action, string? details = null)
    {
        var elapsed = DateTime.UtcNow - _startTime;
        var timestamp = $"{elapsed.TotalSeconds:F3}s";

        var entry = details != null
            ? $"[{timestamp}] {action}: {details}"
            : $"[{timestamp}] {action}";

        _interactions.Add(entry);
    }

    /// <summary>
    /// Records an action with formatted arguments.
    /// </summary>
    /// <param name="action">Action format string</param>
    /// <param name="args">Format arguments</param>
    public void RecordFormat(string action, params object[] args)
    {
        Record(string.Format(action, args));
    }

    /// <summary>
    /// Records a command invocation.
    /// </summary>
    /// <param name="commandName">Name of the command</param>
    /// <param name="parameter">Optional command parameter</param>
    public void RecordCommand(string commandName, object? parameter = null)
    {
        var details = parameter != null ? $"with parameter: {parameter}" : "no parameter";
        Record($"Command invoked: {commandName}", details);
    }

    /// <summary>
    /// Records a property change.
    /// </summary>
    /// <param name="propertyName">Name of the property</param>
    /// <param name="oldValue">Previous value</param>
    /// <param name="newValue">New value</param>
    public void RecordPropertyChange(string propertyName, object? oldValue, object? newValue)
    {
        Record($"Property changed: {propertyName}", $"{oldValue} → {newValue}");
    }

    /// <summary>
    /// Records an assertion being verified.
    /// </summary>
    /// <param name="assertion">Description of what is being asserted</param>
    /// <param name="result">Whether the assertion passed</param>
    public void RecordAssertion(string assertion, bool result)
    {
        var status = result ? "✓ PASS" : "✗ FAIL";
        Record($"{status}: {assertion}");
    }

    /// <summary>
    /// Records an exception that occurred.
    /// </summary>
    /// <param name="exception">The exception</param>
    /// <param name="context">Optional context about when it occurred</param>
    public void RecordException(Exception exception, string? context = null)
    {
        var prefix = context != null ? $"Exception in {context}" : "Exception";
        Record($"{prefix}: {exception.GetType().Name}", exception.Message);
    }

    /// <summary>
    /// Records navigation or view change.
    /// </summary>
    /// <param name="from">Source view/location</param>
    /// <param name="to">Destination view/location</param>
    public void RecordNavigation(string from, string to)
    {
        Record($"Navigation: {from} → {to}");
    }

    /// <summary>
    /// Saves the recording to a file.
    /// </summary>
    /// <param name="testName">Name of the test (used for file naming)</param>
    /// <returns>Path to the saved recording file</returns>
    public string SaveRecording(string testName)
    {
        var fileName = SanitizeFileName(testName);
        var filePath = Path.Combine(RecordingsDir, $"{fileName}.log");

        var content = new StringBuilder();
        content.AppendLine($"Test Recording: {testName}");
        content.AppendLine($"Started: {_startTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
        content.AppendLine($"Duration: {(DateTime.UtcNow - _startTime).TotalSeconds:F3}s");
        content.AppendLine(new string('=', 80));
        content.AppendLine();

        foreach (var interaction in _interactions)
        {
            content.AppendLine(interaction);
        }

        content.AppendLine();
        content.AppendLine(new string('=', 80));
        content.AppendLine($"Total Interactions: {_interactions.Count}");

        File.WriteAllText(filePath, content.ToString());

        return filePath;
    }

    /// <summary>
    /// Gets all recorded interactions as a list.
    /// </summary>
    /// <returns>List of interaction entries</returns>
    public IReadOnlyList<string> GetInteractions() => _interactions.AsReadOnly();

    /// <summary>
    /// Gets the recording as a formatted string.
    /// </summary>
    /// <returns>Formatted recording string</returns>
    public string GetRecordingText()
    {
        var sb = new StringBuilder();
        foreach (var interaction in _interactions)
        {
            sb.AppendLine(interaction);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Clears all recorded interactions (useful for multi-phase tests).
    /// </summary>
    public void Clear()
    {
        _interactions.Clear();
    }

    /// <summary>
    /// Adds a section separator for organizing long recordings.
    /// </summary>
    /// <param name="sectionName">Name of the section</param>
    public void AddSection(string sectionName)
    {
        Record($"--- {sectionName} ---");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = fileName;

        foreach (var c in invalid)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        return sanitized;
    }

    /// <summary>
    /// Cleans up old recording files (useful for cleanup between test runs).
    /// </summary>
    public static void CleanupRecordings()
    {
        if (Directory.Exists(RecordingsDir))
        {
            foreach (var file in Directory.GetFiles(RecordingsDir, "*.log"))
            {
                File.Delete(file);
            }
        }
    }
}

/// <summary>
/// Base class for tests that want to use interaction recording.
/// </summary>
public abstract class RecordedTestBase
{
    protected TestInteractionRecorder Recorder { get; } = new();

    /// <summary>
    /// Records an interaction.
    /// </summary>
    protected void Record(string action, string? details = null)
        => Recorder.Record(action, details);

    /// <summary>
    /// Records a command invocation.
    /// </summary>
    protected void RecordCommand(string commandName, object? parameter = null)
        => Recorder.RecordCommand(commandName, parameter);

    /// <summary>
    /// Records a property change.
    /// </summary>
    protected void RecordPropertyChange(string propertyName, object? oldValue, object? newValue)
        => Recorder.RecordPropertyChange(propertyName, oldValue, newValue);

    /// <summary>
    /// Records an assertion.
    /// </summary>
    protected void RecordAssertion(string assertion, bool result)
        => Recorder.RecordAssertion(assertion, result);

    /// <summary>
    /// Records navigation.
    /// </summary>
    protected void RecordNavigation(string from, string to)
        => Recorder.RecordNavigation(from, to);

    /// <summary>
    /// Saves the recording with the test name.
    /// </summary>
    protected string SaveRecording(string testName)
        => Recorder.SaveRecording(testName);

    /// <summary>
    /// Adds a section separator.
    /// </summary>
    protected void AddSection(string sectionName)
        => Recorder.AddSection(sectionName);
}
