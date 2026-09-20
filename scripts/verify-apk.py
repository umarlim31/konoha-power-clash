#!/usr/bin/env python3
"""Artifact sanity only, not install/runtime evidence."""
import sys
import zipfile
with zipfile.ZipFile(sys.argv[1]) as archive:
    if archive.testzip() is not None:
        raise SystemExit('FAIL: APK zip integrity')
    names = set(archive.namelist())
    required = {'AndroidManifest.xml', 'lib/arm64-v8a/libil2cpp.so', 'lib/arm64-v8a/libunity.so'}
    if not required.issubset(names) or not any(n.endswith('.dex') for n in names):
        raise SystemExit('FAIL: required ARM64/IL2CPP APK payload missing')
print('APK structure verified; install and runtime remain untested.')
