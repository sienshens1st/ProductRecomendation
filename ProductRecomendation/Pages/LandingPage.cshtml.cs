using egitlab_PotionNetCore.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using ProductRecomendation.Data;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ProductRecomendation.Pages
{
    public class LandingPageModel : PageModel
    {

        private readonly ApplicationDbContext _context;

        public LandingPageModel(ApplicationDbContext context)
        {
            _context = context;
        }


        public class OutputRecommendation
        {
            public string item_code { get; set; }
            public string item_desc { get; set; }
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

        public void OnGet()
        {
            TempData["NoData"] = "true";
            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (role == "admin" || role == "salesman")
            {
                Response.Redirect("MainMenu");
                return;
            }
            string rayon = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "rayon_exp_code").Value;
            

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
                var itemName = _context.tb_product.Where(x => x.item_code == item).FirstOrDefault().item_desc.ToString();
                outRecommendationList.Add(new OutputRecommendation { item_code = item, item_desc = itemName });
            };

            Console.Write(outRecommendationList);


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
                var grossvalue = String.Format(CultureInfo.CreateSpecificCulture("id-id"), "Rp. {0:N}", int.Parse(item.GROSS_SALES_AMOUNT));
                var trans_date_parsed = DateTime.ParseExact(item.TRX_DATE, "MM/dd/yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture).ToString("dd-MMM-yyyy");
                var itemName = _context.tb_product.Where(x => x.item_code == item.ITEM_CODE).FirstOrDefault().item_desc.ToString();
                outTransactionHistoryList.Add(new TransactionHistory { 
                    ITEM_CODE = item.ITEM_CODE, 
                    ITEM_NAME = itemName,
                    SALES_QTY = item.SALES_QTY,
                    GROSS_SALES_AMOUNT = grossvalue.Remove(grossvalue.Length - 1),
                    TRX_DATE = trans_date_parsed
                });
            };

            int splitmonth;

            if (int.Parse(recommendationDate.Split('-')[0]) == 1)
            {
                splitmonth = 12;
            }
            else {
                splitmonth = int.Parse(recommendationDate.Split('-')[0]) - 1;
            };
        

            var month = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(splitmonth);
            var year = recommendationDate.Split('-')[1];
            historyInfo = String.Format("25 {0} {1} - 25 {0} {2} ", month, int.Parse(year)-1 ,year);
            TempData["NoData"] = "false";

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