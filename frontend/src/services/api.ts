import axios from 'axios';
import type {
  RegulatoryAgency,
  RegulatoryDocument,
  RegulatoryDocumentDetail,
  DocumentAnalysis,
  ComplianceFramework,
  ComplianceFrameworkDetail,
  ChangeImpactAssessment,
  RegulatoryAlert,
  PagedResult,
  DashboardStats,
  DocumentSearchRequest,
  AlertFilter,
} from '../types';

const API_BASE_URL = process.env.REACT_APP_API_URL || '/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor for auth tokens
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export const regulatoryApi = {
  // Agencies
  getAgencies: async (params?: {
    agencyType?: string;
    jurisdiction?: string;
    monitoringEnabled?: boolean;
    searchTerm?: string;
  }): Promise<RegulatoryAgency[]> => {
    const response = await apiClient.get('/agencies', { params });
    return response.data;
  },

  // Dashboard
  getDashboardStats: async (timeframe: string): Promise<DashboardStats> => {
    const response = await apiClient.get(`/regulatorydocuments/stats`, {
      params: { timeframe },
    });
    return response.data;
  },

  // Documents
  getDocuments: async (
    request: DocumentSearchRequest
  ): Promise<PagedResult<RegulatoryDocument>> => {
    const response = await apiClient.get('/regulatorydocuments', { params: request });
    return response.data;
  },

  getRecentDocuments: async (count: number = 20): Promise<RegulatoryDocument[]> => {
    const response = await apiClient.get('/regulatorydocuments/recent', {
      params: { count },
    });
    return response.data;
  },

  getDocument: async (id: string): Promise<RegulatoryDocumentDetail> => {
    const response = await apiClient.get(`/regulatorydocuments/${id}`);
    return response.data;
  },

  searchDocuments: async (
    query: string,
    filter?: Partial<DocumentSearchRequest>
  ): Promise<RegulatoryDocument[]> => {
    const response = await apiClient.get('/regulatorydocuments/search', {
      params: { query, ...filter },
    });
    return response.data;
  },

  analyzeDocument: async (id: string): Promise<DocumentAnalysis> => {
    const response = await apiClient.post(`/regulatorydocuments/${id}/analyze`);
    return response.data;
  },

  getDocumentAnalysis: async (id: string): Promise<DocumentAnalysis> => {
    const response = await apiClient.get(`/regulatorydocuments/${id}/analysis`);
    return response.data;
  },

  bulkAnalyze: async (documentIds: string[]): Promise<{ queued: number; failed: number }> => {
    const response = await apiClient.post('/regulatorydocuments/bulk-analyze', { documentIds });
    return response.data;
  },

  // Alerts
  getActiveAlerts: async (): Promise<RegulatoryAlert[]> => {
    const response = await apiClient.get('/regulatorydocuments/alerts', {
      params: { status: ['active'] },
    });
    return response.data;
  },

  getAlerts: async (filter: AlertFilter): Promise<RegulatoryAlert[]> => {
    const response = await apiClient.get('/regulatorydocuments/alerts', { params: filter });
    return response.data;
  },

  acknowledgeAlert: async (alertId: string, acknowledgedBy?: string): Promise<RegulatoryAlert> => {
    const response = await apiClient.post(`/regulatorydocuments/alerts/${alertId}/acknowledge`, {
      acknowledgedBy,
    });
    return response.data;
  },

  resolveAlert: async (
    alertId: string,
    resolvedBy?: string,
    resolutionNotes?: string
  ): Promise<RegulatoryAlert> => {
    const response = await apiClient.post(`/regulatorydocuments/alerts/${alertId}/resolve`, {
      resolvedBy,
      resolutionNotes,
    });
    return response.data;
  },

  // Compliance Frameworks
  getComplianceFrameworks: async (companyId?: string): Promise<ComplianceFramework[]> => {
    const response = await apiClient.get('/complianceframeworks', {
      params: companyId ? { companyId } : {},
    });
    return response.data;
  },

  getComplianceFramework: async (id: string): Promise<ComplianceFrameworkDetail> => {
    const response = await apiClient.get(`/complianceframeworks/${id}`);
    return response.data;
  },

  createComplianceFramework: async (
    data: Partial<ComplianceFramework>
  ): Promise<ComplianceFramework> => {
    const response = await apiClient.post('/complianceframeworks', data);
    return response.data;
  },

  updateComplianceFramework: async (
    id: string,
    data: Partial<ComplianceFramework>
  ): Promise<ComplianceFramework> => {
    const response = await apiClient.put(`/complianceframeworks/${id}`, data);
    return response.data;
  },

  updateFrameworkLifecycle: async (
    id: string,
    data: { status: string; owner?: string; nextReviewDate?: string }
  ): Promise<ComplianceFramework> => {
    const response = await apiClient.put(`/complianceframeworks/${id}/lifecycle`, data);
    return response.data;
  },

  syncFramework: async (
    id: string
  ): Promise<{ newRegulationsFound: number; syncedAt: string }> => {
    const response = await apiClient.post(`/complianceframeworks/${id}/sync`);
    return response.data;
  },

  // Impact Assessments
  assessImpact: async (
    frameworkId: string,
    documentId: string
  ): Promise<ChangeImpactAssessment> => {
    const response = await apiClient.post(
      `/complianceframeworks/${frameworkId}/impact-assessment`,
      { documentId }
    );
    return response.data;
  },

  getFrameworkAssessments: async (frameworkId: string): Promise<ChangeImpactAssessment[]> => {
    const response = await apiClient.get(
      `/complianceframeworks/${frameworkId}/impact-assessments`
    );
    return response.data;
  },
};

export default apiClient;
