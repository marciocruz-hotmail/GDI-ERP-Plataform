from pathlib import Path
ROOT = Path(r"c:\Marcio\Projetos\GDI-ERP-Plataform")
BAD_OPEN = "<" + "m" + "otion"
BAD_CLOSE = "</" + "m" + "otion>"
GOOD_OPEN = "<div"
GOOD_CLOSE = "</motion>"
GOOD_CLOSE = "</" + "div>"

for p in ROOT.rglob("Index.cshtml"):
    if "Areas" not in str(p):
        continue
    t = p.read_text(encoding="utf-8")
    if BAD_OPEN not in t and BAD_CLOSE not in t:
        continue
    n = t.replace(BAD_OPEN, GOOD_OPEN).replace(BAD_CLOSE, GOOD_CLOSE)
    p.write_text(n, encoding="utf-8")
    print("fixed", p.relative_to(ROOT))
