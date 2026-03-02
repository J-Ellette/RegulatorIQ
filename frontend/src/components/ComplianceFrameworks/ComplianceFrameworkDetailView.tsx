import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Grid,
  MenuItem,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { ArrowBack, Assessment } from '@mui/icons-material';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { regulatoryApi } from '../../services/api';
import type { ChangeImpactAssessment, FrameworkRegulationMapping } from '../../types';

interface ComplianceFrameworkDetailViewProps {
  frameworkId: string;
}

const ComplianceFrameworkDetailView: React.FC<ComplianceFrameworkDetailViewProps> = ({ frameworkId }) => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [lifecycleStatus, setLifecycleStatus] = useState('active');
  const [lifecycleOwner, setLifecycleOwner] = useState('');
  const [lifecycleReviewDate, setLifecycleReviewDate] = useState('');

  const {
    data: framework,
    isLoading: frameworkLoading,
    error: frameworkError,
  } = useQuery({
    queryKey: ['compliance-framework', frameworkId],
    queryFn: () => regulatoryApi.getComplianceFramework(frameworkId),
    enabled: !!frameworkId,
  });

  const {
    data: assessments,
    isLoading: assessmentsLoading,
    error: assessmentsError,
  } = useQuery({
    queryKey: ['framework-assessments', frameworkId],
    queryFn: () => regulatoryApi.getFrameworkAssessments(frameworkId),
    enabled: !!frameworkId,
  });

  const assessImpactMutation = useMutation({
    mutationFn: (documentId: string) => regulatoryApi.assessImpact(frameworkId, documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['framework-assessments', frameworkId] });
    },
  });

  const updateLifecycleMutation = useMutation({
    mutationFn: () =>
      regulatoryApi.updateFrameworkLifecycle(frameworkId, {
        status: lifecycleStatus,
        owner: lifecycleOwner || undefined,
        nextReviewDate: lifecycleReviewDate || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['compliance-framework', frameworkId] });
      queryClient.invalidateQueries({ queryKey: ['compliance-frameworks'] });
    },
  });

  useEffect(() => {
    if (!framework) return;
    setLifecycleStatus(framework.status || 'active');
    setLifecycleOwner(framework.owner || '');
    setLifecycleReviewDate(framework.nextReviewDate ? framework.nextReviewDate.slice(0, 10) : '');
  }, [framework]);

  if (frameworkLoading) {
    return (
      <Box display="flex" justifyContent="center" sx={{ mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (frameworkError || !framework) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">Failed to load framework details.</Alert>
      </Box>
    );
  }

  const getAssessmentForDocument = (documentId?: string): ChangeImpactAssessment | undefined => {
    if (!documentId || !assessments?.length) return undefined;
    return assessments.find((assessment) => assessment.documentId === documentId);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Button startIcon={<ArrowBack />} sx={{ mb: 2 }} onClick={() => navigate('/frameworks')}>
        Back to Frameworks
      </Button>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h4" gutterBottom>
            {framework.frameworkName}
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 2 }}>
            {framework.description || 'No description provided.'}
          </Typography>
          <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
            {framework.frameworkVersion ? <Chip label={`v${framework.frameworkVersion}`} /> : null}
            <Chip label={`Updated ${new Date(framework.lastUpdated).toLocaleDateString()}`} variant="outlined" />
            {(framework.industrySegments || []).map((segment) => (
              <Chip key={segment} size="small" label={segment} color="primary" variant="outlined" />
            ))}
            {(framework.geographicScope || []).map((scope) => (
              <Chip key={scope} size="small" label={scope} color="secondary" variant="outlined" />
            ))}
          </Box>
        </CardContent>
      </Card>

      <Grid container spacing={3}>
        <Grid item xs={12} lg={8}>
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Lifecycle
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} md={4}>
                  <TextField
                    select
                    fullWidth
                    label="Status"
                    value={lifecycleStatus}
                    onChange={(event) => setLifecycleStatus(event.target.value)}
                  >
                    <MenuItem value="active">Active</MenuItem>
                    <MenuItem value="in_review">In Review</MenuItem>
                    <MenuItem value="approved">Approved</MenuItem>
                    <MenuItem value="archived">Archived</MenuItem>
                  </TextField>
                </Grid>
                <Grid item xs={12} md={4}>
                  <TextField
                    fullWidth
                    label="Owner"
                    value={lifecycleOwner}
                    onChange={(event) => setLifecycleOwner(event.target.value)}
                  />
                </Grid>
                <Grid item xs={12} md={4}>
                  <TextField
                    fullWidth
                    type="date"
                    label="Next Review"
                    InputLabelProps={{ shrink: true }}
                    value={lifecycleReviewDate}
                    onChange={(event) => setLifecycleReviewDate(event.target.value)}
                  />
                </Grid>
              </Grid>
              <Box sx={{ mt: 2 }}>
                <Button
                  variant="contained"
                  onClick={() => updateLifecycleMutation.mutate()}
                  disabled={updateLifecycleMutation.isPending}
                >
                  {updateLifecycleMutation.isPending ? 'Saving...' : 'Save Lifecycle'}
                </Button>
              </Box>
            </CardContent>
          </Card>

          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Regulation Mappings
              </Typography>
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Document</TableCell>
                      <TableCell>Agency</TableCell>
                      <TableCell>Mapping Type</TableCell>
                      <TableCell>Compliance</TableCell>
                      <TableCell>Implementation</TableCell>
                      <TableCell>Impact</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {(framework.regulationMappings || []).map((mapping: FrameworkRegulationMapping) => {
                      const latestAssessment = getAssessmentForDocument(mapping.documentId);
                      return (
                        <TableRow key={mapping.id} hover>
                          <TableCell>{mapping.document?.title || 'Unlinked document'}</TableCell>
                          <TableCell>{mapping.document?.agency?.abbreviation || 'N/A'}</TableCell>
                          <TableCell>{mapping.mappingType || 'N/A'}</TableCell>
                          <TableCell>{mapping.complianceStatus || 'N/A'}</TableCell>
                          <TableCell>{mapping.implementationStatus || 'N/A'}</TableCell>
                          <TableCell>
                            {mapping.documentId ? (
                              <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                                {latestAssessment?.impactScore ? (
                                  <Chip
                                    size="small"
                                    label={`Score ${latestAssessment.impactScore}`}
                                    color={
                                      latestAssessment.riskLevel === 'critical' || latestAssessment.riskLevel === 'high'
                                        ? 'error'
                                        : latestAssessment.riskLevel === 'medium'
                                          ? 'warning'
                                          : 'default'
                                    }
                                  />
                                ) : null}
                                <Button
                                  size="small"
                                  variant="outlined"
                                  startIcon={<Assessment />}
                                  onClick={() => assessImpactMutation.mutate(mapping.documentId!)}
                                  disabled={
                                    assessImpactMutation.isPending &&
                                    assessImpactMutation.variables === mapping.documentId
                                  }
                                >
                                  {assessImpactMutation.isPending &&
                                  assessImpactMutation.variables === mapping.documentId
                                    ? 'Running...'
                                    : 'Run'}
                                </Button>
                              </Box>
                            ) : (
                              <Typography variant="body2" color="text.secondary">
                                N/A
                              </Typography>
                            )}
                          </TableCell>
                        </TableRow>
                      );
                    })}
                    {(framework.regulationMappings || []).length === 0 && (
                      <TableRow>
                        <TableCell colSpan={6} align="center">
                          No regulation mappings found.
                        </TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} lg={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Recent Assessments
              </Typography>
              {assessmentsLoading ? (
                <CircularProgress size={24} />
              ) : assessmentsError ? (
                <Alert severity="error">Failed to load impact assessments.</Alert>
              ) : !assessments?.length ? (
                <Typography color="text.secondary">No assessments run yet.</Typography>
              ) : (
                <Box sx={{ display: 'grid', gap: 1.5 }}>
                  {assessments.slice(0, 8).map((assessment) => {
                    const mappedDocument = (framework.regulationMappings || []).find(
                      (mapping) => mapping.documentId === assessment.documentId
                    )?.document;
                    return (
                      <Paper key={assessment.id} variant="outlined" sx={{ p: 1.5 }}>
                        <Typography variant="subtitle2" noWrap>
                          {mappedDocument?.title || 'Document assessment'}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {new Date(assessment.assessmentDate).toLocaleString()}
                        </Typography>
                        <Box sx={{ mt: 1, display: 'flex', gap: 1 }}>
                          <Chip size="small" label={`Impact ${assessment.impactScore ?? 'N/A'}`} />
                          {assessment.riskLevel ? (
                            <Chip
                              size="small"
                              label={assessment.riskLevel}
                              color={
                                assessment.riskLevel === 'critical' || assessment.riskLevel === 'high'
                                  ? 'error'
                                  : assessment.riskLevel === 'medium'
                                    ? 'warning'
                                    : 'default'
                              }
                            />
                          ) : null}
                        </Box>
                      </Paper>
                    );
                  })}
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default ComplianceFrameworkDetailView;
