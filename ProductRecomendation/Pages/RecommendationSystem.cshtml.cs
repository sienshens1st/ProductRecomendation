using ClosedXML.Excel;
using egitlab_PotionNetCore.Pages;
using egitlab_PotionNetCore.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProductRecomendation.Data;
using ProductRecomendation.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProductRecomendation.Pages
{
    [AuthorizeCustom(Role.admin,Role.salesman)]
    public class RecommendationSystemModel : PageModel
    {


        private readonly ApplicationDbContext _context;

        public RecommendationSystemModel(ApplicationDbContext context)
        {
            _context = context;
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
            public string flag_active { get;set; }
        }

        public class ListExport
        {
            public string item_code { get; set; }
            public string item_desc { get; set; }
        }





        [BindProperty]
        public InputRec Input { get; set; }

        public IList<tb_user> DdlCustomer { get; set; }

        public IList<OutputRecommendation> outRecommendationList { get; set; }

        public bool isSearched = false;

        UrlString conf = new UrlString();
        public async Task OnGetAsync()
        {
            DdlCustomer = new List<tb_user>();
            string role = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "role").Value;
            string rayon = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "rayon_exp_code").Value;

            await loadDdlCustomer(role, rayon);
                
        }


        public async Task loadDdlCustomer(string role, string rayon)
        {
            if(role == "admin")
            {
                var customers = await _context.tb_user.Where(x => x.role_id == 3).ToListAsync();
                DdlCustomer = customers;
            }
            else
            {
                var customers = await _context.tb_user.Where(x => x.rayon_exp_code == rayon && x.role_id == 3).ToListAsync();
                DdlCustomer = customers;
            }
           
        }


        public async Task OnPostAsync()
        {

            string role = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "role").Value;
            string rayon = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "rayon_exp_code").Value;

            string pickedDate = Input.recommendationDate;
            string pickedUser = Input.customerShipTo;

            bool isDataExist = _context.tb_transaction.Where(x=> x.transaction_date == pickedDate).Any();

            if (!isDataExist)
            {
                TempData["MessageFailed"] = "Data Transaction didn't exist.";
                await loadDdlCustomer(role, rayon);
                return;
            }

            var result = await getRecommendation(pickedDate,pickedUser);
            if (result.IsSuccessful != true)
            {
                TempData["MessageFailed"] = result.Content;
                await loadDdlCustomer(role, rayon);
                return;
            }

            outRecommendationList = new List<OutputRecommendation>();
            var listResult = JsonConvert.DeserializeObject<IList<string>>(result.Content);

            foreach (var item in listResult)
            {
                var itemDb = _context.tb_product.Where(x => x.item_code == item).FirstOrDefault();

                if (itemDb.flag_active == "N") continue;

                outRecommendationList.Add(new OutputRecommendation { item_code = item, item_desc = itemDb.item_desc, flag_active = itemDb.flag_active  });

                if (outRecommendationList.Count == 10) break;
            };

            Console.Write(outRecommendationList);

            await loadDdlCustomer(role, rayon);
            isSearched = true;
        }


        public async Task<IActionResult> OnPostExportAsync(string recDate, string shipTo)
        {
            string role = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "role").Value;
            string rayon = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "rayon_exp_code").Value;

            string pickedDate = recDate;
            string pickedUser = shipTo;

            int splitmonth = int.Parse(pickedDate.Split('-')[0]);

            var month = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(splitmonth);

            var filename = pickedUser + "_" + month + "_Recommendation.xlsx";


            bool isDataExist = _context.tb_transaction.Where(x => x.transaction_date == pickedDate).Any();

            if (!isDataExist)
            {
                TempData["MessageFailed"] = "Data Transaction didn't exist.";
                await loadDdlCustomer(role, rayon);
                return Page();
            }

            var result = await getRecommendation(pickedDate, pickedUser);
            if (result.IsSuccessful != true)
            {
                TempData["MessageFailed"] = result.Content;
                await loadDdlCustomer(role, rayon);
                return Page();
            }

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



            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Recommendation");
                ws.Columns().AdjustToContents();
                ws.Cell(1, 1).Value = "Customer:";
                ws.Cell(1, 2).Value = pickedUser;
                ws.Cell(2, 1).Value = "Recommendation for:";
                ws.Cell(2, 2).DataType = XLDataType.Text;
                ws.Cell(2, 2).Value = "'"+ month + " - " + pickedDate.Split('-')[1];

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

        public async Task<IRestResponse> getRecommendation(string filename, string customer)
        {
            var client = new RestClient(conf.PythonApiUrl + "/Recommendation/?filename=" + filename+ "&&customerShipTo=" + customer);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Content-Type", "application/json");
            IRestResponse response = await client.ExecuteAsync(request);
            return response;
        }
    } // end class
}
