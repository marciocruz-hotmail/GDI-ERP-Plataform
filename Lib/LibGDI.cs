using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.GDI
{
    public static class LibGDI
    {
        public static g_produtos_ncm CadastrarProdutoNCM(GdiPlataformEntities db, String NCM)
        {
            try
            {
                g_produtos_ncm record_g_produtos_ncm = new g_produtos_ncm();
                if (NCM.EmptyIfNull().ToString().Trim().Length > 0)
                {
                    g_produtos_ncm_ibptax record_g_produtos_ncm_ibptax = db.g_produtos_ncm_ibptax.Where(p => p.codigo_ncm == NCM).FirstOrDefault();
                    record_g_produtos_ncm.ativo = true;
                    record_g_produtos_ncm.codigo_ncm = NCM;
                    record_g_produtos_ncm.id_cst_icms_entrada = 1;
                    record_g_produtos_ncm.id_cst_ipi_entrada = 1;
                    record_g_produtos_ncm.id_cst_ipi_saida = 1;
                    record_g_produtos_ncm.id_cst_pis_entrada = 1;
                    record_g_produtos_ncm.id_cst_pis_saida = 1;
                    record_g_produtos_ncm.id_cst_cofins_entrada = 1;
                    record_g_produtos_ncm.id_cst_cofins_saida = 1;
                    record_g_produtos_ncm.datahora_cadastro = LibDateTime.getDataHoraBrasilia(); ;
                    record_g_produtos_ncm.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    record_g_produtos_ncm.id_coligada = 1;
                    record_g_produtos_ncm.id_filial = 1;
                    if (record_g_produtos_ncm_ibptax != null)
                    {
                        record_g_produtos_ncm.tributo_federal_nacional = record_g_produtos_ncm_ibptax.tributo_federal_nacional;
                        record_g_produtos_ncm.tributo_federal_importado = record_g_produtos_ncm_ibptax.tributo_federal_importado;
                        record_g_produtos_ncm.tributo_estadual = record_g_produtos_ncm_ibptax.tributo_estadual;
                        record_g_produtos_ncm.tributo_municipal = record_g_produtos_ncm_ibptax.tributo_municipal;
                    }
                    db.g_produtos_ncm.Add(record_g_produtos_ncm);
                    db.SaveChanges();
                }
                else
                {
                    record_g_produtos_ncm.id_produto_ncm = 0;
                }

                return record_g_produtos_ncm;
            }
            catch (Exception e)
            {
                throw (e);
            }
        }

        #region CadastrarUnidadeMedida
        public static g_unidade_medida CadastrarUnidadeMedida(GdiPlataformEntities db, String UnidadeMedida)
        {
            try
            {
                g_unidade_medida record_g_unidade_medida = new g_unidade_medida();
                if (UnidadeMedida.EmptyIfNull().ToString().Trim().Length > 0)
                {
                    record_g_unidade_medida.ativo = true;
                    record_g_unidade_medida.codigo = UnidadeMedida;
                    record_g_unidade_medida.descricao = UnidadeMedida;
                    record_g_unidade_medida.datahora_cadastro = LibDateTime.getDataHoraBrasilia(); ; ;
                    record_g_unidade_medida.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    record_g_unidade_medida.id_coligada = 1;
                    record_g_unidade_medida.id_filial = 1;
                    db.g_unidade_medida.Add(record_g_unidade_medida);
                }
                else
                {
                    record_g_unidade_medida.id_unidade_medida = 0;
                }
                return record_g_unidade_medida;
            }
            catch (Exception e)
            {
                throw (e);
            }
        }
        #endregion
    }
}