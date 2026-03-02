# RegulatorIQ: Natural Gas Regulatory Change Management Platform
## Complete Build Sheet for Automated Compliance Tracking & Framework Updates

---

## **Project Overview**

**Platform Name:** RegulatorIQ  
**Purpose:** AI-powered regulatory change tracking and automated compliance framework updates for natural gas industry  
**Target Users:** Natural gas companies, compliance officers, legal teams, regulatory affairs professionals  
**Core Focus:** State and federal regulatory monitoring with automated compliance framework synchronization  
**Key Value:** Real-time regulatory intelligence, automated impact assessment, proactive compliance management

---

## **Technology Stack**

### **Backend Infrastructure**
```
Backend Stack:
├── .NET 8 / ASP.NET Core (Primary API)
├── Entity Framework Core (Database ORM)
├── PostgreSQL (Primary database)
├── Redis (Caching & session management)
├── Elasticsearch (Full-text search & analytics)
├── Apache Kafka (Event streaming)
├── Hangfire (Background job processing)
├── SignalR (Real-time notifications)
└── Azure Service Bus (Message queuing)
```

### **AI/ML Processing Pipeline**
```
AI/ML Stack:
├── Python 3.11+ with FastAPI (ML services)
├── spaCy + transformers (NLP processing)
├── Hugging Face transformers (Legal document analysis)
├── scikit-learn (Classification algorithms)
├── Apache Airflow (ML pipeline orchestration)
├── MLflow (Model management & versioning)
├── BERT fine-tuned for legal text
└── GPT-4 API (Document summarization)
```

### **Web Scraping & Data Collection**
```
Data Collection Stack:
├── Scrapy (Primary web scraping framework)
├── Selenium (Dynamic content scraping)
├── Beautiful Soup (HTML parsing)
├── Requests + aiohttp (HTTP clients)
├── Playwright (Modern browser automation)
├── PDF parsing (PyPDF2, pdfplumber)
├── OCR capabilities (Tesseract)
└── RSS/XML feed parsers
```

### **Frontend & User Interface**
```
Frontend Stack:
├── React 18+ with TypeScript
├── Material-UI v5 (Component library)
├── Redux Toolkit (State management)
├── React Query (Server state management)
├── D3.js + Recharts (Data visualization)
├── React PDF (Document viewing)
├── Socket.IO client (Real-time updates)
└── Progressive Web App capabilities
```

---

## **Regulatory Data Sources & Monitoring**

### **Federal Level Sources**
```python
# regulatoriq/data_sources/federal.py
from typing import List, Dict, Any
import asyncio
import aiohttp
from dataclasses import dataclass
from datetime import datetime

@dataclass
class RegulatorySource:
    name: str
    base_url: str
    api_endpoint: str
    scrape_method: str
    update_frequency: str
    priority_level: int

class FederalRegulatoryMonitor:
    """Monitor federal regulatory agencies for natural gas regulations"""
    
    def __init__(self):
        self.sources = {
            'ferc': RegulatorySource(
                name='Federal Energy Regulatory Commission',
                base_url='https://www.ferc.gov',
                api_endpoint='/docs-filing/elibrary',
                scrape_method='api_and_scrape',
                update_frequency='hourly',
                priority_level=1
            ),
            'doe': RegulatorySource(
                name='Department of Energy',
                base_url='https://www.energy.gov',
                api_endpoint='/offices/policy',
                scrape_method='rss_and_scrape',
                update_frequency='daily',
                priority_level=2
            ),
            'epa': RegulatorySource(
                name='Environmental Protection Agency',
                base_url='https://www.epa.gov',
                api_endpoint='/regulations',
                scrape_method='federal_register',
                update_frequency='daily',
                priority_level=1
            ),
            'cftc': RegulatorySource(
                name='Commodity Futures Trading Commission',
                base_url='https://www.cftc.gov',
                api_endpoint='/lawregulation',
                scrape_method='rss_feed',
                update_frequency='daily',
                priority_level=2
            ),
            'phmsa': RegulatorySource(
                name='Pipeline and Hazardous Materials Safety Admin',
                base_url='https://www.phmsa.dot.gov',
                api_endpoint='/regulations',
                scrape_method='scrape_and_parse',
                update_frequency='daily',
                priority_level=1
            )
        }
        
        self.session = None
        
    async def monitor_ferc_filings(self) -> List[Dict[str, Any]]:
        """Monitor FERC eLibrary for new filings and orders"""
        
        # FERC eLibrary API parameters for natural gas
        search_params = {
            'class': 'nat-gas',
            'date_from': self._get_last_check_date(),
            'date_to': datetime.now().strftime('%Y-%m-%d'),
            'document_types': [
                'order',
                'notice',
                'rule',
                'proposed_rule',
                'filing'
            ]
        }
        
        filings = []
        
        # Use FERC's REST API
        async with aiohttp.ClientSession() as session:
            for doc_type in search_params['document_types']:
                url = f"{self.sources['ferc'].base_url}/api/filings"
                params = {
                    'api_key': self._get_api_key('ferc'),
                    'class': search_params['class'],
                    'type': doc_type,
                    'date_from': search_params['date_from'],
                    'date_to': search_params['date_to'],
                    'format': 'json'
                }
                
                async with session.get(url, params=params) as response:
                    if response.status == 200:
                        data = await response.json()
                        filings.extend(self._parse_ferc_filings(data))
        
        return filings
    
    async def monitor_federal_register(self) -> List[Dict[str, Any]]:
        """Monitor Federal Register for natural gas regulations"""
        
        api_url = "https://www.federalregister.gov/api/v1/documents.json"
        
        # Search parameters for natural gas regulations
        params = {
            'conditions[agencies][]': ['environmental-protection-agency', 
                                     'energy-department',
                                     'transportation-department'],
            'conditions[term]': 'natural gas OR pipeline OR LNG OR methane',
            'conditions[type][]': ['RULE', 'PRORULE', 'NOTICE'],
            'conditions[publication_date][gte]': self._get_last_check_date(),
            'order': 'newest',
            'per_page': 100,
            'fields[]': ['title', 'abstract', 'html_url', 'pdf_url', 
                        'publication_date', 'agencies', 'docket_id']
        }
        
        regulations = []
        
        async with aiohttp.ClientSession() as session:
            async with session.get(api_url, params=params) as response:
                if response.status == 200:
                    data = await response.json()
                    regulations = self._parse_federal_register_docs(data['results'])
        
        return regulations
    
    def _parse_ferc_filings(self, data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """Parse FERC filing data into standardized format"""
        filings = []
        
        for filing in data.get('results', []):
            parsed_filing = {
                'source': 'FERC',
                'document_id': filing.get('accession_number'),
                'title': filing.get('description'),
                'document_type': filing.get('sub_type'),
                'publication_date': filing.get('filed_date'),
                'effective_date': filing.get('effective_date'),
                'url': filing.get('url'),
                'pdf_url': filing.get('pdf_url'),
                'docket_number': filing.get('docket'),
                'summary': filing.get('summary', ''),
                'raw_content': '',
                'impact_assessment': None,
                'compliance_requirements': [],
                'priority_score': self._calculate_priority_score(filing)
            }
            
            filings.append(parsed_filing)
        
        return filings
    
    def _calculate_priority_score(self, document: Dict[str, Any]) -> int:
        """Calculate priority score based on document characteristics"""
        score = 0
        
        # Document type weights
        type_weights = {
            'final_rule': 10,
            'interim_rule': 8,
            'proposed_rule': 6,
            'order': 7,
            'notice': 4,
            'guidance': 3
        }
        
        doc_type = document.get('document_type', '').lower()
        score += type_weights.get(doc_type, 2)
        
        # Keyword analysis for priority
        high_priority_keywords = [
            'pipeline safety', 'lng', 'methane emissions', 
            'transportation rates', 'capacity release',
            'environmental review', 'certificate'
        ]
        
        title = document.get('title', '').lower()
        summary = document.get('summary', '').lower()
        text = f"{title} {summary}"
        
        for keyword in high_priority_keywords:
            if keyword in text:
                score += 2
        
        # Effective date proximity
        effective_date = document.get('effective_date')
        if effective_date:
            days_until_effective = self._days_until_date(effective_date)
            if days_until_effective <= 30:
                score += 5
            elif days_until_effective <= 90:
                score += 3
        
        return min(score, 20)  # Cap at 20
```

