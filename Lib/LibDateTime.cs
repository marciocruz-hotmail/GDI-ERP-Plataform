using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;

namespace GdiPlataform.Lib
{
    public static class LibDateTime
    {
        private static readonly string[] DateFormats = new[]
        {
            // ISO
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",

            // pt-BR
            "dd/MM/yyyy",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss.fff",

            // en-US (AM/PM)
            "MM/dd/yyyy hh:mm:ss",
            "M/d/yyyy h:mm:ss",
            "MM/dd/yyyy hh:mm:ss tt",
            "M/d/yyyy h:mm:ss tt"
        };

        public static DateTime getDataHoraBrasilia()
        {
            try
            {
                return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }

        public static DateTime getDataBrasilia()
        {
            try
            {
                DateTime DataHoraAtual = getDataHoraBrasilia();
                return new DateTime(DataHoraAtual.Year, DataHoraAtual.Month, DataHoraAtual.Day);
            }
            catch (Exception)
            {
                return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            }
        }


        public static DateTime getPrimeiroDiaMesPassado()
        {
            try
            {
                DateTime dataReferencia = LibDateTime.getDataHoraBrasilia();
                dataReferencia = dataReferencia.AddMonths(-1);
                dataReferencia = new DateTime(dataReferencia.Year, dataReferencia.Month, 1);
                return dataReferencia;
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }

        public static DateTime getUltimoDiaMesPassado()
        {
            try
            {
                DateTime dataReferencia = LibDateTime.getDataHoraBrasilia();
                dataReferencia = new DateTime(dataReferencia.Year, dataReferencia.Month, 1);
                dataReferencia = dataReferencia.AddDays(-1);
                return dataReferencia;
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }


        public static DateTime getPrimeiroDiaMesAtual()
        {
            try
            {
                DateTime dataReferencia = LibDateTime.getDataHoraBrasilia();
                dataReferencia = new DateTime(dataReferencia.Year, dataReferencia.Month, 1);
                return dataReferencia;
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }

        public static DateTime getUltimoDiaMesAtual()
        {
            try
            {
                DateTime dataReferencia = LibDateTime.getDataHoraBrasilia();
                dataReferencia = dataReferencia.AddMonths(1);
                dataReferencia = new DateTime(dataReferencia.Year, dataReferencia.Month, 1);
                dataReferencia = dataReferencia.AddDays(-1);
                return dataReferencia;
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }

        public static DateTime getUltimoDiaMesReferencia(DateTime DataReferencia)
        {
            try
            {
                DataReferencia = DataReferencia.AddMonths(1);
                DataReferencia = new DateTime(DataReferencia.Year, DataReferencia.Month, 1, 23, 59, 59);
                DataReferencia = DataReferencia.AddDays(-1);
                return DataReferencia;
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }

        public static DateTime getPrimeiroDiaMesReferencia(DateTime DataReferencia)
        {
            try
            {
                DateTime dataReferencia = new DateTime(DataReferencia.Year, DataReferencia.Month, 1);
                return dataReferencia;
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }

        /*public static DateTime GetDateTimeDataRow(String DataRowString)
        {
            DataRowString = DataRowString.EmptyIfNull().ToString().Trim().ToUpperInvariant().Replace(" AM","").Replace(" PM","").Replace("AM", "").Replace("PM", "");
            DateTime DataRetorno = new DateTime(1900, 1, 1);
            try
            {
                DateTime.TryParseExact(DataRowString, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DataRetorno);
            }
            catch (Exception) { }
            return DataRetorno;
        }*/

        public static DateTime GetDateTimeDataRow(DataRow row, string columnName)
        {
            DateTime DataRetorno = new DateTime(1900, 1, 1);

            if (row == null || row.Table == null || !row.Table.Columns.Contains(columnName))
                return DataRetorno;

            var raw = row[columnName];

            if (raw == null || raw == DBNull.Value)
                return DataRetorno;

            // 1) Se já veio como DateTime, não faça parse
            if (raw is DateTime dt)
            {
                DataRetorno = dt;
            }

            // 2) Caso venha como DateTimeOffset
            if (raw is DateTimeOffset dto)
            {
                DataRetorno = dto.DateTime;
            }

            // 3) Se for string (ou outro tipo), tenta parse com formatos conhecidos
            var s = raw.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(s))
                return DataRetorno;

            // Tente primeiro com Invariant (bom para ISO), depois pt-BR, depois en-US
            if (!DateTime.TryParseExact(s, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DataRetorno))
            {
                if (!DateTime.TryParseExact(s, DateFormats, new CultureInfo("pt-BR"), DateTimeStyles.AllowWhiteSpaces, out DataRetorno))
                {
                    DateTime.TryParseExact(s, DateFormats, new CultureInfo("en-US"), DateTimeStyles.AllowWhiteSpaces, out DataRetorno);
                }
            };

            return DataRetorno;
        }

        public static DateTime GetDiaUtilAnterior(DateTime dataReferencia)
        {
            var data = dataReferencia.Date;

            do
            {
                data = data.AddDays(-1);
            }
            while (data.DayOfWeek == DayOfWeek.Saturday ||
                   data.DayOfWeek == DayOfWeek.Sunday);

            return data;
        }
    }
}