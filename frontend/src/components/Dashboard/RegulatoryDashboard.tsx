import React, { useState } from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Tooltip,
  Alert,
  CircularProgress,
} from '@mui/material';
import {
  Visibility,
  GetApp,
  Warning,
  CheckCircle,
  Schedule,
  TrendingUp,
  Notifications,
  Assessment,
  MonitorHeart,
} from '@mui/icons-material';
import type { AlertColor } from '@mui/material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { regulatoryApi } from '../../services/api';
import type {
  RegulatoryDocument,
  ComplianceFramework,
  RegulatoryAlert,
  MonitoringRun,
  SourceSummary,
} from '../../types';

const RegulatoryDashboard: React.FC = () => {
  const [timeframe, setTimeframe] = useState<'week' | 'month' | 'quarter'>('month');
  const [providerFilter, setProviderFilter] = useState<'all' | 'ai' | 'rules' | 'unknown'>('all');
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const { data: stats, isLoading: statsLoading } = useQuery({
    queryKey: ['dashboard-stats', timeframe],
    queryFn: () => regulatoryApi.getDashboardStats(timeframe),
  });

  const { data: recentDocuments, isLoading: documentsLoading } = useQuery({
    queryKey: ['recent-documents'],
    queryFn: () => regulatoryApi.getRecentDocuments(20),
  });

  const { data: alerts, isLoading: alertsLoading } = useQuery({
    queryKey: ['regulatory-alerts'],
    queryFn: () => regulatoryApi.getActiveAlerts(),
  });

  const { data: frameworks, isLoading: frameworksLoading } = useQuery({
    queryKey: ['compliance-frameworks'],
    queryFn: () => regulatoryApi.getComplianceFrameworks(),
  });

  const { data: monitoringRuns, isLoading: monitoringRunsLoading } = useQuery({
    queryKey: ['monitoring-runs-dashboard'],
    queryFn: () => regulatoryApi.getMonitoringRuns({ take: 20 }),
  });

  const { data: monitoringSummary, isLoading: monitoringSummaryLoading } = useQuery({
    queryKey: ['monitoring-summary-dashboard'],
    queryFn: () => regulatoryApi.getMonitoringSummary(24),
  });

  const { data: monitoringRunningRuns } = useQuery({
    queryKey: ['monitoring-running-dashboard-indicator'],
    queryFn: () => regulatoryApi.getMonitoringRuns({ status: 'running', take: 1 }),
    refetchInterval: 5000,
  });

  const acknowledgeAlert = useMutation({
    mutationFn: (alertId: string) => regulatoryApi.acknowledgeAlert(alertId, 'dashboard-user'),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['regulatory-alerts'] });
    },
  });

  const handleAnalyzeDocument = useMutation({
    mutationFn: (documentId: string) => regulatoryApi.analyzeDocument(documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['recent-documents'] });
    },
  });

  const handleViewDocument = (id: string) => navigate(`/documents/${id}/analysis`);
  const handleDownloadPdf = (url?: string) => {
    if (url) window.open(url, '_blank');
  };

  const latestFederalRun = monitoringRuns?.find((run) => run.runType === 'federal');
  const latestStateRun = monitoringRuns?.find((run) => run.runType === 'state');
  const activeMonitoringRun = monitoringRunningRuns?.[0];

  const sourceFailures = Object.entries(monitoringSummary?.bySource ?? {})
    .map(([source, metrics]) => ({ source, metrics }))
    .filter((entry) => entry.metrics.failures > 0)
    .sort((a, b) => b.metrics.failures - a.metrics.failures)
    .slice(0, 5);

  const filteredRecentDocuments = (recentDocuments ?? []).filter((doc) => {
    if (providerFilter === 'all') return true;

    if (providerFilter === 'unknown') {
      return doc.analysisStatus === 'completed' && !(doc.analysisProvider ?? '').trim();
    }

    return (
      doc.analysisStatus === 'completed' &&
      (doc.analysisProvider ?? '').toLowerCase() === providerFilter
    );
  });

  return (
    <Box sx={{ p: 3 }}>
      <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
        <Typography variant="h4">Regulatory Intelligence Dashboard</Typography>
        <Box>
          {(['week', 'month', 'quarter'] as const).map((tf) => (
            <Button
              key={tf}
              variant={timeframe === tf ? 'contained' : 'outlined'}
              size="small"
              onClick={() => setTimeframe(tf)}
              sx={{ ml: 1 }}
            >
              {tf.charAt(0).toUpperCase() + tf.slice(1)}
            </Button>
          ))}
        </Box>
      </Box>

      {/* Key Metrics */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom>
                    New Regulations
                  </Typography>
                  <Typography variant="h4">
                    {statsLoading ? <CircularProgress size={24} /> : stats?.newRegulations ?? 0}
                  </Typography>
                </Box>
                <TrendingUp color="primary" sx={{ fontSize: 40 }} />
              </Box>
              <Typography variant="body2" color="textSecondary">
                Last {timeframe}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom>
                    Pending Deadlines
                  </Typography>
                  <Typography variant="h4" color="warning.main">
                    {statsLoading ? <CircularProgress size={24} /> : stats?.pendingDeadlines ?? 0}
                  </Typography>
                </Box>
                <Schedule color="warning" sx={{ fontSize: 40 }} />
              </Box>
              <Typography variant="body2" color="textSecondary">
                Next 90 days
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom>
                    Impact Assessments
                  </Typography>
                  <Typography variant="h4">
                    {statsLoading ? <CircularProgress size={24} /> : stats?.impactAssessments ?? 0}
                  </Typography>
                </Box>
                <Assessment color="info" sx={{ fontSize: 40 }} />
              </Box>
              <Typography variant="body2" color="textSecondary">
                Completed
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom>
                    Compliance Gaps
                  </Typography>
                  <Typography variant="h4" color="error.main">
                    {statsLoading ? <CircularProgress size={24} /> : stats?.complianceGaps ?? 0}
                  </Typography>
                </Box>
                <Warning color="error" sx={{ fontSize: 40 }} />
              </Box>
              <Typography variant="body2" color="textSecondary">
                Requires attention
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Alerts & Frameworks Section */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 1 }}>
                <Typography variant="h6">
                  <Notifications color="primary" sx={{ mr: 1, verticalAlign: 'middle' }} />
                  Active Alerts
                </Typography>
                <Button size="small" onClick={() => navigate('/alerts')}>
                  View History
                </Button>
              </Box>

              {alertsLoading ? (
                <CircularProgress />
              ) : (
                <Box>
                  {alerts?.slice(0, 5).map((alert: RegulatoryAlert) => (
                    <Alert
                      key={alert.id}
                      severity={toAlertColor(alert.severity)}
                      sx={{ mb: 1 }}
                      action={
                        alert.status === 'acknowledged' ? (
                          <Chip size="small" label="Acknowledged" color="success" />
                        ) : (
                          <Button
                            size="small"
                            onClick={() => acknowledgeAlert.mutate(alert.id)}
                            disabled={
                              acknowledgeAlert.isPending && acknowledgeAlert.variables === alert.id
                            }
                          >
                            {acknowledgeAlert.isPending && acknowledgeAlert.variables === alert.id
                              ? 'Ack...'
                              : 'Acknowledge'}
                          </Button>
                        )
                      }
                    >
                      <Typography variant="subtitle2">{alert.title}</Typography>
                      <Typography variant="body2">{alert.message}</Typography>
                    </Alert>
                  ))}

                  {(!alerts || alerts.length === 0) && (
                    <Typography color="textSecondary">No active alerts</Typography>
                  )}
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Compliance Framework Status
              </Typography>

              {frameworksLoading ? (
                <CircularProgress />
              ) : (
                <Box>
                  {frameworks?.map((framework: ComplianceFramework) => (
                    <Box
                      key={framework.id}
                      sx={{ mb: 2, p: 2, border: 1, borderColor: 'grey.300', borderRadius: 1 }}
                    >
                      <Box display="flex" justifyContent="space-between" alignItems="center">
                        <Typography variant="subtitle1">{framework.frameworkName}</Typography>
                        <Chip
                          label={framework.status ?? 'active'}
                          color={framework.status === 'up-to-date' ? 'success' : 'warning'}
                          size="small"
                        />
                      </Box>
                      <Typography variant="body2" color="textSecondary">
                        Last updated: {new Date(framework.lastUpdated).toLocaleDateString()}
                      </Typography>
                    </Box>
                  ))}
                  {(!frameworks || frameworks.length === 0) && (
                    <Typography color="textSecondary">No frameworks configured</Typography>
                  )}
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
                <Typography variant="h6">
                  <MonitorHeart color="primary" sx={{ mr: 1, verticalAlign: 'middle' }} />
                  Monitoring Health (Last 24h)
                </Typography>
                <Box display="flex" gap={1}>
                  <Button size="small" onClick={() => queryClient.invalidateQueries({ queryKey: ['monitoring-runs-dashboard'] })}>
                    Refresh
                  </Button>
                  <Button size="small" color="error" variant="outlined" onClick={() => navigate('/monitoring?status=failed')}>
                    Open Failed Runs
                  </Button>
                  <Button size="small" variant="outlined" onClick={() => navigate('/monitoring')}>
                    View Monitoring
                  </Button>
                </Box>
              </Box>

              {monitoringRunsLoading || monitoringSummaryLoading ? (
                <CircularProgress size={24} />
              ) : (
                <Grid container spacing={2}>
                  {activeMonitoringRun && (
                    <Grid item xs={12}>
                      <Alert severity="info" icon={<CircularProgress size={16} />}>
                        Job in progress: {activeMonitoringRun.runType} monitoring started{' '}
                        {new Date(activeMonitoringRun.startedAt).toLocaleTimeString()}.
                      </Alert>
                    </Grid>
                  )}
                  <Grid item xs={12} md={4}>
                    <Paper variant="outlined" sx={{ p: 2 }}>
                      <Typography variant="subtitle2" color="textSecondary" gutterBottom>
                        Federal Monitor
                      </Typography>
                      <MonitoringRunHealthChip run={latestFederalRun} />
                      <Typography variant="body2" sx={{ mt: 1 }}>
                        Last run: {formatRunTime(latestFederalRun?.startedAt)}
                      </Typography>
                    </Paper>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Paper variant="outlined" sx={{ p: 2 }}>
                      <Typography variant="subtitle2" color="textSecondary" gutterBottom>
                        State Monitor
                      </Typography>
                      <MonitoringRunHealthChip run={latestStateRun} />
                      <Typography variant="body2" sx={{ mt: 1 }}>
                        Last run: {formatRunTime(latestStateRun?.startedAt)}
                      </Typography>
                    </Paper>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Paper variant="outlined" sx={{ p: 2 }}>
                      <Typography variant="subtitle2" color="textSecondary" gutterBottom>
                        Run Totals (24h)
                      </Typography>
                      <Typography variant="body2">Runs: {monitoringSummary?.totalRuns ?? 0}</Typography>
                      <Typography variant="body2">Fetched: {monitoringSummary?.totalDocumentsFetched ?? 0}</Typography>
                      <Typography variant="body2">Added: {monitoringSummary?.totalDocumentsAdded ?? 0}</Typography>
                      <Typography variant="body2">Failures: {monitoringSummary?.totalFailures ?? 0}</Typography>
                    </Paper>
                  </Grid>
                  <Grid item xs={12}>
                    <Typography variant="subtitle2" gutterBottom>
                      Failures by Source
                    </Typography>
                    {sourceFailures.length > 0 ? (
                      <Box display="flex" gap={1} flexWrap="wrap">
                        {sourceFailures.map(({ source, metrics }) => (
                          <Chip
                            key={source}
                            color="error"
                            variant="outlined"
                            label={`${source}: ${metrics.failures} failure${metrics.failures === 1 ? '' : 's'}`}
                          />
                        ))}
                      </Box>
                    ) : (
                      <Typography color="textSecondary" variant="body2">
                        No source failures in the selected window.
                      </Typography>
                    )}
                  </Grid>
                </Grid>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Recent Documents Table */}
      <Card>
        <CardContent>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
            <Typography variant="h6">Recent Regulatory Documents</Typography>
            <Box display="flex" gap={1}>
              {(['all', 'ai', 'rules', 'unknown'] as const).map((filter) => (
                <Button
                  key={filter}
                  variant={providerFilter === filter ? 'contained' : 'outlined'}
                  size="small"
                  onClick={() => setProviderFilter(filter)}
                >
                  {filter.toUpperCase()}
                </Button>
              ))}
              <Button variant="outlined" onClick={() => navigate('/documents')}>
                View All Documents
              </Button>
            </Box>
          </Box>

          <TableContainer component={Paper} variant="outlined">
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Title</TableCell>
                  <TableCell>Agency</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Publication Date</TableCell>
                  <TableCell>Effective Date</TableCell>
                  <TableCell>Priority</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Provider</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {documentsLoading ? (
                  <TableRow>
                    <TableCell colSpan={9} align="center">
                      <CircularProgress />
                    </TableCell>
                  </TableRow>
                ) : filteredRecentDocuments.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={9} align="center">
                      <Typography color="textSecondary">
                        {providerFilter === 'all'
                          ? 'No recent documents.'
                          : `No ${providerFilter.toUpperCase()} analyses in recent documents.`}
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  filteredRecentDocuments.map((doc: RegulatoryDocument) => (
                    <TableRow key={doc.id} hover>
                      <TableCell>
                        <Typography variant="body2" noWrap sx={{ maxWidth: 300 }}>
                          {doc.title}
                        </Typography>
                      </TableCell>
                      <TableCell>{doc.agency?.abbreviation}</TableCell>
                      <TableCell>
                        <Chip label={doc.documentType} size="small" />
                      </TableCell>
                      <TableCell>
                        {doc.publicationDate
                          ? new Date(doc.publicationDate).toLocaleDateString()
                          : 'N/A'}
                      </TableCell>
                      <TableCell>
                        {doc.effectiveDate
                          ? new Date(doc.effectiveDate).toLocaleDateString()
                          : 'N/A'}
                      </TableCell>
                      <TableCell>
                        <PriorityChip priority={doc.priorityScore} />
                      </TableCell>
                      <TableCell>
                        <AnalysisStatusChip status={doc.analysisStatus} />
                      </TableCell>
                      <TableCell>
                        <AnalysisProviderChip provider={doc.analysisProvider} status={doc.analysisStatus} />
                      </TableCell>
                      <TableCell>
                        <Tooltip title="View Document">
                          <IconButton size="small" onClick={() => handleViewDocument(doc.id)}>
                            <Visibility />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Download PDF">
                          <IconButton size="small" onClick={() => handleDownloadPdf(doc.pdfUrl)}>
                            <GetApp />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Analyze">
                          <IconButton
                            size="small"
                            onClick={() => handleAnalyzeDocument.mutate(doc.id)}
                            disabled={handleAnalyzeDocument.isPending}
                          >
                            <Assessment />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
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

const toAlertColor = (severity?: string): AlertColor => {
  switch (severity?.toLowerCase()) {
    case 'critical':
    case 'high':
    case 'error':
      return 'error';
    case 'medium':
    case 'warning':
      return 'warning';
    case 'success':
      return 'success';
    default:
      return 'info';
  }
};

const PriorityChip: React.FC<{ priority: number }> = ({ priority }) => {
  const getPriorityProps = (score: number) => {
    if (score >= 15) return { label: 'Critical', color: 'error' as const };
    if (score >= 10) return { label: 'High', color: 'warning' as const };
    if (score >= 5) return { label: 'Medium', color: 'info' as const };
    return { label: 'Low', color: 'default' as const };
  };

  const props = getPriorityProps(priority);
  return <Chip {...props} size="small" />;
};

const AnalysisStatusChip: React.FC<{ status?: string }> = ({ status }) => {
  const getStatusProps = (s?: string) => {
    switch (s) {
      case 'completed':
        return { label: 'Analyzed', color: 'success' as const };
      case 'in_progress':
        return { label: 'Processing', color: 'info' as const };
      case 'failed':
        return { label: 'Failed', color: 'error' as const };
      default:
        return { label: 'Pending', color: 'default' as const };
    }
  };

  return <Chip {...getStatusProps(status)} size="small" />;
};

const AnalysisProviderChip: React.FC<{ provider?: string; status?: string }> = ({ provider, status }) => {
  if (status !== 'completed') {
    return <Chip label="—" size="small" variant="outlined" />;
  }

  if (!provider) {
    return <Chip label="Unknown" size="small" variant="outlined" />;
  }

  return <Chip label={provider.toUpperCase()} size="small" variant="outlined" />;
};

const MonitoringRunHealthChip: React.FC<{ run?: MonitoringRun }> = ({ run }) => {
  if (!run) {
    return <Chip size="small" label="No Data" variant="outlined" />;
  }

  if (run.status === 'completed' && run.failureCount === 0) {
    return <Chip size="small" label="Healthy" color="success" />;
  }

  if (run.status === 'failed') {
    return <Chip size="small" label="Failed" color="error" />;
  }

  return <Chip size="small" label="Degraded" color="warning" />;
};

const formatRunTime = (value?: string): string => {
  if (!value) {
    return 'No runs yet';
  }

  return new Date(value).toLocaleString();
};

export default RegulatoryDashboard;