### **State Level Sources (Texas Focus)**
```python
# regulatoriq/data_sources/texas.py
from typing import List, Dict, Any
import asyncio
from datetime import datetime, timedelta

class TexasRegulatoryMonitor:
    """Monitor Texas state agencies for natural gas regulations"""
    
    def __init__(self):
        self.sources = {
            'trc': {
                'name': 'Texas Railroad Commission',
                'base_url': 'https://www.rrc.texas.gov',
                'rules_url': '/legal/rules/',
                'orders_url': '/legal/hearings/',
                'priority': 1
            },
            'tceq': {
                'name': 'Texas Commission on Environmental Quality',
                'base_url': 'https://www.tceq.texas.gov',
                'rules_url': '/rules/',
                'priority': 1
            },
            'tsos': {
                'name': 'Texas Secretary of State',
                'base_url': 'https://www.sos.state.tx.us',
                'register_url': '/texreg/',
                'priority': 2
            },
            'tpuc': {
                'name': 'Public Utility Commission of Texas',
                'base_url': 'https://www.puc.texas.gov',
                'rules_url': '/rules/',
                'priority': 2
            }
        }
    
    async def monitor_railroad_commission(self) -> List[Dict[str, Any]]:
        """Monitor Texas Railroad Commission for natural gas regulations"""
        
        regulations = []
        
        # Monitor TRC rules and amendments
        rules_data = await self._scrape_trc_rules()
        regulations.extend(rules_data)
        
        # Monitor hearing orders and decisions
        orders_data = await self._scrape_trc_orders()
        regulations.extend(orders_data)
        
        # Monitor permit applications and approvals
        permits_data = await self._scrape_trc_permits()
        regulations.extend(permits_data)
        
        return regulations
    
    async def _scrape_trc_rules(self) -> List[Dict[str, Any]]:
        """Scrape TRC rules related to natural gas"""
        
        # TRC rules are organized by title
        gas_utility_titles = [
            'Title 16, Part 1 - Gas Utilities',
            'Title 16, Part 2 - Pipeline Safety',
            'Title 16, Part 3 - Gas Well Gas'
        ]
        
        rules = []
        
        async with aiohttp.ClientSession() as session:
            for title in gas_utility_titles:
                url = f"{self.sources['trc']['base_url']}/legal/rules/{title.lower().replace(' ', '-')}"
                
                try:
                    async with session.get(url) as response:
                        if response.status == 200:
                            html_content = await response.text()
                            parsed_rules = self._parse_trc_rules_html(html_content, title)
                            rules.extend(parsed_rules)
                except Exception as e:
                    print(f"Error scraping TRC rules for {title}: {e}")
        
        return rules
    
    async def monitor_texas_register(self) -> List[Dict[str, Any]]:
        """Monitor Texas Register for all agency rule changes"""
        
        # Texas Register publishes weekly
        recent_registers = []
        
        # Get last 4 weeks of registers
        for week_offset in range(4):
            register_date = datetime.now() - timedelta(weeks=week_offset)
            register_url = self._build_texas_register_url(register_date)
            
            try:
                register_content = await self._download_texas_register(register_url)
                gas_related_items = self._extract_gas_regulations(register_content)
                recent_registers.extend(gas_related_items)
            except Exception as e:
                print(f"Error processing Texas Register for {register_date}: {e}")
        
        return recent_registers
    
    def _extract_gas_regulations(self, register_content: str) -> List[Dict[str, Any]]:
        """Extract natural gas related regulations from Texas Register"""
        
        import re
        from bs4 import BeautifulSoup
        
        soup = BeautifulSoup(register_content, 'html.parser')
        regulations = []
        
        # Search for gas-related keywords in sections
        gas_keywords = [
            'natural gas', 'pipeline', 'lng', 'gas utility',
            'methane', 'compression', 'distribution', 'transmission'
        ]
        
        # Find all rule sections
        rule_sections = soup.find_all(['div', 'section'], 
                                    class_=re.compile(r'rule|proposed|adopted'))
        
        for section in rule_sections:
            section_text = section.get_text().lower()
            
            if any(keyword in section_text for keyword in gas_keywords):
                regulation = self._parse_texas_register_section(section)
                if regulation:
                    regulations.append(regulation)
        
        return regulations

# Multi-state monitoring system
class MultiStateMonitor:
    """Monitor multiple states for natural gas regulations"""
    
    def __init__(self):
        self.state_monitors = {
            'alabama': AlabamaRegulatoryMonitor(),
            'alaska': AlaskaRegulatoryMonitor(),
            'arizona': ArizonaRegulatoryMonitor(),
            'arkansas': ArkansasRegulatoryMonitor(),
            'california': CaliforniaRegulatoryMonitor(),
            'colorado': ColoradoRegulatoryMonitor(),
            'connecticut': ConnecticutRegulatoryMonitor(),
            'delaware': DelawareRegulatoryMonitor(),
            'florida': FloridaRegulatoryMonitor(),
            'georgia': GeorgiaRegulatoryMonitor(),
            'hawaii': HawaiiRegulatoryMonitor(),
            'idaho': IdahoRegulatoryMonitor(),
            'illinois': IllinoisRegulatoryMonitor(),
            'indiana': IndianaRegulatoryMonitor(),
            'iowa': IowaRegulatoryMonitor(),
            'kansas': KansasRegulatoryMonitor(),
            'kentucky': KentuckyRegulatoryMonitor(),
            'louisiana': LouisianaRegulatoryMonitor(),
            'maine': MaineRegulatoryMonitor(),
            'maryland': MarylandRegulatoryMonitor(),
            'massachusetts': MassachusettsRegulatoryMonitor(),
            'michigan': MichiganRegulatoryMonitor(),
            'minnesota': MinnesotaRegulatoryMonitor(),
            'mississippi': MississippiRegulatoryMonitor(),
            'missouri': MissouriRegulatoryMonitor(),
            'montana': MontanaRegulatoryMonitor(),
            'nebraska': NebraskaRegulatoryMonitor(),
            'nevada': NevadaRegulatoryMonitor(),
            'new_hampshire': NewHampshireRegulatoryMonitor(),
            'new_jersey': NewJerseyRegulatoryMonitor(),
            'new_mexico': NewMexicoRegulatoryMonitor(),
            'new_york': NewYorkRegulatoryMonitor(),
            'north_carolina': NorthCarolinaRegulatoryMonitor(),
            'north_dakota': NorthDakotaRegulatoryMonitor(),
            'ohio': OhioRegulatoryMonitor(),
            'oklahoma': OklahomaRegulatoryMonitor(),
            'oregon': OregonRegulatoryMonitor(),
            'pennsylvania': PennsylvaniaRegulatoryMonitor(),
            'rhode_island': RhodeIslandRegulatoryMonitor(),
            'south_carolina': SouthCarolinaRegulatoryMonitor(),
            'south_dakota': SouthDakotaRegulatoryMonitor(),
            'tennessee': TennesseeRegulatoryMonitor(),
            'texas': TexasRegulatoryMonitor(),
            'utah': UtahRegulatoryMonitor(),
            'vermont': VermontRegulatoryMonitor(),
            'virginia': VirginiaRegulatoryMonitor(),
            'washington': WashingtonRegulatoryMonitor(),
            'west_virginia': WestVirginiaRegulatoryMonitor(),
            'wisconsin': WisconsinRegulatoryMonitor(),
            'wyoming': WyomingRegulatoryMonitor()
        }
    
    async def monitor_all_states(self) -> Dict[str, List[Dict[str, Any]]]:
        """Monitor all configured states concurrently"""
        
        tasks = []
        for state, monitor in self.state_monitors.items():
            task = asyncio.create_task(
                monitor.get_all_regulations(),
                name=f"monitor_{state}"
            )
            tasks.append((state, task))
        
        results = {}
        
        for state, task in tasks:
            try:
                regulations = await task
                results[state] = regulations
            except Exception as e:
                print(f"Error monitoring {state}: {e}")
                results[state] = []
        
        return results
```

---

## **AI-Powered Document Analysis**

