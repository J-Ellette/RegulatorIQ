namespace RegulatorIQ.Services;

public static class MonitoringJobControl
{
    public const string PausedJobsSetKey = "regulatoriq:paused-monitoring-jobs";

    public static readonly HashSet<string> SupportedJobIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "federal-monitoring",
        "state-monitoring",
        "document-processing",
        "compliance-alerts",
        "framework-updates"
    };
}
