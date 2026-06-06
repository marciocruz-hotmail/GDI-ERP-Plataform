# Relatório de compatibilidade: .NET Framework 4.7.2 → 4.8.1

**Repositório:** GDI-ERP-Plataform  
**Data da análise:** 11/05/2026 (actualizado 2026-05-25)  
**Escopo:** bump TFM opcional no **mesmo** monólito MVC/IIS (sem ASP.NET Core).

> **Decisão 2026-05-25:** migração para **ASP.NET Core** / .NET 6+ **cancelada**. Este relatório não cobre reescrita Core.

---

## Resumo executivo

| Área | Conclusão |
|------|-----------|
| **Projetos** | Um único `GDI-ERP-Plataform.csproj` (ASP.NET MVC). Solução em `GDI-ERP-Plataform.slnx`. Não há `.vbproj`, `.sln` clássico, `.aspx`/`.ascx`/`.asmx` nem `.xaml` de aplicação. |
| **Migração de TFM** | `TargetFrameworkVersion` e `Web.config` (`compilation` / `httpRuntime`) estão em **4.7.2**; alinhar a **4.8.1** é o passo central esperado. |
| **Risco principal identificado** | Uso repetido de `SecurityProtocolType.Tls12 \| SecurityProtocolType.Ssl3` — **Ssl3** é inseguro e tende a ser problemático em runtimes e políticas modernas. |
| **WWF / WebForms clássicos** | Sem `System.Activities` nem WebForms server controls no padrão analisado. |
| **Criptografia explícita (BCL)** | Não foram encontrados `MD5*`, `SHA1*`, `*Managed` legados nem `RijndaelManaged` no código `.cs` da aplicação. |

---

## 1. Target Framework

### 1.1 `<TargetFrameworkVersion>` em `.csproj` / `.vbproj`

- [x] Verificado: existe **um** projeto MSBuild: `GDI-ERP-Plataform.csproj`.
- [x] Valor atual: `v4.7.2`.

```21:22:GDI-ERP-Plataform.csproj
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <MvcBuildViews>false</MvcBuildViews>
```

- [x] Projetos a atualizar para `v4.8.1`: `GDI-ERP-Plataform.csproj` (único `.csproj` no repositório).
- [ ] `.vbproj`: nenhum arquivo encontrado.

### 1.2 `targetFramework` em `web.config` / `app.config`

**`Web.config` (raiz):**

```19:22:Web.config
  <system.web>
    <globalization culture="pt-BR" uiCulture="pt-BR" requestEncoding="utf-8" responseEncoding="utf-8" fileEncoding="utf-8" />
    <compilation debug="true" targetFramework="4.7.2" />
    <httpRuntime targetFramework="4.7.2" maxRequestLength="102400" executionTimeout="3600" />
```

- [x] Afetado: linhas **21–22** — devem passar a **4.8.1** em conjunto com o `.csproj` para comportamento coerente de ASP.NET.

**`Db/InserirMetadata.exe.config` (utilitário / exe, não o site):**

```3:5:Db/InserirMetadata.exe.config
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5.2" />
    </startup>
```

- [x] Observação: SKU **4.5.2** — fora do site principal; se o EXE for mantido, planejar atualização separada.

**`Web.Release.config`:** não redefine `targetFramework` (apenas transformação de `compilation`).

**`Views/Web.config` e `Areas/*/Views/web.config`:** sem nó `compilation targetFramework`.

### 1.3 Solução

- Arquivo: `GDI-ERP-Plataform.slnx` — referencia apenas `GDI-ERP-Plataform.csproj`.
- `.sln` tradicional: não encontrado no workspace.

---

## 2. Pacotes NuGet

### 2.1 Origem das referências

- [x] `packages.config` na raiz — **112** entradas `<package>`, todas com `targetFramework="net472"`.
- [x] `PackageReference` no `.csproj`: **não** utilizado (referências via `HintPath` para `packages\...`).

### 2.2 Lista de pacotes (resumo)

Lista completa em `packages.config` (**78** pacotes, 2026-05-25). Grupos relevantes:

| Categoria | Exemplos de IDs |
|-----------|-----------------|
| ASP.NET MVC / Web API | `Microsoft.AspNet.Mvc` 5.3.0, `Microsoft.AspNet.WebApi.*` 5.3.0 / Client 6.0.0, Razor/WebPages 3.3.0 |
| EF | `EntityFramework` 6.5.2 |
| Office / planilhas | `ClosedXML` 0.105.0, `NPOI` 2.8.0, `DocumentFormat.OpenXml` 3.5.1 |
| PDF / barcode | **Rotativa** 1.7.3, **Zen.Barcode.Rendering.Framework** 3.1.10729.1 |
| Cloud / HTTP | `AWSSDK.S3`, `RestSharp`, `Newtonsoft.Json` |
| Legado / bundling | `Antlr` 3.5.0.2, `Microsoft.AspNet.Web.Optimization` 1.1.3, `WebGrease` 1.6.0 |
| BCL / compat | `System.Text.Json` 10.0.7, `Microsoft.Bcl.*` 10.0.7, `Microsoft.Extensions.*`, facades `System.*` |

### 2.3 Pacotes sem evidência de incompatibilidade com 4.8.1

Não foi feita consulta ao NuGet.org pacote a pacote. Em geral, pacotes com `net472` / `net461` / `netstandard2.0` tendem a funcionar em .NET Framework 4.8.1.

### 2.4 Risco de manutenção (não necessariamente TFM)

- **Rotativa** 1.7.3 — pouco ativo; depende de **wkhtmltopdf** externo.
- **Zen.Barcode.Rendering.Framework** 3.1.10729.1 — tratar como legado.
- **Microsoft.AspNet.Web.Optimization** / **WebGrease** / **Antlr** / **Modernizr** — pilha clássica de bundling.

