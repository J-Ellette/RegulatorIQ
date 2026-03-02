import React, { Suspense, useEffect, useState } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider, createTheme, CssBaseline } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Provider } from 'react-redux';
import { CircularProgress, Box, Snackbar, Alert } from '@mui/material';
import { store } from './store';
import {
  connectNotifications,
  subscribeToNotifications,
  type AppNotification,
} from './services/notifications';

// Only load devtools in development
const ReactQueryDevtools =
  process.env.NODE_ENV === 'development'
    ? React.lazy(() =>
        import('@tanstack/react-query-devtools').then((m) => ({
          default: m.ReactQueryDevtools,
        }))
      )
    : null;

const RegulatoryDashboard = React.lazy(
  () => import('./components/Dashboard/RegulatoryDashboard')
);
const DocumentAnalysisView = React.lazy(
  () => import('./components/Documents/DocumentAnalysisView')
);
const DocumentsListView = React.lazy(
  () => import('./components/Documents/DocumentsListView')
);
const ComplianceFrameworksView = React.lazy(
  () => import('./components/ComplianceFrameworks/ComplianceFrameworksView')
);
const ComplianceFrameworkDetailView = React.lazy(
  () => import('./components/ComplianceFrameworks/ComplianceFrameworkDetailView')
);
const AlertsHistoryView = React.lazy(
  () => import('./components/Alerts/AlertsHistoryView')
);

const theme = createTheme({
  palette: {
    primary: {
      main: '#1565c0',
    },
    secondary: {
      main: '#f57c00',
    },
  },
  typography: {
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
  },
});

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      retry: 2,
    },
  },
});

const LoadingFallback = () => (
  <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
    <CircularProgress />
  </Box>
);

const DocumentAnalysisPage: React.FC = () => {
  const segments = window.location.pathname.split('/');
  // path is /documents/:id/analysis — id is at index 2
  const id = segments[2] ?? '';
  return <DocumentAnalysisView documentId={id} />;
};

const ComplianceFrameworkDetailPage: React.FC = () => {
  const segments = window.location.pathname.split('/');
  const id = segments[2] ?? '';
  return <ComplianceFrameworkDetailView frameworkId={id} />;
};

const AppShell: React.FC = () => {
  const [notification, setNotification] = useState<AppNotification | null>(null);

  useEffect(() => {
    connectNotifications();
    const unsubscribe = subscribeToNotifications((incomingNotification) => {
      setNotification(incomingNotification);
      queryClient.invalidateQueries({ queryKey: ['regulatory-alerts'] });
      queryClient.invalidateQueries({ queryKey: ['compliance-frameworks'] });
      if (incomingNotification.frameworkId) {
        queryClient.invalidateQueries({
          queryKey: ['compliance-framework', incomingNotification.frameworkId],
        });
        queryClient.invalidateQueries({
          queryKey: ['framework-assessments', incomingNotification.frameworkId],
        });
      }
    });

    return () => {
      unsubscribe();
    };
  }, []);

  return (
    <>
      <Router>
        <Suspense fallback={<LoadingFallback />}>
          <Routes>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<RegulatoryDashboard />} />
            <Route path="/documents" element={<DocumentsListView />} />
            <Route path="/documents/:id/analysis" element={<DocumentAnalysisPage />} />
            <Route path="/frameworks" element={<ComplianceFrameworksView />} />
            <Route path="/frameworks/:id" element={<ComplianceFrameworkDetailPage />} />
            <Route path="/frameworks/:id/assessments" element={<ComplianceFrameworkDetailPage />} />
            <Route path="/alerts" element={<AlertsHistoryView />} />
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </Suspense>
      </Router>
      <Snackbar
        open={!!notification}
        autoHideDuration={6000}
        onClose={() => setNotification(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert onClose={() => setNotification(null)} severity={notification?.severity ?? 'info'}>
          {notification?.message}
        </Alert>
      </Snackbar>
    </>
  );
};

const App: React.FC = () => {
  return (
    <Provider store={store}>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <AppShell />
          {ReactQueryDevtools && (
            <Suspense fallback={null}>
              <ReactQueryDevtools />
            </Suspense>
          )}
        </ThemeProvider>
      </QueryClientProvider>
    </Provider>
  );
};

export default App;
