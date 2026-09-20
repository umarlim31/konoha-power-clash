#!/usr/bin/env python3
"""Fail closed when Unity does not emit passing results for every expected test."""
import sys
import xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
cases = root.findall('.//test-case')
if len(cases) < 8 or any(case.get('result') != 'Passed' for case in cases):
    raise SystemExit('FAIL: fewer than 8 tests or a failed/skipped/inconclusive test')
if root.get('result') != 'Passed':
    raise SystemExit('FAIL: Unity test run did not pass')
print(f'Unity EditMode: {len(cases)} tests passed. Device validation still pending.')
