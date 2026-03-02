using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.Storage;
using RegulatorIQ.Data;
using RegulatorIQ.DTOs;
using RegulatorIQ.Models;
using RegulatorIQ.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace RegulatorIQ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoringController : ControllerBase
    {
        private readonly RegulatorIQContext _context;
        private readonly ILogger<MonitoringController> _logger;

        public MonitoringController(RegulatorIQContext context, ILogger<MonitoringController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("trigger")]
        public async Task<ActionResult<TriggerMonitoringRunResponse>> TriggerMonitoringRun(
            [FromBody] TriggerMonitoringRunRequest request)
        {
            try
            {
                var runType = (request.RunType ?? "all").Trim().ToLowerInvariant();
                var triggeredJobs = new List<string>();
                var actor = ResolveActor(request.ActedBy);
                var actionAt = DateTime.UtcNow;

                if (runType is "all" or "federal")
                {
                    if (!IsJobPaused("federal-monitoring"))
                    {
                        RecurringJob.TriggerJob("federal-monitoring");
                        triggeredJobs.Add("federal-monitoring");
                    }
                }

                if (runType is "all" or "state")
                {
                    if (!IsJobPaused("state-monitoring"))
                    {
                        RecurringJob.TriggerJob("state-monitoring");
                        triggeredJobs.Add("state-monitoring");
                    }
                }

                if (triggeredJobs.Count == 0)
                {
                    return BadRequest("Invalid runType or selected jobs are paused. Expected one of: all, federal, state.");
                }

                foreach (var jobId in triggeredJobs)
                {
                    AddMonitoringAuditEntry(
                        action: "monitoring_job_triggered",
                        jobId: jobId,
                        changedBy: actor,
                        oldData: new { isPaused = IsJobPaused(jobId) },
                        newData: new { isPaused = IsJobPaused(jobId), triggeredAtUtc = actionAt, runType },
                        changeReason: string.IsNullOrWhiteSpace(request.Reason) ? "Monitoring job triggered" : request.Reason!.Trim(),
                        createdAtUtc: actionAt);
                }

                await _context.SaveChangesAsync();

                var response = new TriggerMonitoringRunResponse
                {
                    RequestedRunType = runType,
                    TriggeredJobs = triggeredJobs,
                    TriggeredAtUtc = actionAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering monitoring run for {RunType}", request.RunType);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("runs")]
        public async Task<ActionResult<List<MonitoringRunDto>>> GetRuns(
            [FromQuery] string? runType,
            [FromQuery] string? status,
            [FromQuery] int take = 50)
        {
            try
            {
                var cappedTake = Math.Clamp(take, 1, 200);
                var query = _context.MonitoringRuns.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(runType))
                {
                    query = query.Where(r => r.RunType == runType);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(r => r.Status == status);
                }

                var runs = await query
                    .OrderByDescending(r => r.StartedAt)
                    .Take(cappedTake)
                    .ToListAsync();

                return Ok(runs.Select(MapRunToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monitoring runs");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("summary")]
        public async Task<ActionResult<MonitoringSummaryDto>> GetSummary([FromQuery] int hours = 24)
        {
            try
            {
                var boundedHours = Math.Clamp(hours, 1, 24 * 30);
                var windowEnd = DateTime.UtcNow;
                var windowStart = windowEnd.AddHours(-boundedHours);

                var runs = await _context.MonitoringRuns
                    .AsNoTracking()
                    .Where(r => r.StartedAt >= windowStart)
                    .ToListAsync();

                var completedRuns = runs.Where(r => r.CompletedAt.HasValue).ToList();
                var durations = completedRuns
                    .Select(r => (r.CompletedAt!.Value - r.StartedAt).TotalMilliseconds)
                    .ToList();

                var bySource = new Dictionary<string, SourceSummaryDto>(StringComparer.OrdinalIgnoreCase);
                foreach (var run in runs)
                {
                    var sourceMetrics = ParseSourceMetrics(run.SourceMetrics);
                    foreach (var (source, metric) in sourceMetrics)
                    {
                        if (!bySource.TryGetValue(source, out var current))
                        {
                            current = new SourceSummaryDto();
                            bySource[source] = current;
                        }

                        current.Fetched += metric.Fetched;
                        current.Added += metric.Added;
                        current.Skipped += metric.Skipped;
                        current.Failures += metric.Failures;
                    }
                }

                var summary = new MonitoringSummaryDto
                {
                    WindowStartUtc = windowStart,
                    WindowEndUtc = windowEnd,
                    TotalRuns = runs.Count,
                    SuccessfulRuns = runs.Count(r => r.Status == "completed"),
                    FailedRuns = runs.Count(r => r.Status == "failed"),
                    TotalDocumentsFetched = runs.Sum(r => r.DocumentsFetched),
                    TotalDocumentsAdded = runs.Sum(r => r.DocumentsAdded),
                    TotalFailures = runs.Sum(r => r.FailureCount),
                    AverageDurationMs = durations.Count > 0 ? durations.Average() : null,
                    BySource = bySource
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monitoring summary");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("jobs")]
        public ActionResult<List<MonitoringJobStatusDto>> GetMonitoringJobs()
        {
            try
            {
                var jobs = JobStorage.Current
                    .GetConnection()
                    .GetRecurringJobs()
                    .Where(job => MonitoringJobControl.SupportedJobIds.Contains(job.Id))
                    .Select(job => new MonitoringJobStatusDto
                    {
                        Id = job.Id,
                        Cron = job.Cron,
                        LastJobState = job.LastJobState,
                        LastExecution = job.LastExecution,
                        NextExecution = job.NextExecution,
                        Error = job.Error,
                        IsPaused = IsJobPaused(job.Id)
                    })
                    .OrderBy(job => job.Id)
                    .ToList();

                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monitoring jobs status");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("jobs/audit")]
        public async Task<ActionResult<PagedResult<MonitoringJobAuditEntryDto>>> GetMonitoringJobAudit(
            [FromQuery] string? jobId,
            [FromQuery] string? action,
            [FromQuery] string? actor,
            [FromQuery] string? reason,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortDir,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100)
        {
            try
            {
                var cappedSkip = Math.Max(0, skip);
                var cappedTake = Math.Clamp(take, 1, 500);
                var query = _context.RegulatoryAuditLogs
                    .AsNoTracking()
                    .Where(a =>
                        a.EntityType == "monitoring_job" &&
                        (a.Action == "monitoring_job_triggered" ||
                         a.Action == "monitoring_job_paused" ||
                         a.Action == "monitoring_job_resumed"));

                if (fromUtc.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= fromUtc.Value);
                }

                if (toUtc.HasValue)
                {
                    query = query.Where(a => a.CreatedAt <= toUtc.Value);
                }

                if (!string.IsNullOrWhiteSpace(jobId))
                {
                    var normalizedJobId = jobId.Trim().ToLowerInvariant();
                    query = query.Where(a =>
                        (a.NewData ?? string.Empty).Contains($"\"jobId\":\"{normalizedJobId}\"") ||
                        (a.OldData ?? string.Empty).Contains($"\"jobId\":\"{normalizedJobId}\""));
                }

                if (!string.IsNullOrWhiteSpace(action))
                {
                    var normalizedAction = action.Trim().ToLowerInvariant();
                    if (normalizedAction is "monitoring_job_triggered" or "monitoring_job_paused" or "monitoring_job_resumed")
                    {
                        query = query.Where(a => a.Action == normalizedAction);
                    }
                }

                if (!string.IsNullOrWhiteSpace(actor))
                {
                    var actorSearch = actor.Trim();
                    query = query.Where(a => a.ChangedBy != null && EF.Functions.ILike(a.ChangedBy, $"%{actorSearch}%"));
                }

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    var reasonSearch = reason.Trim();
                    query = query.Where(a => a.ChangeReason != null && EF.Functions.ILike(a.ChangeReason, $"%{reasonSearch}%"));
                }

                var normalizedSortBy = (sortBy ?? "createdAt").Trim().ToLowerInvariant();
                var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

                query = normalizedSortBy switch
                {
                    "action" => descending ? query.OrderByDescending(a => a.Action) : query.OrderBy(a => a.Action),
                    "actor" => descending ? query.OrderByDescending(a => a.ChangedBy) : query.OrderBy(a => a.ChangedBy),
                    "job" => descending ? query.OrderByDescending(a => a.NewData) : query.OrderBy(a => a.NewData),
                    _ => descending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt)
                };

                var totalCount = await query.CountAsync();

                var entries = await query
                    .Skip(cappedSkip)
                    .Take(cappedTake)
                    .ToListAsync();

                var result = entries.Select(entry => new MonitoringJobAuditEntryDto
                {
                    Id = entry.Id,
                    Action = entry.Action,
                    JobId = ExtractJobId(entry.NewData) ?? ExtractJobId(entry.OldData),
                    ChangedBy = entry.ChangedBy,
                    ChangeReason = entry.ChangeReason,
                    CreatedAt = entry.CreatedAt,
                    OldData = ParseAuditJson(entry.OldData),
                    NewData = ParseAuditJson(entry.NewData)
                }).ToList();

                return Ok(new PagedResult<MonitoringJobAuditEntryDto>
                {
                    Items = result,
                    TotalCount = totalCount,
                    Page = (cappedSkip / cappedTake) + 1,
                    PageSize = cappedTake,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)cappedTake)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monitoring job audit history");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("jobs/audit/summary")]
        public async Task<ActionResult<MonitoringJobAuditSummaryDto>> GetMonitoringJobAuditSummary(
            [FromQuery] string? jobId,
            [FromQuery] string? actor,
            [FromQuery] string? reason,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc)
        {
            try
            {
                var query = _context.RegulatoryAuditLogs
                    .AsNoTracking()
                    .Where(a =>
                        a.EntityType == "monitoring_job" &&
                        (a.Action == "monitoring_job_triggered" ||
                         a.Action == "monitoring_job_paused" ||
                         a.Action == "monitoring_job_resumed"));

                if (fromUtc.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= fromUtc.Value);
                }

                if (toUtc.HasValue)
                {
                    query = query.Where(a => a.CreatedAt <= toUtc.Value);
                }

                if (!string.IsNullOrWhiteSpace(jobId))
                {
                    var normalizedJobId = jobId.Trim().ToLowerInvariant();
                    query = query.Where(a =>
                        (a.NewData ?? string.Empty).Contains($"\"jobId\":\"{normalizedJobId}\"") ||
                        (a.OldData ?? string.Empty).Contains($"\"jobId\":\"{normalizedJobId}\""));
                }

                if (!string.IsNullOrWhiteSpace(actor))
                {
                    var actorSearch = actor.Trim();
                    query = query.Where(a => a.ChangedBy != null && EF.Functions.ILike(a.ChangedBy, $"%{actorSearch}%"));
                }

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    var reasonSearch = reason.Trim();
                    query = query.Where(a => a.ChangeReason != null && EF.Functions.ILike(a.ChangeReason, $"%{reasonSearch}%"));
                }

                var summaryRows = await query
                    .Select(a => new
                    {
                        a.Action,
                        a.ChangedBy,
                        a.NewData,
                        a.OldData
                    })
                    .ToListAsync();

                var topActors = summaryRows
                    .GroupBy(row => string.IsNullOrWhiteSpace(row.ChangedBy) ? "system" : row.ChangedBy!.Trim())
                    .Select(group => new MonitoringJobAuditBreakdownItemDto
                    {
                        Key = group.Key,
                        Count = group.Count()
                    })
                    .OrderByDescending(item => item.Count)
                    .ThenBy(item => item.Key)
                    .Take(5)
                    .ToList();

                var topJobs = summaryRows
                    .Select(row => ExtractJobId(row.NewData) ?? ExtractJobId(row.OldData) ?? "unknown")
                    .GroupBy(job => job)
                    .Select(group => new MonitoringJobAuditBreakdownItemDto
                    {
                        Key = group.Key,
                        Count = group.Count()
                    })
                    .OrderByDescending(item => item.Count)
                    .ThenBy(item => item.Key)
                    .Take(5)
                    .ToList();

                var summary = new MonitoringJobAuditSummaryDto
                {
                    Total = summaryRows.Count,
                    Triggered = summaryRows.Count(a => a.Action == "monitoring_job_triggered"),
                    Paused = summaryRows.Count(a => a.Action == "monitoring_job_paused"),
                    Resumed = summaryRows.Count(a => a.Action == "monitoring_job_resumed"),
                    TopActors = topActors,
                    TopJobs = topJobs
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monitoring job audit summary");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("jobs/audit/export")]
        public async Task<IActionResult> ExportMonitoringJobAudit(
            [FromQuery] string? jobId,
            [FromQuery] string? action,
            [FromQuery] string? actor,
            [FromQuery] string? reason,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] int take = 5000)
        {
            try
            {
                var cappedTake = Math.Clamp(take, 1, 10000);
                var query = _context.RegulatoryAuditLogs
                    .AsNoTracking()
                    .Where(a =>
                        a.EntityType == "monitoring_job" &&
                        (a.Action == "monitoring_job_triggered" ||
                         a.Action == "monitoring_job_paused" ||
                         a.Action == "monitoring_job_resumed"));

                if (fromUtc.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= fromUtc.Value);
                }

                if (toUtc.HasValue)
                {
                    query = query.Where(a => a.CreatedAt <= toUtc.Value);
                }

                if (!string.IsNullOrWhiteSpace(jobId))
                {
                    var normalizedJobId = jobId.Trim().ToLowerInvariant();
                    query = query.Where(a =>
                        (a.NewData ?? string.Empty).Contains($"\"jobId\":\"{normalizedJobId}\"") ||
                        (a.OldData ?? string.Empty).Contains($"\"jobId\":\"{normalizedJobId}\""));
                }

                if (!string.IsNullOrWhiteSpace(action))
                {
                    var normalizedAction = action.Trim().ToLowerInvariant();
                    if (normalizedAction is "monitoring_job_triggered" or "monitoring_job_paused" or "monitoring_job_resumed")
                    {
                        query = query.Where(a => a.Action == normalizedAction);
                    }
                }

                if (!string.IsNullOrWhiteSpace(actor))
                {
                    var actorSearch = actor.Trim();
                    query = query.Where(a => a.ChangedBy != null && EF.Functions.ILike(a.ChangedBy, $"%{actorSearch}%"));
                }

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    var reasonSearch = reason.Trim();
                    query = query.Where(a => a.ChangeReason != null && EF.Functions.ILike(a.ChangeReason, $"%{reasonSearch}%"));
                }

                var entries = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(cappedTake)
                    .ToListAsync();

                var csv = new StringBuilder();
                csv.AppendLine("createdAtUtc,jobId,action,changedBy,changeReason");

                foreach (var entry in entries)
                {
                    var csvRow = string.Join(",",
                        EscapeCsv(entry.CreatedAt.ToString("O")),
                        EscapeCsv(ExtractJobId(entry.NewData) ?? ExtractJobId(entry.OldData) ?? string.Empty),
                        EscapeCsv(entry.Action),
                        EscapeCsv(entry.ChangedBy ?? string.Empty),
                        EscapeCsv(entry.ChangeReason ?? string.Empty));
                    csv.AppendLine(csvRow);
                }

                var fileName = $"monitoring-job-audit-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting monitoring job audit history");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("jobs/{jobId}/trigger")]
        public async Task<ActionResult<TriggerMonitoringJobResponse>> TriggerMonitoringJob(
            [FromRoute] string jobId,
            [FromBody] MonitoringJobActionRequest? request)
        {
            try
            {
                var normalizedJobId = (jobId ?? string.Empty).Trim().ToLowerInvariant();
                var actor = ResolveActor(request?.ActedBy);
                var actionAt = DateTime.UtcNow;

                if (!MonitoringJobControl.SupportedJobIds.Contains(normalizedJobId))
                {
                    return BadRequest($"Unsupported jobId '{jobId}'.");
                }

                if (IsJobPaused(normalizedJobId))
                {
                    return Conflict($"Job '{normalizedJobId}' is paused. Resume it before triggering.");
                }

                RecurringJob.TriggerJob(normalizedJobId);

                AddMonitoringAuditEntry(
                    action: "monitoring_job_triggered",
                    jobId: normalizedJobId,
                    changedBy: actor,
                    oldData: new { isPaused = false },
                    newData: new { isPaused = false, triggeredAtUtc = actionAt },
                    changeReason: string.IsNullOrWhiteSpace(request?.Reason) ? "Monitoring job triggered" : request!.Reason!.Trim(),
                    createdAtUtc: actionAt);

                await _context.SaveChangesAsync();

                return Ok(new TriggerMonitoringJobResponse
                {
                    JobId = normalizedJobId,
                    TriggeredAtUtc = actionAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering monitoring job {JobId}", jobId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("jobs/{jobId}/pause")]
        public async Task<ActionResult<MonitoringJobControlResponse>> PauseMonitoringJob(
            [FromRoute] string jobId,
            [FromBody] MonitoringJobActionRequest? request)
        {
            try
            {
                var normalizedJobId = (jobId ?? string.Empty).Trim().ToLowerInvariant();
                var actor = ResolveActor(request?.ActedBy);
                var actionAt = DateTime.UtcNow;

                if (!MonitoringJobControl.SupportedJobIds.Contains(normalizedJobId))
                {
                    return BadRequest($"Unsupported jobId '{jobId}'.");
                }

                var wasPaused = IsJobPaused(normalizedJobId);

                using (var connection = JobStorage.Current.GetConnection())
                using (var transaction = connection.CreateWriteTransaction())
                {
                    transaction.AddToSet(MonitoringJobControl.PausedJobsSetKey, normalizedJobId);
                    transaction.Commit();
                }

                AddMonitoringAuditEntry(
                    action: "monitoring_job_paused",
                    jobId: normalizedJobId,
                    changedBy: actor,
                    oldData: new { isPaused = wasPaused },
                    newData: new { isPaused = true, updatedAtUtc = actionAt },
                    changeReason: string.IsNullOrWhiteSpace(request?.Reason) ? "Monitoring job paused" : request!.Reason!.Trim(),
                    createdAtUtc: actionAt);

                await _context.SaveChangesAsync();

                return Ok(new MonitoringJobControlResponse
                {
                    JobId = normalizedJobId,
                    IsPaused = true,
                    UpdatedAtUtc = actionAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing monitoring job {JobId}", jobId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("jobs/{jobId}/resume")]
        public async Task<ActionResult<MonitoringJobControlResponse>> ResumeMonitoringJob(
            [FromRoute] string jobId,
            [FromBody] MonitoringJobActionRequest? request)
        {
            try
            {
                var normalizedJobId = (jobId ?? string.Empty).Trim().ToLowerInvariant();
                var actor = ResolveActor(request?.ActedBy);
                var actionAt = DateTime.UtcNow;

                if (!MonitoringJobControl.SupportedJobIds.Contains(normalizedJobId))
                {
                    return BadRequest($"Unsupported jobId '{jobId}'.");
                }

                var wasPaused = IsJobPaused(normalizedJobId);

                using (var connection = JobStorage.Current.GetConnection())
                using (var transaction = connection.CreateWriteTransaction())
                {
                    transaction.RemoveFromSet(MonitoringJobControl.PausedJobsSetKey, normalizedJobId);
                    transaction.Commit();
                }

                AddMonitoringAuditEntry(
                    action: "monitoring_job_resumed",
                    jobId: normalizedJobId,
                    changedBy: actor,
                    oldData: new { isPaused = wasPaused },
                    newData: new { isPaused = false, updatedAtUtc = actionAt },
                    changeReason: string.IsNullOrWhiteSpace(request?.Reason) ? "Monitoring job resumed" : request!.Reason!.Trim(),
                    createdAtUtc: actionAt);

                await _context.SaveChangesAsync();

                return Ok(new MonitoringJobControlResponse
                {
                    JobId = normalizedJobId,
                    IsPaused = false,
                    UpdatedAtUtc = actionAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming monitoring job {JobId}", jobId);
                return StatusCode(500, "Internal server error");
            }
        }

        private static bool IsJobPaused(string jobId)
        {
            var pausedJobs = JobStorage.Current
                .GetConnection()
                .GetAllItemsFromSet(MonitoringJobControl.PausedJobsSetKey);

            return pausedJobs.Any(paused => paused.Equals(jobId, StringComparison.OrdinalIgnoreCase));
        }

        private string ResolveActor(string? actedBy)
        {
            if (!string.IsNullOrWhiteSpace(actedBy))
            {
                return actedBy.Trim();
            }

            var claimActor = User?.FindFirst(ClaimTypes.Name)?.Value
                ?? User?.FindFirst(ClaimTypes.Email)?.Value
                ?? User?.Identity?.Name;

            return string.IsNullOrWhiteSpace(claimActor) ? "system" : claimActor.Trim();
        }

        private void AddMonitoringAuditEntry(
            string action,
            string jobId,
            string changedBy,
            object? oldData,
            object? newData,
            string? changeReason,
            DateTime createdAtUtc)
        {
            _context.RegulatoryAuditLogs.Add(new RegulatoryAuditLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                EntityType = "monitoring_job",
                OldData = SerializeAuditData(jobId, oldData),
                NewData = SerializeAuditData(jobId, newData),
                ChangedBy = changedBy,
                ChangeReason = string.IsNullOrWhiteSpace(changeReason) ? null : changeReason.Trim(),
                CreatedAt = createdAtUtc
            });
        }

        private static string SerializeAuditData(string jobId, object? payload)
        {
            return JsonSerializer.Serialize(new
            {
                jobId,
                data = payload
            });
        }

        private static object? ParseAuditJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<JsonElement>(json);
            }
            catch
            {
                return null;
            }
        }

        private static string? ExtractJobId(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty("jobId", out var jobIdElement) &&
                    jobIdElement.ValueKind == JsonValueKind.String)
                {
                    return jobIdElement.GetString();
                }
            }
            catch
            {
            }

            return null;
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains('"'))
            {
                value = value.Replace("\"", "\"\"");
            }

            if (value.Contains(',') || value.Contains('\n') || value.Contains('\r') || value.Contains('"'))
            {
                return $"\"{value}\"";
            }

            return value;
        }

        private static MonitoringRunDto MapRunToDto(MonitoringRun run)
        {
            var sourceMetrics = ParseSourceMetrics(run.SourceMetrics);
            return new MonitoringRunDto
            {
                Id = run.Id,
                RunType = run.RunType,
                TriggeredBy = run.TriggeredBy,
                Status = run.Status,
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                DurationMs = run.CompletedAt.HasValue
                    ? (long?)Math.Round((run.CompletedAt.Value - run.StartedAt).TotalMilliseconds)
                    : null,
                DocumentsFetched = run.DocumentsFetched,
                DocumentsAdded = run.DocumentsAdded,
                DocumentsSkipped = run.DocumentsSkipped,
                FailureCount = run.FailureCount,
                SourceMetrics = sourceMetrics,
                ErrorSummary = run.ErrorSummary
            };
        }

        private static Dictionary<string, SourceSummaryDto> ParseSourceMetrics(string? sourceMetricsJson)
        {
            if (string.IsNullOrWhiteSpace(sourceMetricsJson))
            {
                return new Dictionary<string, SourceSummaryDto>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, SourceSummaryDto>>(sourceMetricsJson)
                    ?? new Dictionary<string, SourceSummaryDto>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, SourceSummaryDto>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
