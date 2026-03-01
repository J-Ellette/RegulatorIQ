-- Core database schema for regulatory change management

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Regulatory agencies and sources
CREATE TABLE regulatory_agencies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    abbreviation VARCHAR(10),
    agency_type VARCHAR(50),
    jurisdiction VARCHAR(100),
    website_url VARCHAR(500),
    api_endpoint VARCHAR(500),
    contact_info JSONB,
    monitoring_enabled BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Regulatory documents
CREATE TABLE regulatory_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    agency_id UUID REFERENCES regulatory_agencies(id),
    document_id VARCHAR(100) UNIQUE,
    title TEXT NOT NULL,
    document_type VARCHAR(100),
    publication_date DATE,
    effective_date DATE,
    comment_deadline DATE,
    compliance_date DATE,
    source_url VARCHAR(1000),
    pdf_url VARCHAR(1000),
    raw_content TEXT,
    processed_content TEXT,
    docket_number VARCHAR(100),
    federal_register_number VARCHAR(50),
    cfr_citation VARCHAR(100),
    status VARCHAR(50) DEFAULT 'active',
    priority_score INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    content_search_vector tsvector GENERATED ALWAYS AS (
        to_tsvector('english', title || ' ' || COALESCE(processed_content, ''))
    ) STORED
);

-- Document analysis results
CREATE TABLE document_analyses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES regulatory_documents(id) ON DELETE CASCADE,
    analysis_version INTEGER DEFAULT 1,
    classification JSONB,
    entities_extracted JSONB,
    compliance_requirements JSONB,
    impact_assessment JSONB,
    timeline_analysis JSONB,
    affected_parties TEXT[],
    summary TEXT,
    actionable_items JSONB,
    related_regulations TEXT[],
    confidence_score DECIMAL(3,2),
    analysis_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    analyzer_version VARCHAR(20)
);

-- Compliance requirements extracted from documents
CREATE TABLE compliance_requirements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES regulatory_documents(id),
    requirement_text TEXT NOT NULL,
    requirement_type VARCHAR(100),
    applicability TEXT[],
    deadline DATE,
    severity VARCHAR(20),
    implementation_guidance TEXT,
    estimated_cost_impact DECIMAL(15,2),
    complexity_level INTEGER,
    citation VARCHAR(200),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Company compliance frameworks
CREATE TABLE compliance_frameworks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL,
    framework_name VARCHAR(255) NOT NULL,
    framework_version VARCHAR(20),
    description TEXT,
    industry_segments TEXT[],
    geographic_scope TEXT[],
    framework_data JSONB,
    last_updated TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Compliance framework mappings to regulations
CREATE TABLE framework_regulation_mappings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    framework_id UUID REFERENCES compliance_frameworks(id),
    document_id UUID REFERENCES regulatory_documents(id),
    requirement_id UUID REFERENCES compliance_requirements(id),
    mapping_type VARCHAR(50),
    compliance_status VARCHAR(50),
    implementation_status VARCHAR(50),
    notes TEXT,
    assigned_to VARCHAR(255),
    due_date DATE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Change impact assessments
CREATE TABLE change_impact_assessments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES regulatory_documents(id),
    framework_id UUID REFERENCES compliance_frameworks(id),
    impact_score DECIMAL(5,2),
    affected_processes JSONB,
    required_updates JSONB,
    timeline_conflicts JSONB,
    estimated_cost DECIMAL(15,2),
    implementation_complexity INTEGER,
    risk_level VARCHAR(20),
    assessment_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    assessed_by VARCHAR(255)
);

-- Monitoring and alerting
CREATE TABLE regulatory_alerts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    alert_type VARCHAR(100),
    document_id UUID REFERENCES regulatory_documents(id),
    framework_id UUID REFERENCES compliance_frameworks(id),
    severity VARCHAR(20),
    title VARCHAR(500),
    message TEXT,
    alert_data JSONB,
    status VARCHAR(50) DEFAULT 'active',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    acknowledged_at TIMESTAMP WITH TIME ZONE,
    acknowledged_by VARCHAR(255)
);

-- Audit trail
CREATE TABLE regulatory_audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100),
    entity_id UUID,
    old_data JSONB,
    new_data JSONB,
    changed_by VARCHAR(255),
    change_reason TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX idx_documents_agency_date ON regulatory_documents(agency_id, publication_date DESC);
CREATE INDEX idx_documents_effective_date ON regulatory_documents(effective_date) WHERE effective_date IS NOT NULL;
CREATE INDEX idx_documents_priority ON regulatory_documents(priority_score DESC);
CREATE INDEX idx_documents_search ON regulatory_documents USING GIN(content_search_vector);
CREATE INDEX idx_requirements_deadline ON compliance_requirements(deadline) WHERE deadline IS NOT NULL;
CREATE INDEX idx_alerts_status_created ON regulatory_alerts(status, created_at DESC);
CREATE INDEX idx_frameworks_company ON compliance_frameworks(company_id);

-- Seed initial agencies
INSERT INTO regulatory_agencies (name, abbreviation, agency_type, jurisdiction, website_url, api_endpoint) VALUES
('Federal Energy Regulatory Commission', 'FERC', 'federal', 'Federal', 'https://www.ferc.gov', '/docs-filing/elibrary'),
('Department of Energy', 'DOE', 'federal', 'Federal', 'https://www.energy.gov', '/offices/policy'),
('Environmental Protection Agency', 'EPA', 'federal', 'Federal', 'https://www.epa.gov', '/regulations'),
('Commodity Futures Trading Commission', 'CFTC', 'federal', 'Federal', 'https://www.cftc.gov', '/lawregulation'),
('Pipeline and Hazardous Materials Safety Administration', 'PHMSA', 'federal', 'Federal', 'https://www.phmsa.dot.gov', '/regulations'),
('Texas Railroad Commission', 'TRC', 'state', 'Texas', 'https://www.rrc.texas.gov', '/legal/rules/'),
('Texas Commission on Environmental Quality', 'TCEQ', 'state', 'Texas', 'https://www.tceq.texas.gov', '/rules/'),
('Public Utility Commission of Texas', 'TPUC', 'state', 'Texas', 'https://www.puc.texas.gov', '/rules/');
