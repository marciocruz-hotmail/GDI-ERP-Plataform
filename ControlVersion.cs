using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform
{
    public static class ControlVersion
    {
        public static String getVersion()
        {
            return "2026.51.31 - 01/06/2026";
        }
        public static String getShortVersion()
        {
            return "2026.51.31";
        }
    }
}

// ########## HISTÓRICO DAS VERSÕES ##########
// 2026.51.31 - 01/06/2026 - gdi-select2: pesquisa local Select2 em listas estáticas (2+ options; antes limite ≤5)
// 2026.51.30 - 01/06/2026 - gdi-select2: allowClear em lookups Ajax (option vazia + select2:clear → change)
// 2026.51.29 - 25/05/2026 - NF entrada nacional: Select2 Ajax (GdiPageScripts) + layout tabela MVC
// 2026.51.28 - 25/05/2026 - Atualização Enotas
// 2026.51.24 - 21/05/2026 - G-PERF-20: TempusDominus no layout por flag; rotas jsDatepicker; Parametros DT; IndexPops jstree
// 2026.51.23 - 20/05/2026 - LibMessageConfirm/Checklist + GdiConfirmDesativarAnexo; migração 17 LibMessageDialog (2 botões) em views
// 2026.51.22 - 20/05/2026 - gdi-datatables-defaults: restaura $.fn.DataTable().api() (corrige .draw em Limpar/Pesquisar)
// 2026.51.21 - 20/05/2026 - DataTables deferLoading: mensagem aguardando filtro (nao Carregando); hook global
// 2026.51.20 - 20/05/2026 - DataTables PT-BR global: gdi-datatables-defaults (lazy _Blank + registry); GdiDataTablesPtBr
// 2026.51.19 - 20/05/2026 - DataTables: colspan vazio (dt-empty) — CSS :has + remoção host em grelhas <=10 col.
// 2026.51.18 - 20/05/2026 - gdi-datatables-defaults.js: oLanguage PT-BR global; dt-empty visível no host
// 2026.51.17 - 20/05/2026 - start.css: DataTables gdi-dt-scroll-host quebra linha por omissão; .dt-wrap/.dt-nowrap
// 2026.51.16 - 20/05/2026 - GdiMainModalLoad: aguarda Tempus defer do host (evita reload ~25s no modal)
// 2026.51.15 - 20/05/2026 - G-PERF-20: bootstrap.bundle em páginas sem DataTables; GdiMainModalShow
// 2026.51.14 - 20/05/2026 - G-PERF-20f: GdiMainModalLoad extrai body de _Modal.cshtml (modais hubs relatório)
// 2026.51.13 - 20/05/2026 - G-PERF-20f: GdiMainModalLoad executa scripts do modal; hub RelatoriosRegulamentacao layout
// 2026.51.12 - 20/05/2026 - G-PERF-20f: lazy load scripts #mainModal (GdiLoadScriptOnce, patch jQuery.load)
// 2026.51.11 - 20/05/2026 - G-PERF-20 Fase 4 lote C: LayoutLite por action (CreateEdit, IndexPops, forms COMEX/NF)
// 2026.51.10 - 20/05/2026 - G-PERF-20 Fase 4 lote B: Treinamentos + Pedidos portal [GdiPageScripts]; preset LayoutPortalCliente
// 2026.51.09 - 20/05/2026 - G-PERF-20 Fase 3: partials agregados Head/ScriptsOptional + GdiPageScriptsView; data-gdi-page-scripts
// 2026.51.08 - 20/05/2026 - G-PERF-20e: [GdiPageScripts] hubs validados; presets LayoutHub*; RelatoriosFinanceiros +Select2
// 2026.51.07 - 20/05/2026 - G-PERF-20d: _Layout scripts condicionais + GdiPageScriptsActionFilter; opt-out DT hubs relatório
// 2026.51.06 - 20/05/2026 - G-PERF-20c-bis: Tempus fora do _Layout; partials em 22 views (jsDatepicker + hubs modais)
// 2026.51.05 - 20/05/2026 - G-PERF-20c: jstree fora do _Layout global; partials CentrosCustos + ClassificacaoFinanceira
// 2026.51.04 - 20/05/2026 - G-PERF-20b: _Layout scripts no body + defer (DataTables/Select2/jstree/Tempus); partial _LayoutScriptsAuthenticated
// 2026.51.03 - 20/05/2026 - PUB-1/PUB-2: health /health, Release Web.config, cache-bust publish
// 2026.51.02 - 20/05/2026 - Tabelas MVC gdi-form-table-* / sidebar portal
// 2026.51.01 - 20/05/2026 - Tabelas MVC scroll-body-horizontal (lote forms)
// 2026.51.00 - 20/05/2026 - Edit in line / Filtros
// 2026.50.00 - 19/05/2026 - Modernização e globalização de SweetAlert2
// 2026.49.00 - 08/05/2026 - Validação de lotes nos movimentos
// 2026.48.00 - 04/05/2026 - Módulo Compras
// 2026.47.00 - 02/05/2026 - Pós Venda
// 2026.46.00 - 01/05/2026 - Pós Venda
// 2026.45.00 - 30/04/2026 - Duimp - Atualização em produção
// 2026.44.00 - 30/04/2026 - Duimp - Atualização em produção
// 2026.43.00 - 28/04/2026 - Duimp - Atualização em produção
// 2026.42.00 - 28/04/2026 - Duimp Tabelas
// 2026.41.00 - 27/04/2026 - Duimp Tabelas
// 2026.40.00 - 23/04/2026 - Pos-Venda Tabelas
// 2026.39.00 - 15/04/2026 - Cadastro de lotes e verificação de duplicidades
// 2026.38.00 - 09/04/2026 - Recebimento de produtos importados
// 2026.37.00 - 09/04/2026 - Gestão de Lotes
// 2026.36.00 - 08/04/2026 - Conferencia de lotes na importacao
// 2026.35.00 - 08/04/2026 - Conferencia de lotes na importacao
// 2026.34.00 - 06/04/2026 - Novo certificado itau GDI SP
// 2026.33.00 - 06/04/2026 - Nova NFE Nacionalização
// 2026.32.00 - 03/04/2026 - Tag cBenef sefaz SP
// 27.23 - 30/03/2026 - Controle de Lotes - Expedição
// 27.22 - 25/03/2026 - Controle de Lotes - Expedição
// 27.21 - 20/03/2026 - Atualizações
// 27.20 - 18/03/2026 - Correção erro ordenação relatório Excel Paulo
// 27.19 - 17/03/2026 - Controle de Lotes - Melhorias
// 27.18 - 16/03/2026 - Controle de Lotes
// 27.17 - 16/03/2026 - Erro na separação de estoque
// 27.16 - 16/03/2026 - Política de Senhas
// 27.15 - 16/03/2026 - Política de Senhas
// 27.14 - 16/03/2026 - Gestão de Lotes de Produtos
// 27.13 - 12/03/2026 - Update FormModalPedidoCartaCorrecao
// 27.12 - 08/03/2026 - Documentação ISO
// 27.11 - 03/03/2026 - Relatório de Regulamentação PF
// 27.10 - 03/03/2026 - Saldo do Adiantamento
// 27.09 - 02/03/2026 - Gestão comercial
// 27.08 - 26/02/2026 - Atualização de demandas solicitadas no chamado
// 27.07 - 26/02/2026 - Atualizar competências de estoque
// 27.06 - 24/02/2026 - Congelar posição do estoque
// 27.05 - 24/02/2026 - Validações de separação 
// 27.04 - 24/02/2026 - Atualização certificado Digital Itau - Senha
// 27.03 - 24/02/2026 - Atualização certificado Digital Itau
// 27.02 - 24/02/2026 - Correções/Atualizações Saldo de Adiantamento e NFe Referenciada no pedido
// 27.01 - 20/02/2026 - Migração VS2026
// 26.50 - Validação do Saldo de Adiantamento somente no antecipado
// 26.49 - Observações da negociação
// 26.48 - Correções/Atualizações Gerais
// 26.47 - GED Pops
// 26.46 - Parametrização por tipo de operação
// 26.45 - Novo departamento de Atendimento (Compras e tecnologia)
// 26.44 - Confirmar CheckList
// 26.43 - Atualização carta de correção filial SP
// 26.42 - Movimentos - Expedição somente na filial de origem do pedido
// 26.41 - Ajustes de coligada e filial Global
// 26.40 - Ajustes/Correções Gerais
// 26.39 - Ajustes/Correções Gerais
// 26.38 - Historico comercial do item por cliente
// 26.37 - Gestão de Atendimentos - Limite de Crédito Inicial
// 26.36 - Gestão de Atendimentos - Anexos
// 26.35 - Gestão de Atendimentos - Anexos
// 26.34 - Gestão de Atendimentos
// 26.33 - Perfil de acesso
// 26.32 - Gestão de Atendimentos - Conclusão
// 26.31 - Gestão de Atendimentos - Atividades
// 26.30 - Gestão de Atendimentos - Atividades
// 26.29 - Gestão de Atendimentos - Atividades
// 26.28 - Gestão de Atendimentos - Atividades
// 26.27 - Gestão de Atividades
// 26.26 - Erro Anexo Documentos
// 26.25 - 18/01/2026 - Atualização Versão
// 26.24 - 18/01/2026 - Ajustes Modal
// 26.23 - 17/01/2026 - Universidade Corporativa GDI Aviação
// 26.22 - 17/01/2026 - Universidade Corporativa GDI Aviação
// 26.21 - 15/01/2026 - Relatório consolidado contas caixas paulo
// 26.20 - 14/01/2026 - Correção - Painel Gestão Mobile
// 26.19 - 14/01/2026 - GED - Qualidade
// 26.18 - 13/01/2026 - Correção erro upload anexos
// 26.17 - 12/01/2026 - Correção erro upload anexos
// 26.16 - 12/01/2026 - Correção erro upload anexos
// 26.15 - 12/01/2026 - Atualização ficha estoque
// 26.12 - 05/01/2026 - Correção erro ficha estoque
// 26.11 - 05/01/2026 - Ajuste NFE Gateway na NFE
// 26.10 - 05/01/2026 - Ajuste NFE Gateway na NFE
// 26.09 - 01/01/2026 - Ajustes IBS e CBS
// 26.08 - 01/01/2026 - Ajustes de erros nos relatórios
// 26.07 - 01/01/2026 - Ajustes de erros
// 26.06 - 01/01/2026 - Ajustes de erros
// 26.05 - 01/01/2026 - Ajustes de erros
// 26.04 - 01/01/2026 - Ajustes de erros
// 26.03 - 01/01/2026 - Ajustes de erros
// 26.02 - 01/01/2026 - Ajustes de erros
// 26.01 - 01/01/2026 - Atualização para SQL Server
// 23.08 - 15/12/2025 - Obs cnpj correto (matriz ou filial) no boleto e na cotação
// 23.07 - 14/12/2025 - Consistir saldo de adiantamento somente no antecipado
// 23.06 - 14/12/2025 - Separação de endereço, numero e complemento
// 23.05 - 13/12/2025 - Anexo na visão dos lançamentos financeiros
// 23.04 - 02/12/2025 - Filial SP - Geração dos Lançamentos na Conta Caixa da Filial
// 23.03 - 01/12/2025 - Filial SP
// 23.02 - 01/12/2025 - Filial SP
// 23.01 - 01/12/2025 - Filial SP
// 23.01 - 30/11/2025 - Filial SP
// 23.00 - 28/11/2025 - Filial SP
// 22.47 - 24/11/2025 - Relatório de Carteira de Clientes
// 22.46 - 22/11/2025 - Inventário
// 22.45 - 21/11/2025 - Inventário
// 22.44 - 19/11/2025 - Inventário
// 22.43 - 17/11/2025 - Aviso difal painel de Pedidos
// 22.42 - 14/11/2025 - Arquivo anexo no lançamento financeiro
// 22.41 - 11/11/2025 - Nova Conta Caixa - volta tudo para BH
// 22.40 - 03/11/2025 - Correção erro emissão bolecode itau
// 22.39 - 03/11/2025 - Novo CFOP Operações
// 22.38 - 29/10/2025 - Filial SP
// 22.38 - 27/10/2025 - Filial SP
// 22.37 - 08/10/2025 - Processamento de notas fiscais com muitos itens
// 22.36 - 15/09/2025 - Cadastro de novos produtos, processamento por blocos
// 22.35 - 09/09/2025 - Download XML Anp
// 22.34 - 27/08/2025 - Relatório Produtos Comercializados
// 22.33 - 14/08/2025 - Relatório Regulamentação Jogue Limpo
// 22.32 - 12/08/2025 - Relatório Regulamentação PF
// 22.31 - 25/07/2025 - Correção - Erro ao gerar relatório financeiro excel
// 22.30 - 23/07/2025 - Atualização servidor AWS SES
// 22.29 - 16/07/2025 - Atualização dos dados do contato na aprovação do pedido
// 22.28 - 14/07/2025 - Notificação Automática de Pedidos - WhatsApp, Email e Arquivos Anexados
// 22.27 - 11/07/2025 - Notificação Automática de Pedidos - WhatsApp, Email e Arquivos Anexados
// 22.26 - 10/07/2025 - Notificação Automática de Pedidos - WhatsApp
// 22.25 - 10/07/2025 - Notificação Automática de Pedidos
// 22.24 - 09/07/2025 - Notificação Automática de Pedidos
// 22.23 - 03/07/2025 - Peso do item na conferencia da importação
// 22.220 - 27/06/2025 - CFOP Operacoes trazendo da base de dados
// 22.219 - 18/06/2025 - Informações Complementares na NF
// 22.218 - 17/06/2025 - Parametrização das operações
// 22.217 - 17/06/2025 - Update
// 22.216 - 17/06/2025 - Update
// 22.215 - 16/06/2025 - Atualização CFOP Devolução brindes
// 22.214 - 16/06/2025 - Atualização Painel de Pedidos
// 22.213 - 16/06/2025 - Operação 020
// 22.211 - 12/06/2025 - Códigos Variação Itens
// 22.210 - 06/06/2025 - WallPaper
// 22.209 - 05/06/2025 - Ajustes
// 22.208 - 03/06/2025 - Recebimento da Importação
// 22.207 - 03/06/2025 - Update
// 22.206 - 27/05/2025 - Filtro no painel de pedidos
// 22.205 - 26/05/2025 - Destinatários nos pedidos
// 22.204 - 23/05/2025 - Associar item da invoice com o produto
// 22.203 - 23/05/2025 - Erro no Recebimento importacao 
// 22.202 - 23/05/2025 - Processamento de compra nacional
// 22.201 - 21/05/2025 - Produtos novos por filtro de usuário
// 22.200 - 18/05/2025 - Atualização Valor Pedido
// 22.200 - 17/05/2025 - Anexo nos Pedidos
// 22.177 - 13/05/2025 - LibFiles
// 22.176 - 12/05/2025 - Geração Boleto PDF
// 22.175 - 11/05/2025 - Recebimento Compra Nacional - e Devolução
// 22.174 - 10/05/2025 - Download PDF Invoice
// 22.173 - 09/05/2025 - Validação do Estoque
// 22.172 - 08/05/2025 - Ajuste Relatórios Atrasados
// 22.171 - 06/05/2025 - Relatórios Atrasados
// 22.170 - 06/05/2025 - Relatórios Vendedores
// 22.169 - 05/05/2025 - Erro perfil de usuário
// 22.168 - 05/05/2025 - Correção erro recebimento material
// 22.167 - 03/05/2025 - Atualização Imagem Inicial
// 22.166 - 02/05/2025 - Atualização do cadastro de clientes
// 22.165 - 01/05/2025 - Processamento Notas de Entrada
// 22.164 - 30/04/2025 - Conferencia dos itens na entrada
// 22.163 - 30/04/2025 - Conferencia dos itens na entrada
// 22.162 - 30/04/2025 - Conferencia dos itens na entrada
// 22.161 - 30/04/2025 - Informações do despachante no comex
// 22.160 - 29/04/2025 - CD - Centro de Distribuição nas Notas de Entrada
// 22.159 - 24/04/2025 - Copia dos pedidos
// 22.158 - 22/04/2025 - Copia dos pedidos
// 22.157 - 22/04/2025 - Recebimento/Baixa Estoque
// 22.156 - 20/04/2025 - Recebimento Importação
// 22.155 - 17/04/2025 - Local de Estoque na Nota de Entrada
// 22.154 - 16/04/2025 - Erro Separação
// 22.153 - 14/04/2025 - Inventário 
// 22.152 - 14/04/2025 - Inventário 
// 22.151 - 13/04/2025 - Inventário 
// 22.150 - 12/04/2025 - Relatório Regulamentação Ibama/ANP
// 22.149 - 11/04/2025 - Relatório Regulamentação Ibama/ANP
// 22.148 - 07/04/2025 - Correção erro relatório comissão
// 22.147 - 07/04/2025 - Correção erro relatório comissão e Desativação GED
// 22.146 - 31/03/2025 - Correção quebra de página PDF da Invoice Comercial
// 22.145 - 27/03/2025 - Correção quebra de página PDF da Invoice Comercial
// 22.144 - 26/03/2025 - Filiais / Contexto
// 22.143 - 24/03/2025 - Processamento NF Entrada
// 22.142 - 24/03/2025 - Update
// 22.142 - 17/03/2025 - Atualização de Segurança
// 22.141 - 17/03/2025 - Cotação Dollar - UOL Economia
// 22.140 - 14/03/2025 - Transferência Gerencial
// 22.139 - 11/03/2025 - GED Integrado - Início
// 22.138 - 27/02/2025 - Relatório Vendedores
// 22.137 - 27/02/2025 - Certificado Itau Atualizado
// 22.136 - 27/02/2025 - Nova Operação - Serviços
// 22.135 - 24/02/2025 - Certificado Itau Atualizado
// 22.134 - 21/02/2025 - Recebimento de Importação
// 22.133 - 21/02/2025 - Recebimento de Importação
// 22.132 - 20/02/2025 - Recebimento de Importação
// 22.131 - 20/02/2025 - Recebimento de Importação
// 22.130 - 19/02/2025 - Ajuste lista suspensa modal inserir item pedido
// 22.129 - 17/02/2025 - Tipos de Movimentações do Estoque
// 22.128 - 17/02/2025 - Posição Atual do Estoque
// 22.127 - 17/02/2025 - Posição Atual do Estoque
// 22.126 - 16/02/2025 - Inventário de Produtos
// 22.125 - 16/02/2025 - Inventário de Produtos
// 22.124 - 13/02/2025 - Cadastro de Produtos
// 22.123 - 12/02/2025 - Ficha Estoque
// 22.122 - 12/02/2025 - Baixa Estoque
// 22.121 - 12/02/2025 - Baixa Estoque
// 22.120 - 11/02/2025 - Baixa Estoque
// 22.119 - 09/02/2025 - Inventário
// 22.118 - 08/02/2025 - Inventário
// 22.117 - 08/02/2025 - Inventário
// 22.116 - 06/02/2025 - Update
// 22.115 - 05/02/2025 - Venda e Remessa Futura
// 22.114 - 30/01/2025 - Venda e Remessa Futura
// 22.113 - 27/01/2025 - Modal Aprovação 8 ou 9 dígitos no telefone
// 22.112 - 27/01/2025 - Relatorio de comissionamento
// 22.111 - 27/01/2025 - Contato na confirmação do pedido
// 22.110 - 26/01/2025 - RElatórios Vendedores
// 22.109 - 22/01/2025 - RElatórios Vendedores
// 22.108 - 16/01/2025 - Aprovação no Pedido - Atualizações
// 22.107 - 12/01/2025 - Aprovação no Pedido
// 22.106 - 08/01/2025 - Item Regulamentado
// 22.105 - 18/12/2024 - Pedidos - Pesquisa por valor
// 22.101 - 10/12/2024 - Inclusão de Tarefas/Requisições nos Clientes
// 22.100 - 09/12/2024 - Inclusão de Tarefas/Requisições nos Pedidos
// 22.002 - 21/11/2024 - Controllers
// 22.001 - 14/11/2024 - Protocolo de Cancelamento Siare
// 21.099 - 12/11/2024 - Atualização do cadastro de produtos
// 21.098 - 11/11/2024 - Atualização do cadastro de produtos
// 21.097 - 04/11/2024 - Cálculo do Markup na criação do novo pedido (ainda não salvou os ids nas tabelas)
// 21.096 - 29/10/2024 - Validação Markup ao alterar o pedido
// 21.095 - 17/10/2024 - Validação Markup dos itens ao aprovar o pedido
// 21.094 - 08/10/2024 - Informação de lote na separação
// 21.093 - 30/09/2024 - Informação de lote na separação
// 21.092 - 30/09/2024 - Correção numero do banco nos boletos
// 21.091 - 27/09/2024 - Pedidos de Vendas - Validações
// 21.090 - 25/09/2024 - GED
// 21.089 - 24/09/2024 - Duplicar item financeiro
// 21.088 - 16/09/2024 - Importação de Itens
// 21.087 - 11/09/2024 - Importação de Itens
// 21.086 - 06/09/2024 - Cálculo Difal
// 21.085 - 29/08/2024 - Cálculo Markup
// 21.084 - 27/08/2024 - Não atualizar o valor do item ao duplicar item do pedido
// 21.083 - 27/08/2024 - Atualização Fob Produtos
// 21.082 - 27/08/2024 - Correção erros cadastro novos itens
// 21.081 - 27/08/2024 - Busca de produtos por PN e PN Auxiliar
// 21.080 - 26/08/2024 - Cadastro de Produtos
// 21.079 - 23/08/2024 - Aprovação de pedidos (Validação de itens temporários)
// 21.078 - 21/08/2024 - Cadastro de produtos COMEX
// 21.077 - 19/08/2024 - Espelho digital gerencial - Correção
// 21.076 - 18/08/2024 - Espelho digital gerencial
// 21.075 - 16/08/2024 - Dados de Modelo, Série e Registro das aeronaves
// 21.074 - 16/08/2024 - Cotação nos pedidos
// 21.073 - 15/08/2024 - Nova trilha de auditoria
// 21.072 - 14/08/2024 - Novo modelo de boleto
// 21.071 - 13/08/2024 - Validação duplicidade de produto
// 21.070 - 07/08/2024 - GED da Qualidade
// 21.069 - 06/08/2024 - Filtro de Adiantamento nos lançamentos financeiros
// 21.068 - 05/08/2024 - Markup pela cotação do dollar do dia
// 21.067 - 05/08/2024 - Markup
// 21.066 - 04/08/2024 - Formatação PN
// 21.065 - 04/08/2024 - Preço FOB
// 21.064 - 31/07/2024 - Importação NF Devolução
// 21.063 - 31/07/2024 - Custos de Importação
// 21.062 - 29/07/2024 - Custos de Importação
// 21.061 - 25/07/2024 - Informações de Frete Complementar na atividade de Entrega
// 21.060 - 24/07/2024 - Geração de financeiro com adiantamento
// 21.059 - 23/07/2024 - Ordenação Cambio
// 21.058 - 23/07/2024 - Correção erro importar movimentos
// 21.057 - 22/07/2024 - Gestão a vista
// 21.056 - 19/07/2024 - Gestão a vista
// 21.055 - 18/07/2024 - Acerto codigo produto carregar itens
// 21.054 - 17/07/2024 - Acertar cadastro manual produtos
// 21.053 - 11/07/2024 - Carregar lista importação
// 21.052 - 10/07/2024 - Importar lista itens / Upload GED Comex
// 21.051 - 05/07/2024 - Importar lista itens
// 21.051 - 05/07/2024 - Importar lista itens
// 21.050 - 03/07/2024 - Cancelar importação de produtos temporarios
// 21.049 - 02/07/2024 - Atualização da base de cálculo icms considerando o valor do frete quando e CIF
// 21.048 - 02/07/2024 - Mostrar erros no carregamento do DataTable
// 21.047 - 28/06/2024 - Histórico de Pedidos
// 21.046 - 24/06/2024 - Financeiro Cambio
// 21.045 - 24/06/2024 - Geração Excel
// 21.044 - 19/06/2024 - Fechamento Importação
// 21.043 - 19/06/2024 - Relatórios Cadastrais & Atualização Expedição Pedido (Com Frete)
// 21.042 - 16/06/2024 - Atualizações
// 21.041 - 15/06/2024 - Ajustes Codigos Fontes
// 21.040 - 14/06/2024 - Atualização Layouts
// 21.039 - 12/06/2024 - Autid Trail - Atualização de vendedores
// 21.038 - 11/06/2024 - Informações Complementares na NF
// 21.037 - 11/06/2024 - AuditTrail Movimentos
// 21.036 - 10/06/2024 - Drop GMovimentosLog
// 21.035 - 09/06/2024 - Cadastro de Produtos
// 21.034 - 09/06/2024 - Audit Trail
// 21.033 - 29/05/2024 - Processamento NF Importacao
// 21.032 - 16/05/2024 - Módulo Custos
// 21.031 - 15/05/2024 - Módulo Custos
// 21.030 - 08/05/2024 - Opção de todas as contas caixas por parametro no banco de dados
// 21.029 - 06/05/2024 - Atualização do cadastro de produtos
// 21.026 - 03/05/2024 - Importação de novos produtos via planilha
// 21.025 - 01/05/2024 - Atualização da tradução do item
// 21.024 - 30/04/2024 - Atualização do cadastro de produtos
// 21.023 - 16/04/2024 - Cadastro de Produtos Importados
// 21.022 - 11/04/2024 - Size and Color - Report Invoice
// 21.021 - 27/03/2024 - Erro ao acessar interface de novos produtos
// 21.020 - 27/03/2024 - Carregar Produtos Automático na Invoice
// 21.019 - 26/03/2024 - Edição de Produtos
// 21.018 - 25/03/2024 - Coligada e Filial
// 21.017 - 25/03/2024 - Medições Aferições
// 21.016 - 21/03/2024 - Histórico Comercial do Item
// 21.015 - 19/03/2024 - Data Referência GED
// 21.012 - 12/03/2024 - Retirar SSL
// 21.011 - 12/03/2024 - Robo Itau SecurityProtocolType.Tls12;
// 21.001 - 11/03/2024 - NOVO ERPGDI E NOVO SERVIDOR
// 20.01 - 26/01/2024 - NOVO ERPGDI
// 20.02 - 06/02/2024 - SITUAÇÕES DOS DOCUMENTOS NA RECEITA FEDERAL
// 20.03 - 06/02/2024 - CEP NO ROBO DA RECEITA FEDERAL
// 20.04 - 16/02/2024 - FILTRO DE REGISTROS DE VENDEDORES NA COTAÇÃO E NO PAINEL DE PEDIDOS - VENDEDOR SO VISUALIZA SEUS PEDIDOS/COTAÇÕES
// 20.05 - 20/02/2024 - UPDATE
// 20.06 - 23/02/2024 - CONTA CAIXA 
// 20.07 - 23/02/2024 - ATUALIZAÇÃO CERTIFICADO BOLECODE ITAU
// 20.08 - 29/02/2024 - UPDATE DB