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
by_name = {d['name']: d for d in definitions}
assert 'Konoha.Runtime' in by_name['Konoha.EditModeTests']['references'], 'Tests must reach runtime logic'
assert by_name['Konoha.Editor'].get('includePlatforms') == ['Editor']
assert 'includePlatforms' not in by_name['Konoha.Runtime'], 'Runtime assembly must ship in the player'

# Owner-supplied hero art is uploaded from GitHub web, which cannot create Unity .meta
# files; Unity creates them on import. Only this folder may lack them (reported below).
art_root = root / 'Assets/Konoha/Art'
warnings = []
guids = []
for path in root.glob('Assets/**/*'):
    if path.suffix == '.meta' or 'Generated' in path.parts:
        continue
    meta = Path(str(path) + '.meta')
    if not meta.exists() and art_root in path.parents:
        warnings.append(f'Hero art without .meta (Unity will create one): {path.relative_to(root)}')
        continue
    assert meta.exists(), f'Missing Unity identity: {meta}'
    guid = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(), re.M)
    assert guid, f'Invalid GUID: {meta}'
    guids.append(guid.group(1))
assert len(guids) == len(set(guids)), 'Duplicate Unity GUID'
for path in root.glob('scripts/*.py'):
    ast.parse(path.read_text(), filename=str(path))

sources = list(root.glob('Assets/**/*.cs'))
assert len(sources) >= 12
declaration = re.compile(r'^    (?:public |internal )?(?:static |sealed |abstract |readonly )*'
                         r'(?:partial )?(?:class|struct|enum|interface) (\w+)', re.M)
declared = {}
for path in sources:
    text = path.read_text()
    assert text.strip(), f'Empty C# file: {path}'
    relative = path.relative_to(root / 'Assets/Konoha')
    editor_only = relative.parts[0] in ('Editor', 'Tests')
    # The Runtime assembly ships in the APK: no editor API outside Editor/Tests.
    if not editor_only:
        assert 'using UnityEditor' not in text, f'Editor API in runtime assembly: {path}'
    namespace = re.search(r'^namespace ([\w.]+)', text, re.M)
    for name in declaration.findall(text):
        key = (namespace.group(1) if namespace else '', name)
        assert key not in declared, f'Duplicate type {key} in {path} and {declared[key]}'
        declared[key] = path

# Campaign logic layer: plain C# (no MonoBehaviour); pure data files stay engine-free.
logic = root / 'Assets/Konoha/Campaign/Logic'
for path in logic.glob('*.cs'):
    text = path.read_text()
    assert 'namespace Konoha.Campaign' in text, f'Logic outside Konoha.Campaign: {path}'
    assert not re.search(r':\s*(MonoBehaviour|NetworkBehaviour|ScriptableObject)\b', text), f'Engine component in logic: {path}'
    if path.name != 'CampaignObjectiveDirector.cs':
        assert 'using UnityEngine' not in text, f'Pure logic must not depend on UnityEngine: {path}'

# One campaign version everywhere the owner sees or installs it.
preview = (root / 'Assets/Konoha/Editor/CampaignPreviewProject.cs').read_text()
menu = re.search(r'Prepare Jalur Takhta Preview ([\d.]+)"', preview).group(1)
bundle = re.search(r'bundleVersion = "([\d.]+)"', preview).group(1)
footer = re.search(r'"JALUR TAKHTA ([\d.]+)  •  SOLO PREVIEW"', preview).group(1)
code = int(re.search(r'bundleVersionCode = (\d+);', preview).group(1))
assert menu == bundle == footer, f'Campaign version mismatch: menu {menu}, bundle {bundle}, footer {footer}'
assert (root / f'Docs/JALUR_TAKHTA_{bundle}.md').exists(), f'Missing version note Docs/JALUR_TAKHTA_{bundle}.md'

# Hero model naming convention (see Docs/PANDUAN_MEGA_3D.md). Misnamed files are ignored
# by the generator, so report them.
heroes = ('Mega', 'Gemoy', 'Abah', 'PakWi')
clips = ('Idle', 'Run', 'Attack', 'Skill', 'Hit', 'Jump', 'Runtuh')
for path in (art_root / 'Heroes').glob('*/*.fbx') if (art_root / 'Heroes').exists() else []:
    hero, stem = path.parent.name, path.stem
    valid = hero in heroes and (stem == hero or stem in (f'{hero}@{clip}' for clip in clips))
    if not valid:
        warnings.append(f'Hero file not recognised by HeroVisualCatalog: {path.relative_to(root)}')

for warning in warnings:
    print('WARN:', warning)
print(f'PASS: JSON, assembly boundaries, unique Unity GUIDs, {len(sources)} source files, '
      f'{len(declared)} unique types, runtime free of editor API, pure campaign logic, '
      f'campaign version {bundle} (code {code}), Python syntax.')
print('NOT RUN: C# compilation, Unity import/tests, APK build, Android hardware.')
