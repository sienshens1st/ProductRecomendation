using ClosedXML.Excel;
using DinkToPdf.Contracts;
using DinkToPdf;
using egitlab_PotionNetCore.Data;
using egitlab_PotionNetCore.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using ProductRecomendation.Data;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ProductRecomendation.Pages.RecommendationSystemModel;
using Microsoft.AspNetCore.Hosting;

namespace ProductRecomendation.Pages
{
    public class LandingPageModel : PageModel
    {


        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnv;
        private readonly IConverter _converter;

        public LandingPageModel(ApplicationDbContext context, IConverter converter, IWebHostEnvironment webHostEnv)
        {
            _context = context;
            _converter = converter;
            _webHostEnv = webHostEnv;
        }



        public class OutputRecommendation
        {
            public string item_code { get; set; }
            public string item_desc { get; set; }
            public string flag_active { get; set; }
        }

        public class TransactionHistory
        {
            public string GROSS_SALES_AMOUNT { get; set; }
            public string ITEM_CODE { get; set; }
            public string ITEM_NAME { get; set; }
            public string SALES_QTY { get; set; }
            public string TRX_DATE { get; set; }
        }

        public IList<OutputRecommendation> outRecommendationList { get; set; }

        public IList<TransactionHistory> outTransactionHistoryList { get; set; }

        public string historyInfo { get; set; }

        UrlString conf = new UrlString();

        [BindProperty]
        public string recommendationDate { get; set; }

        public IActionResult OnGet(string q, string a)
        {
            try
            {
                TempData["NoData"] = "true";
                var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;


                if (User.Claims.FirstOrDefault(c => c.Type == "username")?.Value == null)
                {
                    return Page();
                }

                string rayon = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "rayon_exp_code").Value;

                if (!String.IsNullOrEmpty(q) && !String.IsNullOrEmpty(a))
                {
                    if (TempData["jsonExportContent"] == null) return Page();


                    byte[] file = ExportPdf(q, a);
                    return File(file, "application/pdf");
                }

                return Page();

            }
            catch (Exception ex)
            {
                TempData["MessageFailed"] = ex.Message;
                return Page();
            }

        }

        public async Task OnPostAsync()
        {

            string username = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "username").Value;

            bool isDataExist = _context.tb_transaction.Where(x => x.transaction_date == recommendationDate).Any();

            if (!isDataExist)
            {
                TempData["MessageFailed"] = "Data Transaction didn't exist.";
                TempData["NoData"] = "true";
                return;
            }

            var result = await getRecommendation(recommendationDate, username);
            if (result.IsSuccessful != true)
            {
                TempData["MessageFailed"] = result.Content;
                TempData["NoData"] = "true";
                return;
            }

            outRecommendationList = new List<OutputRecommendation>();
            var listResult = JsonConvert.DeserializeObject<IList<string>>(result.Content);

            foreach (var item in listResult)
            {
                var itemDb = _context.tb_product.Where(x => x.item_code == item).FirstOrDefault();

                if (itemDb.flag_active == "N") continue;

                outRecommendationList.Add(new OutputRecommendation { item_code = item, item_desc = itemDb.item_desc, flag_active = itemDb.flag_active });

                if (outRecommendationList.Count == 10) break;
            };

            TempData["jsonExportContent"] = result.Content;
            TempData.Keep("jsonExportContent");


            //get transaction history

            var result2 = await getTransactionHistory(recommendationDate, username);
            if (result2.IsSuccessful != true)
            {
                TempData["MessageFailed"] = result2.Content;
                TempData["NoData"] = "true";
                return;
            }

            outTransactionHistoryList = new List<TransactionHistory>();
            var listResultHistory = JsonConvert.DeserializeObject<IList<TransactionHistory>>(result2.Content);

            foreach (var item in listResultHistory)
            {
                //var grossvalue = String.Format(CultureInfo.CreateSpecificCulture("id-id"), "Rp. {0:N}", item.GROSS_SALES_AMOUNT);
                var trans_date_parsed = DateTime.ParseExact(item.TRX_DATE, "MM/dd/yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture).ToString("dd-MMM-yyyy");
                var itemName = _context.tb_product.Where(x => x.item_code == item.ITEM_CODE).FirstOrDefault().item_desc.ToString();
                outTransactionHistoryList.Add(new TransactionHistory
                {
                    ITEM_CODE = item.ITEM_CODE,
                    ITEM_NAME = itemName,
                    SALES_QTY = item.SALES_QTY,
                    GROSS_SALES_AMOUNT = "Rp. " + item.GROSS_SALES_AMOUNT,
                    TRX_DATE = trans_date_parsed
                });
            };

            int splitmonth;

            if (int.Parse(recommendationDate.Split('-')[0]) == 1)
            {
                splitmonth = 12;
            }
            else
            {
                splitmonth = int.Parse(recommendationDate.Split('-')[0]) - 1;
            };


            var month = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(splitmonth);
            var year = recommendationDate.Split('-')[1];
            historyInfo = String.Format("25 {0} {1} - 25 {0} {2} ", month, int.Parse(year) - 1, year);
            TempData["NoData"] = "false";

        }





