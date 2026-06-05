#!/usr/bin/env python3
"""Delega helpers privados JsonDataTableException* → GdiMvcJsonResults.DataTableError."""
import glob
import os
import re

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

PATTERNS = [
    (
        """        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            string errorMessage = LibExceptions.getExceptionShortMessage(e);
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = e.ToString(),
                yesFilterOnOff = yesFilterOnOff ?? "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }""",
        """        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }""",
        "3-param",
    ),
    (
        """        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param)
        {
            string errorMessage = LibExceptions.getExceptionShortMessage(e);
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = e.ToString(),
                yesFilterOnOff = "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }""",
        """        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, "0"), JsonRequestBehavior.AllowGet);
        }""",
        "2-param",
    ),
    (
        """        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string filterOnOff)
        {
            return Json(new
            {
                errorMessage         = LibExceptions.getExceptionShortMessage(e),
                severity             = "error",
                stackTrace           = e.ToString(),
                yesFilterOnOff       = filterOnOff ?? "0",
                sEcho                = param?.sEcho,
                iTotalRecords        = 0,
                iTotalDisplayRecords = 0,
                aaData               = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }""",
        """        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string filterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, filterOnOff), JsonRequestBehavior.AllowGet);
        }""",
        "qa-filterOnOff",
    ),
    (
        """        private JsonResult JsonDataTableExceptionFretes(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(new
            {
                errorMessage = LibExceptions.getExceptionShortMessage(e),
                severity = "error",
                stackTrace = e.ToString(),
                yesFilterOnOff = yesFilterOnOff ?? "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }""",
        """        private JsonResult JsonDataTableExceptionFretes(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }""",
        "fretes",
    ),
]

USING = "using GdiPlataform.Lib;"


def ensure_using(content):
    if USING in content:
        return content, False
    m = re.search(r"(using GdiPlataform\.[^\n]+;\n)", content)
    if m:
        insert_at = m.end()
        return content[:insert_at] + USING + "\n" + content[insert_at:], True
    return content, False


def main():
    files = glob.glob(os.path.join(REPO, "Areas", "**", "Controllers", "*.cs"), recursive=True)
    changed = []
    for path in sorted(files):
        text = open(path, encoding="utf-8", errors="replace").read()
        if "JsonDataTableException" not in text:
            continue
        orig = text
        applied = []
        for old, new, label in PATTERNS:
            if old in text:
                text = text.replace(old, new)
                applied.append(label)
        if text == orig:
            continue
        text, added_using = ensure_using(text)
        open(path, "w", encoding="utf-8", newline="\n").write(text)
        rel = os.path.relpath(path, REPO).replace("\\", "/")
        changed.append((rel, applied, added_using))

    print(f"Updated: {len(changed)}")
    for rel, applied, added_using in changed:
        u = " +using Lib" if added_using else ""
        print(f"  {rel} [{', '.join(applied)}]{u}")


if __name__ == "__main__":
    main()
