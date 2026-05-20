using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Services
{
    public class PedidosVendaServices
    {
        private String MsgProcessamento;
        private GdiPlataformEntities db;
        public PedidosVendaServices()
        {
            MsgProcessamento = String.Empty;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }
        public String GetMsgProcessamento()
        {
            return MsgProcessamento;
        }

    }
}