### 2.5 `NuGet.config`

- Não encontrado no repositório.

### 2.6 Observação em `Web.config` (binding redirects)

Redirects para `Microsoft.IdentityModel.*` e JWT em **8.15.0.0**, enquanto `packages.config` referencia **8.18.0** — validar em runtime após upgrade.

---

## 3. Criptografia e segurança

### 3.1 SHA1 / MD5 / *Managed / RijndaelManaged

- Nenhuma ocorrência relevante em `*.cs` (busca por APIs legadas da checklist).

### 3.2 `CryptographicException`

- Nenhuma ocorrência em `*.cs`.

### 3.3 Uso de certificados (BCL)

- `System.Security.Cryptography.X509Certificates` em `Robos/Itau/RoboItauBolecode.cs` (integração bancária).

### 3.4 TLS / SSL

**Padrão:** `ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3`

| Arquivo | Linhas |
|---------|--------|
| `Robos/SintegraWS/RoboSintegraWS.cs` | 85, 183, 281 |
| `Robos/Itau/RoboItauBolecode.cs` | 53, 219 |
| `Lib/LibEmail.cs` | 30 |
| `Robos/CotacaoDolar/RoboCotacaoDolar.cs` | 83, 128 |
| `Robos/Aws/BotAwsEmail.cs` | 33 |
| `Robos/CpfCnpj/RoboCpfCnpj.cs` | 86, 188 |
| `Robos/ENotas/RoboEnotasNFE.cs` | 329, 703, 1641, 1993, 2112, 2379, 2523, 2609, 2702, 2806 |

**Recomendação:** remover **Ssl3**; usar **Tls12** (e **Tls13** onde suportado) ou default do runtime após testes por endpoint.

**Nota:** `ControlVersion.cs` linha 331 — comentário histórico sobre `Tls12` (Itaú).

---

## 4. ASP.NET / WebForms

- Modelo: ASP.NET **MVC** (sem `.aspx`/`.ascx`/`.asmx` no repositório).
- `ScriptManager` / `UpdatePanel`: não encontrados.
- `CheckBox` + `InputAttributes` / `LabelAttributes`: não encontrados.
- Parse manual de `multipart/form-data`: não encontrado.
- `machineKey` em `*.config` do repo: não presente (menções apenas em documentação interna).

---

## 5. WPF / WinForms

### Referências no `.csproj`

- `PresentationCore`, `WindowsBase`, `System.Windows.Forms`.

### Uso no código

- **`System.Windows`:** `Areas/gc/Controllers/ComexImportacoesController.cs` — `Clipboard.SetText(...)` (várias linhas ~1616–2181). Uso em **servidor** IIS é frágil; exige testes.
- **`System.Windows.Interop`:** import em `Areas/gc/Controllers/MovimentosController.cs` — possível using não utilizado.
- **`System.Drawing`:** `MovimentosController.cs` (GDI+ no servidor).

### Checklist WPF (Grid, AppContext)

- Sem ficheiros `.xaml` de UI → N/A.

---

## 6. Windows Workflow Foundation (WWF)

- Sem `System.Activities`, workflows `.xaml` ou carregamento de instâncias persistidas.
- Mudança de checksum SHA1→SHA256 em WWF: N/A.

---

## 7. COM Interop / WinRT

- `[assembly: ComVisible(false)]` em `Properties/AssemblyInfo.cs`.
- Sem `[ComImport]`, `DispatchWrapper`, `SafeArray` identificados em `*.cs`.

---

## 8. Reflexão e APIs internas

- `Assembly.LoadFrom` / `LoadFile`: não encontrados.
- `AppDomain.CreateDomain`: não encontrado.
- `Type.GetType(`: não encontrado (padrão exato pesquisado).

---

## 9. Threading e async

- `Thread.Abort`: não encontrado.
- `ThreadPool.QueueUserWorkItem`: não encontrado.
- `SynchronizationContext`: não encontrado em `*.cs`.

---

## 10. Entity Framework / dados

- **EF 6.5.2** com `SqlProviderServices` e modelo EDMX (`Db/ModelDbGdiPlataform.*`), `DbContext` em `Db/ModelDbGdiPlataform.Context1.cs`.
- **Provider:** `System.Data.SqlClient` nas connection strings em `Web.config`.
- **`TransactionScope`:** não encontrado em `*.cs`.

**Segurança:** credenciais em texto claro em `Web.config` — tratar com segredos/configuração por ambiente (independente da migração de TFM).

---

## Inventário de extensões (checklist do pedido)

| Tipo | Situação |
|------|----------|
| `*.csproj` | 1 ficheiro |
| `*.sln` | 0 (existe `*.slnx`) |
| `*.vbproj`, `*.vb` | 0 |
| `*.xaml`, `*.aspx`, `*.ascx`, `*.asmx` | 0 |
| `*.config` | Web.config, transforms, Views/Areas, `Db/InserirMetadata.exe.config` |
| `packages.config` | 1 |
| `NuGet.config` | 0 |

---

## Plano de verificação pós-upgrade (recomendado)

1. Atualizar `v4.7.2` → `v4.8.1` no `.csproj` e `4.7.2` → `4.8.1` em `<compilation>` e `<httpRuntime>` do `Web.config`.
2. Restaurar pacotes, recompilar; testar integrações dos **Robos** (TLS), **Rotativa/PDF**, **Graph/MSAL**, **EF** em homologação.
3. Rever `ServicePointManager` e remover **Ssl3** após validar cada parceiro.
4. Validar `Clipboard` em `ComexImportacoesController` no IIS real.

---

*Documento gerado com base em pesquisa estática no repositório; não substitui testes de regressão nem validação em ambiente de produção.*
