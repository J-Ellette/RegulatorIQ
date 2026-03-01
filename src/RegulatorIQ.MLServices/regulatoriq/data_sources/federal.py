# regulatoriq/data_sources/federal.py
from typing import List, Dict, Any, Optional
import asyncio
import aiohttp
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
import os


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

        self._last_check_date: Optional[str] = None

    async def monitor_ferc_filings(self) -> List[Dict[str, Any]]:
        """Monitor FERC eLibrary for new filings and orders"""

        search_params = {
            'class': 'nat-gas',
            'date_from': self._get_last_check_date(),
            'date_to': datetime.now().strftime('%Y-%m-%d'),
            'document_types': ['order', 'notice', 'rule', 'proposed_rule', 'filing']
        }

        filings = []

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

                try:
                    async with session.get(url, params=params, timeout=aiohttp.ClientTimeout(total=30)) as response:
                        if response.status == 200:
                            data = await response.json()
                            filings.extend(self._parse_ferc_filings(data))
                except Exception as e:
                    print(f"Error fetching FERC filings for type {doc_type}: {e}")

        return filings

    async def monitor_federal_register(self) -> List[Dict[str, Any]]:
        """Monitor Federal Register for natural gas regulations"""

        api_url = "https://www.federalregister.gov/api/v1/documents.json"

        params = {
            'conditions[agencies][]': [
                'environmental-protection-agency',
                'energy-department',
                'transportation-department'
            ],
            'conditions[term]': 'natural gas OR pipeline OR LNG OR methane',
            'conditions[type][]': ['RULE', 'PRORULE', 'NOTICE'],
            'conditions[publication_date][gte]': self._get_last_check_date(),
            'order': 'newest',
            'per_page': 100,
            'fields[]': [
                'title', 'abstract', 'html_url', 'pdf_url',
                'publication_date', 'agencies', 'docket_id'
            ]
        }

        regulations = []

        async with aiohttp.ClientSession() as session:
            try:
                async with session.get(api_url, params=params, timeout=aiohttp.ClientTimeout(total=30)) as response:
                    if response.status == 200:
                        data = await response.json()
                        regulations = self._parse_federal_register_docs(data.get('results', []))
            except Exception as e:
                print(f"Error fetching Federal Register documents: {e}")

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

    def _parse_federal_register_docs(self, results: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        """Parse Federal Register results into standardized format"""
        documents = []

        for doc in results:
            parsed_doc = {
                'source': 'Federal Register',
                'document_id': doc.get('document_number'),
                'title': doc.get('title'),
                'document_type': doc.get('type', '').lower(),
                'publication_date': doc.get('publication_date'),
                'effective_date': None,
                'url': doc.get('html_url'),
                'pdf_url': doc.get('pdf_url'),
                'docket_number': doc.get('docket_id'),
                'summary': doc.get('abstract', ''),
                'raw_content': '',
                'impact_assessment': None,
                'compliance_requirements': [],
                'priority_score': self._calculate_priority_score(doc)
            }

            documents.append(parsed_doc)

        return documents

    def _calculate_priority_score(self, document: Dict[str, Any]) -> int:
        """Calculate priority score based on document characteristics"""
        score = 0

        type_weights = {
            'final_rule': 10,
            'rule': 10,
            'interim_rule': 8,
            'proposed_rule': 6,
            'prorule': 6,
            'order': 7,
            'notice': 4,
            'guidance': 3
        }

        doc_type = document.get('document_type', document.get('type', '')).lower()
        score += type_weights.get(doc_type, 2)

        high_priority_keywords = [
            'pipeline safety', 'lng', 'methane emissions',
            'transportation rates', 'capacity release',
            'environmental review', 'certificate'
        ]

        title = document.get('title', document.get('description', '')).lower()
        summary = document.get('summary', document.get('abstract', '')).lower()
        text = f"{title} {summary}"

        for keyword in high_priority_keywords:
            if keyword in text:
                score += 2

        effective_date = document.get('effective_date')
        if effective_date:
            days_until_effective = self._days_until_date(effective_date)
            if days_until_effective <= 30:
                score += 5
            elif days_until_effective <= 90:
                score += 3

        return min(score, 20)

    def _get_last_check_date(self) -> str:
        if self._last_check_date:
            return self._last_check_date
        # Default to 7 days ago
        return (datetime.now(timezone.utc) - timedelta(days=7)).strftime('%Y-%m-%d')

    def _get_api_key(self, source: str) -> str:
        return os.environ.get(f"{source.upper()}_API_KEY", "")

    def _days_until_date(self, date_str: str) -> int:
        try:
            target = datetime.strptime(date_str, '%Y-%m-%d').replace(tzinfo=timezone.utc)
            return (target - datetime.now(timezone.utc)).days
        except Exception:
            return 999