### **Legal Document Processing Pipeline**
```python
# regulatoriq/ai/document_analyzer.py
import spacy
from transformers import AutoTokenizer, AutoModel, pipeline
import torch
from typing import List, Dict, Any, Tuple
import re
from dataclasses import dataclass

@dataclass
class ComplianceRequirement:
    requirement_id: str
    description: str
    deadline: str
    applicability: List[str]
    severity: str
    citation: str
    implementation_guidance: str

class LegalDocumentAnalyzer:
    """AI-powered analysis of regulatory documents for natural gas industry"""
    
    def __init__(self):
        # Load legal-domain fine-tuned models
        self.tokenizer = AutoTokenizer.from_pretrained("nlpaueb/legal-bert-base-uncased")
        self.model = AutoModel.from_pretrained("nlpaueb/legal-bert-base-uncased")
        
        # Load spaCy for entity extraction
        self.nlp = spacy.load("en_core_web_lg")
        
        # Classification pipeline for regulation types
        self.classifier = pipeline(
            "text-classification",
            model="microsoft/DialoGPT-medium",
            tokenizer="microsoft/DialoGPT-medium"
        )
        
        # Load domain-specific patterns
        self.load_gas_industry_patterns()
    
    def load_gas_industry_patterns(self):
        """Load natural gas industry-specific patterns and terminology"""
        
        self.gas_entities = {
            'facilities': [
                'pipeline', 'compressor station', 'metering station',
                'lng terminal', 'storage facility', 'processing plant',
                'distribution system', 'transmission system'
            ],
            'regulations': [
                'part 192', 'part 193', 'part 199', 'part 40 cfr',
                'pipeline safety', 'integrity management',
                'operator qualifications', 'leak detection'
            ],
            'compliance_areas': [
                'safety management', 'environmental protection',
                'reporting requirements', 'inspection requirements',
                'maintenance standards', 'emergency procedures'
            ]
        }
        
        # Regex patterns for key information extraction
        self.patterns = {
            'effective_date': re.compile(r'effective\s+(?:date\s+)?(?:on\s+)?(\w+\s+\d{1,2},\s+\d{4})', re.I),
            'compliance_date': re.compile(r'comply\s+(?:with\s+)?(?:by\s+)?(\w+\s+\d{1,2},\s+\d{4})', re.I),
            'citation': re.compile(r'(\d+\s+CFR\s+\d+(?:\.\d+)?)|(\d+\s+U\.S\.C\s+\d+)', re.I),
            'penalty': re.compile(r'\$[\d,]+(?:\.\d{2})?(?:\s+(?:per\s+day|maximum|fine))?', re.I)
        }
    
    async def analyze_document(self, document: Dict[str, Any]) -> Dict[str, Any]:
        """Comprehensive analysis of a regulatory document"""
        
        content = document.get('content', '')
        doc_type = document.get('document_type', '')
        
        analysis_result = {
            'document_id': document['document_id'],
            'classification': await self._classify_document(content, doc_type),
            'entities': self._extract_entities(content),
            'compliance_requirements': await self._extract_compliance_requirements(content),
            'impact_assessment': await self._assess_impact(content, document),
            'timeline_analysis': self._extract_timeline(content),
            'affected_parties': self._identify_affected_parties(content),
            'summary': await self._generate_summary(content),
            'actionable_items': await self._extract_actionable_items(content),
            'related_regulations': await self._find_related_regulations(content),
            'confidence_score': 0.0
        }
        
        # Calculate overall confidence score
        analysis_result['confidence_score'] = self._calculate_confidence_score(analysis_result)
        
        return analysis_result
    
    async def _classify_document(self, content: str, doc_type: str) -> Dict[str, Any]:
        """Classify document type and regulatory area"""
        
        # Use BERT embeddings for document classification
        inputs = self.tokenizer(content[:512], return_tensors="pt", truncation=True)
        
        with torch.no_grad():
            outputs = self.model(**inputs)
            embeddings = outputs.last_hidden_state.mean(dim=1)
        
        # Classify into predefined categories
        categories = self._classify_regulatory_category(content)
        
        return {
            'primary_category': categories['primary'],
            'secondary_categories': categories['secondary'],
            'regulatory_type': self._determine_regulatory_type(doc_type, content),
            'urgency_level': self._assess_urgency(content),
            'scope': self._determine_scope(content)
        }
    
    def _extract_entities(self, content: str) -> Dict[str, List[str]]:
        """Extract domain-specific entities from document"""
        
        doc = self.nlp(content)
        entities = {
            'facilities': [],
            'regulations': [],
            'dates': [],
            'organizations': [],
            'locations': [],
            'monetary_amounts': []
        }
        
        # Extract standard NER entities
        for ent in doc.ents:
            if ent.label_ == "ORG":
                entities['organizations'].append(ent.text)
            elif ent.label_ == "GPE":
                entities['locations'].append(ent.text)
            elif ent.label_ == "DATE":
                entities['dates'].append(ent.text)
            elif ent.label_ == "MONEY":
                entities['monetary_amounts'].append(ent.text)
        
        # Extract domain-specific entities
        content_lower = content.lower()
        
        for facility in self.gas_entities['facilities']:
            if facility in content_lower:
                entities['facilities'].append(facility)
        
        for regulation in self.gas_entities['regulations']:
            if regulation in content_lower:
                entities['regulations'].append(regulation)
        
        return entities
    
    async def _extract_compliance_requirements(self, content: str) -> List[ComplianceRequirement]:
        """Extract specific compliance requirements from document"""
        
        requirements = []
        
        # Split document into sections
        sections = self._split_into_sections(content)
        
        for section in sections:
            # Look for requirement indicators
            requirement_indicators = [
                'shall', 'must', 'required to', 'operator shall',
                'company must', 'entity shall', 'person must'
            ]
            
            sentences = self._split_into_sentences(section)
            
            for sentence in sentences:
                if any(indicator in sentence.lower() for indicator in requirement_indicators):
                    requirement = await self._parse_requirement(sentence, section)
                    if requirement:
                        requirements.append(requirement)
        
        return requirements
    
    async def _parse_requirement(self, sentence: str, context: str) -> ComplianceRequirement:
        """Parse individual compliance requirement"""
        
        # Extract key components using patterns
        deadline_match = self.patterns['compliance_date'].search(context)
        deadline = deadline_match.group(1) if deadline_match else "Not specified"
        
        citation_match = self.patterns['citation'].search(context)
        citation = citation_match.group(0) if citation_match else "TBD"
        
        # Determine severity based on keywords
        severity_keywords = {
            'high': ['safety', 'emergency', 'immediate', 'critical'],
            'medium': ['reporting', 'maintenance', 'inspection'],
            'low': ['administrative', 'notification', 'documentation']
        }
        
        severity = 'medium'  # default
        sentence_lower = sentence.lower()
        
        for level, keywords in severity_keywords.items():
            if any(keyword in sentence_lower for keyword in keywords):
                severity = level
                break
        
        # Use GPT for implementation guidance
        guidance_prompt = f"Provide implementation guidance for this compliance requirement: {sentence}"
        implementation_guidance = await self._get_ai_guidance(guidance_prompt)
        
        return ComplianceRequirement(
            requirement_id=f"REQ_{hash(sentence) % 10000:04d}",
            description=sentence.strip(),
            deadline=deadline,
            applicability=self._determine_applicability(sentence),
            severity=severity,
            citation=citation,
            implementation_guidance=implementation_guidance
        )
    
    def _determine_applicability(self, requirement_text: str) -> List[str]:
        """Determine which entities the requirement applies to"""
        
        applicability_map = {
            'pipeline operator': ['operator', 'pipeline operator', 'transmission operator'],
            'gas utility': ['utility', 'gas utility', 'distribution company'],
            'lng facility': ['lng', 'liquefied natural gas', 'lng facility'],
            'storage operator': ['storage', 'underground storage', 'storage operator'],
            'all entities': ['person', 'entity', 'company', 'all operators']
        }
        
        applicable_to = []
        req_lower = requirement_text.lower()
        
        for category, keywords in applicability_map.items():
            if any(keyword in req_lower for keyword in keywords):
                applicable_to.append(category)
        
        return applicable_to if applicable_to else ['all entities']
    
    async def _assess_impact(self, content: str, document: Dict[str, Any]) -> Dict[str, Any]:
        """Assess potential business impact of regulation"""
        
        impact_factors = {
            'operational_changes': 0,
            'cost_implications': 0,
            'timeline_pressure': 0,
            'technical_complexity': 0,
            'compliance_risk': 0
        }
        
        # Analyze for operational impact keywords
        operational_keywords = [
            'modify procedures', 'new equipment', 'training required',
            'system changes', 'process updates', 'inspection frequency'
        ]
        
        content_lower = content.lower()
        
        for keyword in operational_keywords:
            if keyword in content_lower:
                impact_factors['operational_changes'] += 1
        
        # Look for cost indicators
        cost_patterns = self.patterns['penalty'].findall(content)
        if cost_patterns:
            impact_factors['cost_implications'] = len(cost_patterns)
        
        # Assess timeline pressure
        dates = self._extract_timeline(content)
        if dates:
            days_to_compliance = self._calculate_days_to_compliance(dates)
            if days_to_compliance < 90:
                impact_factors['timeline_pressure'] = 3
            elif days_to_compliance < 180:
                impact_factors['timeline_pressure'] = 2
            else:
                impact_factors['timeline_pressure'] = 1
        
        # Calculate overall impact score
        impact_score = sum(impact_factors.values()) / len(impact_factors)
        
        return {
            'impact_score': impact_score,
            'factors': impact_factors,
            'priority_level': self._categorize_priority(impact_score),
            'estimated_compliance_cost': self._estimate_compliance_cost(content),
            'implementation_complexity': self._assess_complexity(content)
        }
    
    async def _generate_summary(self, content: str) -> str:
        """Generate executive summary of regulation"""
        
        # Use extractive summarization for initial summary
        summary_sentences = self._extractive_summarization(content, max_sentences=3)
        
        # Enhance with GPT for better readability
        enhanced_summary = await self._enhance_summary_with_ai(summary_sentences)
        
        return enhanced_summary
    
    async def _enhance_summary_with_ai(self, initial_summary: str) -> str:
        """Use AI to enhance summary for business context"""
        
        prompt = f"""
        Rewrite this regulatory summary for natural gas industry executives:
        
        {initial_summary}
        
        Focus on:
        - Business impact
        - Key deadlines
        - Required actions
        - Compliance implications
        
        Keep it concise and actionable.
        """
        
        enhanced = await self._get_ai_response(prompt)
        return enhanced
    
    async def _get_ai_response(self, prompt: str) -> str:
        """Get response from AI service (GPT-4 or similar)"""
        
        # Implementation would call OpenAI API or similar
        # This is a placeholder for the actual implementation
        
        import openai
        
        try:
            response = await openai.ChatCompletion.acreate(
                model="gpt-4",
                messages=[
                    {"role": "system", "content": "You are a regulatory compliance expert for the natural gas industry."},
                    {"role": "user", "content": prompt}
                ],
                max_tokens=500,
                temperature=0.3
            )
            
            return response.choices[0].message.content.strip()
            
        except Exception as e:
            print(f"Error getting AI response: {e}")
            return "AI enhancement temporarily unavailable."

# Change impact analyzer
class ChangeImpactAnalyzer:
    """Analyze the impact of regulatory changes on existing compliance frameworks"""
    
    def __init__(self):
        self.compliance_framework_db = None
        self.impact_calculator = ImpactCalculator()
    
    async def analyze_change_impact(self, 
                                  new_regulation: Dict[str, Any],
                                  existing_framework: Dict[str, Any]) -> Dict[str, Any]:
        """Analyze how new regulation impacts existing compliance framework"""
        
        impact_analysis = {
            'affected_processes': [],
            'required_updates': [],
            'timeline_conflicts': [],
            'cost_impact': 0.0,
            'risk_assessment': {},
            'implementation_roadmap': []
        }
        
        # Compare new requirements with existing framework
        new_requirements = new_regulation.get('compliance_requirements', [])
        existing_requirements = existing_framework.get('requirements', [])
        
        for new_req in new_requirements:
            conflicts = self._find_requirement_conflicts(new_req, existing_requirements)
            if conflicts:
                impact_analysis['affected_processes'].extend(conflicts)
        
        # Analyze timeline implications
        timeline_analysis = await self._analyze_timeline_impact(new_regulation, existing_framework)
        impact_analysis['timeline_conflicts'] = timeline_analysis
        
        # Calculate cost implications
        cost_analysis = await self._calculate_cost_impact(new_regulation, existing_framework)
        impact_analysis['cost_impact'] = cost_analysis
        
        # Generate implementation roadmap
        roadmap = await self._generate_implementation_roadmap(impact_analysis)
        impact_analysis['implementation_roadmap'] = roadmap
        
        return impact_analysis
```

---

## **Database Schema & Data Management**

