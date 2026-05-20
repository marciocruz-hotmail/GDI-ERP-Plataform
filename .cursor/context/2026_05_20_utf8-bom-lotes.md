# Grupo 2.11 — UTF-8 sem BOM (lotes por área)

**Data:** 2026-05-20  
**Checklist:** `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md` §2.11

## Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Inventário (read-only)                                    │
│    Scripts/2026_05_20_gdi_inventory_utf8_bom.py [--fail]     │
│    → conta BOM/UTF-16 por grupo (Areas/g, Areas/gc, Lib…)   │
└──────────────────────────┬──────────────────────────────────┘
                           │ se utf8_bom > 0
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Conversão por lote (só encoding, sem refactor funcional)  │
│    Scripts/2026_05_20_gdi_utf8_no_bom_areas.py <pasta>       │
│    ou 2026_05_20_gdi_utf8_no_bom_area_a.py (atalho Areas/a)  │
└──────────────────────────┬──────────────────────────────────┘
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Re-scan inventário → 0 BOM; PR só encoding se houve diff  │
└─────────────────────────────────────────────────────────────┘
```

| Script | Função | Quando usar |
|--------|--------|-------------|
| `2026_05_20_gdi_inventory_utf8_bom.py` | Scan read-only | Antes/depois de lote; CI local `--fail` |
| `2026_05_20_gdi_utf8_no_bom_areas.py` | Remove BOM / normaliza UTF-8 | Lote por pasta: `Areas/g`, `Lib`, `--project` (projeto inteiro) |
| `2026_05_20_gdi_utf8_no_bom_area_a.py` | Igual, só `Areas/a` | Atalho; equivalente a `areas.py Areas/a` |

**Regra de PR:** uma PR = só encoding (sem alteração de lógica, filtros, DataTables, etc.).

## Estado do repositório (2026-05-20)

| Evento | Resultado |
|--------|-----------|
| Lote global anterior | `areas.py --project` — **146** ficheiros convertidos (CHANGELOG 2026-05-20) |
| Inventário §2.11 | `inventory_utf8_bom.py` projeto inteiro → **0** BOM, **0** UTF-16 |
| Lotes por área (smoke) | `area_a.py`, `areas.py Areas/g`, `Areas/gc`, `Lib` → **0** convertidos |

**Conclusão:** scripts **continuam válidos** para regressões (VS a gravar com BOM). Não é necessário novo lote de conversão nesta sessão.

## Procedimento recomendado (regressão)

```powershell
# 1) Inventário
python Scripts/2026_05_20_gdi_inventory_utf8_bom.py --fail

# 2) Se falhar, um lote por área (exemplo)
python Scripts/2026_05_20_gdi_utf8_no_bom_areas.py Areas/gc
python Scripts/2026_05_20_gdi_inventory_utf8_bom.py --fail
```

Ordem sugerida de lotes futuros (se BOM reaparecer): `Areas/a` → `Areas/g` → `Areas/gc` → `Areas/crm` → `Areas/qa` → `Lib` → `Views` → raiz (`Web.config`, `.csproj`).

## Diferenças entre scripts de conversão

- **`area_a.py`:** hardcoded `Areas/a`; aceita cp1252 como fallback (legado).
- **`areas.py`:** qualquer pasta ou `--project`; não tenta cp1252; ignora `node_modules`, `bin`, `obj`, etc.

Preferir **`areas.py`** para lotes novos; manter **`area_a.py`** apenas se o lote for só auditoria (`Areas/a`).

## Editor

Configurar Visual Studio / Cursor: **UTF-8 without signature** para `.cs` e `.cshtml`, evitando reintrodução de BOM após cada conversão.