        public byte[] ExportPdf(string recDate, string shipTo)
        {
            int splitmonth = int.Parse(recDate.Split('-')[0]);
            var month = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(splitmonth);

            var listResult = JsonConvert.DeserializeObject<IList<string>>(TempData["jsonExportContent"].ToString());
            TempData.Keep("jsonExportContent");
            var listExport = new List<ListExport>();

            foreach (var item in listResult)
            {
                var itemDb = _context.tb_product.Where(x => x.item_code == item).FirstOrDefault();

                if (itemDb.flag_active == "N") continue;

                listExport.Add(new ListExport { item_code = item, item_desc = itemDb.item_desc });


                if (listExport.Count == 10) break;
            };



            var path = _webHostEnv.WebRootPath;

            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10 },
                DocumentTitle = shipTo + " " + month + " Recommendation",
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                HtmlContent = PdfHtmlGenerator.GetHtmlString(listExport, path, shipTo, recDate),
                WebSettings = { DefaultEncoding = "utf-8", UserStyleSheet = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "styles.css") },
                FooterSettings = { Right = "Page [page] of [toPage]", FontSize = 7 },

            };
            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };


            var file = _converter.Convert(pdf);

            return file;
        }

        public IActionResult OnPostExportExcel(string recDate, string shipTo)
        {
            //List<ListExport> listExport = await getListExportRecSysAsync(recDate, shipTo);


            var listResult = JsonConvert.DeserializeObject<IList<string>>(TempData["jsonExportContent"].ToString());
            TempData.Keep("jsonExportContent");
            var listExport = new List<ListExport>();

            foreach (var item in listResult)
            {
                var itemDb = _context.tb_product.Where(x => x.item_code == item).FirstOrDefault();

                if (itemDb.flag_active == "N") continue;

                listExport.Add(new ListExport { item_code = item, item_desc = itemDb.item_desc });


                if (listExport.Count == 10) break;
            };


            string pickedDate = recDate;
            string pickedUser = shipTo;

            int splitmonth = int.Parse(pickedDate.Split('-')[0]);
            var month = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(splitmonth);
            var filename = pickedUser + "_" + month + "_Recommendation.xlsx";

            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Recommendation");
                ws.Columns().AdjustToContents();
                ws.Cell(1, 1).Value = "Customer:";
                ws.Cell(1, 2).Value = pickedUser;
                ws.Cell(2, 1).Value = "Recommendation for:";
                ws.Cell(2, 2).DataType = XLDataType.Text;
                ws.Cell(2, 2).Value = "'" + month + " - " + pickedDate.Split('-')[1];

                ws.Cell(4, 1).Value = "ITEM CODE";
                ws.Cell(4, 1).Style.Font.Bold = true;
                ws.Cell(4, 2).Value = "ITEM DESCRIPTION";
                ws.Cell(4, 2).Style.Font.Bold = true;
                ws.Cell(5, 1).InsertData(listExport);
                ws.Columns().AdjustToContents();

                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    return File(MyMemoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
                }
            }

        }



        public async Task<List<ListExport>> getListExportRecSysAsync(string recDate, string shipTo)
        {
            string role = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "role").Value;
            string rayon = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "rayon_exp_id").Value;

            var pickedDate = recDate;
            var pickedUser = shipTo;


            var result = await getRecommendation(pickedDate, pickedUser);

            outRecommendationList = new List<OutputRecommendation>();
            var listResult = JsonConvert.DeserializeObject<IList<string>>(result.Content);
            var listExport = new List<ListExport>();

            foreach (var item in listResult)
            {
                var itemDb = _context.tb_product.Where(x => x.item_code == item).FirstOrDefault();

                if (itemDb.flag_active == "N") continue;

                listExport.Add(new ListExport { item_code = item, item_desc = itemDb.item_desc });


                if (listExport.Count == 10) break;
            };


            return listExport;

        }





        public async Task<IRestResponse> getRecommendation(string filename, string customer)
        {
            var client = new RestClient(conf.PythonApiUrl + "/Recommendation/?filename=" + filename + "&&customerShipTo=" + customer);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Content-Type", "application/json");
            IRestResponse response = await client.ExecuteAsync(request);
            return response;
        }


        public async Task<IRestResponse> getTransactionHistory(string filename, string customer)
        {
            var client = new RestClient(conf.PythonApiUrl + "/TransactionHistory/?filename=" + filename + "&&customerShipTo=" + customer);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Content-Type", "application/json");
            IRestResponse response = await client.ExecuteAsync(request);
            return response;
        }

    }//end class
}
