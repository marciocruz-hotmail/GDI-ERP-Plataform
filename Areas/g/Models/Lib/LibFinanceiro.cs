using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.g.Lib
{
    public static class LibFinanceiro
    {
        public static CstFinanceiroImpostos CalcularImpostos(g_clientes record_g_clientes, decimal valor)
        {
            CstFinanceiroImpostos recordFinanceiroImpostos = new CstFinanceiroImpostos();

            // ISS
            if (record_g_clientes.iss_percentual > 0)
            {
                recordFinanceiroImpostos.iss_percentual = record_g_clientes.iss_percentual;
                if (record_g_clientes.iss_tipo.Equals("D"))
                {
                    recordFinanceiroImpostos.iss_valor = 0;
                    recordFinanceiroImpostos.iss_display = ((valor / 100) * record_g_clientes.iss_percentual);
                }
                else if (record_g_clientes.iss_tipo.Equals("+"))
                {
                    recordFinanceiroImpostos.iss_valor = ((valor / 100) * record_g_clientes.iss_percentual);
                    recordFinanceiroImpostos.iss_display = ((valor / 100) * record_g_clientes.iss_percentual);
                }
                else if (record_g_clientes.iss_tipo.Equals("-"))
                {
                    recordFinanceiroImpostos.iss_valor = ((valor / 100) * record_g_clientes.iss_percentual) * -1;
                    recordFinanceiroImpostos.iss_display = ((valor / 100) * record_g_clientes.iss_percentual);
                }
            }

            // IR
            if (record_g_clientes.ir_percentual > 0)
            {
                recordFinanceiroImpostos.ir_percentual = record_g_clientes.ir_percentual;
                if (record_g_clientes.ir_tipo.Equals("D"))
                {
                    recordFinanceiroImpostos.ir_valor = 0;
                    recordFinanceiroImpostos.ir_display = ((valor / 100) * record_g_clientes.ir_percentual);
                }
                else if (record_g_clientes.ir_tipo.Equals("+"))
                {
                    recordFinanceiroImpostos.ir_valor = ((valor / 100) * record_g_clientes.ir_percentual);
                    recordFinanceiroImpostos.ir_display = ((valor / 100) * record_g_clientes.ir_percentual);
                }
                else if (record_g_clientes.ir_tipo.Equals("-"))
                {
                    recordFinanceiroImpostos.ir_valor = ((valor / 100) * record_g_clientes.ir_percentual) * -1;
                    recordFinanceiroImpostos.ir_display = ((valor / 100) * record_g_clientes.ir_percentual);
                }
            }

            // PIS
            if (record_g_clientes.pis_percentual > 0)
            {
                recordFinanceiroImpostos.pis_percentual = record_g_clientes.pis_percentual;
                if (record_g_clientes.pis_tipo.Equals("D"))
                {
                    recordFinanceiroImpostos.pis_valor = 0;
                    recordFinanceiroImpostos.pis_display = ((valor / 100) * record_g_clientes.pis_percentual);
                }
                else if (record_g_clientes.pis_tipo.Equals("+"))
                {
                    recordFinanceiroImpostos.pis_valor = ((valor / 100) * record_g_clientes.pis_percentual);
                    recordFinanceiroImpostos.pis_display = ((valor / 100) * record_g_clientes.pis_percentual);
                }
                else if (record_g_clientes.pis_tipo.Equals("-"))
                {
                    recordFinanceiroImpostos.pis_valor = ((valor / 100) * record_g_clientes.pis_percentual) * -1;
                    recordFinanceiroImpostos.pis_display = ((valor / 100) * record_g_clientes.pis_percentual);
                }
            }

            // COFINS
            if (record_g_clientes.cofins_percentual > 0)
            {
                recordFinanceiroImpostos.cofins_percentual = record_g_clientes.cofins_percentual;
                if (record_g_clientes.cofins_tipo.Equals("D"))
                {
                    recordFinanceiroImpostos.cofins_valor = 0;
                    recordFinanceiroImpostos.cofins_display = ((valor / 100) * record_g_clientes.cofins_percentual);
                }
                else if (record_g_clientes.cofins_tipo.Equals("+"))
                {
                    recordFinanceiroImpostos.cofins_valor = ((valor / 100) * record_g_clientes.cofins_percentual);
                    recordFinanceiroImpostos.cofins_display = ((valor / 100) * record_g_clientes.cofins_percentual);
                }
                else if (record_g_clientes.cofins_tipo.Equals("-"))
                {
                    recordFinanceiroImpostos.cofins_valor = ((valor / 100) * record_g_clientes.cofins_percentual) * -1;
                    recordFinanceiroImpostos.cofins_display = ((valor / 100) * record_g_clientes.cofins_percentual);
                }
            }

            // csll
            if (record_g_clientes.csll_percentual > 0)
            {
                recordFinanceiroImpostos.csll_percentual = record_g_clientes.csll_percentual;
                if (record_g_clientes.csll_tipo.Equals("D"))
                {
                    recordFinanceiroImpostos.csll_valor = 0;
                    recordFinanceiroImpostos.csll_display = ((valor / 100) * record_g_clientes.csll_percentual);
                }
                else if (record_g_clientes.csll_tipo.Equals("+"))
                {
                    recordFinanceiroImpostos.csll_valor = ((valor / 100) * record_g_clientes.csll_percentual);
                    recordFinanceiroImpostos.csll_display = ((valor / 100) * record_g_clientes.csll_percentual);
                }
                else if (record_g_clientes.csll_tipo.Equals("-"))
                {
                    recordFinanceiroImpostos.csll_valor = ((valor / 100) * record_g_clientes.csll_percentual) * -1;
                    recordFinanceiroImpostos.csll_display = ((valor / 100) * record_g_clientes.csll_percentual);
                }
            }

            // pcc
            if (record_g_clientes.pcc_percentual > 0)
            {
                recordFinanceiroImpostos.pcc_percentual = record_g_clientes.pcc_percentual;
                if (record_g_clientes.pcc_tipo.Equals("D"))
                {
                    recordFinanceiroImpostos.pcc_valor = 0;
                    recordFinanceiroImpostos.pcc_display = ((valor / 100) * record_g_clientes.pcc_percentual);
                }
                else if (record_g_clientes.pcc_tipo.Equals("+"))
                {
                    recordFinanceiroImpostos.pcc_valor = ((valor / 100) * record_g_clientes.pcc_percentual);
                    recordFinanceiroImpostos.pcc_display = ((valor / 100) * record_g_clientes.pcc_percentual);
                }
                else if (record_g_clientes.pcc_tipo.Equals("-"))
                {
                    recordFinanceiroImpostos.pcc_valor = ((valor / 100) * record_g_clientes.pcc_percentual) * -1;
                    recordFinanceiroImpostos.pcc_display = ((valor / 100) * record_g_clientes.pcc_percentual);
                }
            }

            // inss
            if (record_g_clientes.inss_percentual > 0)
            {
                recordFinanceiroImpostos.inss_percentual = record_g_clientes.inss_percentual;
                if (record_g_clientes.inss_tipo.Equals("D"))
                {
                    recordFinanceiroImpostos.inss_valor = 0;
                    recordFinanceiroImpostos.inss_display = ((valor / 100) * record_g_clientes.inss_percentual);
                }
                else if (record_g_clientes.inss_tipo.Equals("+"))
                {
                    recordFinanceiroImpostos.inss_valor = ((valor / 100) * record_g_clientes.inss_percentual);
                    recordFinanceiroImpostos.inss_display = ((valor / 100) * record_g_clientes.inss_percentual);
                }
                else if (record_g_clientes.inss_tipo.Equals("-"))
                {
                    recordFinanceiroImpostos.inss_valor = ((valor / 100) * record_g_clientes.inss_percentual) * -1;
                    recordFinanceiroImpostos.inss_display = ((valor / 100) * record_g_clientes.inss_percentual);
                }
            }

            // ir
            /*if (record_g_clientes.inss_percentual > 0)
            {
                recordFinanceiroImpostos.inss_percentual = record_g_clientes.inss_percentual;
                if (record_g_clientes.inss_tipo.Equals("D"))
                {
                    recordFinanceiroImpostos.inss_valor = 0;
                    recordFinanceiroImpostos.inss_display = ((valor / 100) * record_g_clientes.inss_percentual);
                }
                else if (record_g_clientes.inss_tipo.Equals("+"))
                {
                    recordFinanceiroImpostos.inss_valor = ((valor / 100) * record_g_clientes.inss_percentual);
                    recordFinanceiroImpostos.inss_display = ((valor / 100) * record_g_clientes.inss_percentual);
                }
                else if (record_g_clientes.inss_tipo.Equals("-"))
                {
                    recordFinanceiroImpostos.inss_valor = ((valor / 100) * record_g_clientes.inss_percentual) * -1;
                    recordFinanceiroImpostos.inss_display = ((valor / 100) * record_g_clientes.inss_percentual);
                }
            }*/

            return recordFinanceiroImpostos;
        }
    }

}