using Amazon.S3;
using Amazon.S3.Model;
using GdiPlataform.Areas.qa.Models;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;
using GdiPlataform.Robos.Aws;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GdiPlataform.Areas.qa.Controllers
{
    public class TreinamentosController : Controller
    {
        private GdiPlataformEntities db;

        public TreinamentosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-graduation-cap", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Universidade Corporativa - GDI Aviação";
            return View();
        }

        public ActionResult IndexTreinamentoAviacao001()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-boxes-packing", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Treinamento de Identificação de Peças Falsas / Não Homologadas";
            ListaConteudoTreinamentoModel ListaArquivos = new ListaConteudoTreinamentoModel();
            
            qa_lms_cursos_arquivos CursoArquivo1 = db.qa_lms_cursos_arquivos.Find(1);
            ListaArquivos.id_arquivo_video1 = 1;
            ListaArquivos.titulo_video1 = CursoArquivo1.nome.EmptyIfNull().ToString();
            ListaArquivos.presigned_url_video1 = GerarPresignedGetUrl(CursoArquivo1.s3_key);

            qa_lms_cursos_arquivos CursoArquivo2 = db.qa_lms_cursos_arquivos.Find(2);
            ListaArquivos.id_arquivo_pdf1 = 2;
            ListaArquivos.titulo_pdf1 = CursoArquivo2.nome.EmptyIfNull().ToString();
            ListaArquivos.presigned_url_pdf1 = GerarPresignedGetUrl(CursoArquivo2.s3_key);

            qa_lms_cursos_arquivos CursoArquivo3 = db.qa_lms_cursos_arquivos.Find(3);
            ListaArquivos.id_arquivo_pdf2 = 3;
            ListaArquivos.titulo_pdf2 = CursoArquivo3.nome.EmptyIfNull().ToString();
            ListaArquivos.presigned_url_pdf2 = GerarPresignedGetUrl(CursoArquivo3.s3_key);

            qa_lms_cursos_arquivos CursoArquivo4 = db.qa_lms_cursos_arquivos.Find(4);
            ListaArquivos.id_arquivo_pdf3 = 4;
            ListaArquivos.titulo_pdf3 = CursoArquivo4.nome.EmptyIfNull().ToString();
            ListaArquivos.presigned_url_pdf3 = GerarPresignedGetUrl(CursoArquivo4.s3_key);

            return View(ListaArquivos);
        }

        private string GerarPresignedGetUrl(string s3Key)
        {
            var bucket = GdiAwsS3Credentials.ResolveBucketErp();
            GdiAwsS3BucketRules.ThrowIfBucketNotAllowed(bucket, "LMS / treinamentos S3");
            using (var s3Client = GdiAwsS3Credentials.CreateS3Client())
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucket,
                    Key = s3Key,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.UtcNow.AddMinutes(120)
                };
                return s3Client.GetPreSignedURL(request);
            }
        }

    }
}