### **PostgreSQL Database Schema**
```sql
-- Core database schema for regulatory change management

-- Regulatory agencies and sources
CREATE TABLE regulatory_agencies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    abbreviation VARCHAR(10),
    agency_type VARCHAR(50), -- 'federal', 'state', 'local'
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
    document_id VARCHAR(100) UNIQUE, -- External document ID
    title TEXT NOT NULL,
    document_type VARCHAR(100), -- 'rule', 'order', 'notice', 'guidance'
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
    status VARCHAR(50) DEFAULT 'active', -- 'active', 'superseded', 'withdrawn'
    priority_score INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    -- Full text search
    content_search_vector tsvector GENERATED ALWAYS AS (
        to_tsvector('english', title || ' ' || COALESCE(processed_content, ''))
    ) STORED
);

-- Document analysis results
CREATE TABLE document_analyses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES regulatory_documents(id) ON DELETE CASCADE,
    analysis_version INTEGER DEFAULT 1,
    classification JSONB, -- Document classification results
    entities_extracted JSONB, -- Named entities found
    compliance_requirements JSONB, -- Structured compliance requirements
    impact_assessment JSONB, -- Business impact analysis
    timeline_analysis JSONB, -- Important dates and deadlines
    affected_parties TEXT[], -- Who this affects
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
    requirement_type VARCHAR(100), -- 'safety', 'reporting', 'maintenance', etc.
    applicability TEXT[], -- Who must comply
    deadline DATE,
    severity VARCHAR(20), -- 'high', 'medium', 'low'
    implementation_guidance TEXT,
    estimated_cost_impact DECIMAL(15,2),
    complexity_level INTEGER, -- 1-5 scale
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
    industry_segments TEXT[], -- 'transmission', 'distribution', 'lng', 'storage'
    geographic_scope TEXT[], -- States/regions covered
    framework_data JSONB, -- Complete framework structure
    last_updated TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Compliance framework mappings to regulations
CREATE TABLE framework_regulation_mappings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    framework_id UUID REFERENCES compliance_frameworks(id),
    document_id UUID REFERENCES regulatory_documents(id),
    requirement_id UUID REFERENCES compliance_requirements(id),
    mapping_type VARCHAR(50), -- 'direct', 'indirect', 'potential'
    compliance_status VARCHAR(50), -- 'compliant', 'non-compliant', 'needs_review'
    implementation_status VARCHAR(50), -- 'implemented', 'in_progress', 'planned'
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
    alert_type VARCHAR(100), -- 'new_regulation', 'deadline_approaching', 'impact_detected'
    document_id UUID REFERENCES regulatory_documents(id),
    framework_id UUID REFERENCES compliance_frameworks(id),
    severity VARCHAR(20),
    title VARCHAR(500),
    message TEXT,
    alert_data JSONB,
    status VARCHAR(50) DEFAULT 'active', -- 'active', 'acknowledged', 'resolved'
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    acknowledged_at TIMESTAMP WITH TIME ZONE,
    acknowledged_by VARCHAR(255)
);

-- Audit trail
CREATE TABLE regulatory_audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100), -- 'document', 'framework', 'requirement'
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

-- Full text search configuration
CREATE INDEX idx_documents_fulltext ON regulatory_documents USING GIN(content_search_vector);

-- Seed data: Federal agencies
INSERT INTO regulatory_agencies (name, abbreviation, agency_type, jurisdiction, website_url, monitoring_enabled) VALUES
('Federal Energy Regulatory Commission', 'FERC', 'federal', 'Federal', 'https://www.ferc.gov', true),
('Pipeline and Hazardous Materials Safety Administration', 'PHMSA', 'federal', 'Federal', 'https://www.phmsa.dot.gov', true),
('Environmental Protection Agency', 'EPA', 'federal', 'Federal', 'https://www.epa.gov', true),
('Department of Energy', 'DOE', 'federal', 'Federal', 'https://www.energy.gov', true);

-- Seed data: State regulatory agencies (one per state, all 50 states)
INSERT INTO regulatory_agencies (name, abbreviation, agency_type, jurisdiction, website_url, monitoring_enabled) VALUES
('Alabama Public Service Commission', 'ALPSC', 'state', 'Alabama', 'https://www.psc.state.al.us', true),
('Alaska Regulatory Commission of Alaska', 'RCA', 'state', 'Alaska', 'https://rca.alaska.gov', true),
('Arizona Corporation Commission', 'ACC', 'state', 'Arizona', 'https://azcc.gov', true),
('Arkansas Public Service Commission', 'ARPSC', 'state', 'Arkansas', 'https://www.apsc.arkansas.gov', true),
('California Public Utilities Commission', 'CAPUC', 'state', 'California', 'https://www.cpuc.ca.gov', true),
('Colorado Public Utilities Commission', 'COPUC', 'state', 'Colorado', 'https://puc.colorado.gov', true),
('Connecticut Public Utilities Regulatory Authority', 'PURA', 'state', 'Connecticut', 'https://portal.ct.gov/PURA', true),
('Delaware Public Service Commission', 'DPSC', 'state', 'Delaware', 'https://depsc.delaware.gov', true),
('Florida Public Service Commission', 'FPSC', 'state', 'Florida', 'https://www.floridapsc.com', true),
('Georgia Public Service Commission', 'GPSC', 'state', 'Georgia', 'https://psc.ga.gov', true),
('Hawaii Public Utilities Commission', 'HPUC', 'state', 'Hawaii', 'https://puc.hawaii.gov', true),
('Idaho Public Utilities Commission', 'IPUC', 'state', 'Idaho', 'https://puc.idaho.gov', true),
('Illinois Commerce Commission', 'ICC', 'state', 'Illinois', 'https://icc.illinois.gov', true),
('Indiana Utility Regulatory Commission', 'IURC', 'state', 'Indiana', 'https://www.in.gov/iurc', true),
('Iowa Utilities Board', 'IUB', 'state', 'Iowa', 'https://iub.iowa.gov', true),
('Kansas Corporation Commission', 'KCC', 'state', 'Kansas', 'https://kcc.ks.gov', true),
('Kentucky Public Service Commission', 'KPSC', 'state', 'Kentucky', 'https://psc.ky.gov', true),
('Louisiana Public Service Commission', 'LPSC', 'state', 'Louisiana', 'https://lpsc.louisiana.gov', true),
('Maine Public Utilities Commission', 'MEPUC', 'state', 'Maine', 'https://www.maine.gov/mpuc', true),
('Maryland Public Service Commission', 'MDPSC', 'state', 'Maryland', 'https://www.psc.state.md.us', true),
('Massachusetts Department of Public Utilities', 'MDPU', 'state', 'Massachusetts', 'https://www.mass.gov/orgs/department-of-public-utilities', true),
('Michigan Public Service Commission', 'MIPSC', 'state', 'Michigan', 'https://www.michigan.gov/mpsc', true),
('Minnesota Public Utilities Commission', 'MNPUC', 'state', 'Minnesota', 'https://mn.gov/puc', true),
('Mississippi Public Service Commission', 'MSPSC', 'state', 'Mississippi', 'https://www.psc.ms.gov', true),
('Missouri Public Service Commission', 'MOPSC', 'state', 'Missouri', 'https://psc.mo.gov', true),
('Montana Public Service Commission', 'MTPSC', 'state', 'Montana', 'https://psc.mt.gov', true),
('Nebraska Public Service Commission', 'NPSC', 'state', 'Nebraska', 'https://psc.nebraska.gov', true),
('Nevada Public Utilities Commission', 'NPUC', 'state', 'Nevada', 'https://puc.nv.gov', true),
('New Hampshire Public Utilities Commission', 'NHPUC', 'state', 'New Hampshire', 'https://www.puc.nh.gov', true),
('New Jersey Board of Public Utilities', 'NJBPU', 'state', 'New Jersey', 'https://www.nj.gov/bpu', true),
('New Mexico Public Regulation Commission', 'NMPRC', 'state', 'New Mexico', 'https://www.nmprc.state.nm.us', true),
('New York Public Service Commission', 'NYPSC', 'state', 'New York', 'https://www.dps.ny.gov', true),
('North Carolina Utilities Commission', 'NCUC', 'state', 'North Carolina', 'https://www.ncuc.net', true),
('North Dakota Public Service Commission', 'NDPSC', 'state', 'North Dakota', 'https://www.psc.nd.gov', true),
('Public Utilities Commission of Ohio', 'PUCO', 'state', 'Ohio', 'https://puco.ohio.gov', true),
('Oklahoma Corporation Commission', 'OCC', 'state', 'Oklahoma', 'https://www.occeweb.com', true),
('Oregon Public Utility Commission', 'OPUC', 'state', 'Oregon', 'https://www.oregon.gov/puc', true),
('Pennsylvania Public Utility Commission', 'PAPUC', 'state', 'Pennsylvania', 'https://www.puc.pa.gov', true),
('Rhode Island Public Utilities Commission', 'RIPUC', 'state', 'Rhode Island', 'https://www.ripuc.ri.gov', true),
('South Carolina Public Service Commission', 'SCPSC', 'state', 'South Carolina', 'https://psc.sc.gov', true),
('South Dakota Public Utilities Commission', 'SDPUC', 'state', 'South Dakota', 'https://puc.sd.gov', true),
('Tennessee Regulatory Authority', 'TRA', 'state', 'Tennessee', 'https://www.tn.gov/tra', true),
-- Texas has two primary regulatory bodies relevant to natural gas: TRC (production/pipelines) and TCEQ (environmental)
('Texas Railroad Commission', 'TRC', 'state', 'Texas', 'https://www.rrc.texas.gov', true),
('Texas Commission on Environmental Quality', 'TCEQ', 'state', 'Texas', 'https://www.tceq.texas.gov', true),
('Utah Public Service Commission', 'UPSC', 'state', 'Utah', 'https://psc.utah.gov', true),
('Vermont Public Utility Commission', 'VPUC', 'state', 'Vermont', 'https://puc.vermont.gov', true),
('Virginia State Corporation Commission', 'VASCC', 'state', 'Virginia', 'https://www.scc.virginia.gov', true),
('Washington Utilities and Transportation Commission', 'WUTC', 'state', 'Washington', 'https://www.utc.wa.gov', true),
('West Virginia Public Service Commission', 'WVPSC', 'state', 'West Virginia', 'https://psc.wv.gov', true),
('Public Service Commission of Wisconsin', 'PSCW', 'state', 'Wisconsin', 'https://psc.wi.gov', true),
('Wyoming Public Service Commission', 'WYPSC', 'state', 'Wyoming', 'https://psc.wyo.gov', true);
```

### **Entity Framework Core Models**
```csharp
// Models/RegulatoryDocument.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegulatorIQ.Models
{
    public class RegulatoryDocument
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid AgencyId { get; set; }
        public RegulatoryAgency Agency { get; set; }
        
        [MaxLength(100)]
        public string DocumentId { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        [MaxLength(100)]
        public string DocumentType { get; set; }
        
        public DateTime? PublicationDate { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? CommentDeadline { get; set; }
        public DateTime? ComplianceDate { get; set; }
        
        [MaxLength(1000)]
        public string SourceUrl { get; set; }
        
        [MaxLength(1000)]
        public string PdfUrl { get; set; }
        
        public string RawContent { get; set; }
        public string ProcessedContent { get; set; }
        
        [MaxLength(100)]
        public string DocketNumber { get; set; }
        
        [MaxLength(50)]
        public string FederalRegisterNumber { get; set; }
        
        [MaxLength(100)]
        public string CfrCitation { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = "active";
        
        public int PriorityScore { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<DocumentAnalysis> Analyses { get; set; }
        public ICollection<ComplianceRequirement> ComplianceRequirements { get; set; }
        public ICollection<ChangeImpactAssessment> ImpactAssessments { get; set; }
    }

    public class DocumentAnalysis
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid DocumentId { get; set; }
        public RegulatoryDocument Document { get; set; }
        
        public int AnalysisVersion { get; set; } = 1;
        
        [Column(TypeName = "jsonb")]
        public string Classification { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string EntitiesExtracted { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string ComplianceRequirements { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string ImpactAssessment { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string TimelineAnalysis { get; set; }
        
        public string[] AffectedParties { get; set; }
        
        public string Summary { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string ActionableItems { get; set; }
        
        public string[] RelatedRegulations { get; set; }
        
        [Column(TypeName = "decimal(3,2)")]
        public decimal ConfidenceScore { get; set; }
        
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(20)]
        public string AnalyzerVersion { get; set; }
    }

    public class ComplianceFramework
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid CompanyId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string FrameworkName { get; set; }
        
        [MaxLength(20)]
        public string FrameworkVersion { get; set; }
        
        public string Description { get; set; }
        
        public string[] IndustrySegments { get; set; }
        public string[] GeographicScope { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string FrameworkData { get; set; }
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<FrameworkRegulationMapping> RegulationMappings { get; set; }
        public ICollection<ChangeImpactAssessment> ImpactAssessments { get; set; }
    }
}

// Data/RegulatorIQContext.cs
using Microsoft.EntityFrameworkCore;
using RegulatorIQ.Models;

namespace RegulatorIQ.Data
{
    public class RegulatorIQContext : DbContext
    {
        public RegulatorIQContext(DbContextOptions<RegulatorIQContext> options) : base(options)
        {
        }

        public DbSet<RegulatoryAgency> RegulatoryAgencies { get; set; }
        public DbSet<RegulatoryDocument> RegulatoryDocuments { get; set; }
        public DbSet<DocumentAnalysis> DocumentAnalyses { get; set; }
        public DbSet<ComplianceRequirement> ComplianceRequirements { get; set; }
        public DbSet<ComplianceFramework> ComplianceFrameworks { get; set; }
        public DbSet<FrameworkRegulationMapping> FrameworkRegulationMappings { get; set; }
        public DbSet<ChangeImpactAssessment> ChangeImpactAssessments { get; set; }
        public DbSet<RegulatoryAlert> RegulatoryAlerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure PostgreSQL-specific features
            modelBuilder.HasPostgresExtension("uuid-ossp");
            
            // Configure indexes
            modelBuilder.Entity<RegulatoryDocument>()
                .HasIndex(d => new { d.AgencyId, d.PublicationDate })
                .HasDatabaseName("idx_documents_agency_date");
                
            modelBuilder.Entity<RegulatoryDocument>()
                .HasIndex(d => d.EffectiveDate)
                .HasDatabaseName("idx_documents_effective_date")
                .HasFilter("effective_date IS NOT NULL");
                
            // Configure JSON columns
            modelBuilder.Entity<DocumentAnalysis>()
                .Property(da => da.Classification)
                .HasColumnType("jsonb");
                
            modelBuilder.Entity<ComplianceFramework>()
                .Property(cf => cf.FrameworkData)
                .HasColumnType("jsonb");
                
            // Configure relationships
            modelBuilder.Entity<RegulatoryDocument>()
                .HasOne(d => d.Agency)
                .WithMany()
                .HasForeignKey(d => d.AgencyId);
                
            modelBuilder.Entity<DocumentAnalysis>()
                .HasOne(da => da.Document)
                .WithMany(d => d.Analyses)
                .HasForeignKey(da => da.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
                
            base.OnModelCreating(modelBuilder);
        }
    }
}
```

