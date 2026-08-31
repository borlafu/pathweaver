#!/usr/bin/env python3
"""Extracts TextMesh Pro's essential resources into Assets/.

TextMesh Pro ships its runtime shaders, settings asset, and default sprite asset inside a
`.unitypackage` in the uGUI package rather than as importable package content. Without them every
label renders as flat magenta, because its material points at a shader that is not in the project.

The Editor imports that file through `TMP_PackageResourceImporter.ImportResources`, which calls
`AssetDatabase.ImportPackage`. That is queued rather than performed, so in `-batchmode -quit` the
Editor exits before anything lands — which is how this script came to exist.

A `.unitypackage` is a gzipped tar of one directory per asset, each holding `pathname`, `asset`, and
`asset.meta`. Unpacking it by hand is exactly what the Editor does, and preserving each `asset.meta`
preserves the GUIDs the package's own assets use to reference one another.

Usage:
    ./scripts/import-tmp-resources.py [--force]
"""

from __future__ import annotations

import argparse
import os
import shutil
import sys
import tarfile
from pathlib import Path

UNITY_VERSION = "6000.5.9f1"
PACKAGE = (
    f"/Applications/Unity/Hub/Editor/{UNITY_VERSION}/Unity.app/Contents/Resources/"
    "PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/"
    "TMP Essential Resources.unitypackage"
)
DESTINATION = "Assets/TextMesh Pro"


def repository_root() -> Path:
    return Path(__file__).resolve().parent.parent


def extract(package: Path, root: Path) -> list[str]:
    """Writes every asset in the package to its recorded project path."""
    written: list[str] = []

    with tarfile.open(package, "r:gz") as archive:
        entries = {}
        for member in archive.getmembers():
            if not member.isfile():
                continue
            guid, _, kind = member.name.partition("/")
            entries.setdefault(guid, {})[kind] = member

        for guid, files in sorted(entries.items()):
            if "pathname" not in files or "asset" not in files:
                # A folder entry carries a pathname and a meta but no asset. Folders are created
                # implicitly by the files inside them, so there is nothing to do.
                continue

            pathname = archive.extractfile(files["pathname"]).read().decode("utf-8")
            # The pathname file occasionally carries a trailing line Unity ignores.
            relative = pathname.splitlines()[0].strip()

            target = root / relative
            if not target.resolve().is_relative_to(root.resolve()):
                raise SystemExit(f"Refusing to write outside the project: {relative}")

            target.parent.mkdir(parents=True, exist_ok=True)

            with archive.extractfile(files["asset"]) as source, open(target, "wb") as sink:
                shutil.copyfileobj(source, sink)

            if "asset.meta" in files:
                # Preserved rather than regenerated, so the GUIDs the package's own assets use to
                # reference one another keep pointing at the right files.
                with archive.extractfile(files["asset.meta"]) as source, open(
                    f"{target}.meta", "wb"
                ) as sink:
                    shutil.copyfileobj(source, sink)

            written.append(relative)

    return written


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--force",
        action="store_true",
        help="overwrite an existing import instead of refusing",
    )
    arguments = parser.parse_args()

    package = Path(PACKAGE)
    if not package.is_file():
        print(f"No package at {package}", file=sys.stderr)
        print(f"Is Unity {UNITY_VERSION} installed with the uGUI package?", file=sys.stderr)
        return 1

    root = repository_root()
    destination = root / DESTINATION

    if destination.exists() and not arguments.force:
        print(f"{DESTINATION} already exists. Pass --force to overwrite.")
        return 0

    written = extract(package, root)
    if not written:
        print("The package contained no assets, which should be impossible.", file=sys.stderr)
        return 1

    print(f"Imported {len(written)} asset(s):")
    for relative in written:
        print(f"  {relative}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
