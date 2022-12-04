using ClosedXML.Excel;
using CsvHelper.TypeConversion;
using DinkToPdf;
using DinkToPdf.Contracts;
using DocumentFormat.OpenXml.Drawing.Charts;
using egitlab_PotionNetCore.Data;
using egitlab_PotionNetCore.Pages;
using egitlab_PotionNetCore.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProductRecomendation.Data;
using ProductRecomendation.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColorMode = DinkToPdf.ColorMode;
using Orientation = DinkToPdf.Orientation;
using PaperKind = DinkToPdf.PaperKind;

namespace ProductRecomendation.Pages
{
    [AuthorizeCustom(Role.admin, Role.salesman)]
    public class RecommendationSystemModel : PageModel
    {


        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnv;
        private readonly IConverter _converter;

        public RecommendationSystemModel(ApplicationDbContext context, IConverter converter, IWebHostEnvironment webHostEnv)
        {
            _context = context;
            _converter = converter;
            _webHostEnv = webHostEnv;
        }


        public class InputRec
        {
            public string recommendationDate { get; set; }
            public string customerShipTo { get; set; }
        }

        public class OutputRecommendation
        {
            public string item_code { get; set; }
            public string item_desc { get; set; }
            public string flag_active { get; set; }
        }

        public class ListExport
        {
            public string item_code { get; set; }
            public string item_desc { get; set; }
        }

        public class userViewModel
        {
            public int user_id { get; set; }
            public int role_id { get; set; }
            public string username { get; set; }

            public string password { get; set; }

            public string rayon_exp_id { get; set; }
            public string flag_active { get; set; }

            public string lastupdate_by { get; set; }

            public DateTime lastupdate_date { get; set; }

            public string rayon_exp_code { get; set; }
        }



        [BindProperty]
        public InputRec Input { get; set; }

        public IList<userViewModel> DdlCustomer { get; set; }

        public IList<OutputRecommendation> outRecommendationList { get; set; }


        public bool isSearched = false;

        UrlString conf = new UrlString();
        public async Task<IActionResult> OnGetAsync(string q, string a)
        {
            try
            {
                DdlCustomer = new List<userViewModel>();
                string role = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "role").Value;
                string rayon_id = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "rayon_exp_id").Value;

                await loadDdlCustomer(role, rayon_id);

                if (!String.IsNullOrEmpty(q) && !String.IsNullOrEmpty(a))
                {
                    if (TempData["jsonExportContent"] == null) return Page();


                    byte[] file = ExportPdf(q, a);
                    return File(file, "application/pdf");
                }

                return Page();
            }
            catch(Exception ex)
            {
                TempData["MessageFailed"] = ex.Message;
                return Page();
            }


        }


        public async Task loadDdlCustomer(string role, string rayon_id)
        {

            if (role == "admin")
            {
                var joineddata = await _context.tb_user.Where(x => x.role_id == 3).Select(x => new userViewModel
                {
                    user_id = x.user_id,
                    role_id = x.role_id,
                    username = x.username,
                    password = x.password,
                    rayon_exp_id = x.rayon_exp_id,
                    flag_active = x.flag_active,
                    lastupdate_by = x.lastupdate_by,
                    lastupdate_date = x.lastupdate_date,
                    rayon_exp_code = _context.tb_rayon.FirstOrDefault(y => y.rayon_exp_id.ToString() == x.rayon_exp_id).rayon_exp_code
                }).ToListAsync();

                DdlCustomer = joineddata;
            }
            else
            {

                var joineddata = await _context.tb_user.Where(x => x.rayon_exp_id == rayon_id && x.role_id == 3).Select(x => new userViewModel
                {
                    user_id = x.user_id,
                    role_id = x.role_id,
                    username = x.username,
                    password = x.password,
                    rayon_exp_id = x.rayon_exp_id,
                    flag_active = x.flag_active,
                    lastupdate_by = x.lastupdate_by,
                    lastupdate_date = x.lastupdate_date,
                    rayon_exp_code = _context.tb_rayon.FirstOrDefault(y => y.rayon_exp_id.ToString() == x.rayon_exp_id).rayon_exp_code
                }).ToListAsync();
                DdlCustomer = joineddata;
            }

        }


        public async Task OnPostAsync()
        {

            string role = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "role").Value;
            string rayon = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "rayon_exp_id").Value;

            string pickedDate = Input.recommendationDate;
            string pickedUser = Input.customerShipTo;

            bool isDataExist = _context.tb_transaction.Where(x => x.transaction_date == pickedDate).Any();

            if (!isDataExist)
            {
                TempData["MessageFailed"] = "Data Transaction didn't exist.";
                await loadDdlCustomer(role, rayon);
                return;
            }

            var result = await getRecommendation(pickedDate, pickedUser);
            if (result.IsSuccessful != true)
            {
                TempData["MessageFailed"] = result.Content;
                await loadDdlCustomer(role, rayon);
                return;
            }

            outRecommendationList = new List<OutputRecommendation>();
            var listResult = JsonConvert.DeserializeObject<IList<string>>(result.Content);

            TempData["jsonExportContent"] = result.Content;
            TempData.Keep("jsonExportContent");

            foreach (var item in listResult)
            {
                var itemDb = _context.tb_product.Where(x => x.item_code == item).FirstOrDefault();

                if (itemDb.flag_active == "N") continue;

                outRecommendationList.Add(new OutputRecommendation { item_code = item, item_desc = itemDb.item_desc, flag_active = itemDb.flag_active });

                if (outRecommendationList.Count == 10) break;
            };

            Console.Write(outRecommendationList);

            await loadDdlCustomer(role, rayon);
            isSearched = true;
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
                    isSearched = false;
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
    } // end class
}
