using RPMailCore.Models;
using RPMailUI.Resources;

namespace RPMailUI.Models;

public static class MailTaskStatusUiExtensions
{
    extension(MailTaskStatus status)
    {
        public string Text => status switch
        {
            MailTaskStatus.Ready => Strings.TaskStatusReady,
            MailTaskStatus.Pending => Strings.TaskStatusPending,
            MailTaskStatus.Running => Strings.TaskStatusRunning,
            MailTaskStatus.Success => Strings.TaskStatusSuccess,
            MailTaskStatus.Failed => Strings.TaskStatusFailed,
            _ => status.ToString(),
        };
    }
}
