Remover Rotativa
Python com WeasyPrint, como microserviço
python# pdf_service.py — Flask/FastAPI minimalista
from weasyprint import HTML
@app.post("/render")
def render():
    pdf_bytes = HTML(string=request.data).write_pdf()
    return Response(pdf_bytes, mimetype="application/pdf")
ASP.NET chama via HttpClient. WeasyPrint é puro Python + libs C (Pango/Cairo).
Prós: isolamento total; o ERP fica desacoplado.
Contras: introduz Python + serviço Windows no servidor — não é "uma classe", é uma stack inteira nova. CSS suportado por WeasyPrint é bom, mas não é Chromium — testes obrigatórios para boleto.