---

## **RESTful API & Services**

### **ASP.NET Core API Controllers**
```csharp
// Controllers/RegulatoryDocumentsController.cs
using Microsoft.AspNetCore.Mvc;
using RegulatorIQ.Services;
using RegulatorIQ.Models;
using RegulatorIQ.DTOs;

namespace RegulatorIQ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegulatoryDocumentsController : ControllerBase
    {
        private readonly IRegulatoryDocumentService _documentService;
        private readonly IDocumentAnalysisService _analysisService;
        private readonly ILogger<RegulatoryDocumentsController> _logger;

        public RegulatoryDocumentsController(
            IRegulatoryDocumentService documentService,
            IDocumentAnalysisService analysisService,
            ILogger<RegulatoryDocumentsController> logger)
        {
            _documentService = documentService;
            _analysisService = analysisService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<RegulatoryDocumentDto>>> GetDocuments(
            [FromQuery] DocumentSearchRequest request)
        {
            try
            {
                var result = await _documentService.SearchDocumentsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving regulatory documents");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RegulatoryDocumentDetailDto>> GetDocument(Guid id)
        {
            try
            {
                var document = await _documentService.GetDocumentByIdAsync(id);
                if (document == null)
                {
                    return NotFound($"Document with ID {id} not found");
                }

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/analyze")]
        public async Task<ActionResult<DocumentAnalysisDto>> AnalyzeDocument(Guid id)
        {
            try
            {
                var analysis = await _analysisService.AnalyzeDocumentAsync(id);
                if (analysis == null)
                {
                    return NotFound($"Document with ID {id} not found");
                }

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing document {DocumentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<RegulatoryDocumentDto>>> SearchDocuments(
            [FromQuery] string query,
            [FromQuery] DocumentFilter filter)
        {
            try
            {
                var results = await _documentService.FullTextSearchAsync(query, filter);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents with query: {Query}", query);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("alerts")]
        public async Task<ActionResult<List<RegulatoryAlertDto>>> GetAlerts(
            [FromQuery] AlertFilter filter)
        {
            try
            {
                var alerts = await _documentService.GetRegulatoryAlertsAsync(filter);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving regulatory alerts");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("bulk-analyze")]
        public async Task<ActionResult<BulkAnalysisResult>> BulkAnalyzeDocuments(
            [FromBody] BulkAnalysisRequest request)
        {
            try
            {
                var result = await _analysisService.BulkAnalyzeAsync(request.DocumentIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk analysis");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ComplianceFrameworksController : ControllerBase
    {
        private readonly IComplianceFrameworkService _frameworkService;
        private readonly IChangeImpactService _impactService;
        private readonly ILogger<ComplianceFrameworksController> _logger;

        public ComplianceFrameworksController(
            IComplianceFrameworkService frameworkService,
            IChangeImpactService impactService,
            ILogger<ComplianceFrameworksController> logger)
        {
            _frameworkService = frameworkService;
            _impactService = impactService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<ComplianceFrameworkDto>>> GetFrameworks(
            [FromQuery] Guid companyId)
        {
            try
            {
                var frameworks = await _frameworkService.GetFrameworksByCompanyAsync(companyId);
                return Ok(frameworks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving frameworks for company {CompanyId}", companyId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ComplianceFrameworkDto>> CreateFramework(
            [FromBody] CreateFrameworkRequest request)
        {
            try
            {
                var framework = await _frameworkService.CreateFrameworkAsync(request);
                return CreatedAtAction(nameof(GetFramework), new { id = framework.Id }, framework);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating compliance framework");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ComplianceFrameworkDetailDto>> GetFramework(Guid id)
        {
            try
            {
                var framework = await _frameworkService.GetFrameworkByIdAsync(id);
                if (framework == null)
                {
                    return NotFound($"Framework with ID {id} not found");
                }

                return Ok(framework);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving framework {FrameworkId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/impact-assessment")]
        public async Task<ActionResult<ChangeImpactAssessmentDto>> AssessImpact(
            Guid id,
            [FromBody] ImpactAssessmentRequest request)
        {
            try
            {
                var assessment = await _impactService.AssessChangeImpactAsync(id, request.DocumentId);
                return Ok(assessment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assessing impact for framework {FrameworkId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ComplianceFrameworkDto>> UpdateFramework(
            Guid id,
            [FromBody] UpdateFrameworkRequest request)
        {
            try
            {
                var framework = await _frameworkService.UpdateFrameworkAsync(id, request);
                if (framework == null)
                {
                    return NotFound($"Framework with ID {id} not found");
                }

                return Ok(framework);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating framework {FrameworkId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/sync")]
        public async Task<ActionResult<FrameworkSyncResult>> SyncFramework(Guid id)
        {
            try
            {
                var result = await _frameworkService.SyncWithLatestRegulationsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing framework {FrameworkId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

// Services/RegulatoryDocumentService.cs
using RegulatorIQ.Data;
using RegulatorIQ.Models;
using RegulatorIQ.DTOs;
using Microsoft.EntityFrameworkCore;

namespace RegulatorIQ.Services
{
    public interface IRegulatoryDocumentService
    {
        Task<PagedResult<RegulatoryDocumentDto>> SearchDocumentsAsync(DocumentSearchRequest request);
        Task<RegulatoryDocumentDetailDto> GetDocumentByIdAsync(Guid id);
        Task<List<RegulatoryDocumentDto>> FullTextSearchAsync(string query, DocumentFilter filter);
        Task<List<RegulatoryAlertDto>> GetRegulatoryAlertsAsync(AlertFilter filter);
        Task<RegulatoryDocument> CreateDocumentAsync(CreateDocumentRequest request);
        Task UpdateDocumentAsync(Guid id, UpdateDocumentRequest request);
    }

    public class RegulatoryDocumentService : IRegulatoryDocumentService
    {
        private readonly RegulatorIQContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<RegulatoryDocumentService> _logger;

        public RegulatoryDocumentService(
            RegulatorIQContext context,
            IMapper mapper,
            ILogger<RegulatoryDocumentService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<RegulatoryDocumentDto>> SearchDocumentsAsync(
            DocumentSearchRequest request)
        {
            var query = _context.RegulatoryDocuments
                .Include(d => d.Agency)
                .AsQueryable();

            // Apply filters
            if (request.AgencyIds?.Any() == true)
            {
                query = query.Where(d => request.AgencyIds.Contains(d.AgencyId));
            }

            if (request.DocumentTypes?.Any() == true)
            {
                query = query.Where(d => request.DocumentTypes.Contains(d.DocumentType));
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(d => d.PublicationDate >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(d => d.PublicationDate <= request.ToDate.Value);
            }

            if (request.MinPriority.HasValue)
            {
                query = query.Where(d => d.PriorityScore >= request.MinPriority.Value);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                // Use PostgreSQL full-text search
                query = query.Where(d => 
                    EF.Functions.ToTsVector("english", d.Title + " " + d.ProcessedContent)
                    .Matches(EF.Functions.ToTsQuery("english", request.SearchTerm)));
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "date" => request.SortDesc ? 
                    query.OrderByDescending(d => d.PublicationDate) :
                    query.OrderBy(d => d.PublicationDate),
                "priority" => request.SortDesc ?
                    query.OrderByDescending(d => d.PriorityScore) :
                    query.OrderBy(d => d.PriorityScore),
                _ => query.OrderByDescending(d => d.PublicationDate)
            };

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var documents = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var documentDtos = _mapper.Map<List<RegulatoryDocumentDto>>(documents);

            return new PagedResult<RegulatoryDocumentDto>
            {
                Items = documentDtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }

        public async Task<List<RegulatoryDocumentDto>> FullTextSearchAsync(
            string query, 
            DocumentFilter filter)
        {
            var searchQuery = _context.RegulatoryDocuments
                .Include(d => d.Agency)
                .Where(d => 
                    EF.Functions.ToTsVector("english", d.Title + " " + d.ProcessedContent)
                    .Matches(EF.Functions.ToTsQuery("english", query)));

            // Apply additional filters
            if (filter.AgencyIds?.Any() == true)
            {
                searchQuery = searchQuery.Where(d => filter.AgencyIds.Contains(d.AgencyId));
            }

            if (filter.EffectiveDateFrom.HasValue)
            {
                searchQuery = searchQuery.Where(d => d.EffectiveDate >= filter.EffectiveDateFrom);
            }

            if (filter.EffectiveDateTo.HasValue)
            {
                searchQuery = searchQuery.Where(d => d.EffectiveDate <= filter.EffectiveDateTo);
            }

            // Order by relevance and date
            var results = await searchQuery
                .OrderByDescending(d => 
                    EF.Functions.ToTsVector("english", d.Title + " " + d.ProcessedContent)
                    .Rank(EF.Functions.ToTsQuery("english", query)))
                .ThenByDescending(d => d.PublicationDate)
                .Take(100)
                .ToListAsync();

            return _mapper.Map<List<RegulatoryDocumentDto>>(results);
        }

        public async Task<List<RegulatoryAlertDto>> GetRegulatoryAlertsAsync(AlertFilter filter)
        {
            var query = _context.RegulatoryAlerts
                .Include(a => a.Document)
                .ThenInclude(d => d.Agency)
                .AsQueryable();

            if (filter.CompanyId.HasValue)
            {
                // Filter alerts relevant to company's frameworks
                query = query.Where(a => 
                    _context.ComplianceFrameworks
                        .Where(f => f.CompanyId == filter.CompanyId.Value)
                        .SelectMany(f => f.RegulationMappings)
                        .Select(m => m.DocumentId)
                        .Contains(a.DocumentId.Value));
            }

            if (filter.Severity?.Any() == true)
            {
                query = query.Where(a => filter.Severity.Contains(a.Severity));
            }

            if (filter.Status?.Any() == true)
            {
                query = query.Where(a => filter.Status.Contains(a.Status));
            }

            if (filter.AlertTypes?.Any() == true)
            {
                query = query.Where(a => filter.AlertTypes.Contains(a.AlertType));
            }

            var alerts = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(100)
                .ToListAsync();

            return _mapper.Map<List<RegulatoryAlertDto>>(alerts);
        }
    }
}
```

---

## **React Frontend Implementation**

