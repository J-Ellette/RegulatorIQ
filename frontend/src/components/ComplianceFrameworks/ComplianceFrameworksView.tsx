import React, { useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  CardActions,
  Typography,
  Button,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
  Alert,
  Divider,
  List,
  ListItem,
  ListItemText,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Add,
  Sync,
  Assessment,
  Business,
  CheckCircle,
  Warning,
  ArrowForward,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { regulatoryApi } from '../../services/api';
import type { ComplianceFramework } from '../../types';

const ComplianceFrameworksView: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [newFrameworkName, setNewFrameworkName] = useState('');
  const [newFrameworkVersion, setNewFrameworkVersion] = useState('');
  const [newFrameworkDescription, setNewFrameworkDescription] = useState('');
  const [createError, setCreateError] = useState('');

  // In a real app, companyId would come from auth context
  const companyId = '00000000-0000-0000-0000-000000000001';

  const { data: frameworks, isLoading, error } = useQuery({
    queryKey: ['compliance-frameworks', companyId],
    queryFn: () => regulatoryApi.getComplianceFrameworks(companyId),
  });

  const createFramework = useMutation({
    mutationFn: () =>
      regulatoryApi.createComplianceFramework({
        companyId,
        frameworkName: newFrameworkName,
        frameworkVersion: newFrameworkVersion || undefined,
        description: newFrameworkDescription || undefined,
      } as any),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['compliance-frameworks'] });
      setCreateDialogOpen(false);
      setNewFrameworkName('');
      setNewFrameworkVersion('');
      setNewFrameworkDescription('');
      setCreateError('');
    },
    onError: () => {
      setCreateError('Failed to create framework. Please try again.');
    },
  });

  const syncFramework = useMutation({
    mutationFn: (id: string) => regulatoryApi.syncFramework(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['compliance-frameworks'] });
    },
  });

  const handleCreate = () => {
    if (!newFrameworkName.trim()) {
      setCreateError('Framework name is required.');
      return;
    }
    createFramework.mutate();
  };

  return (
    <Box sx={{ p: 3 }}>
      <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
        <Typography variant="h4">Compliance Frameworks</Typography>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={() => setCreateDialogOpen(true)}
        >
          New Framework
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load compliance frameworks.
        </Alert>
      )}

      {isLoading ? (
        <Box display="flex" justifyContent="center" sx={{ mt: 4 }}>
          <CircularProgress />
        </Box>
      ) : frameworks?.length === 0 ? (
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 6 }}>
            <Business sx={{ fontSize: 60, color: 'text.secondary', mb: 2 }} />
            <Typography variant="h6" color="textSecondary" gutterBottom>
              No Compliance Frameworks
            </Typography>
            <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
              Create your first compliance framework to start tracking regulatory requirements.
            </Typography>
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => setCreateDialogOpen(true)}
            >
              Create Framework
            </Button>
          </CardContent>
        </Card>
      ) : (
        <Grid container spacing={3}>
          {frameworks?.map((framework: ComplianceFramework) => (
            <Grid item xs={12} md={6} lg={4} key={framework.id}>
              <FrameworkCard
                framework={framework}
                onSync={() => syncFramework.mutate(framework.id)}
                syncing={syncFramework.isPending && syncFramework.variables === framework.id}
                onViewDetails={() => navigate(`/frameworks/${framework.id}`)}
                onViewAssessments={() => navigate(`/frameworks/${framework.id}/assessments`)}
              />
            </Grid>
          ))}
        </Grid>
      )}

      {/* Create Framework Dialog */}
      <Dialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Create Compliance Framework</DialogTitle>
        <DialogContent>
          {createError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {createError}
            </Alert>
          )}
          <TextField
            autoFocus
            margin="dense"
            label="Framework Name"
            fullWidth
            required
            value={newFrameworkName}
            onChange={(e) => setNewFrameworkName(e.target.value)}
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Version"
            fullWidth
            placeholder="e.g. 1.0"
            value={newFrameworkVersion}
            onChange={(e) => setNewFrameworkVersion(e.target.value)}
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Description"
            fullWidth
            multiline
            rows={3}
            value={newFrameworkDescription}
            onChange={(e) => setNewFrameworkDescription(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleCreate}
            disabled={createFramework.isPending}
          >
            {createFramework.isPending ? <CircularProgress size={20} /> : 'Create'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

interface FrameworkCardProps {
  framework: ComplianceFramework;
  onSync: () => void;
  syncing: boolean;
  onViewDetails: () => void;
  onViewAssessments: () => void;
}

const FrameworkCard: React.FC<FrameworkCardProps> = ({
  framework,
  onSync,
  syncing,
  onViewDetails,
  onViewAssessments,
}) => {
  const isUpToDate = framework.status === 'up-to-date';
  const lastUpdated = new Date(framework.lastUpdated);
  const daysSinceUpdate = Math.floor(
    (Date.now() - lastUpdated.getTime()) / (1000 * 60 * 60 * 24)
  );
  const needsReview = daysSinceUpdate > 7;

  return (
    <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <CardContent sx={{ flexGrow: 1 }}>
        <Box display="flex" justifyContent="space-between" alignItems="flex-start" sx={{ mb: 1 }}>
          <Typography variant="h6" sx={{ lineHeight: 1.3 }}>
            {framework.frameworkName}
          </Typography>
          <Chip
            label={isUpToDate ? 'Current' : needsReview ? 'Needs Review' : 'Active'}
            color={isUpToDate ? 'success' : needsReview ? 'warning' : 'default'}
            size="small"
            icon={isUpToDate ? <CheckCircle /> : needsReview ? <Warning /> : undefined}
          />
        </Box>

        {framework.frameworkVersion && (
          <Typography variant="body2" color="textSecondary" sx={{ mb: 1 }}>
            v{framework.frameworkVersion}
          </Typography>
        )}

        {framework.description && (
          <Typography variant="body2" sx={{ mb: 2 }}>
            {framework.description}
          </Typography>
        )}

        <Divider sx={{ my: 1.5 }} />

        <List dense disablePadding>
          {framework.industrySegments?.length ? (
            <ListItem disablePadding sx={{ mb: 0.5 }}>
              <ListItemText
                primary="Industry Segments"
                secondary={framework.industrySegments.join(', ')}
                primaryTypographyProps={{ variant: 'caption', color: 'textSecondary' }}
                secondaryTypographyProps={{ variant: 'body2' }}
              />
            </ListItem>
          ) : null}

          {framework.geographicScope?.length ? (
            <ListItem disablePadding sx={{ mb: 0.5 }}>
              <ListItemText
                primary="Geographic Scope"
                secondary={framework.geographicScope.join(', ')}
                primaryTypographyProps={{ variant: 'caption', color: 'textSecondary' }}
                secondaryTypographyProps={{ variant: 'body2' }}
              />
            </ListItem>
          ) : null}

          <ListItem disablePadding>
            <ListItemText
              primary="Last Updated"
              secondary={lastUpdated.toLocaleDateString()}
              primaryTypographyProps={{ variant: 'caption', color: 'textSecondary' }}
              secondaryTypographyProps={{ variant: 'body2' }}
            />
          </ListItem>
        </List>
      </CardContent>

      <CardActions sx={{ px: 2, pb: 2 }}>
        <Button
          size="small"
          startIcon={syncing ? <CircularProgress size={14} /> : <Sync />}
          onClick={onSync}
          disabled={syncing}
        >
          Sync
        </Button>
        <Button size="small" startIcon={<Assessment />} onClick={onViewAssessments}>
          Assessments
        </Button>
        <Box flexGrow={1} />
        <Tooltip title="View Details">
          <IconButton size="small" onClick={onViewDetails}>
            <ArrowForward />
          </IconButton>
        </Tooltip>
      </CardActions>
    </Card>
  );
};

export default ComplianceFrameworksView;
