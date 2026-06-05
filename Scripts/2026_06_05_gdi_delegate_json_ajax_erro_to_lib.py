#!/usr/bin/env python3
"""Delega helpers privados JsonAjaxErro* / JsonPedidoNaoEncontrado → GdiMvcJsonResults."""
import glob
import os
import re

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

PATTERNS = [
    (
        """        private JsonResult JsonPedidoNaoEncontrado(int idMovimento = 0)
        {
            String msg = "Pedido não localizado.";
            if (idMovimento > 0) { msg = "Pedido Nº " + idMovimento.ToString() + " não localizado."; }
            return Json(new { success = false, msg = msg }, JsonRequestBehavior.AllowGet);
        }""",
        """        private JsonResult JsonPedidoNaoEncontrado(int idMovimento = 0)
        {
            return Json(GdiMvcJsonResults.PedidoNaoEncontrado(idMovimento), JsonRequestBehavior.AllowGet);
        }""",
    ),
    (
        """        private JsonResult JsonAjaxErro(Exception ex)
        {
            return Json(new { success = false, msg = LibExceptions.getExceptionShortMessage(ex) }, JsonRequestBehavior.AllowGet);
        }""",
        """        private JsonResult JsonAjaxErro(Exception ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailure(ex), JsonRequestBehavior.AllowGet);
        }""",
    ),
    (
        """        private JsonResult JsonAjaxErroValidacao(DbEntityValidationException ex)
        {
            return Json(new { success = false, msg = LibExceptions.getDbEntityValidationException(ex) }, JsonRequestBehavior.AllowGet);
        }""",
        """        private JsonResult JsonAjaxErroValidacao(DbEntityValidationException ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet);
        }""",
    ),
    (
        """        private JsonResult JsonAjaxErroComItems(Exception ex)
        {
            return Json(new { success = false, msg = LibExceptions.getExceptionShortMessage(ex), items = new object[0] }, JsonRequestBehavior.AllowGet);
        }""",
        """        private JsonResult JsonAjaxErroComItems(Exception ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailureWithItems(ex), JsonRequestBehavior.AllowGet);
        }""",
    ),
]

USING = "using GdiPlataform.Lib;"


def ensure_using(content):
    if USING in content:
        return content, False
    m = re.search(r"(using GdiPlataform\.[^\n]+;\n)", content)
    if m:
        return content[: m.end()] + USING + "\n" + content[m.end() :], True
    return content, False


def main():
    files = glob.glob(os.path.join(REPO, "Areas", "**", "Controllers", "*.cs"), recursive=True)
    changed = []
    for path in sorted(files):
        text = open(path, encoding="utf-8", errors="replace").read()
        if "JsonAjaxErro" not in text and "JsonPedidoNaoEncontrado" not in text:
            continue
        orig = text
        for old, new in PATTERNS:
            text = text.replace(old, new)
        if text == orig:
            continue
        text, added = ensure_using(text)
        open(path, "w", encoding="utf-8", newline="\n").write(text)
        rel = os.path.relpath(path, REPO).replace("\\", "/")
        changed.append((rel, added))
    print(f"Updated: {len(changed)}")
    for rel, added in changed:
        print(f"  {rel}" + (" +using Lib" if added else ""))


if __name__ == "__main__":
    main()
