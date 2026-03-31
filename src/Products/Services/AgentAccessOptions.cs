namespace Products.Services;

public class AgentAccessOptions
{
    public const string SectionName = "AgentAccess";

    public int RequestsPerMinute { get; set; } = 60;

    public List<AgentCredential> Agents { get; set; } = [];
}

public class AgentCredential
{
    public string AgentId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
