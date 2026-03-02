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
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { regulatoryApi } from '../../services/api';
import type { RegulatoryDocument, ComplianceFramework, RegulatoryAlert } from '../../types';

const RegulatoryDashboard: React.FC = () => {
  const [timeframe, setTimeframe] = useState<'week' | 'month' | 'quarter'>('month');
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

  const handleAnalyzeDocument = useMutation({
    mutationFn: (documentId: string) => regulatoryApi.analyzeDocument(documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['recent-documents'] });
    },
  });

  const handleViewDocument = (id: string) => navigate(`/documents/${id}/analysis`);
  const handleViewAlert = (id: string) => navigate(`/alerts/${id}`);
  const handleDownloadPdf = (url?: string) => {
    if (url) window.open(url, '_blank');
  };

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
              <Typography variant="h6" gutterBottom>
                <Notifications color="primary" sx={{ mr: 1, verticalAlign: 'middle' }} />
                Active Alerts
              </Typography>

              {alertsLoading ? (
                <CircularProgress />
              ) : (
                <Box>
                  {alerts?.slice(0, 5).map((alert: RegulatoryAlert) => (
                    <Alert
                      key={alert.id}
                      severity={alert.severity ?? 'info'}
                      sx={{ mb: 1 }}
                      action={
                        <Button size="small" onClick={() => handleViewAlert(alert.id)}>
                          View
                        </Button>
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

      {/* Recent Documents Table */}
      <Card>
        <CardContent>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
            <Typography variant="h6">Recent Regulatory Documents</Typography>
            <Button variant="outlined" onClick={() => navigate('/documents')}>
              View All Documents
            </Button>
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
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {documentsLoading ? (
                  <TableRow>
                    <TableCell colSpan={8} align="center">
                      <CircularProgress />
                    </TableCell>
                  </TableRow>
                ) : (
                  recentDocuments?.map((doc: RegulatoryDocument) => (
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

export default RegulatoryDashboard;
