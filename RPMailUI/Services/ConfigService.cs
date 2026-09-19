using R3;
using RPMailCore.Models;
using RPMailUI.Models;

namespace RPMailUI.Services;

/// <summary>
/// 顶层配置管理模块. 作为配置的唯一事实汇聚点, 并作为所有与配置相关服务的底层依赖.
/// </summary>
public class ConfigService : IDisposable
{
	public ReactiveProperty<MailConfig> Root { get; } = new(MailConfig.CreateTemplate());
	public void Dispose()
	{
		Root.Dispose();
	}
}