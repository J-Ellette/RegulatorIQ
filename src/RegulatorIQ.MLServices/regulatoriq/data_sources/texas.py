# regulatoriq/data_sources/texas.py
from typing import List, Dict, Any, Optional
import asyncio
import aiohttp
from datetime import datetime, timedelta
import re


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

        rules_data = await self._scrape_trc_rules()
        regulations.extend(rules_data)

        orders_data = await self._scrape_trc_orders()
        regulations.extend(orders_data)

        permits_data = await self._scrape_trc_permits()
        regulations.extend(permits_data)

        return regulations

    async def _scrape_trc_rules(self) -> List[Dict[str, Any]]:
        """Scrape TRC rules related to natural gas"""

        gas_utility_titles = [
            'Title 16, Part 1 - Gas Utilities',
            'Title 16, Part 2 - Pipeline Safety',
            'Title 16, Part 3 - Gas Well Gas'
        ]

        rules = []

        async with aiohttp.ClientSession() as session:
            for title in gas_utility_titles:
                url = (f"{self.sources['trc']['base_url']}/legal/rules/"
                       f"{title.lower().replace(' ', '-')}")

                try:
                    async with session.get(url, timeout=aiohttp.ClientTimeout(total=30)) as response:
                        if response.status == 200:
                            html_content = await response.text()
                            parsed_rules = self._parse_trc_rules_html(html_content, title)
                            rules.extend(parsed_rules)
                except Exception as e:
                    print(f"Error scraping TRC rules for {title}: {e}")

        return rules

    async def _scrape_trc_orders(self) -> List[Dict[str, Any]]:
        """Scrape TRC hearing orders"""
        orders = []

        async with aiohttp.ClientSession() as session:
            url = f"{self.sources['trc']['base_url']}{self.sources['trc']['orders_url']}"
            try:
                async with session.get(url, timeout=aiohttp.ClientTimeout(total=30)) as response:
                    if response.status == 200:
                        html_content = await response.text()
                        orders = self._parse_trc_orders_html(html_content)
            except Exception as e:
                print(f"Error scraping TRC orders: {e}")

        return orders

    async def _scrape_trc_permits(self) -> List[Dict[str, Any]]:
        """Scrape TRC permit applications relevant to natural gas"""
        # Placeholder - returns empty list until actual scraper is implemented
        return []

    async def monitor_texas_register(self) -> List[Dict[str, Any]]:
        """Monitor Texas Register for all agency rule changes"""

        recent_registers = []

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

    async def get_all_regulations(self) -> List[Dict[str, Any]]:
        """Get all regulations from all monitored Texas agencies"""
        results = []

        trc_data = await self.monitor_railroad_commission()
        results.extend(trc_data)

        register_data = await self.monitor_texas_register()
        results.extend(register_data)

        return results

    def _extract_gas_regulations(self, register_content: str) -> List[Dict[str, Any]]:
        """Extract natural gas related regulations from Texas Register"""
        try:
            from bs4 import BeautifulSoup
        except ImportError:
            return []

        soup = BeautifulSoup(register_content, 'html.parser')
        regulations = []

        gas_keywords = [
            'natural gas', 'pipeline', 'lng', 'gas utility',
            'methane', 'compression', 'distribution', 'transmission'
        ]

        rule_sections = soup.find_all(['div', 'section'],
                                      class_=re.compile(r'rule|proposed|adopted'))

        for section in rule_sections:
            section_text = section.get_text().lower()

            if any(keyword in section_text for keyword in gas_keywords):
                regulation = self._parse_texas_register_section(section)
                if regulation:
                    regulations.append(regulation)

        return regulations

    def _parse_trc_rules_html(self, html_content: str, title: str) -> List[Dict[str, Any]]:
        """Parse TRC rules HTML into standardized format"""
        rules = []

        try:
            from bs4 import BeautifulSoup
            soup = BeautifulSoup(html_content, 'html.parser')

            # Extract rule entries - structure varies by page
            rule_links = soup.find_all('a', href=re.compile(r'rule|chapter', re.I))

            for link in rule_links[:10]:  # Limit to first 10
                rules.append({
                    'source': 'TRC',
                    'document_id': f"TRC-{hash(link.get('href', '')) % 100000}",
                    'title': link.get_text(strip=True),
                    'document_type': 'rule',
                    'publication_date': None,
                    'effective_date': None,
                    'url': f"{self.sources['trc']['base_url']}{link.get('href', '')}",
                    'pdf_url': None,
                    'docket_number': None,
                    'summary': title,
                    'raw_content': '',
                    'priority_score': 5
                })
        except Exception as e:
            print(f"Error parsing TRC rules HTML: {e}")

        return rules

    def _parse_trc_orders_html(self, html_content: str) -> List[Dict[str, Any]]:
        """Parse TRC orders HTML into standardized format"""
        orders = []

        try:
            from bs4 import BeautifulSoup
            soup = BeautifulSoup(html_content, 'html.parser')

            order_links = soup.find_all('a', href=re.compile(r'order|hearing', re.I))

            for link in order_links[:10]:
                orders.append({
                    'source': 'TRC',
                    'document_id': f"TRC-ORDER-{hash(link.get('href', '')) % 100000}",
                    'title': link.get_text(strip=True),
                    'document_type': 'order',
                    'publication_date': None,
                    'effective_date': None,
                    'url': f"{self.sources['trc']['base_url']}{link.get('href', '')}",
                    'pdf_url': None,
                    'docket_number': None,
                    'summary': '',
                    'raw_content': '',
                    'priority_score': 7
                })
        except Exception as e:
            print(f"Error parsing TRC orders HTML: {e}")

        return orders

    def _parse_texas_register_section(self, section) -> Optional[Dict[str, Any]]:
        """Parse a Texas Register section into standardized format"""
        try:
            title = section.find(['h1', 'h2', 'h3', 'h4'])
            title_text = title.get_text(strip=True) if title else 'Untitled'

            return {
                'source': 'Texas Register',
                'document_id': f"TXREG-{hash(title_text) % 100000}",
                'title': title_text,
                'document_type': 'rule',
                'publication_date': datetime.now().strftime('%Y-%m-%d'),
                'effective_date': None,
                'url': None,
                'pdf_url': None,
                'docket_number': None,
                'summary': section.get_text(strip=True)[:500],
                'raw_content': section.get_text(),
                'priority_score': 5
            }
        except Exception:
            return None

    def _build_texas_register_url(self, date: datetime) -> str:
        year = date.strftime('%Y')
        week = date.strftime('%W')
        return f"{self.sources['tsos']['base_url']}/texreg/{year}/week{week}/"

    async def _download_texas_register(self, url: str) -> str:
        async with aiohttp.ClientSession() as session:
            async with session.get(url, timeout=aiohttp.ClientTimeout(total=30)) as response:
                if response.status == 200:
                    return await response.text()
                return ""


