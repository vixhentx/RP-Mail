using RPMailCore.Models;

namespace RPMailUI.Models;

/// <summary>
/// 标记配置变更来源, 用于断回环
/// </summary>
public enum ConfChangingSource
{
	/// <summary>
	/// 用户在UI中的直接编辑
	/// </summary>
	UserEdit,
	/// <summary>
	/// 配置导入, 包括模块导入, 全局导入, 持久化的首次加载等等
	/// </summary>
	Import
}

public sealed record ConfPipe(MailConfig Value, ConfChangingSource Source)
	: IEquatable<ConfPipe>, IEquatable<MailConfig>
{
	public bool Equals(ConfPipe? other)
		=> other is not null && Value.Equals(other.Value);
	public bool Equals(MailConfig? other)
		=> other is not null && Value.Equals(other);
	public override int GetHashCode() => Value.GetHashCode();
}