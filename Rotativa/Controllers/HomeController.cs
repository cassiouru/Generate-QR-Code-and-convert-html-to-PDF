using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using Rotativa.Models;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using ZXing;
using ZXing.Common;

namespace Rotativa.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Report()
        {
            //Busca o model
            var model = new { Nome = "Cassio" };

            string htmlText = RenderViewToString("Index", model);

            byte[] buffer = RenderPDF(htmlText);

            return File(buffer, "application/PDF");
        }

        private string RenderViewToString(string viewName, object viewData)
        {
            var renderedView = new StringBuilder();

            var controller = this;

            using (var responseWriter = new StringWriter(renderedView))
            {
                var fakeResponse = new HttpResponse(responseWriter);

                var fakeContext = new HttpContext(System.Web.HttpContext.Current.Request, fakeResponse);

                var fakeControllerContext = new ControllerContext(new HttpContextWrapper(fakeContext), controller.ControllerContext.RouteData,
                    controller.ControllerContext.Controller);

                var oldContext = System.Web.HttpContext.Current;
                System.Web.HttpContext.Current = fakeContext;

                using (var viewPage = new ViewPage())
                {
                    var viewContext = new ViewContext(fakeControllerContext, new FakeView(), new ViewDataDictionary(), new TempDataDictionary(), responseWriter);

                    var html = new HtmlHelper(viewContext, viewPage);
                    html.RenderPartial(viewName, viewData);
                    System.Web.HttpContext.Current = oldContext;
                }
            }

            return renderedView.ToString();
        }

        private byte[] RenderPDF(string htmlText)
        {
            byte[] renderedBuffer;

            const int HorizontalMargin = 40;
            const int VerticalMargin = 40;

            using (var outputMemoryStream = new MemoryStream())
            {
                using (var pdfDocument = new Document(PageSize.A4, HorizontalMargin, HorizontalMargin, VerticalMargin, VerticalMargin))
                {
                    PdfWriter pdfWriter = PdfWriter.GetInstance(pdfDocument, outputMemoryStream);
                    pdfWriter.CloseStream = false;

                    pdfDocument.Open();
                    using (var htmlViewReader = new StringReader(htmlText))
                    {
                        XMLWorkerHelper.GetInstance().ParseXHtml(pdfWriter, pdfDocument, htmlViewReader);
                    }

                    //Para renderizar base 64 no text sharp
                    Image QRCode = null;
                    Byte[] bytes = this.GetQRCodeBase64("http://meucondominio.com.br", "QR Code", 75, 75);
                    QRCode = Image.GetInstance(bytes);
                    pdfDocument.Add(QRCode);

                }

                renderedBuffer = new byte[outputMemoryStream.Position];
                outputMemoryStream.Position = 0;
                outputMemoryStream.Read(renderedBuffer, 0, renderedBuffer.Length);
            }

            return renderedBuffer;
        }

        private byte[] GetQRCodeBase64(string url, string alt = "QR code", int height = 500, int width = 500, int margin = 0)
        {
            var qrWriter = new BarcodeWriter();
            qrWriter.Format = BarcodeFormat.QR_CODE;
            qrWriter.Options = new EncodingOptions() { Height = height, Width = width, Margin = margin };

            using (var q = qrWriter.Write(url))
            {
                using (var ms = new MemoryStream())
                {
                    q.Save(ms, ImageFormat.Png);

                    return ms.ToArray();
                }
            }
        }
    }
}