class OklahomaRegulatoryMonitor:
    """Monitor Oklahoma state agencies for natural gas regulations"""

    def __init__(self):
        self.sources = {
            'occ': {
                'name': 'Oklahoma Corporation Commission',
                'base_url': 'https://www.occeweb.com',
                'rules_url': '/rules/',
                'orders_url': '/orders/',
                'priority': 1
            },
            'odeq': {
                'name': 'Oklahoma Department of Environmental Quality',
                'base_url': 'https://www.deq.ok.gov',
                'rules_url': '/rules/',
                'priority': 1
            }
        }

    async def get_all_regulations(self) -> List[Dict[str, Any]]:
        """Get all regulations from Oklahoma agencies"""
        results = []

        occ_rules = await self._scrape_occ_rules()
        results.extend(occ_rules)

        return results

    async def _scrape_occ_rules(self) -> List[Dict[str, Any]]:
        """Scrape Oklahoma Corporation Commission rules for natural gas"""
        rules = []

        async with aiohttp.ClientSession() as session:
            url = f"{self.sources['occ']['base_url']}{self.sources['occ']['rules_url']}"
            try:
                async with session.get(url, timeout=aiohttp.ClientTimeout(total=30)) as response:
                    if response.status == 200:
                        html_content = await response.text()
                        rules = self._parse_occ_rules_html(html_content)
            except Exception as e:
                print(f"Error scraping OCC rules: {e}")

        return rules

    def _parse_occ_rules_html(self, html_content: str) -> List[Dict[str, Any]]:
        """Parse OCC rules HTML into standardized format"""
        rules = []

        try:
            from bs4 import BeautifulSoup
            soup = BeautifulSoup(html_content, 'html.parser')

            rule_links = soup.find_all('a', href=re.compile(r'rule|chapter|gas', re.I))

            for link in rule_links[:10]:
                rules.append({
                    'source': 'OCC',
                    'document_id': f"OCC-{hash(link.get('href', '')) % 100000}",
                    'title': link.get_text(strip=True),
                    'document_type': 'rule',
                    'publication_date': None,
                    'effective_date': None,
                    'url': f"{self.sources['occ']['base_url']}{link.get('href', '')}",
                    'pdf_url': None,
                    'docket_number': None,
                    'summary': '',
                    'raw_content': '',
                    'priority_score': 5
                })
        except Exception as e:
            print(f"Error parsing OCC rules HTML: {e}")

        return rules