### **Main Dashboard Components**
```typescript
// src/components/Dashboard/RegulatoryDashboard.tsx
import React, { useState, useEffect } from 'react';
import {
  Grid, Card, CardContent, Typography, Box, Chip, Button,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Paper, IconButton, Tooltip, Alert, CircularProgress
} from '@mui/material';
import {
  Visibility, GetApp, Warning, CheckCircle, Schedule,
  TrendingUp, Notifications, Assessment
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { regulatoryApi } from '../../services/api';
import { RegulatoryDocument, ComplianceFramework, RegulatoryAlert } from '../../types';

interface DashboardStats {
  newRegulations: number;
  pendingDeadlines: number;
  impactAssessments: number;
  complianceGaps: number;
}

const RegulatoryDashboard: React.FC = () => {
  const [timeframe, setTimeframe] = useState<'week' | 'month' | 'quarter'>('month');
  const queryClient = useQueryClient();

  // Fetch dashboard statistics
  const { data: stats, isLoading: statsLoading } = useQuery({
    queryKey: ['dashboard-stats', timeframe],
    queryFn: () => regulatoryApi.getDashboardStats(timeframe)
  });

  // Fetch recent regulatory documents
  const { data: recentDocuments, isLoading: documentsLoading } = useQuery({
    queryKey: ['recent-documents'],
    queryFn: () => regulatoryApi.getRecentDocuments(20)
  });

  // Fetch active alerts
  const { data: alerts, isLoading: alertsLoading } = useQuery({
    queryKey: ['regulatory-alerts'],
    queryFn: () => regulatoryApi.getActiveAlerts()
  });

  // Fetch compliance frameworks
  const { data: frameworks, isLoading: frameworksLoading } = useQuery({
    queryKey: ['compliance-frameworks'],
    queryFn: () => regulatoryApi.getComplianceFrameworks()
  });

  const handleAnalyzeDocument = useMutation({
    mutationFn: (documentId: string) => regulatoryApi.analyzeDocument(documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['recent-documents'] });
    }
  });

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Regulatory Intelligence Dashboard
      </Typography>

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
                    {statsLoading ? <CircularProgress size={24} /> : stats?.newRegulations || 0}
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
                    {statsLoading ? <CircularProgress size={24} /> : stats?.pendingDeadlines || 0}
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
                    {statsLoading ? <CircularProgress size={24} /> : stats?.impactAssessments || 0}
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
                    {statsLoading ? <CircularProgress size={24} /> : stats?.complianceGaps || 0}
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

      {/* Alerts Section */}
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
                      severity={alert.severity as any}
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
                    <Typography color="textSecondary">
                      No active alerts
                    </Typography>
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
                    <Box key={framework.id} sx={{ mb: 2, p: 2, border: 1, borderColor: 'grey.300', borderRadius: 1 }}>
                      <Box display="flex" justifyContent="space-between" alignItems="center">
                        <Typography variant="subtitle1">{framework.frameworkName}</Typography>
                        <Chip 
                          label={framework.status}
                          color={framework.status === 'up-to-date' ? 'success' : 'warning'}
                          size="small"
                        />
                      </Box>
                      <Typography variant="body2" color="textSecondary">
                        Last updated: {new Date(framework.lastUpdated).toLocaleDateString()}
                      </Typography>
                    </Box>
                  ))}
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
            <Button variant="outlined" href="/documents">
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
                        {doc.publicationDate ? new Date(doc.publicationDate).toLocaleDateString() : 'N/A'}
                      </TableCell>
                      <TableCell>
                        {doc.effectiveDate ? new Date(doc.effectiveDate).toLocaleDateString() : 'N/A'}
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
                            disabled={handleAnalyzeDocument.isLoading}
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

// Helper Components
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
  const getStatusProps = (status?: string) => {
    switch (status) {
      case 'completed':
        return { label: 'Analyzed', color: 'success' as const, icon: <CheckCircle /> };
      case 'in_progress':
        return { label: 'Processing', color: 'info' as const, icon: <CircularProgress size={16} /> };
      case 'failed':
        return { label: 'Failed', color: 'error' as const, icon: <Warning /> };
      default:
        return { label: 'Pending', color: 'default' as const };
    }
  };

  const props = getStatusProps(status);
  return <Chip {...props} size="small" />;
};

export default RegulatoryDashboard;
```

### **Document Analysis Interface**
```typescript
// src/components/Documents/DocumentAnalysisView.tsx
import React, { useState } from 'react';
import {
  Box, Grid, Card, CardContent, Typography, Chip, Button,
  Accordion, AccordionSummary, AccordionDetails, List, ListItem,
  ListItemText, ListItemIcon, Divider, Alert, Paper, LinearProgress
} from '@mui/material';
import {
  ExpandMore, Warning, Info, CheckCircle, Schedule,
  Assessment, Timeline, Business, Download
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { regulatoryApi } from '../../services/api';
import { DocumentAnalysis, ComplianceRequirement } from '../../types';

interface DocumentAnalysisViewProps {
  documentId: string;
}

const DocumentAnalysisView: React.FC<DocumentAnalysisViewProps> = ({ documentId }) => {
  const [expandedSection, setExpandedSection] = useState<string | false>('summary');

  const { data: analysis, isLoading, error } = useQuery({
    queryKey: ['document-analysis', documentId],
    queryFn: () => regulatoryApi.getDocumentAnalysis(documentId),
    enabled: !!documentId
  });

  const { data: document } = useQuery({
    queryKey: ['document', documentId],
    queryFn: () => regulatoryApi.getDocument(documentId),
    enabled: !!documentId
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
      <Alert severity="error">
        Failed to load document analysis. Please try again.
      </Alert>
    );
  }

  const handleSectionChange = (panel: string) => (
    event: React.SyntheticEvent,
    isExpanded: boolean
  ) => {
    setExpandedSection(isExpanded ? panel : false);
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
              <Box sx={{ display: 'flex', gap: 1, mt: 1 }}>
                <Chip label={`Priority: ${analysis.classification?.urgencyLevel || 'Medium'}`} 
                      color={getPriorityColor(analysis.classification?.urgencyLevel)} />
                <Chip label={`Confidence: ${(analysis.confidenceScore * 100).toFixed(0)}%`} />
                <Chip label={analysis.classification?.primaryCategory} variant="outlined" />
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
                  {analysis.impactAssessment?.impactScore?.toFixed(1) || 'N/A'}
                </Typography>
                <Typography variant="body2">Impact Score</Typography>
              </Paper>
            </Grid>
            <Grid item xs={12} md={4}>
              <Paper sx={{ p: 2, textAlign: 'center' }}>
                <Typography variant="h4" color="warning.main">
                  {analysis.complianceRequirements?.length || 0}
                </Typography>
                <Typography variant="body2">Requirements</Typography>
              </Paper>
            </Grid>
            <Grid item xs={12} md={4}>
              <Paper sx={{ p: 2, textAlign: 'center' }}>
                <Typography variant="h4" color="info.main">
                  {analysis.timelineAnalysis?.criticalDates?.length || 0}
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
            Compliance Requirements ({analysis.complianceRequirements?.length || 0})
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <List>
            {analysis.complianceRequirements?.map((requirement: ComplianceRequirement, index: number) => (
              <React.Fragment key={requirement.requirementId}>
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
                        <Box sx={{ display: 'flex', gap: 1, mt: 1 }}>
                          <Chip label={requirement.severity} 
                                color={getSeverityColor(requirement.severity)} 
                                size="small" />
                          <Chip label={`Due: ${requirement.deadline}`} 
                                variant="outlined" 
                                size="small" />
                          {requirement.applicability.map((app, idx) => (
                            <Chip key={idx} label={app} variant="outlined" size="small" />
                          ))}
                        </Box>
                      </Box>
                    }
                    secondary={
                      <Typography variant="body2" sx={{ mt: 1 }}>
                        <strong>Implementation Guidance:</strong> {requirement.implementationGuidance}
                      </Typography>
                    }
                  />
                </ListItem>
                {index < (analysis.complianceRequirements?.length || 0) - 1 && <Divider />}
              </React.Fragment>
            ))}
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
          <TimelineChart timelineData={analysis.timelineAnalysis} />
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
              <ImpactFactorsChart factors={analysis.impactAssessment?.factors} />
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
                  Implementation Complexity
                </Typography>
                <ComplexityIndicator level={analysis.impactAssessment?.implementationComplexity} />
                
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
            Recommended Actions
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          <ActionableItemsList items={analysis.actionableItems} />
        </AccordionDetails>
      </Accordion>
    </Box>
  );
};

// Helper functions and components
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

const getSeverityColor = (severity: string): 'error' | 'warning' | 'info' => {
  switch (severity?.toLowerCase()) {
    case 'high':
      return 'error';
    case 'medium':
      return 'warning';
    case 'low':
      return 'info';
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
    maximumFractionDigits: 0
  }).format(amount);
};

export default DocumentAnalysisView;
```

---

## **Background Processing & Automation**

