import React, { useMemo, useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TableSortLabel,
  Paper,
  Chip,
  IconButton,
  Collapse,
  CircularProgress,
  Alert,
  Button,
} from '@mui/material';
import { KeyboardArrowDown, KeyboardArrowUp, Refresh } from '@mui/icons-material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { regulatoryApi } from '../../services/api';
import type {
  MonitoringRun,
  MonitoringSummary,
  MonitoringJobStatus,
  MonitoringJobAuditEntry,
  SourceSummary,
} from '../../types';

type RunTypeFilter = 'all' | 'federal' | 'state';
type StatusFilter = 'all' | 'completed' | 'failed' | 'running';
type SummaryWindowHours = 6 | 24 | 72 | 168;
type AuditWindow = '24h' | '72h' | '7d' | '30d';
type AuditPageSize = 10 | 25 | 50 | 100;
type AuditActionFilter =
  | 'all'
  | 'monitoring_job_triggered'
  | 'monitoring_job_paused'
  | 'monitoring_job_resumed';
type AuditSortBy = 'createdAt' | 'action' | 'actor' | 'job';
type SortDirection = 'asc' | 'desc';

const SUMMARY_WINDOWS: SummaryWindowHours[] = [6, 24, 72, 168];
const AUDIT_PAGE_SIZES: AuditPageSize[] = [10, 25, 50, 100];

const parseRunType = (value: string | null): RunTypeFilter => {
  const normalized = (value ?? 'all').toLowerCase();
  if (normalized === 'federal' || normalized === 'state') {
    return normalized;
  }

  return 'all';
};

const parseStatus = (value: string | null): StatusFilter => {
  const normalized = (value ?? 'all').toLowerCase();
  if (normalized === 'completed' || normalized === 'failed' || normalized === 'running') {
    return normalized;
  }

  return 'all';
};

const parseHours = (value: string | null): SummaryWindowHours => {
  const parsed = Number.parseInt(value ?? '24', 10);
  if (SUMMARY_WINDOWS.includes(parsed as SummaryWindowHours)) {
    return parsed as SummaryWindowHours;
  }

  return 24;
};

const parseAuditWindow = (value: string | null): AuditWindow => {
  const normalized = (value ?? '7d').toLowerCase();
  if (normalized === '24h' || normalized === '72h' || normalized === '7d' || normalized === '30d') {
    return normalized;
  }

  return '7d';
};

const parseAuditPage = (value: string | null): number => {
  const parsed = Number.parseInt(value ?? '0', 10);
  if (Number.isNaN(parsed) || parsed < 0) {
    return 0;
  }

  return parsed;
};

const parseAuditPageSize = (value: string | null): AuditPageSize => {
  const parsed = Number.parseInt(value ?? '25', 10);
  if (AUDIT_PAGE_SIZES.includes(parsed as AuditPageSize)) {
    return parsed as AuditPageSize;
  }

  return 25;
};

const parseAuditJob = (value: string | null): string => {
  const normalized = (value ?? 'all').trim();
  return normalized.length > 0 ? normalized : 'all';
};

const parseAuditAction = (value: string | null): AuditActionFilter => {
  const normalized = (value ?? 'all').toLowerCase();
  if (
    normalized === 'monitoring_job_triggered' ||
    normalized === 'monitoring_job_paused' ||
    normalized === 'monitoring_job_resumed'
  ) {
    return normalized;
  }

  return 'all';
};

const parseAuditActor = (value: string | null): string => {
  return (value ?? '').trim();
};

const parseAuditReason = (value: string | null): string => {
  return (value ?? '').trim();
};

const parseAuditSortBy = (value: string | null): AuditSortBy => {
  const normalized = (value ?? 'createdAt').trim();
  if (normalized === 'action' || normalized === 'actor' || normalized === 'job') {
    return normalized;
  }

  return 'createdAt';
};

const parseSortDirection = (value: string | null): SortDirection => {
  return value === 'asc' ? 'asc' : 'desc';
};

