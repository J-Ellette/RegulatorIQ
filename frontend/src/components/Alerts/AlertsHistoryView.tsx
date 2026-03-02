import React, { useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  FormControl,
  Grid,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { ArrowBack } from '@mui/icons-material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip as RechartsTooltip,
  XAxis,
  YAxis,
  CartesianGrid,
} from 'recharts';
import { regulatoryApi } from '../../services/api';
import type { RegulatoryAlert } from '../../types';

const AlertsHistoryView: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'acknowledged' | 'resolved'>('all');
  const [severityFilter, setSeverityFilter] = useState<'all' | 'low' | 'medium' | 'high' | 'critical'>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [resolutionNotes, setResolutionNotes] = useState<Record<string, string>>({});

  const { data: alerts, isLoading, error } = useQuery({
    queryKey: ['alerts-history', statusFilter, severityFilter, typeFilter],
    queryFn: () =>
      regulatoryApi.getAlerts({
        status: statusFilter === 'all' ? undefined : [statusFilter],
        severity: severityFilter === 'all' ? undefined : [severityFilter],
        alertTypes: typeFilter === 'all' ? undefined : [typeFilter],
      }),
  });

  const acknowledgeMutation = useMutation({
    mutationFn: (alertId: string) => regulatoryApi.acknowledgeAlert(alertId, 'alerts-history-user'),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['alerts-history'] });
      queryClient.invalidateQueries({ queryKey: ['regulatory-alerts'] });
    },
  });

  const resolveMutation = useMutation({
    mutationFn: (alertId: string) =>
      regulatoryApi.resolveAlert(
        alertId,
        'alerts-history-user',
        resolutionNotes[alertId] || 'Resolved from alerts history view'
      ),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['alerts-history'] });
      queryClient.invalidateQueries({ queryKey: ['regulatory-alerts'] });
    },
  });

  const alertTypeOptions = useMemo(() => {
    const source = alerts || [];
    return Array.from(new Set(source.map((alert) => alert.alertType).filter(Boolean))) as string[];
  }, [alerts]);

  const slaMetrics = useMemo(() => {
    const source = alerts || [];
    const now = Date.now();

    const acknowledgedDurationsHours = source
      .filter((alert) => alert.acknowledgedAt)
      .map((alert) => (new Date(alert.acknowledgedAt!).getTime() - new Date(alert.createdAt).getTime()) / 3600000)
      .filter((value) => value >= 0);

    const resolvedDurationsHours = source
      .filter((alert) => alert.resolvedAt)
      .map((alert) => (new Date(alert.resolvedAt!).getTime() - new Date(alert.createdAt).getTime()) / 3600000)
      .filter((value) => value >= 0);

    const unresolvedAgesHours = source
      .filter((alert) => alert.status !== 'resolved')
      .map((alert) => (now - new Date(alert.createdAt).getTime()) / 3600000)
      .filter((value) => value >= 0);

    const average = (items: number[]) => (items.length ? items.reduce((sum, item) => sum + item, 0) / items.length : 0);

    return {
      acknowledgedCount: acknowledgedDurationsHours.length,
      resolvedCount: resolvedDurationsHours.length,
      unresolvedCount: unresolvedAgesHours.length,
      avgTimeToAckHours: average(acknowledgedDurationsHours),
      avgTimeToResolveHours: average(resolvedDurationsHours),
      avgUnresolvedAgeHours: average(unresolvedAgesHours),
      maxUnresolvedAgeHours: unresolvedAgesHours.length ? Math.max(...unresolvedAgesHours) : 0,
      unresolvedOver24h: unresolvedAgesHours.filter((value) => value >= 24).length,
      unresolvedOver72h: unresolvedAgesHours.filter((value) => value >= 72).length,
    };
  }, [alerts]);

  const slaTrendData = useMemo(() => {
    const source = alerts || [];
    const dayMap: Record<string, { ack: number[]; resolve: number[] }> = {};

    source.forEach((alert) => {
      const createdDate = new Date(alert.createdAt);
      const key = createdDate.toISOString().slice(0, 10);

      if (!dayMap[key]) {
        dayMap[key] = { ack: [], resolve: [] };
      }

      if (alert.acknowledgedAt) {
        const ackHours =
          (new Date(alert.acknowledgedAt).getTime() - createdDate.getTime()) / 3600000;
        if (ackHours >= 0) {
          dayMap[key].ack.push(ackHours);
        }
      }

      if (alert.resolvedAt) {
        const resolveHours =
          (new Date(alert.resolvedAt).getTime() - createdDate.getTime()) / 3600000;
        if (resolveHours >= 0) {
          dayMap[key].resolve.push(resolveHours);
        }
      }
    });

    const sortedKeys = Object.keys(dayMap).sort();
    const average = (values: number[]) =>
      values.length ? values.reduce((sum, value) => sum + value, 0) / values.length : null;

    const baseSeries = sortedKeys.map((date) => ({
      date,
      ackAvg: average(dayMap[date].ack),
      resolveAvg: average(dayMap[date].resolve),
    }));

    const rollingAverage = (
      items: Array<{ ackAvg: number | null; resolveAvg: number | null }>,
      index: number,
      key: 'ackAvg' | 'resolveAvg',
      windowSize: number
    ) => {
      const start = Math.max(0, index - windowSize + 1);
      const slice = items
        .slice(start, index + 1)
        .map((item) => item[key])
        .filter((value): value is number => value !== null);
      return slice.length ? slice.reduce((sum, value) => sum + value, 0) / slice.length : null;
    };

    return baseSeries.map((item, index) => ({
      date: item.date,
      ack7d: rollingAverage(baseSeries, index, 'ackAvg', 7),
      ack30d: rollingAverage(baseSeries, index, 'ackAvg', 30),
      resolve7d: rollingAverage(baseSeries, index, 'resolveAvg', 7),
      resolve30d: rollingAverage(baseSeries, index, 'resolveAvg', 30),
    }));
  }, [alerts]);

  const severityBreakdown = useMemo(() => {
    const source = alerts || [];
    const severities = ['low', 'medium', 'high', 'critical'];

    const average = (items: number[]) =>
      items.length ? items.reduce((sum, item) => sum + item, 0) / items.length : 0;

    return severities.map((severity) => {
      const group = source.filter(
        (alert) => (alert.severity || 'low').toLowerCase() === severity
      );

      const ackTimes = group
        .filter((alert) => alert.acknowledgedAt)
        .map(
          (alert) =>
            (new Date(alert.acknowledgedAt!).getTime() - new Date(alert.createdAt).getTime()) /
            3600000
        )
        .filter((value) => value >= 0);

      const resolveTimes = group
        .filter((alert) => alert.resolvedAt)
        .map(
          (alert) =>
            (new Date(alert.resolvedAt!).getTime() - new Date(alert.createdAt).getTime()) / 3600000
        )
        .filter((value) => value >= 0);

      const unresolved = group.filter((alert) => alert.status !== 'resolved').length;

      return {
        severity,
        total: group.length,
        unresolved,
        avgAckHours: average(ackTimes),
        avgResolveHours: average(resolveTimes),
      };
    });
  }, [alerts]);

  const statusColor = (status: string) => {
    if (status === 'resolved') return 'success';
    if (status === 'acknowledged') return 'info';
    return 'warning';
  };

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" sx={{ mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">Failed to load alerts history.</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <Button startIcon={<ArrowBack />} sx={{ mb: 2 }} onClick={() => navigate('/dashboard')}>
        Back to Dashboard
      </Button>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h4" gutterBottom>
            Alerts History
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={4}>
              <FormControl fullWidth>
                <InputLabel>Status</InputLabel>
                <Select
                  label="Status"
                  value={statusFilter}
                  onChange={(event) =>
                    setStatusFilter(event.target.value as 'all' | 'active' | 'acknowledged' | 'resolved')
                  }
                >
                  <MenuItem value="all">All</MenuItem>
                  <MenuItem value="active">Active</MenuItem>
                  <MenuItem value="acknowledged">Acknowledged</MenuItem>
                  <MenuItem value="resolved">Resolved</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={4}>
              <FormControl fullWidth>
                <InputLabel>Severity</InputLabel>
                <Select
                  label="Severity"
                  value={severityFilter}
                  onChange={(event) =>
                    setSeverityFilter(event.target.value as 'all' | 'low' | 'medium' | 'high' | 'critical')
                  }
                >
                  <MenuItem value="all">All</MenuItem>
                  <MenuItem value="low">Low</MenuItem>
                  <MenuItem value="medium">Medium</MenuItem>
                  <MenuItem value="high">High</MenuItem>
                  <MenuItem value="critical">Critical</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={4}>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select
                  label="Type"
                  value={typeFilter}
                  onChange={(event) => setTypeFilter(event.target.value)}
                >
                  <MenuItem value="all">All</MenuItem>
                  {alertTypeOptions.map((type) => (
                    <MenuItem key={type} value={type}>
                      {type}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary">
                Average Time to Acknowledge
              </Typography>
              <Typography variant="h5">{formatHours(slaMetrics.avgTimeToAckHours)}</Typography>
              <Typography variant="body2" color="text.secondary">
                Based on {slaMetrics.acknowledgedCount} acknowledged alerts in current filter
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary">
                Average Time to Resolve
              </Typography>
              <Typography variant="h5">{formatHours(slaMetrics.avgTimeToResolveHours)}</Typography>
              <Typography variant="body2" color="text.secondary">
                Based on {slaMetrics.resolvedCount} resolved alerts in current filter
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary">
                Unresolved Aging
              </Typography>
              <Typography variant="h5">
                {slaMetrics.unresolvedCount > 0
                  ? `${formatHours(slaMetrics.avgUnresolvedAgeHours)} avg`
                  : '0h'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {slaMetrics.unresolvedCount} unresolved • max {formatHours(slaMetrics.maxUnresolvedAgeHours)}
              </Typography>
              <Box sx={{ mt: 1, display: 'flex', gap: 1 }}>
                <Chip size="small" color="warning" label={`>24h: ${slaMetrics.unresolvedOver24h}`} />
                <Chip size="small" color="error" label={`>72h: ${slaMetrics.unresolvedOver72h}`} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            SLA Trend (7/30-Day)
          </Typography>
          {slaTrendData.length === 0 ? (
            <Typography color="text.secondary">Not enough data for trend lines.</Typography>
          ) : (
            <Box sx={{ width: '100%', height: 320 }}>
              <ResponsiveContainer>
                <LineChart data={slaTrendData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" tick={{ fontSize: 12 }} />
                  <YAxis tickFormatter={(value) => `${value}h`} width={60} />
                  <RechartsTooltip />
                  <Line
                    type="monotone"
                    dataKey="ack7d"
                    name="Ack 7d"
                    stroke="#1976d2"
                    strokeWidth={2}
                    dot={false}
                  />
                  <Line
                    type="monotone"
                    dataKey="ack30d"
                    name="Ack 30d"
                    stroke="#64b5f6"
                    strokeWidth={2}
                    dot={false}
                  />
                  <Line
                    type="monotone"
                    dataKey="resolve7d"
                    name="Resolve 7d"
                    stroke="#2e7d32"
                    strokeWidth={2}
                    dot={false}
                  />
                  <Line
                    type="monotone"
                    dataKey="resolve30d"
                    name="Resolve 30d"
                    stroke="#81c784"
                    strokeWidth={2}
                    dot={false}
                  />
                </LineChart>
              </ResponsiveContainer>
            </Box>
          )}
        </CardContent>
      </Card>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Per-Severity SLA Breakdown
          </Typography>
          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Severity</TableCell>
                  <TableCell>Total</TableCell>
                  <TableCell>Unresolved</TableCell>
                  <TableCell>Avg Time to Ack</TableCell>
                  <TableCell>Avg Time to Resolve</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {severityBreakdown.map((row) => (
                  <TableRow key={row.severity}>
                    <TableCell>
                      <Chip size="small" label={row.severity} />
                    </TableCell>
                    <TableCell>{row.total}</TableCell>
                    <TableCell>{row.unresolved}</TableCell>
                    <TableCell>{row.total > 0 ? formatHours(row.avgAckHours) : 'N/A'}</TableCell>
                    <TableCell>{row.total > 0 ? formatHours(row.avgResolveHours) : 'N/A'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Title</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Severity</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Created</TableCell>
                  <TableCell>Resolution Notes</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {(alerts || []).map((alertItem: RegulatoryAlert) => (
                  <TableRow key={alertItem.id} hover>
                    <TableCell>
                      <Typography variant="subtitle2">{alertItem.title}</Typography>
                      <Typography variant="body2" color="text.secondary">
                        {alertItem.message}
                      </Typography>
                    </TableCell>
                    <TableCell>{alertItem.alertType || 'N/A'}</TableCell>
                    <TableCell>
                      <Chip size="small" label={alertItem.severity || 'info'} />
                    </TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        label={alertItem.status}
                        color={statusColor(alertItem.status) as 'success' | 'info' | 'warning'}
                      />
                    </TableCell>
                    <TableCell>{new Date(alertItem.createdAt).toLocaleString()}</TableCell>
                    <TableCell>
                      {alertItem.status === 'resolved' ? (
                        <Typography variant="body2">{alertItem.resolutionNotes || 'Resolved'}</Typography>
                      ) : (
                        <TextField
                          size="small"
                          placeholder="Optional notes"
                          value={resolutionNotes[alertItem.id] || ''}
                          onChange={(event) =>
                            setResolutionNotes((previous) => ({
                              ...previous,
                              [alertItem.id]: event.target.value,
                            }))
                          }
                        />
                      )}
                    </TableCell>
                    <TableCell>
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        {alertItem.status === 'active' && (
                          <Button
                            size="small"
                            variant="outlined"
                            onClick={() => acknowledgeMutation.mutate(alertItem.id)}
                            disabled={
                              acknowledgeMutation.isPending && acknowledgeMutation.variables === alertItem.id
                            }
                          >
                            Acknowledge
                          </Button>
                        )}
                        {alertItem.status !== 'resolved' && (
                          <Button
                            size="small"
                            variant="contained"
                            color="success"
                            onClick={() => resolveMutation.mutate(alertItem.id)}
                            disabled={resolveMutation.isPending && resolveMutation.variables === alertItem.id}
                          >
                            Resolve
                          </Button>
                        )}
                      </Box>
                    </TableCell>
                  </TableRow>
                ))}
                {(alerts || []).length === 0 && (
                  <TableRow>
                    <TableCell colSpan={7} align="center">
                      No alerts match current filters.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>
    </Box>
  );
};

const formatHours = (hours: number): string => {
  if (!Number.isFinite(hours) || hours <= 0) {
    return '0h';
  }

  if (hours < 1) {
    const minutes = Math.round(hours * 60);
    return `${minutes}m`;
  }

  if (hours < 24) {
    return `${hours.toFixed(1)}h`;
  }

  const days = Math.floor(hours / 24);
  const remainingHours = Math.round(hours % 24);
  return `${days}d ${remainingHours}h`;
};

export default AlertsHistoryView;
