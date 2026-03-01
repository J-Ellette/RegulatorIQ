"""
RegulatorIQ ML Services - FastAPI application entry point
"""
from fastapi import FastAPI, HTTPException, BackgroundTasks
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Optional, Dict, Any
import asyncio
import os

from regulatoriq.ai.document_analyzer import LegalDocumentAnalyzer, ChangeImpactAnalyzer
from regulatoriq.data_sources.federal import FederalRegulatoryMonitor
from regulatoriq.data_sources.texas import TexasRegulatoryMonitor, MultiStateMonitor

app = FastAPI(
    title="RegulatorIQ ML Services",
    description="AI-powered regulatory document analysis and monitoring services",
    version="1.0.0"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize services
document_analyzer = LegalDocumentAnalyzer()
change_impact_analyzer = ChangeImpactAnalyzer()
federal_monitor = FederalRegulatoryMonitor()
texas_monitor = TexasRegulatoryMonitor()
multi_state_monitor = MultiStateMonitor()


# Request/Response models
class AnalysisRequest(BaseModel):
    document_id: str
    content: str
    document_type: Optional[str] = None
    title: Optional[str] = None


class ImpactAssessmentRequest(BaseModel):
    new_regulation: Dict[str, Any]
    existing_framework: Dict[str, Any]


class MonitoringRequest(BaseModel):
    sources: Optional[List[str]] = None
    date_from: Optional[str] = None
    date_to: Optional[str] = None


class AnalysisResponse(BaseModel):
    document_id: str
    classification: Optional[Dict[str, Any]] = None
    entities: Optional[Dict[str, Any]] = None
    compliance_requirements: Optional[List[Dict[str, Any]]] = None
    impact_assessment: Optional[Dict[str, Any]] = None
    timeline_analysis: Optional[Dict[str, Any]] = None
    affected_parties: Optional[List[str]] = None
    summary: Optional[str] = None
    actionable_items: Optional[List[Dict[str, Any]]] = None
    related_regulations: Optional[List[str]] = None
    confidence_score: float = 0.0


@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "service": "RegulatorIQ ML Services",
        "version": "1.0.0"
    }


@app.post("/analyze", response_model=AnalysisResponse)
async def analyze_document(request: AnalysisRequest):
    """
    Analyze a regulatory document using AI/ML pipeline.
    Extracts compliance requirements, assesses impact, and generates summary.
    """
    try:
        document = {
            'document_id': request.document_id,
            'content': request.content,
            'document_type': request.document_type or '',
            'title': request.title or ''
        }

        analysis = await document_analyzer.analyze_document(document)

        return AnalysisResponse(
            document_id=request.document_id,
            classification=analysis.get('classification'),
            entities=analysis.get('entities'),
            compliance_requirements=analysis.get('compliance_requirements'),
            impact_assessment=analysis.get('impact_assessment'),
            timeline_analysis=analysis.get('timeline_analysis'),
            affected_parties=analysis.get('affected_parties'),
            summary=analysis.get('summary'),
            actionable_items=analysis.get('actionable_items'),
            related_regulations=analysis.get('related_regulations'),
            confidence_score=analysis.get('confidence_score', 0.0)
        )

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Analysis failed: {str(e)}")


@app.post("/impact-assessment")
async def assess_impact(request: ImpactAssessmentRequest):
    """
    Assess the impact of a regulatory change on an existing compliance framework.
    """
    try:
        impact = await change_impact_analyzer.analyze_change_impact(
            request.new_regulation,
            request.existing_framework
        )
        return impact

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Impact assessment failed: {str(e)}")


@app.post("/monitor/federal")
async def monitor_federal_regulations(request: MonitoringRequest):
    """
    Monitor federal regulatory sources for new regulations.
    """
    try:
        results = {}

        if not request.sources or 'federal_register' in request.sources:
            federal_docs = await federal_monitor.monitor_federal_register()
            results['federal_register'] = federal_docs

        if not request.sources or 'ferc' in request.sources:
            ferc_docs = await federal_monitor.monitor_ferc_filings()
            results['ferc'] = ferc_docs

        if not request.sources or 'doe' in request.sources:
            doe_docs = await federal_monitor.monitor_doe_regulations()
            results['doe'] = doe_docs

        if not request.sources or 'epa' in request.sources:
            epa_docs = await federal_monitor.monitor_epa_regulations()
            results['epa'] = epa_docs

        if not request.sources or 'phmsa' in request.sources:
            phmsa_docs = await federal_monitor.monitor_phmsa_regulations()
            results['phmsa'] = phmsa_docs

        total_documents = sum(len(v) for v in results.values())

        return {
            "status": "completed",
            "sources_monitored": list(results.keys()),
            "total_documents": total_documents,
            "documents": results
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Federal monitoring failed: {str(e)}")


@app.post("/monitor/state")
async def monitor_state_regulations(request: MonitoringRequest):
    """
    Monitor state regulatory sources for new regulations.
    """
    try:
        if request.sources and len(request.sources) == 1 and request.sources[0] == 'texas':
            texas_docs = await texas_monitor.get_all_regulations()
            return {
                "status": "completed",
                "states_monitored": ["texas"],
                "total_documents": len(texas_docs),
                "documents": {"texas": texas_docs}
            }

        results = await multi_state_monitor.monitor_all_states()
        total_documents = sum(len(v) for v in results.values())

        return {
            "status": "completed",
            "states_monitored": list(results.keys()),
            "total_documents": total_documents,
            "documents": results
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"State monitoring failed: {str(e)}")


@app.post("/analyze/batch")
async def batch_analyze_documents(requests: List[AnalysisRequest]):
    """
    Analyze multiple regulatory documents in batch.
    """
    if len(requests) > 50:
        raise HTTPException(
            status_code=400,
            detail="Batch size cannot exceed 50 documents"
        )

    results = []
    errors = []

    for req in requests:
        try:
            document = {
                'document_id': req.document_id,
                'content': req.content,
                'document_type': req.document_type or '',
                'title': req.title or ''
            }
            analysis = await document_analyzer.analyze_document(document)
            results.append({
                'document_id': req.document_id,
                'status': 'success',
                'analysis': analysis
            })
        except Exception as e:
            errors.append({
                'document_id': req.document_id,
                'status': 'error',
                'error': str(e)
            })

    return {
        "total": len(requests),
        "successful": len(results),
        "failed": len(errors),
        "results": results,
        "errors": errors
    }


@app.get("/entities/extract")
async def extract_entities(content: str, doc_type: Optional[str] = None):
    """
    Extract named entities from regulatory text.
    """
    try:
        entities = document_analyzer._extract_entities(content)
        return {"entities": entities}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Entity extraction failed: {str(e)}")


@app.get("/classify")
async def classify_document(content: str, doc_type: Optional[str] = None):
    """
    Classify a regulatory document.
    """
    try:
        classification = await document_analyzer._classify_document(content, doc_type or '')
        return {"classification": classification}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Classification failed: {str(e)}")


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=int(os.environ.get("PORT", 8000)),
        reload=os.environ.get("ENVIRONMENT", "production") == "development"
    )
