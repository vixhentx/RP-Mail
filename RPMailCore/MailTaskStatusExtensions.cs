using System.Reflection;

namespace RPMailCore.Models;

public static class MailTaskStatusExtensions
{
    extension(MailTaskStatus status)
    {
        public TaskStatusMetaAttribute Meta =>
            typeof(MailTaskStatus).GetField(status.ToString())?.GetCustomAttribute<TaskStatusMetaAttribute>()
            ?? throw new InvalidOperationException($"Missing {nameof(TaskStatusMetaAttribute)} on {nameof(MailTaskStatus)}.{status}");

        public int Ordinal => status.Meta.Ordinal;

        public string Key => $"TaskStatus.{status}";
    }
}
