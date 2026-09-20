using RPMailCore.Models;

namespace RPMailUI.Models;

public sealed record PersistedConfig
{
	public required string WorkspaceDirectory { get; init; }
	public required MailConfig Mail { get; init; }

	public static PersistedConfig CreateTemplate() => new()
	{
		WorkspaceDirectory = Directory.GetCurrentDirectory(),
		Mail = MailConfig.CreateTemplate(),
	};
}
