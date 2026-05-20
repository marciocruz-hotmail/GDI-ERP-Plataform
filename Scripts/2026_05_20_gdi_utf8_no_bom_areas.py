# -*- coding: utf-8 -*-
"""Converte ficheiros de texto para UTF-8 sem BOM (pastas ou projeto inteiro)."""
from __future__ import print_function
import codecs
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AREAS_ROOT = ROOT / "Areas"

SKIP_DIRS = {
    ".git", "node_modules", "bin", "obj", "packages", "PackageTmp",
    ".vs", "agent-transcripts",
}

SKIP_SUFFIXES = {
    ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".bmp",
    ".dll", ".exe", ".pdf", ".zip", ".7z", ".rar", ".pfx", ".p12",
    ".woff", ".woff2", ".ttf", ".eot", ".otf",
    ".xls", ".xlsx", ".doc", ".docx",
}


def should_skip_path(path):
    try:
        rel = path.relative_to(ROOT)
    except ValueError:
        return True
    for part in rel.parts:
        if part in SKIP_DIRS:
            return True
    if path.suffix.lower() in SKIP_SUFFIXES:
        return True
    return False


def read_text_bytes(raw):
    if raw.startswith(codecs.BOM_UTF8):
        body = raw[3:]
        try:
            return body.decode("utf-8"), "utf-8-sig"
        except UnicodeDecodeError:
            return None, None
    if raw.startswith(b"\xff\xfe"):
        return raw[2:].decode("utf-16-le"), "utf-16-le"
    if raw.startswith(b"\xfe\xff"):
        return raw[2:].decode("utf-16-be"), "utf-16-be"
    try:
        return raw.decode("utf-8"), "utf-8"
    except UnicodeDecodeError:
        return None, None


def group_key(path):
    try:
        rel = path.relative_to(ROOT)
        return str(Path(*rel.parts[:2])) if len(rel.parts) >= 2 else str(rel.parts[0])
    except ValueError:
        return "?"


def resolve_targets(argv):
    """argv: [--root-only] [--project] [pastas relativas ao projeto]"""
    root_only = False
    project = False
    names = []
    for arg in argv:
        if arg in ("--root-only", "-root"):
            root_only = True
        elif arg in ("--project", "-p", "--all"):
            project = True
        else:
            names.append(arg)
    if project:
        return [(ROOT, False)]
    if not names:
        targets = [(ROOT, True)] if root_only else [(AREAS_ROOT, False)]
    else:
        targets = []
        for name in names:
            p = Path(name)
            if not p.is_absolute():
                p = ROOT / p
            targets.append((p.resolve(), root_only))
    return targets


def iter_candidates(base_dir, root_files_only=False):
    if root_files_only:
        for p in sorted(base_dir.iterdir()):
            if p.is_file():
                yield p
        return
    if base_dir.resolve() == ROOT.resolve():
        for p in sorted(base_dir.rglob("*")):
            if p.is_file() and not should_skip_path(p):
                yield p
        return
    for p in sorted(base_dir.rglob("*")):
        if p.is_file():
            yield p


def convert_tree(base_dir, root_files_only=False):
    changed = []
    skipped = []
    for path in iter_candidates(base_dir, root_files_only):
        if should_skip_path(path) and base_dir.resolve() != ROOT.resolve():
            skipped.append(path)
            continue
        if path.suffix.lower() in SKIP_SUFFIXES:
            skipped.append(path)
            continue
        raw = path.read_bytes()
        if not raw:
            text, enc = "", "empty"
        else:
            text, enc = read_text_bytes(raw)
            if text is None:
                skipped.append(path)
                print("SKIP (binario?):", path.relative_to(ROOT))
                continue
        out = text.encode("utf-8")
        # Só regravar se mudar bytes (BOM removido ou re-encode UTF-16/cp1252)
        if enc == "utf-8" and raw == out:
            continue
        if raw != out:
            path.write_bytes(out)
            changed.append((path, enc))
    return changed, skipped


def main():
    targets = resolve_targets(sys.argv[1:])
    all_changed = []
    all_skipped = []
    for base, files_only in targets:
        if not base.is_dir():
            print("Nao encontrado:", base)
            return 1
        ch, sk = convert_tree(base, root_files_only=files_only)
        all_changed.extend(ch)
        all_skipped.extend(sk)
    by_group = defaultdict(int)
    by_enc = defaultdict(int)
    for path, enc in all_changed:
        by_group[group_key(path)] += 1
        by_enc[enc] += 1
    print("Convertidos: %d" % len(all_changed))
    for grp in sorted(by_group):
        print("  %s: %d" % (grp, by_group[grp]))
    for enc in sorted(by_enc):
        print("  encoding [%s]: %d" % (enc, by_enc[enc]))
    if all_skipped:
        print("Ignorados (ext/binario): %d" % len(all_skipped))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
