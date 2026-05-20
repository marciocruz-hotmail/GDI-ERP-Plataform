using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace GdiPlataform.Lib
{
    public static class LibFilesDisk
    {
        public static bool DeleteFilesInDirectory(string Diretorio)
        {
            bool Resultado = false;
            try
            {
                if (Directory.Exists(Diretorio) == true)
                {
                    string[] ListaArquivos = Directory.GetFiles(Diretorio);
                    foreach (string FileName in ListaArquivos)
                    {
                        if (File.Exists(FileName))
                        {
                            try { File.Delete(FileName); } catch { };
                        }
                    }
                }
            }
            catch (Exception) { }
            return Resultado;
        }

    }
}