class LouisianaRegulatoryMonitor:
    """Monitor Louisiana state agencies for natural gas regulations"""

    def __init__(self):
        self.sources = {
            'lpsb': {
                'name': 'Louisiana Public Service Commission',
                'base_url': 'https://lpsc.louisiana.gov',
                'rules_url': '/rules/',
                'priority': 1
            },
            'ldeq': {
                'name': 'Louisiana Department of Environmental Quality',
                'base_url': 'https://deq.louisiana.gov',
                'rules_url': '/rules/',
                'priority': 1
            },
            'lregister': {
                'name': 'Louisiana Register',
                'base_url': 'https://www.doa.la.gov',
                'register_url': '/pages/osr/reg/',
                'priority': 2
            }
        }

    async def get_all_regulations(self) -> List[Dict[str, Any]]:
        """Get all regulations from Louisiana agencies"""
        results = []

        lpsc_rules = await self._scrape_lpsc_rules()
        results.extend(lpsc_rules)

        return results

    async def _scrape_lpsc_rules(self) -> List[Dict[str, Any]]:
        """Scrape Louisiana Public Service Commission rules"""
        rules = []

        async with aiohttp.ClientSession() as session:
            url = f"{self.sources['lpsb']['base_url']}{self.sources['lpsb']['rules_url']}"
            try:
                async with session.get(url, timeout=aiohttp.ClientTimeout(total=30)) as response:
                    if response.status == 200:
                        html_content = await response.text()
                        rules = self._parse_lpsc_rules_html(html_content)
            except Exception as e:
                print(f"Error scraping LPSC rules: {e}")

        return rules

    def _parse_lpsc_rules_html(self, html_content: str) -> List[Dict[str, Any]]:
        """Parse LPSC rules HTML into standardized format"""
        rules = []

        try:
            from bs4 import BeautifulSoup
            soup = BeautifulSoup(html_content, 'html.parser')

            rule_links = soup.find_all('a', href=re.compile(r'rule|gas|pipeline', re.I))

            for link in rule_links[:10]:
                rules.append({
                    'source': 'LPSC',
                    'document_id': f"LPSC-{hash(link.get('href', '')) % 100000}",
                    'title': link.get_text(strip=True),
                    'document_type': 'rule',
                    'publication_date': None,
                    'effective_date': None,
                    'url': f"{self.sources['lpsb']['base_url']}{link.get('href', '')}",
                    'pdf_url': None,
                    'docket_number': None,
                    'summary': '',
                    'raw_content': '',
                    'priority_score': 5
                })
        except Exception as e:
            print(f"Error parsing LPSC rules HTML: {e}")

        return rules