### **Hangfire Background Services**
```csharp
// Services/BackgroundServices/RegulatoryMonitoringService.cs
using Hangfire;
using RegulatorIQ.Data;
using RegulatorIQ.Services;
using Microsoft.EntityFrameworkCore;

namespace RegulatorIQ.Services.BackgroundServices
{
    public interface IRegulatoryMonitoringService
    {
        Task MonitorFederalRegulationsAsync();
        Task MonitorStateRegulationsAsync();
        Task ProcessPendingDocumentsAsync();
        Task GenerateComplianceAlertsAsync();
        Task UpdateComplianceFrameworksAsync();
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public class RegulatoryMonitoringService : IRegulatoryMonitoringService
    {
        private readonly RegulatorIQContext _context;
        private readonly IFederalRegulatoryMonitor _federalMonitor;
        private readonly IStateRegulatoryMonitor _stateMonitor;
        private readonly IDocumentAnalysisService _analysisService;
        private readonly IComplianceAlertService _alertService;
        private readonly ILogger<RegulatoryMonitoringService> _logger;

        public RegulatoryMonitoringService(
            RegulatorIQContext context,
            IFederalRegulatoryMonitor federalMonitor,
            IStateRegulatoryMonitor stateMonitor,
            IDocumentAnalysisService analysisService,
            IComplianceAlertService alertService,
            ILogger<RegulatoryMonitoringService> logger)
        {
            _context = context;
            _federalMonitor = federalMonitor;
            _stateMonitor = stateMonitor;
            _analysisService = analysisService;
            _alertService = alertService;
            _logger = logger;
        }

        [RecurringJob("federal-monitoring", "0 */4 * * *")] // Every 4 hours
        public async Task MonitorFederalRegulationsAsync()
        {
            _logger.LogInformation("Starting federal regulatory monitoring");
            
            try
            {
                // Monitor FERC
                var fercDocuments = await _federalMonitor.MonitorFERCFilingsAsync();
                await ProcessNewDocuments(fercDocuments, "FERC");

                // Monitor Federal Register
                var federalRegisterDocs = await _federalMonitor.MonitorFederalRegisterAsync();
                await ProcessNewDocuments(federalRegisterDocs, "Federal Register");

                // Monitor DOE
                var doeDocuments = await _federalMonitor.MonitorDOERegulationsAsync();
                await ProcessNewDocuments(doeDocuments, "DOE");

                // Monitor EPA
                var epaDocuments = await _federalMonitor.MonitorEPARegulationsAsync();
                await ProcessNewDocuments(epaDocuments, "EPA");

                // Monitor PHMSA
                var phmsaDocuments = await _federalMonitor.MonitorPHMSARegulationsAsync();
                await ProcessNewDocuments(phmsaDocuments, "PHMSA");

                _logger.LogInformation("Federal regulatory monitoring completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during federal regulatory monitoring");
                throw;
            }
        }

        [RecurringJob("state-monitoring", "0 2,14 * * *")] // 2 AM and 2 PM
        public async Task MonitorStateRegulationsAsync()
        {
            _logger.LogInformation("Starting state regulatory monitoring");

            try
            {
                var states = new[]
                {
                    "Alabama", "Alaska", "Arizona", "Arkansas", "California",
                    "Colorado", "Connecticut", "Delaware", "Florida", "Georgia",
                    "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa",
                    "Kansas", "Kentucky", "Louisiana", "Maine", "Maryland",
                    "Massachusetts", "Michigan", "Minnesota", "Mississippi", "Missouri",
                    "Montana", "Nebraska", "Nevada", "New Hampshire", "New Jersey",
                    "New Mexico", "New York", "North Carolina", "North Dakota", "Ohio",
                    "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island", "South Carolina",
                    "South Dakota", "Tennessee", "Texas", "Utah", "Vermont",
                    "Virginia", "Washington", "West Virginia", "Wisconsin", "Wyoming"
                };
                
                foreach (var state in states)
                {
                    _logger.LogInformation("Monitoring {State} regulations", state);
                    
                    var stateDocuments = await _stateMonitor.MonitorStateRegulationsAsync(state);
                    await ProcessNewDocuments(stateDocuments, $"{state} Regulatory");
                }

                _logger.LogInformation("State regulatory monitoring completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during state regulatory monitoring");
                throw;
            }
        }

        [RecurringJob("document-processing", "*/15 * * * *")] // Every 15 minutes
        public async Task ProcessPendingDocumentsAsync()
        {
            _logger.LogInformation("Processing pending documents");

            try
            {
                // Get documents that need analysis
                var pendingDocuments = await _context.RegulatoryDocuments
                    .Where(d => !_context.DocumentAnalyses.Any(a => a.DocumentId == d.Id))
                    .OrderByDescending(d => d.PriorityScore)
                    .ThenBy(d => d.CreatedAt)
                    .Take(10) // Process up to 10 at a time
                    .ToListAsync();

                foreach (var document in pendingDocuments)
                {
                    try
                    {
                        _logger.LogInformation("Analyzing document {DocumentId}: {Title}", 
                                             document.Id, document.Title);

                        // Queue document for AI analysis
                        BackgroundJob.Enqueue<IDocumentAnalysisService>(
                            service => service.AnalyzeDocumentAsync(document.Id));

                        // Add small delay between documents
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing document {DocumentId}", document.Id);
                    }
                }

                _logger.LogInformation("Document processing completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during document processing");
                throw;
            }
        }

        [RecurringJob("compliance-alerts", "0 8,16 * * *")] // 8 AM and 4 PM
        public async Task GenerateComplianceAlertsAsync()
        {
            _logger.LogInformation("Generating compliance alerts");

            try
            {
                // Check for approaching deadlines
                await CheckApproachingDeadlinesAsync();

                // Check for high-impact new regulations
                await CheckHighImpactRegulationsAsync();

                // Check for compliance gaps
                await CheckComplianceGapsAsync();

                // Send alert notifications
                await _alertService.ProcessPendingNotificationsAsync();

                _logger.LogInformation("Compliance alert generation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during compliance alert generation");
                throw;
            }
        }

        [RecurringJob("framework-updates", "0 3 * * *")] // 3 AM daily
        public async Task UpdateComplianceFrameworksAsync()
        {
            _logger.LogInformation("Updating compliance frameworks");

            try
            {
                var frameworks = await _context.ComplianceFrameworks
                    .Where(f => f.LastUpdated < DateTime.UtcNow.AddDays(-1))
                    .ToListAsync();

                foreach (var framework in frameworks)
                {
                    try
                    {
                        await UpdateFrameworkWithNewRegulations(framework);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating framework {FrameworkId}", framework.Id);
                    }
                }

                _logger.LogInformation("Framework updates completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during framework updates");
                throw;
            }
        }

        private async Task ProcessNewDocuments(
            List<Dictionary<string, object>> documents, 
            string source)
        {
            foreach (var docData in documents)
            {
                try
                {
                    var documentId = docData["document_id"]?.ToString();
                    
                    // Check if document already exists
                    var existingDoc = await _context.RegulatoryDocuments
                        .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                    if (existingDoc == null)
                    {
                        // Create new document
                        var newDocument = new RegulatoryDocument
                        {
                            DocumentId = documentId,
                            Title = docData["title"]?.ToString(),
                            DocumentType = docData["document_type"]?.ToString(),
                            PublicationDate = ParseDate(docData["publication_date"]),
                            EffectiveDate = ParseDate(docData["effective_date"]),
                            SourceUrl = docData["url"]?.ToString(),
                            PdfUrl = docData["pdf_url"]?.ToString(),
                            RawContent = docData["raw_content"]?.ToString(),
                            DocketNumber = docData["docket_number"]?.ToString(),
                            PriorityScore = Convert.ToInt32(docData["priority_score"] ?? 0),
                            AgencyId = await GetAgencyIdBySource(source)
                        };

                        _context.RegulatoryDocuments.Add(newDocument);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Added new document {DocumentId} from {Source}", 
                                             documentId, source);

                        // Queue for analysis if high priority
                        if (newDocument.PriorityScore >= 10)
                        {
                            BackgroundJob.Enqueue<IDocumentAnalysisService>(
                                service => service.AnalyzeDocumentAsync(newDocument.Id));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing document from {Source}", source);
                }
            }
        }

        private async Task CheckApproachingDeadlinesAsync()
        {
            var upcomingDeadlines = await _context.ComplianceRequirements
                .Include(cr => cr.Document)
                .ThenInclude(d => d.Agency)
                .Where(cr => cr.Deadline.HasValue && 
                           cr.Deadline.Value <= DateTime.UtcNow.AddDays(90) &&
                           cr.Deadline.Value > DateTime.UtcNow)
                .ToListAsync();

            foreach (var requirement in upcomingDeadlines)
            {
                var daysUntilDeadline = (requirement.Deadline.Value - DateTime.UtcNow.Date).Days;
                
                var alert = new RegulatoryAlert
                {
                    AlertType = "deadline_approaching",
                    DocumentId = requirement.DocumentId,
                    Severity = GetDeadlineSeverity(daysUntilDeadline),
                    Title = $"Compliance Deadline Approaching: {requirement.RequirementText}",
                    Message = $"Deadline in {daysUntilDeadline} days: {requirement.Deadline:yyyy-MM-dd}",
                    AlertData = JsonSerializer.Serialize(new
                    {
                        RequirementId = requirement.Id,
                        DaysRemaining = daysUntilDeadline,
                        Severity = requirement.Severity
                    }),
                    Status = "active"
                };

                _context.RegulatoryAlerts.Add(alert);
            }

            await _context.SaveChangesAsync();
        }

        private async Task CheckHighImpactRegulationsAsync()
        {
            var recentHighImpactDocs = await _context.RegulatoryDocuments
                .Include(d => d.Analyses)
                .Where(d => d.CreatedAt >= DateTime.UtcNow.AddHours(-24) &&
                           d.PriorityScore >= 15)
                .ToListAsync();

            foreach (var document in recentHighImpactDocs)
            {
                var latestAnalysis = document.Analyses
                    .OrderByDescending(a => a.AnalysisDate)
                    .FirstOrDefault();

                if (latestAnalysis != null)
                {
                    var impactData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        latestAnalysis.ImpactAssessment ?? "{}");

                    var impactScore = Convert.ToDouble(impactData.GetValueOrDefault("impact_score", 0));

                    if (impactScore >= 7.0)
                    {
                        var alert = new RegulatoryAlert
                        {
                            AlertType = "high_impact_regulation",
                            DocumentId = document.Id,
                            Severity = "high",
                            Title = $"High-Impact Regulation Published: {document.Title}",
                            Message = $"New regulation with impact score {impactScore:F1} requires immediate review",
                            AlertData = JsonSerializer.Serialize(new
                            {
                                ImpactScore = impactScore,
                                DocumentType = document.DocumentType,
                                Agency = document.Agency?.Name
                            }),
                            Status = "active"
                        };

                        _context.RegulatoryAlerts.Add(alert);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private string GetDeadlineSeverity(int daysUntilDeadline)
        {
            return daysUntilDeadline switch
            {
                <= 7 => "critical",
                <= 30 => "high",
                <= 60 => "medium",
                _ => "low"
            };
        }

        private DateTime? ParseDate(object dateValue)
        {
            if (dateValue == null) return null;
            
            if (DateTime.TryParse(dateValue.ToString(), out var result))
                return result;
                
            return null;
        }

        private async Task<Guid> GetAgencyIdBySource(string source)
        {
            var agency = await _context.RegulatoryAgencies
                .FirstOrDefaultAsync(a => a.Name.Contains(source) || a.Abbreviation == source);
                
            return agency?.Id ?? Guid.Empty;
        }

        private async Task UpdateFrameworkWithNewRegulations(ComplianceFramework framework)
        {
            // Find new regulations that might affect this framework
            var newRegulations = await _context.RegulatoryDocuments
                .Include(d => d.Analyses)
                .Where(d => d.CreatedAt > framework.LastUpdated &&
                           d.Analyses.Any(a => a.AffectedParties.Any(p => 
                               framework.IndustrySegments.Contains(p) ||
                               framework.GeographicScope.Any(g => p.Contains(g)))))
                .ToListAsync();

            foreach (var regulation in newRegulations)
            {
                // Assess impact on framework
                BackgroundJob.Enqueue<IChangeImpactService>(
                    service => service.AssessChangeImpactAsync(framework.Id, regulation.Id));
            }

            // Update framework timestamp
            framework.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    // Startup configuration
    public static class HangfireExtensions
    {
        public static void ConfigureRegulatoryMonitoring(this IRecurringJobManager recurringJobs)
        {
            // Federal monitoring - every 4 hours
            recurringJobs.AddOrUpdate<IRegulatoryMonitoringService>(
                "federal-monitoring",
                service => service.MonitorFederalRegulationsAsync(),
                "0 */4 * * *",
                TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));

            // State monitoring - twice daily
            recurringJobs.AddOrUpdate<IRegulatoryMonitoringService>(
                "state-monitoring", 
                service => service.MonitorStateRegulationsAsync(),
                "0 2,14 * * *",
                TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));

            // Document processing - every 15 minutes
            recurringJobs.AddOrUpdate<IRegulatoryMonitoringService>(
                "document-processing",
                service => service.ProcessPendingDocumentsAsync(),
                "*/15 * * * *");

            // Compliance alerts - twice daily
            recurringJobs.AddOrUpdate<IRegulatoryMonitoringService>(
                "compliance-alerts",
                service => service.GenerateComplianceAlertsAsync(),
                "0 8,16 * * *",
                TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));

            // Framework updates - daily at 3 AM
            recurringJobs.AddOrUpdate<IRegulatoryMonitoringService>(
                "framework-updates",
                service => service.UpdateComplianceFrameworksAsync(),
                "0 3 * * *",
                TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));
        }
    }
}
```

