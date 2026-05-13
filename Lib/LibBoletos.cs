using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GdiPlataform.Security;
using Zen.Barcode;

namespace GdiPlataform.Lib
{
    public static class LibBoletos
    {
        public static String Generate_barcode(string txt, DateTime data_vencimento, String ServerMapPath)
        {
            Code25BarcodeDraw bdw = BarcodeDrawFactory.Code25InterleavedWithoutChecksum;
            System.Drawing.Image img = bdw.Draw(txt, 50, 1);
            MemoryStream stream = new MemoryStream();
            img.Save(stream, ImageFormat.Png);
            String DirTempFiles = ServerMapPath;
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, "barcode");
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
            String fileNameDestino = Path.Combine(DirTempFiles, txt + ".png");
            img.Save(fileNameDestino);

            string nameFileImage = String.Empty;
            nameFileImage += "/_filestemp";
            nameFileImage += "/" + "barcode";
            nameFileImage += "/" + "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString();
            nameFileImage += "/" + txt + ".png";

            return nameFileImage;
        }

        public static string Generate_barcode_base64(string txt)
        {
            Code25BarcodeDraw bdw = BarcodeDrawFactory.Code25InterleavedWithoutChecksum;
            System.Drawing.Image img = bdw.Draw(txt, 50, 1);
            MemoryStream stream = new MemoryStream();
            img.Save(stream, ImageFormat.Png);
            byte[] imageBytes = stream.ToArray();
            string base64String = Convert.ToBase64String(imageBytes);
            return base64String;
        }

        public static String Generate_PixQrCode(int IdFinanceiro, string base64String, DateTime data_vencimento, String ServerMapPath)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            Image ImagemPix = null;
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                ImagemPix = Image.FromStream(ms, true);
            }

            String DirTempFiles = ServerMapPath;
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, "pix");
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
            String fileNameDestino = Path.Combine(DirTempFiles, IdFinanceiro + ".png");
            ImagemPix.Save(fileNameDestino);

            string nameFileImage = String.Empty;
            nameFileImage += "/_filestemp";
            nameFileImage += "/" + "pix";
            nameFileImage += "/" + "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString();
            nameFileImage += "/" + IdFinanceiro.ToString() + ".png";

            return nameFileImage;
        }

    }
}