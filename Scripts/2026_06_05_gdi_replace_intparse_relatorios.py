#!/usr/bin/env python3
"""I-1b: int.Parse -> LibNumbers.ConvertInt em Relatorios*Controller e Ged GetDados."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FILES = [
    ROOT / "Areas" / "gc" / "Controllers" / "RelatoriosComerciaisController.cs",
    ROOT / "Areas" / "gc" / "Controllers" / "RelatoriosFinanceirosController.cs",
    ROOT / "Areas" / "gc" / "Controllers" / "RelatoriosRegulamentacaoController.cs",
    ROOT / "Areas" / "g" / "Controllers" / "GedController.cs",
]

PATTERN = re.compile(r"int\.Parse\(")


def main() -> None:
    total = 0
    for path in FILES:
        text = path.read_text(encoding="utf-8")
        count = len(PATTERN.findall(text))
        if count == 0:
            continue
        new = PATTERN.sub("LibNumbers.ConvertInt(", text)
        path.write_text(new, encoding="utf-8")
        total += count
        print(f"{path.relative_to(ROOT)}: {count} substituicao(oes)")
    print(f"Total: {total}")


if __name__ == "__main__":
    main()