---

## **Deployment & Infrastructure**

### **Docker Compose Configuration**
```yaml
# docker-compose.yml
version: '3.8'

services:
  # PostgreSQL Database
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: regulatoriq
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init-db.sql
    networks:
      - regulatoriq-network

  # Redis Cache
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    networks:
      - regulatoriq-network

  # Elasticsearch
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
      - "ES_JAVA_OPTS=-Xms2g -Xmx2g"
    ports:
      - "9200:9200"
    volumes:
      - elasticsearch_data:/usr/share/elasticsearch/data
    networks:
      - regulatoriq-network

  # Apache Kafka
  kafka:
    image: confluentinc/cp-kafka:latest
    environment:
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
    ports:
      - "9092:9092"
    depends_on:
      - zookeeper
    networks:
      - regulatoriq-network

  zookeeper:
    image: confluentinc/cp-zookeeper:latest
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    networks:
      - regulatoriq-network

  # API Application
  api:
    build:
      context: .
      dockerfile: src/RegulatorIQ.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=regulatoriq;Username=${DB_USER};Password=${DB_PASSWORD}
      - Redis__Configuration=redis:6379
      - Elasticsearch__Uri=http://elasticsearch:9200
      - Kafka__BootstrapServers=kafka:9092
      - OpenAI__ApiKey=${OPENAI_API_KEY}
      - Logging__LogLevel__Default=Information
    ports:
      - "5000:80"
    depends_on:
      - postgres
      - redis
      - elasticsearch
      - kafka
    networks:
      - regulatoriq-network

  # Python ML Services
  ml-services:
    build:
      context: .
      dockerfile: src/RegulatorIQ.MLServices/Dockerfile
    environment:
      - DATABASE_URL=postgresql://${DB_USER}:${DB_PASSWORD}@postgres:5432/regulatoriq
      - REDIS_URL=redis://redis:6379
      - ELASTICSEARCH_URL=http://elasticsearch:9200
      - OPENAI_API_KEY=${OPENAI_API_KEY}
      - HUGGINGFACE_API_TOKEN=${HUGGINGFACE_API_TOKEN}
    depends_on:
      - postgres
      - redis
      - elasticsearch
    networks:
      - regulatoriq-network

  # Web Scraper Services
  scrapers:
    build:
      context: .
      dockerfile: src/RegulatorIQ.Scrapers/Dockerfile
    environment:
      - DATABASE_URL=postgresql://${DB_USER}:${DB_PASSWORD}@postgres:5432/regulatoriq
      - REDIS_URL=redis://redis:6379
      - SELENIUM_HUB_URL=http://selenium-hub:4444/wd/hub
      - FERC_API_KEY=${FERC_API_KEY}
      - FEDERAL_REGISTER_API_KEY=${FEDERAL_REGISTER_API_KEY}
    depends_on:
      - postgres
      - redis
      - selenium-hub
    networks:
      - regulatoriq-network

  # Selenium Hub for browser automation
  selenium-hub:
    image: selenium/hub:4.15.0
    ports:
      - "4444:4444"
    networks:
      - regulatoriq-network

  selenium-chrome:
    image: selenium/node-chrome:4.15.0
    environment:
      HUB_HOST: selenium-hub
      HUB_PORT: 4444
    depends_on:
      - selenium-hub
    networks:
      - regulatoriq-network

  # Frontend Application
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    environment:
      - REACT_APP_API_URL=http://localhost:5000/api
      - REACT_APP_ENVIRONMENT=production
    depends_on:
      - api
    networks:
      - regulatoriq-network

  # Nginx Reverse Proxy
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf
      - ./nginx/ssl:/etc/nginx/ssl
    depends_on:
      - api
      - frontend
    networks:
      - regulatoriq-network

  # Background Job Processing (Hangfire)
  hangfire:
    build:
      context: .
      dockerfile: src/RegulatorIQ.BackgroundServices/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=regulatoriq;Username=${DB_USER};Password=${DB_PASSWORD}
      - Redis__Configuration=redis:6379
      - Hangfire__WorkerCount=5
    depends_on:
      - postgres
      - redis
    networks:
      - regulatoriq-network

volumes:
  postgres_data:
  redis_data:
  elasticsearch_data:

networks:
  regulatoriq-network:
    driver: bridge
```

### **Nginx Configuration**
```nginx
# nginx/nginx.conf
events {
    worker_connections 1024;
}

http {
    upstream api {
        server api:80;
    }

    upstream frontend {
        server frontend:80;
    }

    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;
    limit_req_zone $binary_remote_addr zone=web_limit:10m rate=100r/s;

    server {
        listen 80;
        server_name regulatoriq.com www.regulatoriq.com;

        # Redirect HTTP to HTTPS
        return 301 https://$server_name$request_uri;
    }

    server {
        listen 443 ssl http2;
        server_name regulatoriq.com www.regulatoriq.com;

        ssl_certificate /etc/nginx/ssl/cert.pem;
        ssl_certificate_key /etc/nginx/ssl/private.key;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;

        # Security headers
        add_header X-Frame-Options DENY;
        add_header X-Content-Type-Options nosniff;
        add_header X-XSS-Protection "1; mode=block";
        add_header Strict-Transport-Security "max-age=31536000; includeSubDomains";

        # API routes
        location /api/ {
            limit_req zone=api_limit burst=20 nodelay;
            
            proxy_pass http://api;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            
            # WebSocket support for SignalR
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
        }

        # Frontend application
        location / {
            limit_req zone=web_limit burst=50 nodelay;
            
            proxy_pass http://frontend;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # Health check endpoint
        location /health {
            proxy_pass http://api/health;
            access_log off;
        }
    }
}
```

---

## **Development Timeline & Cost Analysis**

### **Phase 1: Core Infrastructure (Months 1-4) - $125,000**
**Deliverables:**
- Database schema and Entity Framework models
- ASP.NET Core API with basic CRUD operations
- Federal regulatory source monitoring (FERC, Federal Register, EPA)
- Basic document ingestion and storage
- PostgreSQL setup with full-text search

**Team:**
- Senior Backend Developer (.NET): 3 months
- Database Architect: 1.5 months
- DevOps Engineer: 2 months

### **Phase 2: AI/ML Pipeline (Months 5-8) - $148,000**
**Deliverables:**
- Python ML services for document analysis
- NLP pipeline with legal BERT fine-tuning
- Compliance requirement extraction
- Impact assessment algorithms
- Document classification and entity extraction

**Team:**
- ML Engineer: 3.5 months
- NLP Specialist: 3 months
- Python Developer: 2.5 months

### **Phase 3: State Monitoring & Web Scraping (Months 9-12) - $118,000**
**Deliverables:**
- Texas Railroad Commission monitoring
- All 50-state regulatory tracking
- Advanced web scraping with Selenium
- PDF processing and OCR capabilities
- Automated document prioritization

**Team:**
- Web Scraping Specialist: 3 months
- Full-stack Developer: 2.5 months
- QA Engineer: 1.5 months

### **Phase 4: Frontend & User Experience (Months 13-16) - $135,000**
**Deliverables:**
- React dashboard with Material-UI
- Document analysis interface
- Compliance framework management
- Real-time alerts and notifications
- Advanced search and filtering

**Team:**
- Senior Frontend Developer: 3 months
- UI/UX Designer: 2 months
- Frontend Developer: 2.5 months

### **Phase 5: Background Processing & Automation (Months 17-20) - $112,000**
**Deliverables:**
- Hangfire background job processing
- Automated compliance framework updates
- Alert generation and notification system
- Change impact assessment automation
- Performance optimization

**Team:**
- Backend Developer: 2.5 months
- DevOps Engineer: 2 months
- Performance Engineer: 1.5 months

### **Phase 6: Production & Security (Months 21-24) - $98,000**
**Deliverables:**
- Production deployment infrastructure
- Security auditing and penetration testing
- Compliance reporting features
- API documentation and client SDKs
- User training and onboarding

**Team:**
- Security Engineer: 2 months
- DevOps Engineer: 1.5 months
- Technical Writer: 1 month
- Support Engineer: 1.5 months

### **Total Development Cost: $736,000**

### **Annual Operating Costs: $42,000**
- Cloud infrastructure (Azure/AWS): $18,000/year
- Database hosting (managed PostgreSQL): $6,000/year
- AI/ML API costs (OpenAI, Hugging Face): $8,000/year
- Third-party data sources and APIs: $4,000/year
- Security and monitoring tools: $3,000/year
- SSL certificates and domain: $500/year
- Backup and disaster recovery: $2,500/year

### **Revenue Models**

**1. Subscription Tiers:**
- **Starter:** $2,500/month per company (up to 5 users)
  - Federal monitoring only
  - Basic compliance tracking
  - Email alerts
  
- **Professional:** $8,000/month per company (up to 25 users)
  - Federal + state monitoring
  - Advanced analytics and reporting
  - Custom compliance frameworks
  - API access
  
- **Enterprise:** $25,000/month per company (unlimited users)
  - All features included
  - Custom integrations
  - Dedicated support
  - On-premise deployment option

**2. Professional Services:**
- Implementation consulting: $200-400/hour
- Custom framework development: $25,000-75,000 per project
- Regulatory compliance audits: $50,000-150,000 per engagement
- Training and certification programs: $5,000-15,000 per session

**3. Data & Analytics:**
- Regulatory intelligence reports: $5,000-25,000 per report
- Custom monitoring for specific regulations: $2,000-10,000/month
- Historical regulatory database access: $500-2,000/month

### **Market Analysis**

**Target Market Size:**
- **Primary:** 3,500+ natural gas companies in the US
- **Secondary:** 15,000+ companies with natural gas operations
- **Tertiary:** Regulatory consulting firms and law firms

**Competitive Advantages:**
- **Automated Monitoring:** Real-time tracking vs manual processes
- **AI-Powered Analysis:** Intelligent document analysis vs human review
- **Proactive Compliance:** Predictive alerts vs reactive responses
- **Industry Focus:** Natural gas specialization vs generic solutions
- **Multi-jurisdictional:** Federal and state coverage in one platform

### **Expected ROI for Customers**
- **Compliance Officer Time Savings:** 15-25 hours/week
- **Reduced Risk:** 50-80% reduction in missed deadlines
- **Faster Implementation:** 60% faster compliance framework updates
- **Cost Avoidance:** $100,000-1,000,000+ in potential penalties

RegulatorIQ addresses the critical gap in regulatory change management for the natural gas industry, providing automated monitoring, intelligent analysis, and proactive compliance management that significantly reduces regulatory risk while improving operational efficiency.
