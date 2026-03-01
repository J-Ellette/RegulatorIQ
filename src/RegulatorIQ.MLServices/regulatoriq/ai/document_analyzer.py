# regulatoriq/ai/document_analyzer.py
import re
from typing import List, Dict, Any, Optional
from dataclasses import dataclass, field
import os


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
        self._nlp = None
        self._tokenizer = None
        self._model = None

        # Load domain-specific patterns
        self.load_gas_industry_patterns()

    def _load_nlp(self):
        """Lazy-load spaCy model"""
        if self._nlp is None:
            try:
                import spacy
                self._nlp = spacy.load("en_core_web_lg")
            except Exception:
                try:
                    import spacy
                    self._nlp = spacy.load("en_core_web_sm")
                except Exception as e:
                    print(f"spaCy model not available: {e}")
                    self._nlp = None
        return self._nlp

    def _load_bert(self):
        """Lazy-load Legal BERT model"""
        if self._tokenizer is None:
            try:
                from transformers import AutoTokenizer, AutoModel
                self._tokenizer = AutoTokenizer.from_pretrained("nlpaueb/legal-bert-base-uncased")
                self._model = AutoModel.from_pretrained("nlpaueb/legal-bert-base-uncased")
            except Exception as e:
                print(f"Legal BERT model not available: {e}")
        return self._tokenizer, self._model

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

        self.patterns = {
            'effective_date': re.compile(
                r'effective\s+(?:date\s+)?(?:on\s+)?(\w+\s+\d{1,2},\s+\d{4})', re.I),
            'compliance_date': re.compile(
                r'comply\s+(?:with\s+)?(?:by\s+)?(\w+\s+\d{1,2},\s+\d{4})', re.I),
            'citation': re.compile(
                r'(\d+\s+CFR\s+\d+(?:\.\d+)?)|(\d+\s+U\.S\.C\s+\d+)', re.I),
            'penalty': re.compile(
                r'\$[\d,]+(?:\.\d{2})?(?:\s+(?:per\s+day|maximum|fine))?', re.I)
        }

    async def analyze_document(self, document: Dict[str, Any]) -> Dict[str, Any]:
        """Comprehensive analysis of a regulatory document"""

        content = document.get('content', '')
        doc_type = document.get('document_type', '')

        analysis_result = {
            'document_id': document.get('document_id'),
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

        analysis_result['confidence_score'] = self._calculate_confidence_score(analysis_result)

        return analysis_result

    async def _classify_document(self, content: str, doc_type: str) -> Dict[str, Any]:
        """Classify document type and regulatory area"""

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

        entities: Dict[str, List[str]] = {
            'facilities': [],
            'regulations': [],
            'dates': [],
            'organizations': [],
            'locations': [],
            'monetary_amounts': []
        }

        nlp = self._load_nlp()
        if nlp and content:
            doc = nlp(content[:50000])  # Limit for performance

            for ent in doc.ents:
                if ent.label_ == "ORG":
                    entities['organizations'].append(ent.text)
                elif ent.label_ == "GPE":
                    entities['locations'].append(ent.text)
                elif ent.label_ == "DATE":
                    entities['dates'].append(ent.text)
                elif ent.label_ == "MONEY":
                    entities['monetary_amounts'].append(ent.text)

        content_lower = content.lower()

        for facility in self.gas_entities['facilities']:
            if facility in content_lower:
                entities['facilities'].append(facility)

        for regulation in self.gas_entities['regulations']:
            if regulation in content_lower:
                entities['regulations'].append(regulation)

        # Deduplicate
        return {k: list(set(v)) for k, v in entities.items()}

    async def _extract_compliance_requirements(self, content: str) -> List[Dict[str, Any]]:
        """Extract specific compliance requirements from document"""

        requirements = []
        sections = self._split_into_sections(content)

        requirement_indicators = [
            'shall', 'must', 'required to', 'operator shall',
            'company must', 'entity shall', 'person must'
        ]

        for section in sections:
            sentences = self._split_into_sentences(section)

            for sentence in sentences:
                if any(indicator in sentence.lower() for indicator in requirement_indicators):
                    requirement = await self._parse_requirement(sentence, section)
                    if requirement:
                        requirements.append({
                            'requirement_id': requirement.requirement_id,
                            'description': requirement.description,
                            'deadline': requirement.deadline,
                            'applicability': requirement.applicability,
                            'severity': requirement.severity,
                            'citation': requirement.citation,
                            'implementation_guidance': requirement.implementation_guidance
                        })

        return requirements

    async def _parse_requirement(self, sentence: str, context: str) -> Optional[ComplianceRequirement]:
        """Parse individual compliance requirement"""

        if len(sentence.strip()) < 20:
            return None

        deadline_match = self.patterns['compliance_date'].search(context)
        deadline = deadline_match.group(1) if deadline_match else "Not specified"

        citation_match = self.patterns['citation'].search(context)
        citation = citation_match.group(0) if citation_match else "TBD"

        severity_keywords = {
            'high': ['safety', 'emergency', 'immediate', 'critical'],
            'medium': ['reporting', 'maintenance', 'inspection'],
            'low': ['administrative', 'notification', 'documentation']
        }

        severity = 'medium'
        sentence_lower = sentence.lower()

        for level, keywords in severity_keywords.items():
            if any(keyword in sentence_lower for keyword in keywords):
                severity = level
                break

        implementation_guidance = await self._get_ai_guidance(sentence)

        return ComplianceRequirement(
            requirement_id=f"REQ_{abs(hash(sentence)) % 10000:04d}",
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

        operational_keywords = [
            'modify procedures', 'new equipment', 'training required',
            'system changes', 'process updates', 'inspection frequency'
        ]

        content_lower = content.lower()

        for keyword in operational_keywords:
            if keyword in content_lower:
                impact_factors['operational_changes'] += 1

        cost_patterns = self.patterns['penalty'].findall(content)
        if cost_patterns:
            impact_factors['cost_implications'] = len(cost_patterns)

        timeline_data = self._extract_timeline(content)
        if timeline_data and timeline_data.get('critical_dates'):
            impact_factors['timeline_pressure'] = min(len(timeline_data['critical_dates']), 3)

        # Technical complexity based on content length and regulation references
        reg_refs = len(self.patterns['citation'].findall(content))
        impact_factors['technical_complexity'] = min(reg_refs, 5)

        impact_score = sum(impact_factors.values()) / len(impact_factors)

        return {
            'impact_score': impact_score,
            'factors': impact_factors,
            'priority_level': self._categorize_priority(impact_score),
            'estimated_compliance_cost': self._estimate_compliance_cost(impact_score),
            'implementation_complexity': self._assess_complexity(content)
        }

    def _extract_timeline(self, content: str) -> Dict[str, Any]:
        """Extract important dates and deadlines from document"""

        effective_dates = self.patterns['effective_date'].findall(content)
        compliance_dates = self.patterns['compliance_date'].findall(content)

        return {
            'effective_dates': list(set(effective_dates)),
            'compliance_dates': list(set(compliance_dates)),
            'critical_dates': list(set(effective_dates + compliance_dates))
        }

    def _identify_affected_parties(self, content: str) -> List[str]:
        """Identify parties affected by the regulation"""

        affected = []
        content_lower = content.lower()

        party_keywords = {
            'Pipeline Operators': ['pipeline operator', 'transmission company'],
            'Gas Utilities': ['gas utility', 'distribution company', 'local distribution'],
            'LNG Facilities': ['lng', 'liquefied natural gas'],
            'Storage Operators': ['underground storage', 'storage facility'],
            'All Natural Gas Companies': ['natural gas company', 'gas producer']
        }

        for party, keywords in party_keywords.items():
            if any(keyword in content_lower for keyword in keywords):
                affected.append(party)

        return affected if affected else ['Natural Gas Industry']

    async def _generate_summary(self, content: str) -> str:
        """Generate executive summary of regulation"""

        summary_sentences = self._extractive_summarization(content, max_sentences=3)
        return summary_sentences

    async def _extract_actionable_items(self, content: str) -> List[Dict[str, Any]]:
        """Extract actionable items from document"""

        action_items = []
        sentences = self._split_into_sentences(content)

        action_keywords = ['must', 'shall', 'required', 'submit', 'report', 'notify', 'implement']

        for i, sentence in enumerate(sentences[:50]):  # Limit processing
            if any(keyword in sentence.lower() for keyword in action_keywords):
                action_items.append({
                    'item_id': f"ACTION_{i:03d}",
                    'description': sentence.strip(),
                    'priority': 'medium',
                    'category': 'compliance'
                })

        return action_items[:10]  # Return top 10

    async def _find_related_regulations(self, content: str) -> List[str]:
        """Find related regulations mentioned in document"""

        citations = self.patterns['citation'].findall(content)
        related = []

        for citation_tuple in citations:
            for citation in citation_tuple:
                if citation and citation not in related:
                    related.append(citation)

        return related[:20]

    def _calculate_confidence_score(self, analysis: Dict[str, Any]) -> float:
        """Calculate overall confidence score for the analysis"""

        score = 0.5  # Base score

        if analysis.get('classification'):
            score += 0.1

        if analysis.get('entities'):
            entities = analysis['entities']
            if any(entities.get(k) for k in ['facilities', 'regulations', 'organizations']):
                score += 0.1

        requirements = analysis.get('compliance_requirements', [])
        if requirements:
            score += min(len(requirements) * 0.02, 0.1)

        if analysis.get('summary') and len(analysis['summary']) > 50:
            score += 0.1

        if analysis.get('timeline_analysis', {}).get('critical_dates'):
            score += 0.1

        return min(score, 1.0)

    def _classify_regulatory_category(self, content: str) -> Dict[str, Any]:
        """Classify regulatory category based on content"""

        content_lower = content.lower()

        categories = {
            'Pipeline Safety': ['pipeline safety', 'part 192', 'integrity management'],
            'Environmental': ['emissions', 'environmental', 'methane', 'air quality'],
            'Rate/Tariff': ['rate', 'tariff', 'transportation rates', 'capacity'],
            'Permitting': ['permit', 'certificate', 'authorization', 'application'],
            'Reporting': ['reporting', 'notification', 'disclosure', 'filing'],
            'Operations': ['operations', 'maintenance', 'inspection', 'testing']
        }

        primary = 'General'
        secondary = []

        for category, keywords in categories.items():
            if any(keyword in content_lower for keyword in keywords):
                if primary == 'General':
                    primary = category
                else:
                    secondary.append(category)

        return {'primary': primary, 'secondary': secondary}

    def _determine_regulatory_type(self, doc_type: str, content: str) -> str:
        """Determine the regulatory type"""

        type_map = {
            'rule': 'Final Rule',
            'prorule': 'Proposed Rule',
            'order': 'Order',
            'notice': 'Notice',
            'guidance': 'Guidance Document'
        }

        return type_map.get(doc_type.lower(), 'Regulatory Document')

    def _assess_urgency(self, content: str) -> str:
        """Assess urgency level of the document"""

        content_lower = content.lower()

        if any(word in content_lower for word in ['immediate', 'emergency', 'urgent', 'critical']):
            return 'Critical'
        elif any(word in content_lower for word in ['within 30 days', 'within 60 days']):
            return 'High'
        elif any(word in content_lower for word in ['within 90 days', 'within 180 days']):
            return 'Medium'
        else:
            return 'Low'

    def _determine_scope(self, content: str) -> str:
        """Determine the scope of the regulation"""

        content_lower = content.lower()

        if 'nationwide' in content_lower or 'all operators' in content_lower:
            return 'National'
        elif 'interstate' in content_lower:
            return 'Interstate'
        elif 'intrastate' in content_lower:
            return 'Intrastate'
        else:
            return 'General'

    def _split_into_sections(self, content: str) -> List[str]:
        """Split document content into sections"""

        sections = re.split(r'\n{2,}|(?=\b(?:Section|Part|Article|§)\s+\d)', content)
        return [s.strip() for s in sections if len(s.strip()) > 50]

    def _split_into_sentences(self, text: str) -> List[str]:
        """Split text into sentences"""

        sentences = re.split(r'(?<=[.!?])\s+', text)
        return [s.strip() for s in sentences if len(s.strip()) > 20]

    def _extractive_summarization(self, content: str, max_sentences: int = 3) -> str:
        """Simple extractive summarization"""

        sentences = self._split_into_sentences(content)

        if not sentences:
            return "No content available for summarization."

        # Score sentences by keyword density
        important_keywords = [
            'require', 'must', 'shall', 'effective', 'comply',
            'regulation', 'rule', 'standard', 'safety', 'environmental'
        ]

        scored_sentences = []
        for sentence in sentences:
            score = sum(1 for kw in important_keywords if kw in sentence.lower())
            scored_sentences.append((score, sentence))

        # Get top sentences
        top_sentences = sorted(scored_sentences, key=lambda x: x[0], reverse=True)[:max_sentences]

        # Return in original order
        summary_sentences = [s for _, s in top_sentences]
        return ' '.join(summary_sentences[:max_sentences])

    def _categorize_priority(self, impact_score: float) -> str:
        """Categorize priority based on impact score"""

        if impact_score >= 4:
            return 'Critical'
        elif impact_score >= 3:
            return 'High'
        elif impact_score >= 2:
            return 'Medium'
        else:
            return 'Low'

    def _estimate_compliance_cost(self, impact_score: float) -> float:
        """Estimate compliance cost based on impact score"""

        base_cost = 10000
        return base_cost * (1 + impact_score)

    def _assess_complexity(self, content: str) -> int:
        """Assess implementation complexity on 1-5 scale"""

        complexity = 1

        technical_terms = [
            'integrity management', 'cathodic protection', 'odorization',
            'operator qualification', 'leak survey', 'class location'
        ]

        content_lower = content.lower()

        for term in technical_terms:
            if term in content_lower:
                complexity += 1

        return min(complexity, 5)

    async def _get_ai_guidance(self, requirement: str) -> str:
        """Get AI-generated implementation guidance"""

        openai_key = os.environ.get('OPENAI_API_KEY')

        if openai_key:
            try:
                import openai
                client = openai.AsyncOpenAI(api_key=openai_key)
                response = await client.chat.completions.create(
                    model="gpt-4",
                    messages=[
                        {
                            "role": "system",
                            "content": "You are a regulatory compliance expert for the natural gas industry."
                        },
                        {
                            "role": "user",
                            "content": f"Provide brief implementation guidance for: {requirement}"
                        }
                    ],
                    max_tokens=200,
                    temperature=0.3
                )
                return response.choices[0].message.content.strip()
            except Exception as e:
                print(f"Error getting AI guidance: {e}")

        # Fallback guidance
        return (
            "Review this requirement with your compliance team and legal counsel. "
            "Document current practices against this requirement and develop an "
            "implementation plan with clear milestones and responsible parties."
        )


class ChangeImpactAnalyzer:
    """Analyze the impact of regulatory changes on existing compliance frameworks"""

    def __init__(self):
        self.document_analyzer = LegalDocumentAnalyzer()

    async def analyze_change_impact(
        self,
        new_regulation: Dict[str, Any],
        existing_framework: Dict[str, Any]
    ) -> Dict[str, Any]:
        """Analyze how new regulation impacts existing compliance framework"""

        impact_analysis = {
            'affected_processes': [],
            'required_updates': [],
            'timeline_conflicts': [],
            'cost_impact': 0.0,
            'risk_assessment': {},
            'implementation_roadmap': []
        }

        new_requirements = new_regulation.get('compliance_requirements', [])
        existing_requirements = existing_framework.get('requirements', [])

        for new_req in new_requirements:
            conflicts = self._find_requirement_conflicts(new_req, existing_requirements)
            if conflicts:
                impact_analysis['affected_processes'].extend(conflicts)

        timeline_analysis = await self._analyze_timeline_impact(new_regulation, existing_framework)
        impact_analysis['timeline_conflicts'] = timeline_analysis

        cost_analysis = await self._calculate_cost_impact(new_regulation, existing_framework)
        impact_analysis['cost_impact'] = cost_analysis

        roadmap = await self._generate_implementation_roadmap(impact_analysis)
        impact_analysis['implementation_roadmap'] = roadmap

        return impact_analysis

    def _find_requirement_conflicts(
        self,
        new_req: Dict[str, Any],
        existing_requirements: List[Dict[str, Any]]
    ) -> List[str]:
        """Find conflicts between new and existing requirements"""

        conflicts = []
        new_text = new_req.get('description', '').lower()

        for existing_req in existing_requirements:
            existing_text = existing_req.get('description', '').lower()

            # Simple keyword overlap detection
            new_words = set(new_text.split())
            existing_words = set(existing_text.split())
            overlap = new_words & existing_words

            if len(overlap) > 5:  # Significant overlap suggests related requirements
                conflicts.append(f"Potential conflict with existing requirement: {existing_req.get('id', 'unknown')}")

        return conflicts

    async def _analyze_timeline_impact(
        self,
        new_regulation: Dict[str, Any],
        existing_framework: Dict[str, Any]
    ) -> List[Dict[str, Any]]:
        """Analyze timeline conflicts between new regulation and existing framework"""

        conflicts = []

        new_timeline = new_regulation.get('timeline_analysis', {})
        existing_deadlines = existing_framework.get('deadlines', [])

        new_dates = new_timeline.get('critical_dates', [])

        for new_date in new_dates:
            conflicts.append({
                'date': new_date,
                'type': 'new_deadline',
                'description': f"New compliance deadline from regulation"
            })

        return conflicts

    async def _calculate_cost_impact(
        self,
        new_regulation: Dict[str, Any],
        existing_framework: Dict[str, Any]
    ) -> float:
        """Calculate estimated cost impact"""

        impact = new_regulation.get('impact_assessment', {})
        return float(impact.get('estimated_compliance_cost', 0))

    async def _generate_implementation_roadmap(
        self,
        impact_analysis: Dict[str, Any]
    ) -> List[Dict[str, Any]]:
        """Generate implementation roadmap"""

        roadmap = [
            {
                'phase': 1,
                'name': 'Assessment',
                'duration_weeks': 2,
                'description': 'Review new requirements and assess gaps in current framework'
            },
            {
                'phase': 2,
                'name': 'Planning',
                'duration_weeks': 3,
                'description': 'Develop detailed implementation plan and assign responsibilities'
            },
            {
                'phase': 3,
                'name': 'Implementation',
                'duration_weeks': 8,
                'description': 'Execute required changes to processes, procedures, and systems'
            },
            {
                'phase': 4,
                'name': 'Verification',
                'duration_weeks': 2,
                'description': 'Verify compliance with new requirements and document evidence'
            }
        ]

        return roadmap
