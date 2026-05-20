# -*- coding: utf-8 -*-
"""
LEGADO / OBSOLETO — nao executar apos Onda 6b (2026-05-20).

Inseria [Obsolete] nos metodos Load* de Lib/LibDataSets.cs (Fase 5).
O ficheiro LibDataSets.cs foi REMOVIDO; lookups estao em ILookupQueryService.

Substituto: inventario e guardrails em
  Scripts/2026_05_20_gdi_inventory_libdatasets_usage.py
  Scripts/2026_05_20_gdi_audit_lookup_get_names.py
"""
from __future__ import print_function
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "Lib/LibDataSets.cs"

if __name__ == "__main__":
    if not path.is_file():
        print("OBSOLETO: Lib/LibDataSets.cs nao existe (Onda 6b). Script nao altera nada.")
        print("Use ILookupQueryService + Scripts/2026_05_20_gdi_inventory_libdatasets_usage.py --fail")
        sys.exit(0)
    print("AVISO: LibDataSets.cs ainda existe — script legado nao foi atualizado para mutar o ficheiro.")
    print("Contactar manutencao antes de executar.")
    sys.exit(1)
