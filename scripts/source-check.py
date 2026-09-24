#!/usr/bin/env python3
"""Static packaging validation, explicitly not a C# compiler or Unity test runner."""
from pathlib import Path
import ast
import json
import re

root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / 'Packages/manifest.json').read_text())
assert manifest['dependencies']['com.unity.render-pipelines.universal'] == '17.0.3'
assert '6000.0.60f1' in (root / 'ProjectSettings/ProjectVersion.txt').read_text()
definitions = [json.loads(p.read_text()) for p in root.glob('Assets/**/*.asmdef')]
assert len({d['name'] for d in definitions}) == len(definitions) == 3
guids = []
for path in root.glob('Assets/**/*'):
    if path.suffix == '.meta' or 'Generated' in path.parts:
        continue
    meta = Path(str(path) + '.meta')
    assert meta.exists(), f'Missing Unity identity: {meta}'
    guid = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(), re.M)
    assert guid, f'Invalid GUID: {meta}'
    guids.append(guid.group(1))
assert len(guids) == len(set(guids)), 'Duplicate Unity GUID'
for path in root.glob('scripts/*.py'):
    ast.parse(path.read_text(), filename=str(path))
for path in root.glob('Assets/**/*.cs'):
    assert path.read_text().strip(), f'Empty C# file: {path}'
assert len(list(root.glob('Assets/**/*.cs'))) >= 12
print('PASS: JSON, assembly boundaries, unique Unity GUIDs, source files, Python syntax.')
print('NOT RUN: C# compilation, Unity import/tests, APK build, Android hardware.')
