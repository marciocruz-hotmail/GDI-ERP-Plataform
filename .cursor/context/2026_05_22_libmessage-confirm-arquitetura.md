# LibMessage* — pirâmide de APIs (2026-05-20)

## Uso recomendado

| API | Botões | Quando |
|-----|--------|--------|
| `LibMessageSuccess` | 1 (OK) | Sucesso pós-Ajax, confirmação simples |
| `LibMessageAlert` | 1 | Aviso informativo |
| `LibMessageError` | 1 | Erro / inconsistências |
| `LibMessageConfirm` | 2 | Confirmação destrutiva ou ação irreversível |
| `LibMessageConfirmChecklist` | 2 | Mensagem longa (`size: large`) |
| `GdiConfirmDesativarAnexo(onConfirm)` | 2 | Texto unificado desativar anexo GED/pedido |
| `LibMessageDialog` | N | **Legado** — manter só em `start.js`; não usar em views novas |

## Implementação

- Helpers: `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js`
- Shim: `gdi-swal2-dialog-shim.js` — `GdiSwal2.confirm` / `alert`
- Layout: `~/bundles/libui-swal-compat` + `start.js?v=VersionERP`

## Opções comuns (`LibMessageConfirm`)

```javascript
LibMessageConfirm('Confirmação', msgHtml, {
  icon: 'warning',           // question | warning | error
  size: 'large',             // checklist / texto longo (ConfirmChecklist define por omissão)
  closeButton: false,
  onEscape: false,
  backdrop: true,
  confirmLabel: '<i class="fa-solid fa-save me-2"></i>Confirmar',
  cancelLabel: '<i class="fa fa-undo me-2"></i>Cancelar',
  onConfirm: function () { /* ação */ LibMessageHideAll(); },
  hideOnCancel: true         // omissão: LibMessageHideAll no cancelar
});
```

## Migração concluída

- Fase A: ~102 blocos OK único → `LibMessageSuccess` (script `2026_05_22_gdi_migrate_libmessage_dialog_single_ok.py`).
- Fase B: 17 blocos 2 botões → `LibMessageConfirm*` / `GdiConfirmDesativarAnexo`.
- Inventário views: `grep LibMessageDialog Areas` = 0.

## Smoke manual sugerido

1. Pedido: excluir item, desativar anexo.
2. Separação / expedição: checklist antes de enviar form.
3. Estoque medição: cancelar medição (large + warning).
4. Parâmetros: ativar/desativar sistema.
5. Financeiro movimentos: cancelar título.