class NewMexicoRegulatoryMonitor:
    """Monitor New Mexico state agencies for natural gas regulations"""

    def __init__(self):
        self.sources = {
            'nmprc': {
                'name': 'New Mexico Public Regulation Commission',
                'base_url': 'https://www.nmprc.state.nm.us',
                'rules_url': '/rules/',
                'priority': 1
            },
            'nmocd': {
                'name': 'New Mexico Oil Conservation Division',
                'base_url': 'https://www.emnrd.nm.gov/ocd',
                'rules_url': '/regulations/',
                'priority': 1
            }
        }

    async def get_all_regulations(self) -> List[Dict[str, Any]]:
        """Get all regulations from New Mexico agencies"""
        results = []

        nmprc_rules = await self._scrape_nmprc_rules()
        results.extend(nmprc_rules)

        return results

    async def _scrape_nmprc_rules(self) -> List[Dict[str, Any]]:
        """Scrape New Mexico Public Regulation Commission rules"""
        rules = []

        async with aiohttp.ClientSession() as session:
            url = f"{self.sources['nmprc']['base_url']}{self.sources['nmprc']['rules_url']}"
            try:
                async with session.get(url, timeout=aiohttp.ClientTimeout(total=30)) as response:
                    if response.status == 200:
                        html_content = await response.text()
                        rules = self._parse_nmprc_rules_html(html_content)
            except Exception as e:
                print(f"Error scraping NMPRC rules: {e}")

        return rules

    def _parse_nmprc_rules_html(self, html_content: str) -> List[Dict[str, Any]]:
        """Parse NMPRC rules HTML into standardized format"""
        rules = []

        try:
            from bs4 import BeautifulSoup
            soup = BeautifulSoup(html_content, 'html.parser')

            rule_links = soup.find_all('a', href=re.compile(r'rule|gas|nmac', re.I))

            for link in rule_links[:10]:
                rules.append({
                    'source': 'NMPRC',
                    'document_id': f"NMPRC-{hash(link.get('href', '')) % 100000}",
                    'title': link.get_text(strip=True),
                    'document_type': 'rule',
                    'publication_date': None,
                    'effective_date': None,
                    'url': f"{self.sources['nmprc']['base_url']}{link.get('href', '')}",
                    'pdf_url': None,
                    'docket_number': None,
                    'summary': '',
                    'raw_content': '',
                    'priority_score': 5
                })
        except Exception as e:
            print(f"Error parsing NMPRC rules HTML: {e}")

        return rules


class ArkansasRegulatoryMonitor:
    """Monitor Arkansas state agencies for natural gas regulations"""

    def __init__(self):
        self.sources = {
            'apsc': {
                'name': 'Arkansas Public Service Commission',
                'base_url': 'https://www.apscservices.info',
                'rules_url': '/rules/',
                'priority': 1
            },
            'adeq': {
                'name': 'Arkansas Department of Energy and Environment',
                'base_url': 'https://www.healthy.arkansas.gov/programs-services/topics/air-division',
                'rules_url': '/regulations/',
                'priority': 1
            }
        }

    async def get_all_regulations(self) -> List[Dict[str, Any]]:
        """Get all regulations from Arkansas agencies"""
        results = []

        apsc_rules = await self._scrape_apsc_rules()
        results.extend(apsc_rules)

        return results

    async def _scrape_apsc_rules(self) -> List[Dict[str, Any]]:
        """Scrape Arkansas Public Service Commission rules"""
        rules = []

        async with aiohttp.ClientSession() as session:
            url = f"{self.sources['apsc']['base_url']}{self.sources['apsc']['rules_url']}"
            try:
                async with session.get(url, timeout=aiohttp.ClientTimeout(total=30)) as response:
                    if response.status == 200:
                        html_content = await response.text()
                        rules = self._parse_apsc_rules_html(html_content)
            except Exception as e:
                print(f"Error scraping APSC rules: {e}")

        return rules

    def _parse_apsc_rules_html(self, html_content: str) -> List[Dict[str, Any]]:
        """Parse APSC rules HTML into standardized format"""
        rules = []

        try:
            from bs4 import BeautifulSoup
            soup = BeautifulSoup(html_content, 'html.parser')

            rule_links = soup.find_all('a', href=re.compile(r'rule|gas|order', re.I))

            for link in rule_links[:10]:
                rules.append({
                    'source': 'APSC',
                    'document_id': f"APSC-{hash(link.get('href', '')) % 100000}",
                    'title': link.get_text(strip=True),
                    'document_type': 'rule',
                    'publication_date': None,
                    'effective_date': None,
                    'url': f"{self.sources['apsc']['base_url']}{link.get('href', '')}",
                    'pdf_url': None,
                    'docket_number': None,
                    'summary': '',
                    'raw_content': '',
                    'priority_score': 5
                })
        except Exception as e:
            print(f"Error parsing APSC rules HTML: {e}")

        return rules


class MultiStateMonitor:
    """Monitor multiple states for natural gas regulations"""

    def __init__(self):
        self.state_monitors = {
            'texas': TexasRegulatoryMonitor(),
            'oklahoma': OklahomaRegulatoryMonitor(),
            'louisiana': LouisianaRegulatoryMonitor(),
            'new_mexico': NewMexicoRegulatoryMonitor(),
            'arkansas': ArkansasRegulatoryMonitor(),
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
