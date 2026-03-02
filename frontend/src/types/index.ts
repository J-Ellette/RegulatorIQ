export interface RegulatoryAgency {
  id: string;
  name: string;
  abbreviation?: string;
  agencyType?: string;
  jurisdiction?: string;
  websiteUrl?: string;
}

export interface RegulatoryDocument {
  id: string;
  documentId?: string;
  title: string;
  documentType?: string;
  publicationDate?: string;
  effectiveDate?: string;
  commentDeadline?: string;
  complianceDate?: string;
  sourceUrl?: string;
  pdfUrl?: string;
  docketNumber?: string;
  federalRegisterNumber?: string;
  cfrCitation?: string;
  status: string;
  priorityScore: number;
  createdAt: string;
  agency?: RegulatoryAgency;
  analysisStatus?: string;
  analysisProvider?: string;
}

export interface RegulatoryDocumentDetail extends RegulatoryDocument {
  processedContent?: string;
  analyses?: DocumentAnalysis[];
  complianceRequirements?: ComplianceRequirement[];
}

export interface DocumentAnalysis {
  id: string;
  documentId: string;
  analysisVersion: number;
  classification?: DocumentClassification;
  entitiesExtracted?: ExtractedEntities;
  complianceRequirements?: ComplianceRequirement[];
  impactAssessment?: ImpactAssessment;
  timelineAnalysis?: TimelineAnalysis;
  affectedParties?: string[];
  summary?: string;
  actionableItems?: ActionableItem[];
  relatedRegulations?: string[];
  confidenceScore: number;
  analysisDate: string;
  analysisProvider?: string;
  analyzerVersion?: string;
}

export interface DocumentClassification {
  primaryCategory: string;
  secondaryCategories: string[];
  regulatoryType: string;
  urgencyLevel: string;
  scope: string;
}

export interface ExtractedEntities {
  facilities: string[];
  regulations: string[];
  dates: string[];
  organizations: string[];
  locations: string[];
  monetaryAmounts: string[];
}

export interface ImpactAssessment {
  impactScore: number;
  factors: Record<string, number>;
  priorityLevel: string;
  estimatedComplianceCost?: number;
  implementationComplexity?: number;
}

export interface TimelineAnalysis {
  effectiveDates: string[];
  complianceDates: string[];
  criticalDates: string[];
}

export interface ComplianceRequirement {
  id: string;
  requirementId: string;
  description: string;
  requirementType?: string;
  applicability?: string[];
  deadline?: string;
  severity?: string;
  implementationGuidance?: string;
  estimatedCostImpact?: number;
  complexityLevel?: number;
  citation?: string;
}

export interface ActionableItem {
  itemId: string;
  description: string;
  priority: string;
  category: string;
  dueDate?: string;
}

export interface ComplianceFramework {
  id: string;
  companyId: string;
  frameworkName: string;
  frameworkVersion?: string;
  description?: string;
  industrySegments?: string[];
  geographicScope?: string[];
  status: string;
  owner?: string;
  nextReviewDate?: string;
  lastUpdated: string;
  createdAt: string;
}

export interface ComplianceFrameworkDetail extends ComplianceFramework {
  frameworkData?: Record<string, unknown>;
  regulationMappings?: FrameworkRegulationMapping[];
}

export interface FrameworkRegulationMapping {
  id: string;
  frameworkId?: string;
  documentId?: string;
  requirementId?: string;
  mappingType?: string;
  complianceStatus?: string;
  implementationStatus?: string;
  notes?: string;
  assignedTo?: string;
  dueDate?: string;
  document?: RegulatoryDocument;
}

export interface ChangeImpactAssessment {
  id: string;
  documentId?: string;
  frameworkId?: string;
  impactScore?: number;
  affectedProcesses?: string[];
  requiredUpdates?: string[];
  timelineConflicts?: Record<string, unknown>[];
  estimatedCost?: number;
  implementationComplexity?: number;
  riskLevel?: string;
  assessmentDate: string;
  assessedBy?: string;
}

export interface RegulatoryAlert {
  id: string;
  alertType?: string;
  documentId?: string;
  frameworkId?: string;
  severity?: string;
  title?: string;
  message?: string;
  alertData?: Record<string, unknown>;
  status: string;
  createdAt: string;
  acknowledgedAt?: string;
  acknowledgedBy?: string;
  resolvedAt?: string;
  resolvedBy?: string;
  resolutionNotes?: string;
  document?: RegulatoryDocument;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DashboardStats {
  newRegulations: number;
  pendingDeadlines: number;
  impactAssessments: number;
  complianceGaps: number;
}

export interface DocumentSearchRequest {
  searchTerm?: string;
  agencyIds?: string[];
  documentTypes?: string[];
  fromDate?: string;
  toDate?: string;
  minPriority?: number;
  sortBy?: string;
  sortDesc?: boolean;
  page?: number;
  pageSize?: number;
}

export interface AlertFilter {
  companyId?: string;
  severity?: string[];
  status?: string[];
  alertTypes?: string[];
}

export interface SourceSummary {
  fetched: number;
  added: number;
  skipped: number;
  failures: number;
}

export interface MonitoringRun {
  id: string;
  runType: string;
  triggeredBy?: string;
  status: string;
  startedAt: string;
  completedAt?: string;
  durationMs?: number;
  documentsFetched: number;
  documentsAdded: number;
  documentsSkipped: number;
  failureCount: number;
  sourceMetrics?: Record<string, SourceSummary>;
  errorSummary?: string;
}

export interface MonitoringSummary {
  windowStartUtc: string;
  windowEndUtc: string;
  totalRuns: number;
  successfulRuns: number;
  failedRuns: number;
  totalDocumentsFetched: number;
  totalDocumentsAdded: number;
  totalFailures: number;
  averageDurationMs?: number;
  bySource: Record<string, SourceSummary>;
}

export interface TriggerMonitoringRunResponse {
  requestedRunType: string;
  triggeredJobs: string[];
  triggeredAtUtc: string;
}

export interface TriggerMonitoringJobResponse {
  jobId: string;
  triggeredAtUtc: string;
}

export interface MonitoringJobStatus {
  id: string;
  cron?: string;
  lastJobState?: string;
  lastExecution?: string;
  nextExecution?: string;
  error?: string;
  isPaused?: boolean;
}

export interface MonitoringJobControlResponse {
  jobId: string;
  isPaused: boolean;
  updatedAtUtc: string;
}

export interface MonitoringJobAuditEntry {
  id: string;
  action: string;
  jobId?: string;
  changedBy?: string;
  changeReason?: string;
  createdAt: string;
  oldData?: unknown;
  newData?: unknown;
}

export interface MonitoringJobAuditSummary {
  total: number;
  triggered: number;
  paused: number;
  resumed: number;
  topActors: MonitoringJobAuditBreakdownItem[];
  topJobs: MonitoringJobAuditBreakdownItem[];
}

export interface MonitoringJobAuditBreakdownItem {
  key: string;
  count: number;
}