const MonitoringView: React.FC = () => {
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();

  const [runType, setRunType] = useState<RunTypeFilter>(() => parseRunType(searchParams.get('runType')));
  const [status, setStatus] = useState<StatusFilter>(() => parseStatus(searchParams.get('status')));
  const [hours, setHours] = useState<SummaryWindowHours>(() => parseHours(searchParams.get('hours')));
  const [expandedRunId, setExpandedRunId] = useState<string | null>(null);
  const [auditJobFilter, setAuditJobFilter] = useState<string>(() => parseAuditJob(searchParams.get('auditJob')));
  const [auditActionFilter, setAuditActionFilter] = useState<AuditActionFilter>(() => parseAuditAction(searchParams.get('auditAction')));
  const [auditActorInput, setAuditActorInput] = useState<string>(() => parseAuditActor(searchParams.get('auditActor')));
  const [auditActorFilter, setAuditActorFilter] = useState<string>(() => parseAuditActor(searchParams.get('auditActor')));
  const [auditActorPending, setAuditActorPending] = useState<boolean>(false);
  const [auditReasonInput, setAuditReasonInput] = useState<string>(() => parseAuditReason(searchParams.get('auditReason')));
  const [auditReasonFilter, setAuditReasonFilter] = useState<string>(() => parseAuditReason(searchParams.get('auditReason')));
  const [auditReasonPending, setAuditReasonPending] = useState<boolean>(false);
  const [auditSortBy, setAuditSortBy] = useState<AuditSortBy>(() => parseAuditSortBy(searchParams.get('auditSort')));
  const [auditSortDir, setAuditSortDir] = useState<SortDirection>(() => parseSortDirection(searchParams.get('auditDir')));
  const [auditWindow, setAuditWindow] = useState<AuditWindow>(() => parseAuditWindow(searchParams.get('auditWindow')));
  const [auditPage, setAuditPage] = useState<number>(() => parseAuditPage(searchParams.get('auditPage')));
  const [auditPageSize, setAuditPageSize] = useState<number>(() => parseAuditPageSize(searchParams.get('auditPageSize')));
  const [triggerFeedback, setTriggerFeedback] = useState<{ severity: 'success' | 'error' | 'info'; message: string } | null>(null);

  const auditFromUtc = useMemo(() => {
    const now = Date.now();
    const offsetMs =
      auditWindow === '24h'
        ? 24 * 60 * 60 * 1000
        : auditWindow === '72h'
          ? 72 * 60 * 60 * 1000
          : auditWindow === '7d'
            ? 7 * 24 * 60 * 60 * 1000
            : 30 * 24 * 60 * 60 * 1000;

    return new Date(now - offsetMs).toISOString();
  }, [auditWindow]);

  React.useEffect(() => {
    const urlRunType = parseRunType(searchParams.get('runType'));
    const urlStatus = parseStatus(searchParams.get('status'));
    const urlHours = parseHours(searchParams.get('hours'));
    const urlAuditJob = parseAuditJob(searchParams.get('auditJob'));
    const urlAuditAction = parseAuditAction(searchParams.get('auditAction'));
    const urlAuditActor = parseAuditActor(searchParams.get('auditActor'));
    const urlAuditReason = parseAuditReason(searchParams.get('auditReason'));
    const urlAuditSortBy = parseAuditSortBy(searchParams.get('auditSort'));
    const urlAuditSortDir = parseSortDirection(searchParams.get('auditDir'));
    const urlAuditWindow = parseAuditWindow(searchParams.get('auditWindow'));
    const urlAuditPage = parseAuditPage(searchParams.get('auditPage'));
    const urlAuditPageSize = parseAuditPageSize(searchParams.get('auditPageSize'));

    if (urlRunType !== runType) {
      setRunType(urlRunType);
    }

    if (urlStatus !== status) {
      setStatus(urlStatus);
    }

    if (urlHours !== hours) {
      setHours(urlHours);
    }

    if (urlAuditJob !== auditJobFilter) {
      setAuditJobFilter(urlAuditJob);
    }

    if (urlAuditAction !== auditActionFilter) {
      setAuditActionFilter(urlAuditAction);
    }

    if (urlAuditActor !== auditActorFilter) {
      setAuditActorFilter(urlAuditActor);
      setAuditActorInput(urlAuditActor);
    }

    if (urlAuditReason !== auditReasonFilter) {
      setAuditReasonFilter(urlAuditReason);
      setAuditReasonInput(urlAuditReason);
    }

    if (urlAuditSortBy !== auditSortBy) {
      setAuditSortBy(urlAuditSortBy);
    }

    if (urlAuditSortDir !== auditSortDir) {
      setAuditSortDir(urlAuditSortDir);
    }

    if (urlAuditWindow !== auditWindow) {
      setAuditWindow(urlAuditWindow);
    }

    if (urlAuditPage !== auditPage) {
      setAuditPage(urlAuditPage);
    }

    if (urlAuditPageSize !== auditPageSize) {
      setAuditPageSize(urlAuditPageSize);
    }
  }, [searchParams, runType, status, hours, auditJobFilter, auditActionFilter, auditActorFilter, auditReasonFilter, auditSortBy, auditSortDir, auditWindow, auditPage, auditPageSize]);

  React.useEffect(() => {
    if (auditActorInput === auditActorFilter) {
      setAuditActorPending(false);
      return;
    }

    setAuditActorPending(true);

    const timeoutId = window.setTimeout(() => {
      if (auditActorInput !== auditActorFilter) {
        setAuditActorFilter(auditActorInput);
        setAuditPage(0);
      }
      setAuditActorPending(false);
    }, 350);

    return () => window.clearTimeout(timeoutId);
  }, [auditActorInput, auditActorFilter]);

  React.useEffect(() => {
    if (auditReasonInput === auditReasonFilter) {
      setAuditReasonPending(false);
      return;
    }

    setAuditReasonPending(true);

    const timeoutId = window.setTimeout(() => {
      if (auditReasonInput !== auditReasonFilter) {
        setAuditReasonFilter(auditReasonInput);
        setAuditPage(0);
      }
      setAuditReasonPending(false);
    }, 350);

    return () => window.clearTimeout(timeoutId);
  }, [auditReasonInput, auditReasonFilter]);

  React.useEffect(() => {
    const nextParams = new URLSearchParams(searchParams);

    if (runType === 'all') {
      nextParams.delete('runType');
    } else {
      nextParams.set('runType', runType);
    }

    if (status === 'all') {
      nextParams.delete('status');
    } else {
      nextParams.set('status', status);
    }

    if (hours === 24) {
      nextParams.delete('hours');
    } else {
      nextParams.set('hours', String(hours));
    }

    if (auditJobFilter === 'all') {
      nextParams.delete('auditJob');
    } else {
      nextParams.set('auditJob', auditJobFilter);
    }

    if (auditActionFilter === 'all') {
      nextParams.delete('auditAction');
    } else {
      nextParams.set('auditAction', auditActionFilter);
    }

    if (!auditActorFilter) {
      nextParams.delete('auditActor');
    } else {
      nextParams.set('auditActor', auditActorFilter);
    }

    if (!auditReasonFilter) {
      nextParams.delete('auditReason');
    } else {
      nextParams.set('auditReason', auditReasonFilter);
    }

    if (auditSortBy === 'createdAt') {
      nextParams.delete('auditSort');
    } else {
      nextParams.set('auditSort', auditSortBy);
    }

    if (auditSortDir === 'desc') {
      nextParams.delete('auditDir');
    } else {
      nextParams.set('auditDir', auditSortDir);
    }

    if (auditWindow === '7d') {
      nextParams.delete('auditWindow');
    } else {
      nextParams.set('auditWindow', auditWindow);
    }

    if (auditPage === 0) {
      nextParams.delete('auditPage');
    } else {
      nextParams.set('auditPage', String(auditPage));
    }

    if (auditPageSize === 25) {
      nextParams.delete('auditPageSize');
    } else {
      nextParams.set('auditPageSize', String(auditPageSize));
    }

    if (nextParams.toString() !== searchParams.toString()) {
      setSearchParams(nextParams, { replace: true });
    }
  }, [runType, status, hours, auditJobFilter, auditActionFilter, auditActorFilter, auditReasonFilter, auditSortBy, auditSortDir, auditWindow, auditPage, auditPageSize, searchParams, setSearchParams]);

  const queryParams = useMemo(
    () => ({
      take: 100,
      runType: runType === 'all' ? undefined : runType,
      status: status === 'all' ? undefined : status,
    }),
    [runType, status]
  );

  const {
    data: runs,
    isLoading: runsLoading,
    error: runsError,
    refetch: refetchRuns,
  } = useQuery({
    queryKey: ['monitoring-runs', queryParams],
    queryFn: () => regulatoryApi.getMonitoringRuns(queryParams),
  });

  const {
    data: summary,
    isLoading: summaryLoading,
    error: summaryError,
    refetch: refetchSummary,
  } = useQuery({
    queryKey: ['monitoring-summary', hours],
    queryFn: () => regulatoryApi.getMonitoringSummary(hours),
  });

  const {
    data: jobs,
    isLoading: jobsLoading,
    error: jobsError,
    refetch: refetchJobs,
  } = useQuery({
    queryKey: ['monitoring-jobs-status'],
    queryFn: () => regulatoryApi.getMonitoringJobs(),
    refetchInterval: 30000,
  });

  const {
    data: auditEntries,
    isLoading: auditLoading,
    error: auditError,
    refetch: refetchAudit,
  } = useQuery({
    queryKey: ['monitoring-jobs-audit', auditJobFilter, auditActionFilter, auditActorFilter, auditReasonFilter, auditSortBy, auditSortDir, auditWindow, auditPage, auditPageSize],
    queryFn: () =>
      regulatoryApi.getMonitoringJobAudit({
        skip: auditPage * auditPageSize,
        take: auditPageSize,
        fromUtc: auditFromUtc,
        jobId: auditJobFilter === 'all' ? undefined : auditJobFilter,
        action: auditActionFilter === 'all' ? undefined : auditActionFilter,
        actor: auditActorFilter || undefined,
        reason: auditReasonFilter || undefined,
        sortBy: auditSortBy,
        sortDir: auditSortDir,
      }),
    refetchInterval: 30000,
  });

  const {
    data: auditSummary,
    isLoading: auditSummaryLoading,
    error: auditSummaryError,
    refetch: refetchAuditSummary,
  } = useQuery({
    queryKey: ['monitoring-jobs-audit-summary', auditJobFilter, auditActorFilter, auditReasonFilter, auditWindow],
    queryFn: () =>
      regulatoryApi.getMonitoringJobAuditSummary({
        jobId: auditJobFilter === 'all' ? undefined : auditJobFilter,
        actor: auditActorFilter || undefined,
        reason: auditReasonFilter || undefined,
        fromUtc: auditFromUtc,
      }),
    refetchInterval: 30000,
  });

  const { data: runningRuns, isLoading: runningRunsLoading } = useQuery({
    queryKey: ['monitoring-runs-running-indicator'],
    queryFn: () =>
      regulatoryApi.getMonitoringRuns({
        status: 'running',
        take: 1,
      }),
    refetchInterval: 5000,
  });

  const activeRun = runningRuns?.[0];

  React.useEffect(() => {
    if (!activeRun) {
      return;
    }

    queryClient.invalidateQueries({ queryKey: ['monitoring-runs'] });
    queryClient.invalidateQueries({ queryKey: ['monitoring-summary'] });
  }, [activeRun, queryClient]);

  const refreshAll = () => {
    refetchRuns();
    refetchSummary();
    refetchJobs();
    refetchAudit();
    refetchAuditSummary();
  };

  const exportAuditCsv = async () => {
    try {
      const blob = await regulatoryApi.exportMonitoringJobAuditCsv({
        jobId: auditJobFilter === 'all' ? undefined : auditJobFilter,
        action: auditActionFilter === 'all' ? undefined : auditActionFilter,
        actor: auditActorFilter || undefined,
        reason: auditReasonFilter || undefined,
        fromUtc: auditFromUtc,
        take: 10000,
      });

      const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
      const fileName = `monitoring-job-audit-${timestamp}.csv`;
      const downloadUrl = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = downloadUrl;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(downloadUrl);

      setTriggerFeedback({
        severity: 'success',
        message: 'Audit CSV exported successfully.',
      });
    } catch {
      setTriggerFeedback({
        severity: 'error',
        message: 'Failed to export audit CSV. Please try again.',
      });
    }
  };

  const handleAuditSortChange = (column: AuditSortBy) => {
    if (auditSortBy === column) {
      setAuditSortDir((current) => (current === 'asc' ? 'desc' : 'asc'));
    } else {
      setAuditSortBy(column);
      setAuditSortDir(column === 'createdAt' ? 'desc' : 'asc');
    }

    setAuditPage(0);
  };

  const clearAuditFilters = () => {
    setAuditJobFilter('all');
    setAuditActionFilter('all');
    setAuditActorInput('');
    setAuditActorFilter('');
    setAuditActorPending(false);
    setAuditReasonInput('');
    setAuditReasonFilter('');
    setAuditReasonPending(false);
    setAuditWindow('7d');
    setAuditSortBy('createdAt');
    setAuditSortDir('desc');
    setAuditPageSize(25);
    setAuditPage(0);
  };

  const copyCurrentViewLink = async () => {
    try {
      const url = `${window.location.origin}${window.location.pathname}${window.location.search}`;
      await navigator.clipboard.writeText(url);
      setTriggerFeedback({
        severity: 'success',
        message: 'Monitoring view link copied to clipboard.',
      });
    } catch {
      setTriggerFeedback({
        severity: 'error',
        message: 'Failed to copy view link. Please copy the URL from your browser address bar.',
      });
    }
  };

  const triggerRun = useMutation({
    mutationFn: (runType: 'all' | 'federal' | 'state') => regulatoryApi.triggerMonitoringRun(runType),
    onSuccess: (response) => {
      setTriggerFeedback({
        severity: 'success',
        message: `Triggered: ${response.triggeredJobs.join(', ')}`,
      });

      queryClient.invalidateQueries({ queryKey: ['monitoring-runs'] });
      queryClient.invalidateQueries({ queryKey: ['monitoring-summary'] });
      queryClient.invalidateQueries({ queryKey: ['monitoring-jobs-status'] });
      queryClient.invalidateQueries({ queryKey: ['monitoring-jobs-audit'] });
      queryClient.invalidateQueries({ queryKey: ['monitoring-jobs-audit-summary'] });
    },
    onError: () => {
      setTriggerFeedback({
        severity: 'error',
        message: 'Failed to trigger monitoring run. Please try again.',
      });
    },
  });

  const triggerJob = useMutation({
    mutationFn: (jobId: string) => regulatoryApi.triggerMonitoringJob(jobId),
    onSuccess: (response) => {
      setTriggerFeedback({
        severity: 'success',
        message: `Triggered job: ${response.jobId}`,
      });

      queryClient.invalidateQueries({ queryKey: ['monitoring-runs'] });
      queryClient.invalidateQueries({ queryKey: ['monitoring-summary'] });
      queryClient.invalidateQueries({ queryKey: ['monitoring-jobs-status'] });
      queryClient.invalidateQueries({ queryKey: ['monitoring-jobs-audit'] });
      queryClient.invalidateQueries({ queryKey: ['monitoring-jobs-audit-summary'] });
    },
    onError: () => {
      setTriggerFeedback({
        severity: 'error',
        message: 'Failed to trigger monitoring job. Please try again.',
      });
    },
  });

  const setJobPaused = useMutation({
    mutationFn: ({ jobId, pause }: { jobId: string; pause: boolean }) =>
      pause ? regulatoryApi.pauseMonitoringJob(jobId) : regulatoryApi.resumeMonitoringJob(jobId),
    onSuccess: (response) => {
      setTriggerFeedback({
        severity: 'success',
        message: `${response.isPaused ? 'Paused' : 'Resumed'} job: ${response.jobId}`,
      });

      queryClient.invalidateQueries({ queryKey: ['monitoring-jobs-status'] });
      queryClient.invalidateQueries({ queryKey: ['monitoring-jobs-audit'] });
      queryClient.invalidateQueries({ queryKey: ['monitoring-jobs-audit-summary'] });
    },
    onError: () => {
      setTriggerFeedback({
        severity: 'error',
        message: 'Failed to update monitoring job state. Please try again.',
      });
    },
  });

  return (
    <Box sx={{ p: 3 }}>
      <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
        <Typography variant="h4">Monitoring Operations</Typography>
        <Box display="flex" gap={1}>
          <Button
            variant="outlined"
            onClick={() => triggerRun.mutate('federal')}
            disabled={triggerRun.isPending}
          >
            Run Federal Now
          </Button>
          <Button
            variant="outlined"
            onClick={() => triggerRun.mutate('state')}
            disabled={triggerRun.isPending}
          >
            Run State Now
          </Button>
          <Button
            variant="outlined"
            onClick={() => triggerRun.mutate('all')}
            disabled={triggerRun.isPending}
          >
            Run Both Now
          </Button>
          <Button startIcon={<Refresh />} variant="outlined" onClick={refreshAll}>
            Refresh
          </Button>
          <Button variant="outlined" onClick={copyCurrentViewLink}>
            Copy View Link
          </Button>
        </Box>
      </Box>

      {triggerFeedback && (
        <Alert severity={triggerFeedback.severity} sx={{ mb: 2 }} onClose={() => setTriggerFeedback(null)}>
          {triggerFeedback.message}
        </Alert>
      )}

      {!runningRunsLoading && activeRun && (
        <Alert severity="info" sx={{ mb: 2 }} icon={<CircularProgress size={16} />}>
          Job in progress: {activeRun.runType} monitoring started{' '}
          {new Date(activeRun.startedAt).toLocaleTimeString()}. Data refreshes automatically.
        </Alert>
      )}

      {summaryError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load monitoring summary.
        </Alert>
      )}

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
            <Typography variant="h6">Summary</Typography>
            <FormControl size="small" sx={{ minWidth: 140 }}>
              <InputLabel>Window</InputLabel>
              <Select
                label="Window"
                value={hours}
                onChange={(e) => setHours(Number(e.target.value) as SummaryWindowHours)}
              >
                {SUMMARY_WINDOWS.map((value) => (
                  <MenuItem key={value} value={value}>
                    Last {value}h
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Box>
          {summaryLoading ? (
            <CircularProgress size={24} />
          ) : (
            <SummaryGrid summary={summary} />
          )}
        </CardContent>
      </Card>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Job Schedules
          </Typography>

          {jobsError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              Failed to load monitoring job schedules.
            </Alert>
          )}

          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Job</TableCell>
                  <TableCell>State</TableCell>
                  <TableCell>Last Execution</TableCell>
                  <TableCell>Next Execution</TableCell>
                  <TableCell>Cron</TableCell>
                  <TableCell>Action</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {jobsLoading ? (
                  <TableRow>
                    <TableCell colSpan={6} align="center" sx={{ py: 2 }}>
                      <CircularProgress size={20} />
                    </TableCell>
                  </TableRow>
                ) : !jobs || jobs.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} align="center" sx={{ py: 2 }}>
                      <Typography color="textSecondary">No monitoring jobs found.</Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  jobs.map((job) => (
                    <TableRow key={job.id} hover>
                      <TableCell>{job.id}</TableCell>
                      <TableCell>
                        <JobStateChip job={job} />
                      </TableCell>
                      <TableCell>{formatDateTime(job.lastExecution)}</TableCell>
                      <TableCell>{formatDateTime(job.nextExecution)}</TableCell>
                      <TableCell>{job.cron ?? '—'}</TableCell>
                      <TableCell>
                        <Box display="flex" gap={1}>
                          <Button
                            size="small"
                            variant="outlined"
                            onClick={() => triggerJob.mutate(job.id)}
                            disabled={triggerJob.isPending || triggerRun.isPending || !!job.isPaused}
                          >
                            Trigger now
                          </Button>
                          <Button
                            size="small"
                            variant={job.isPaused ? 'contained' : 'outlined'}
                            color={job.isPaused ? 'success' : 'warning'}
                            onClick={() => setJobPaused.mutate({ jobId: job.id, pause: !job.isPaused })}
                            disabled={setJobPaused.isPending || triggerRun.isPending || triggerJob.isPending}
                          >
                            {job.isPaused ? 'Resume' : 'Pause'}
                          </Button>
                        </Box>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
            <Typography variant="h6">Job Action Audit</Typography>
            <Box display="flex" gap={2}>
              <Button variant="outlined" onClick={exportAuditCsv}>
                Export CSV
              </Button>
              <Button variant="outlined" onClick={clearAuditFilters}>
                Clear Audit Filters
              </Button>
              <FormControl size="small" sx={{ minWidth: 160 }}>
                <InputLabel>Window</InputLabel>
                <Select
                  label="Window"
                  value={auditWindow}
                  onChange={(e) => {
                    setAuditWindow(e.target.value as AuditWindow);
                    setAuditPage(0);
                  }}
                >
                  <MenuItem value="24h">Last 24h</MenuItem>
                  <MenuItem value="72h">Last 72h</MenuItem>
                  <MenuItem value="7d">Last 7d</MenuItem>
                  <MenuItem value="30d">Last 30d</MenuItem>
                </Select>
              </FormControl>
              <FormControl size="small" sx={{ minWidth: 220 }}>
                <InputLabel>Job Filter</InputLabel>
                <Select
                  label="Job Filter"
                  value={auditJobFilter}
                  onChange={(e) => {
                    setAuditJobFilter(e.target.value);
                    setAuditPage(0);
                  }}
                >
                  <MenuItem value="all">All jobs</MenuItem>
                  {(jobs ?? []).map((job) => (
                    <MenuItem key={job.id} value={job.id}>
                      {job.id}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl size="small" sx={{ minWidth: 220 }}>
                <InputLabel>Action</InputLabel>
                <Select
                  label="Action"
                  value={auditActionFilter}
                  onChange={(e) => {
                    setAuditActionFilter(e.target.value as AuditActionFilter);
                    setAuditPage(0);
                  }}
                >
                  <MenuItem value="all">All actions</MenuItem>
                  <MenuItem value="monitoring_job_triggered">Triggered</MenuItem>
                  <MenuItem value="monitoring_job_paused">Paused</MenuItem>
                  <MenuItem value="monitoring_job_resumed">Resumed</MenuItem>
                </Select>
              </FormControl>
              <TextField
                size="small"
                label="Actor"
                value={auditActorInput}
                onChange={(e) => setAuditActorInput(e.target.value)}
                helperText={auditActorPending ? 'Applying filter…' : ' '}
                sx={{ minWidth: 220 }}
              />
              <TextField
                size="small"
                label="Reason"
                value={auditReasonInput}
                onChange={(e) => setAuditReasonInput(e.target.value)}
                helperText={auditReasonPending ? 'Applying filter…' : ' '}
                sx={{ minWidth: 240 }}
              />
            </Box>
          </Box>

          {auditError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              Failed to load monitoring job audit history.
            </Alert>
          )}

          {auditSummaryError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              Failed to load audit action summary.
            </Alert>
          )}

          {!auditSummaryLoading && auditSummary && (
            <Box sx={{ mb: 2 }}>
              <Box display="flex" gap={1} flexWrap="wrap" sx={{ mb: 1.5 }}>
                <Chip label={`Total: ${auditSummary.total}`} variant="outlined" />
                <Chip label={`Triggered: ${auditSummary.triggered}`} color="info" variant="outlined" />
                <Chip label={`Paused: ${auditSummary.paused}`} color="warning" variant="outlined" />
                <Chip label={`Resumed: ${auditSummary.resumed}`} color="success" variant="outlined" />
              </Box>

              <Box display="flex" gap={3} flexWrap="wrap">
                <Box>
                  <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                    Top actors
                  </Typography>
                  <Box display="flex" gap={1} flexWrap="wrap">
                    {(auditSummary.topActors ?? []).length === 0 ? (
                      <Chip size="small" variant="outlined" label="None" />
                    ) : (
                      (auditSummary.topActors ?? []).map((item) => (
                        <Chip key={`actor-${item.key}`} size="small" variant="outlined" label={`${item.key}: ${item.count}`} />
                      ))
                    )}
                  </Box>
                </Box>

                <Box>
                  <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                    Top jobs
                  </Typography>
                  <Box display="flex" gap={1} flexWrap="wrap">
                    {(auditSummary.topJobs ?? []).length === 0 ? (
                      <Chip size="small" variant="outlined" label="None" />
                    ) : (
                      (auditSummary.topJobs ?? []).map((item) => (
                        <Chip key={`job-${item.key}`} size="small" variant="outlined" label={`${item.key}: ${item.count}`} />
                      ))
                    )}
                  </Box>
                </Box>
              </Box>
            </Box>
          )}

          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sortDirection={auditSortBy === 'createdAt' ? auditSortDir : false}>
                    <TableSortLabel
                      active={auditSortBy === 'createdAt'}
                      direction={auditSortBy === 'createdAt' ? auditSortDir : 'desc'}
                      onClick={() => handleAuditSortChange('createdAt')}
                    >
                      When (UTC)
                    </TableSortLabel>
                  </TableCell>
                  <TableCell sortDirection={auditSortBy === 'job' ? auditSortDir : false}>
                    <TableSortLabel
                      active={auditSortBy === 'job'}
                      direction={auditSortBy === 'job' ? auditSortDir : 'asc'}
                      onClick={() => handleAuditSortChange('job')}
                    >
                      Job
                    </TableSortLabel>
                  </TableCell>
                  <TableCell sortDirection={auditSortBy === 'action' ? auditSortDir : false}>
                    <TableSortLabel
                      active={auditSortBy === 'action'}
                      direction={auditSortBy === 'action' ? auditSortDir : 'asc'}
                      onClick={() => handleAuditSortChange('action')}
                    >
                      Action
                    </TableSortLabel>
                  </TableCell>
                  <TableCell sortDirection={auditSortBy === 'actor' ? auditSortDir : false}>
                    <TableSortLabel
                      active={auditSortBy === 'actor'}
                      direction={auditSortBy === 'actor' ? auditSortDir : 'asc'}
                      onClick={() => handleAuditSortChange('actor')}
                    >
                      Actor
                    </TableSortLabel>
                  </TableCell>
                  <TableCell>Reason</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {auditLoading ? (
                  <TableRow>
                    <TableCell colSpan={5} align="center" sx={{ py: 2 }}>
                      <CircularProgress size={20} />
                    </TableCell>
                  </TableRow>
                ) : !auditEntries || auditEntries.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} align="center" sx={{ py: 2 }}>
                      <Typography color="textSecondary">No monitoring job audit entries found.</Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  auditEntries.items.map((entry) => (
                    <TableRow key={entry.id} hover>
                      <TableCell>{formatUtcDateTime(entry.createdAt)}</TableCell>
                      <TableCell>{entry.jobId ?? '—'}</TableCell>
                      <TableCell>{formatAuditAction(entry.action)}</TableCell>
                      <TableCell>{entry.changedBy ?? 'system'}</TableCell>
                      <TableCell>{entry.changeReason ?? '—'}</TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
            <TablePagination
              component="div"
              count={auditEntries?.totalCount ?? 0}
              page={auditPage}
              rowsPerPage={auditPageSize}
              onPageChange={(_, newPage) => setAuditPage(newPage)}
              onRowsPerPageChange={(event) => {
                const next = Number(event.target.value);
                setAuditPageSize(next);
                setAuditPage(0);
              }}
              rowsPerPageOptions={[10, 25, 50, 100]}
            />
          </TableContainer>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
            <Typography variant="h6">Execution History</Typography>
            <Box display="flex" gap={2}>
              <FormControl size="small" sx={{ minWidth: 140 }}>
                <InputLabel>Run Type</InputLabel>
                <Select
                  label="Run Type"
                  value={runType}
                  onChange={(e) => setRunType(e.target.value as typeof runType)}
                >
                  <MenuItem value="all">All</MenuItem>
                  <MenuItem value="federal">Federal</MenuItem>
                  <MenuItem value="state">State</MenuItem>
                </Select>
              </FormControl>
              <FormControl size="small" sx={{ minWidth: 140 }}>
                <InputLabel>Status</InputLabel>
                <Select
                  label="Status"
                  value={status}
                  onChange={(e) => setStatus(e.target.value as typeof status)}
                >
                  <MenuItem value="all">All</MenuItem>
                  <MenuItem value="completed">Completed</MenuItem>
                  <MenuItem value="failed">Failed</MenuItem>
                  <MenuItem value="running">Running</MenuItem>
                </Select>
              </FormControl>
            </Box>
          </Box>

          {runsError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              Failed to load monitoring runs.
            </Alert>
          )}

          <TableContainer component={Paper} variant="outlined">
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell />
                  <TableCell>Type</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Started</TableCell>
                  <TableCell>Duration</TableCell>
                  <TableCell>Fetched</TableCell>
                  <TableCell>Added</TableCell>
                  <TableCell>Failures</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {runsLoading ? (
                  <TableRow>
                    <TableCell colSpan={8} align="center" sx={{ py: 3 }}>
                      <CircularProgress size={24} />
                    </TableCell>
                  </TableRow>
                ) : !runs || runs.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={8} align="center" sx={{ py: 3 }}>
                      <Typography color="textSecondary">No monitoring runs found.</Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  runs.map((run) => (
                    <React.Fragment key={run.id}>
                      <TableRow hover>
                        <TableCell>
                          <IconButton
                            size="small"
                            onClick={() =>
                              setExpandedRunId((current) => (current === run.id ? null : run.id))
                            }
                          >
                            {expandedRunId === run.id ? <KeyboardArrowUp /> : <KeyboardArrowDown />}
                          </IconButton>
                        </TableCell>
                        <TableCell>{run.runType}</TableCell>
                        <TableCell>
                          <StatusChip status={run.status} />
                        </TableCell>
                        <TableCell>{new Date(run.startedAt).toLocaleString()}</TableCell>
                        <TableCell>{formatDuration(run.durationMs)}</TableCell>
                        <TableCell>{run.documentsFetched}</TableCell>
                        <TableCell>{run.documentsAdded}</TableCell>
                        <TableCell>{run.failureCount}</TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell colSpan={8} sx={{ py: 0 }}>
                          <Collapse in={expandedRunId === run.id} timeout="auto" unmountOnExit>
                            <Box sx={{ p: 2, backgroundColor: 'grey.50' }}>
                              <Typography variant="subtitle2" gutterBottom>
                                Run Details
                              </Typography>
                              <Typography variant="body2">Triggered By: {run.triggeredBy ?? 'system'}</Typography>
                              <Typography variant="body2">
                                Completed: {run.completedAt ? new Date(run.completedAt).toLocaleString() : 'In progress'}
                              </Typography>
                              {run.errorSummary && (
                                <Alert severity="error" sx={{ mt: 1, mb: 1 }}>
                                  {run.errorSummary}
                                </Alert>
                              )}

                              <Typography variant="subtitle2" sx={{ mt: 1 }} gutterBottom>
                                By Source
                              </Typography>
                              <Grid container spacing={1}>
                                {Object.entries(run.sourceMetrics ?? {}).length === 0 ? (
                                  <Grid item xs={12}>
                                    <Typography color="textSecondary" variant="body2">
                                      No source metrics available.
                                    </Typography>
                                  </Grid>
                                ) : (
                                  Object.entries(run.sourceMetrics ?? {}).map(([source, metrics]) => (
                                    <Grid item xs={12} md={6} key={source}>
                                      <Paper variant="outlined" sx={{ p: 1.5 }}>
                                        <Typography variant="subtitle2">{source}</Typography>
                                        <Typography variant="body2">Fetched: {metrics.fetched}</Typography>
                                        <Typography variant="body2">Added: {metrics.added}</Typography>
                                        <Typography variant="body2">Skipped: {metrics.skipped}</Typography>
                                        <Typography variant="body2">Failures: {metrics.failures}</Typography>
                                      </Paper>
                                    </Grid>
                                  ))
                                )}
                              </Grid>
                            </Box>
                          </Collapse>
                        </TableCell>
                      </TableRow>
                    </React.Fragment>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>
    </Box>
  );
};

const SummaryGrid: React.FC<{ summary?: MonitoringSummary }> = ({ summary }) => {
  const cards = [
    { label: 'Runs', value: summary?.totalRuns ?? 0 },
    { label: 'Successful', value: summary?.successfulRuns ?? 0 },
    { label: 'Failed', value: summary?.failedRuns ?? 0 },
    { label: 'Fetched', value: summary?.totalDocumentsFetched ?? 0 },
    { label: 'Added', value: summary?.totalDocumentsAdded ?? 0 },
    { label: 'Failures', value: summary?.totalFailures ?? 0 },
  ];

  const topFailures = Object.entries(summary?.bySource ?? {})
    .map(([source, metric]) => ({ source, metric }))
    .filter(({ metric }) => metric.failures > 0)
    .sort((a, b) => b.metric.failures - a.metric.failures)
    .slice(0, 5);

  return (
    <>
      <Grid container spacing={2}>
        {cards.map((card) => (
          <Grid item xs={6} md={2} key={card.label}>
            <Paper variant="outlined" sx={{ p: 1.5, textAlign: 'center' }}>
              <Typography variant="h6">{card.value}</Typography>
              <Typography variant="body2" color="textSecondary">
                {card.label}
              </Typography>
            </Paper>
          </Grid>
        ))}
      </Grid>

      <Box sx={{ mt: 2 }}>
        <Typography variant="subtitle2" gutterBottom>
          Top Failure Sources
        </Typography>
        {topFailures.length === 0 ? (
          <Typography variant="body2" color="textSecondary">
            No source failures in this window.
          </Typography>
        ) : (
          <Box display="flex" gap={1} flexWrap="wrap">
            {topFailures.map(({ source, metric }) => (
              <Chip
                key={source}
                color="error"
                variant="outlined"
                label={`${source}: ${metric.failures} failure${metric.failures === 1 ? '' : 's'}`}
              />
            ))}
          </Box>
        )}
      </Box>
    </>
  );
};

const StatusChip: React.FC<{ status: string }> = ({ status }) => {
  switch (status) {
    case 'completed':
      return <Chip size="small" color="success" label="Completed" />;
    case 'failed':
      return <Chip size="small" color="error" label="Failed" />;
    case 'running':
      return <Chip size="small" color="info" label="Running" />;
    default:
      return <Chip size="small" variant="outlined" label={status} />;
  }
};

const JobStateChip: React.FC<{ job: MonitoringJobStatus }> = ({ job }) => {
  if (job.isPaused) {
    return <Chip size="small" color="warning" label="Paused" />;
  }

  if (job.error) {
    return <Chip size="small" color="error" label="Error" />;
  }

  const state = (job.lastJobState ?? '').toLowerCase();
  if (state === 'succeeded') {
    return <Chip size="small" color="success" label="Succeeded" />;
  }

  if (state === 'processing' || state === 'enqueued') {
    return <Chip size="small" color="info" label="Running" />;
  }

  if (state === 'failed') {
    return <Chip size="small" color="error" label="Failed" />;
  }

  return <Chip size="small" variant="outlined" label={job.lastJobState ?? 'Unknown'} />;
};

const formatDateTime = (value?: string): string => {
  if (!value) {
    return '—';
  }

  return new Date(value).toLocaleString();
};

const formatUtcDateTime = (value: string): string => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toISOString().replace('T', ' ').replace('Z', '');
};

const formatAuditAction = (action: MonitoringJobAuditEntry['action']): string => {
  switch (action) {
    case 'monitoring_job_triggered':
      return 'Triggered';
    case 'monitoring_job_paused':
      return 'Paused';
    case 'monitoring_job_resumed':
      return 'Resumed';
    default:
      return action;
  }
};

const formatDuration = (durationMs?: number): string => {
  if (!durationMs || durationMs < 0) {
    return '—';
  }

  if (durationMs < 1000) {
    return `${durationMs} ms`;
  }

  const seconds = Math.round(durationMs / 1000);
  if (seconds < 60) {
    return `${seconds}s`;
  }

  const minutes = Math.floor(seconds / 60);
  const remainderSeconds = seconds % 60;
  return `${minutes}m ${remainderSeconds}s`;
};

export default MonitoringView;
