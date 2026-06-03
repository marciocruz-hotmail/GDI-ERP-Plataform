import json
from collections import Counter

with open(r"c:\Marcio\Projetos\GDI-ERP-Plataform\Scripts\2026_06_01_pesquisar_inventory.json", encoding="utf-8-sig") as f:
    d = json.load(f)

views = [v for v in d["views"] if v["btn_pesquisar"]]
print("TOTAL btnPesquisar + DataTable:", len(views))
print("COM Limpar:", sum(1 for v in views if v["btn_limpar"]))
print("SEM Limpar:", [v["view"] for v in views if not v["btn_limpar"]])
print()

flags = ["btn_limpar", "yes_filter_field", "indicador_fn", "indicador_init", "xhr_yes_filter",
         "error_dt", "defer_loading", "b_server_side", "auto_pesquisa_change", "keypress_enter",
         "restore_filter", "select2_lookup", "datepicker_filtro"]
print("FREQUENCIA (% das views com Pesquisar):")
for k in flags:
    n = sum(1 for v in views if v[k])
    print(f"  {k}: {n}/{len(views)} ({100*n//len(views)}%)")

print("\nYESFILTER CONVENCOES:")
conv_a = sum(1 for v in views if v["yesFilterField_pesquisar"] == "" and v["yesFilterField_limpar"] == "*")
conv_b = sum(1 for v in views if v["yesFilterField_pesquisar"] == "*" and v["yesFilterField_limpar"] == "")
conv_c = sum(1 for v in views if v["yesFilterField_pesquisar"] == "*" and v["yesFilterField_limpar"] == "*")
print(f"  Padrao A (Produtos/Cidades): Pesquisar='' Limpar='*': {conv_a}")
print(f"  Padrao B (Atendimentos/GED): Pesquisar='*' Limpar='': {conv_b}")
print(f"  Padrao C (Financeiro): ambos '*': {conv_c}")

print("\nTABELA:")
for v in sorted(views, key=lambda x: x["view"]):
    yf = "P:%s L:%s" % (v["yesFilterField_pesquisar"] or "?", v["yesFilterField_limpar"] or "-")
    feats = []
    if v["btn_limpar"]: feats.append("Limpar")
    if v["indicador_fn"]: feats.append("Indicador")
    if v["defer_loading"]: feats.append("deferLoading")
    if v["auto_pesquisa_change"]: feats.append("autoChange")
    if v["keypress_enter"]: feats.append("Enter")
    if v["restore_filter"]: feats.append("RestoreFilter")
    if v["select2_lookup"]: feats.append("Select2/Lookup")
    if v["datepicker_filtro"]: feats.append("Datepicker")
    print("|".join([
        v["view"].replace("Areas/", ""),
        v["datatable_ids"][0],
        v["ajax_actions"][0] if v["ajax_actions"] else "?",
        v["fn_pesquisar"][0] if v["fn_pesquisar"] else "?",
        yf,
        ", ".join(feats) or "-",
    ]))
