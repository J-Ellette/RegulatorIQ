import React, { useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Chip,
  Button,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider,
  Alert,
  Paper,
  LinearProgress,
} from '@mui/material';
import {
  ExpandMore,
  Warning,
  CheckCircle,
  Schedule,
  Assessment,
  Timeline,
  Business,
  Download,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { regulatoryApi } from '../../services/api';
import type { ComplianceRequirement } from '../../types';

interface DocumentAnalysisViewProps {
  documentId: string;
}

const DocumentAnalysisView: React.FC<DocumentAnalysisViewProps> = ({ documentId }) => {
  const [expandedSection, setExpandedSection] = useState<string | false>('summary');

  const {
    data: analysis,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['document-analysis', documentId],
    queryFn: () => regulatoryApi.getDocumentAnalysis(documentId),
    enabled: !!documentId,
  });

  const { data: document } = useQuery({
    queryKey: ['document', documentId],
    queryFn: () => regulatoryApi.getDocument(documentId),
    enabled: !!documentId,
  });

  if (isLoading) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography variant="h6">Analyzing document...</Typography>
        <LinearProgress sx={{ mt: 2 }} />
      </Box>
    );
  }

  if (error || !analysis) {
    return (
      <Alert severity="error">Failed to load document analysis. Please try again.</Alert>
    );
  }

  const handleSectionChange =
    (panel: string) => (_event: React.SyntheticEvent, isExpanded: boolean) => {
      setExpandedSection(isExpanded ? panel : false);
    };

  const handleDownloadReport = (id: string) => {
    window.open(`/api/regulatorydocuments/${id}/report`, '_blank');
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* Document Header */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} md={8}>
              <Typography variant="h5" gutterBottom>
                {document?.title}
              </Typography>
              <Typography variant="subtitle1" color="textSecondary" gutterBottom>
                {document?.agency?.name} • {document?.documentType}
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, mt: 1, flexWrap: 'wrap' }}>
                <Chip
                  label={`Priority: ${analysis.classification?.urgencyLevel ?? 'Medium'}`}
                  color={getPriorityColor(analysis.classification?.urgencyLevel)}
                />
                <Chip label={`Confidence: ${(analysis.confidenceScore * 100).toFixed(0)}%`} />
                {analysis.classification?.primaryCategory && (
                  <Chip label={analysis.classification.primaryCategory} variant="outlined" />
                )}
              </Box>
            </Grid>
            <Grid item xs={12} md={4}>
              <Box textAlign="right">
                <Button
                  variant="outlined"
                  startIcon={<Download />}
                  onClick={() => handleDownloadReport(documentId)}
                  sx={{ mb: 1, display: 'block', width: '100%' }}
                >
                  Download Analysis Report
                </Button>
                <Typography variant="body2" color="textSecondary">
                  Analyzed: {new Date(analysis.analysisDate).toLocaleString()}
                </Typography>
              </Box>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Executive Summary */}
      <Accordion
        expanded={expandedSection === 'summary'}
        onChange={handleSectionChange('summary')}
      >
        <AccordionSummary expandIcon={<ExpandMore />}>
          <Typography variant="h6">
            <Assessment sx={{ mr: 1, verticalAlign: 'middle' }} />
            Executive Summary
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <Typography variant="body1" paragraph>
            {analysis.summary}
          </Typography>

          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12} md={4}>
              <Paper sx={{ p: 2, textAlign: 'center' }}>
                <Typography variant="h4" color="primary">
                  {analysis.impactAssessment?.impactScore?.toFixed(1) ?? 'N/A'}
                </Typography>
                <Typography variant="body2">Impact Score</Typography>
              </Paper>
            </Grid>
            <Grid item xs={12} md={4}>
              <Paper sx={{ p: 2, textAlign: 'center' }}>
                <Typography variant="h4" color="warning.main">
                  {analysis.complianceRequirements?.length ?? 0}
                </Typography>
                <Typography variant="body2">Requirements</Typography>
              </Paper>
            </Grid>
            <Grid item xs={12} md={4}>
              <Paper sx={{ p: 2, textAlign: 'center' }}>
                <Typography variant="h4" color="info.main">
                  {analysis.timelineAnalysis?.criticalDates?.length ?? 0}
                </Typography>
                <Typography variant="body2">Key Dates</Typography>
              </Paper>
            </Grid>
          </Grid>
        </AccordionDetails>
      </Accordion>

      {/* Compliance Requirements */}
      <Accordion
        expanded={expandedSection === 'requirements'}
        onChange={handleSectionChange('requirements')}
      >
        <AccordionSummary expandIcon={<ExpandMore />}>
          <Typography variant="h6">
            <CheckCircle sx={{ mr: 1, verticalAlign: 'middle' }} />
            Compliance Requirements ({analysis.complianceRequirements?.length ?? 0})
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <List>
            {analysis.complianceRequirements?.map(
              (requirement: ComplianceRequirement, index: number) => (
                <React.Fragment key={requirement.requirementId ?? index}>
                  <ListItem alignItems="flex-start">
                    <ListItemIcon>
                      <Warning color={getSeverityColor(requirement.severity)} />
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box>
                          <Typography variant="body1" gutterBottom>
                            {requirement.description}
                          </Typography>
                          <Box sx={{ display: 'flex', gap: 1, mt: 1, flexWrap: 'wrap' }}>
                            <Chip
                              label={requirement.severity ?? 'medium'}
                              color={getSeverityColor(requirement.severity)}
                              size="small"
                            />
                            {requirement.deadline && (
                              <Chip
                                label={`Due: ${requirement.deadline}`}
                                variant="outlined"
                                size="small"
                              />
                            )}
                            {requirement.applicability?.map((app, idx) => (
                              <Chip key={idx} label={app} variant="outlined" size="small" />
                            ))}
                          </Box>
                        </Box>
                      }
                      secondary={
                        requirement.implementationGuidance ? (
                          <Typography variant="body2" sx={{ mt: 1 }}>
                            <strong>Implementation Guidance:</strong>{' '}
                            {requirement.implementationGuidance}
                          </Typography>
                        ) : null
                      }
                    />
                  </ListItem>
                  {index < (analysis.complianceRequirements?.length ?? 0) - 1 && <Divider />}
                </React.Fragment>
              )
            )}
            {(!analysis.complianceRequirements || analysis.complianceRequirements.length === 0) && (
              <ListItem>
                <ListItemText primary="No compliance requirements extracted yet." />
              </ListItem>
            )}
          </List>
        </AccordionDetails>
      </Accordion>

      {/* Timeline Analysis */}
      <Accordion
        expanded={expandedSection === 'timeline'}
        onChange={handleSectionChange('timeline')}
      >
        <AccordionSummary expandIcon={<ExpandMore />}>
          <Typography variant="h6">
            <Timeline sx={{ mr: 1, verticalAlign: 'middle' }} />
            Timeline Analysis
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          {analysis.timelineAnalysis ? (
            <Box>
              {analysis.timelineAnalysis.criticalDates?.length > 0 && (
                <>
                  <Typography variant="subtitle1" gutterBottom>Critical Dates</Typography>
                  <List dense>
                    {analysis.timelineAnalysis.criticalDates.map((date, idx) => (
                      <ListItem key={idx}>
                        <ListItemIcon><Schedule /></ListItemIcon>
                        <ListItemText primary={date} />
                      </ListItem>
                    ))}
                  </List>
                </>
              )}
              {analysis.timelineAnalysis.criticalDates?.length === 0 && (
                <Typography color="textSecondary">No critical dates identified.</Typography>
              )}
            </Box>
          ) : (
            <Typography color="textSecondary">Timeline data not available.</Typography>
          )}
        </AccordionDetails>
      </Accordion>

      {/* Impact Assessment */}
      <Accordion
        expanded={expandedSection === 'impact'}
        onChange={handleSectionChange('impact')}
      >
        <AccordionSummary expandIcon={<ExpandMore />}>
          <Typography variant="h6">
            <Business sx={{ mr: 1, verticalAlign: 'middle' }} />
            Business Impact Assessment
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              {analysis.impactAssessment && (
                <Box>
                  <Typography variant="subtitle1" gutterBottom>
                    Impact Factors
                  </Typography>
                  {Object.entries(analysis.impactAssessment?.factors ?? {}).map(
                    ([factor, score]) => (
                      <Box key={factor} sx={{ mb: 1 }}>
                        <Typography variant="body2">
                          {factor.replace(/_/g, ' ')}:{' '}
                          <strong>{String(score)}</strong>
                        </Typography>
                      </Box>
                    )
                  )}
                </Box>
              )}
            </Grid>
            <Grid item xs={12} md={6}>
              <Box>
                <Typography variant="subtitle1" gutterBottom>
                  Estimated Implementation Cost
                </Typography>
                <Typography variant="h4" color="primary" gutterBottom>
                  {formatCurrency(analysis.impactAssessment?.estimatedComplianceCost)}
                </Typography>

                <Typography variant="subtitle1" gutterBottom sx={{ mt: 2 }}>
                  Affected Business Areas
                </Typography>
                <List dense>
                  {analysis.affectedParties?.map((party, index) => (
                    <ListItem key={index}>
                      <ListItemText primary={party} />
                    </ListItem>
                  ))}
                </List>
              </Box>
            </Grid>
          </Grid>
        </AccordionDetails>
      </Accordion>

      {/* Actionable Items */}
      <Accordion
        expanded={expandedSection === 'actions'}
        onChange={handleSectionChange('actions')}
      >
        <AccordionSummary expandIcon={<ExpandMore />}>
          <Typography variant="h6">
            <Schedule sx={{ mr: 1, verticalAlign: 'middle' }} />
            Recommended Actions ({analysis.actionableItems?.length ?? 0})
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <List>
            {analysis.actionableItems?.map((item: any, index: number) => (
              <ListItem key={index} alignItems="flex-start">
                <ListItemIcon>
                  <CheckCircle color="action" />
                </ListItemIcon>
                <ListItemText
                  primary={item.description}
                  secondary={
                    <Box sx={{ display: 'flex', gap: 1, mt: 0.5 }}>
                      <Chip label={item.priority} size="small" />
                      <Chip label={item.category} size="small" variant="outlined" />
                    </Box>
                  }
                />
              </ListItem>
            ))}
            {(!analysis.actionableItems || analysis.actionableItems.length === 0) && (
              <ListItem>
                <ListItemText primary="No actionable items identified." />
              </ListItem>
            )}
          </List>
        </AccordionDetails>
      </Accordion>
    </Box>
  );
};

const getPriorityColor = (priority?: string): 'error' | 'warning' | 'info' | 'default' => {
  switch (priority?.toLowerCase()) {
    case 'critical':
    case 'high':
      return 'error';
    case 'medium':
      return 'warning';
    case 'low':
      return 'info';
    default:
      return 'default';
  }
};

const getSeverityColor = (severity?: string): 'error' | 'warning' | 'info' => {
  switch (severity?.toLowerCase()) {
    case 'high':
      return 'error';
    case 'medium':
      return 'warning';
    default:
      return 'info';
  }
};

const formatCurrency = (amount?: number): string => {
  if (!amount) return 'TBD';
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
};

export default DocumentAnalysisView;
