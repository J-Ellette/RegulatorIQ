import React, { useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Tooltip,
  CircularProgress,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Pagination,
  InputAdornment,
  Alert,
} from '@mui/material';
import {
  Search,
  Visibility,
  GetApp,
  Assessment,
  FilterList,
  Clear,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { regulatoryApi } from '../../services/api';
import type { RegulatoryDocument, RegulatoryAgency } from '../../types';

const DOCUMENT_TYPES = ['rule', 'prorule', 'notice', 'order', 'guidance'];

const DocumentsListView: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [searchTerm, setSearchTerm] = useState('');
  const [selectedAgency, setSelectedAgency] = useState<string>('');
  const [selectedType, setSelectedType] = useState<string>('');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data: agencies } = useQuery({
    queryKey: ['agencies'],
    queryFn: () => regulatoryApi.getAgencies(),
  });

  const { data: result, isLoading, error } = useQuery({
    queryKey: ['documents', searchTerm, selectedAgency, selectedType, page],
    queryFn: () =>
      regulatoryApi.getDocuments({
        searchTerm: searchTerm || undefined,
        agencyIds: selectedAgency ? [selectedAgency] : undefined,
        documentTypes: selectedType ? [selectedType] : undefined,
        page,
        pageSize,
        sortBy: 'date',
        sortDesc: true,
      }),
  });

  const analyzeDocument = useMutation({
    mutationFn: (documentId: string) => regulatoryApi.analyzeDocument(documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
    },
  });

  const handleClearFilters = () => {
    setSearchTerm('');
    setSelectedAgency('');
    setSelectedType('');
    setPage(1);
  };

  const hasFilters = !!(searchTerm || selectedAgency || selectedType);

  return (
    <Box sx={{ p: 3 }}>
      <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
        <Typography variant="h4">Regulatory Documents</Typography>
        <Typography variant="body2" color="textSecondary">
          {result?.totalCount ?? 0} documents
        </Typography>
      </Box>

      {/* Search & Filters */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} md={5}>
              <TextField
                fullWidth
                placeholder="Search documents..."
                value={searchTerm}
                onChange={(e) => {
                  setSearchTerm(e.target.value);
                  setPage(1);
                }}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Search />
                    </InputAdornment>
                  ),
                }}
                size="small"
              />
            </Grid>

            <Grid item xs={12} md={3}>
              <FormControl fullWidth size="small">
                <InputLabel>Agency</InputLabel>
                <Select
                  value={selectedAgency}
                  label="Agency"
                  onChange={(e) => {
                    setSelectedAgency(e.target.value);
                    setPage(1);
                  }}
                >
                  <MenuItem value="">All Agencies</MenuItem>
                  {agencies?.map((agency: RegulatoryAgency) => (
                    <MenuItem key={agency.id} value={agency.id}>
                      {agency.abbreviation ?? agency.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>

            <Grid item xs={12} md={2}>
              <FormControl fullWidth size="small">
                <InputLabel>Type</InputLabel>
                <Select
                  value={selectedType}
                  label="Type"
                  onChange={(e) => {
                    setSelectedType(e.target.value);
                    setPage(1);
                  }}
                >
                  <MenuItem value="">All Types</MenuItem>
                  {DOCUMENT_TYPES.map((t) => (
                    <MenuItem key={t} value={t}>
                      {t.charAt(0).toUpperCase() + t.slice(1)}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>

            <Grid item xs={12} md={2}>
              <Box display="flex" gap={1}>
                <Tooltip title="Filter">
                  <span>
                    <Button
                      variant="outlined"
                      startIcon={<FilterList />}
                      disabled={!hasFilters}
                      size="small"
                    >
                      Active
                    </Button>
                  </span>
                </Tooltip>
                {hasFilters && (
                  <Tooltip title="Clear filters">
                    <IconButton size="small" onClick={handleClearFilters}>
                      <Clear />
                    </IconButton>
                  </Tooltip>
                )}
              </Box>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Documents Table */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load documents. Please try again.
        </Alert>
      )}

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
              <TableCell>Analysis</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={8} align="center" sx={{ py: 4 }}>
                  <CircularProgress />
                </TableCell>
              </TableRow>
            ) : result?.items?.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} align="center" sx={{ py: 4 }}>
                  <Typography color="textSecondary">No documents found.</Typography>
                </TableCell>
              </TableRow>
            ) : (
              result?.items?.map((doc: RegulatoryDocument) => (
                <TableRow key={doc.id} hover>
                  <TableCell sx={{ maxWidth: 320 }}>
                    <Typography variant="body2" noWrap title={doc.title}>
                      {doc.title}
                    </Typography>
                    {doc.docketNumber && (
                      <Typography variant="caption" color="textSecondary">
                        {doc.docketNumber}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {doc.agency?.abbreviation ?? doc.agency?.name ?? '—'}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    {doc.documentType && (
                      <Chip label={doc.documentType} size="small" variant="outlined" />
                    )}
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {doc.publicationDate
                        ? new Date(doc.publicationDate).toLocaleDateString()
                        : '—'}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {doc.effectiveDate
                        ? new Date(doc.effectiveDate).toLocaleDateString()
                        : '—'}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <PriorityChip priority={doc.priorityScore} />
                  </TableCell>
                  <TableCell>
                    <AnalysisStatusChip status={doc.analysisStatus} />
                  </TableCell>
                  <TableCell>
                    <Tooltip title="View Analysis">
                      <IconButton
                        size="small"
                        onClick={() => navigate(`/documents/${doc.id}/analysis`)}
                      >
                        <Visibility />
                      </IconButton>
                    </Tooltip>
                    {doc.pdfUrl && (
                      <Tooltip title="Download PDF">
                        <IconButton size="small" onClick={() => window.open(doc.pdfUrl, '_blank')}>
                          <GetApp />
                        </IconButton>
                      </Tooltip>
                    )}
                    <Tooltip title="Analyze">
                      <IconButton
                        size="small"
                        onClick={() => analyzeDocument.mutate(doc.id)}
                        disabled={analyzeDocument.isPending}
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

      {/* Pagination */}
      {result && result.totalPages > 1 && (
        <Box display="flex" justifyContent="center" sx={{ mt: 3 }}>
          <Pagination
            count={result.totalPages}
            page={page}
            onChange={(_e, p) => setPage(p)}
            color="primary"
          />
        </Box>
      )}
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

  return <Chip {...getPriorityProps(priority)} size="small" />;
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

export default DocumentsListView;
