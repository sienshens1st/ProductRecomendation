using egitlab_PotionNetCore.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using ProductRecomendation.Data;
using RestSharp;
using System;
using System.Collections.Generic;
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


        UrlString conf = new UrlString();

        [BindProperty]
        public string recommendationDate { get; set; }

        public void OnGet()
        {
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
                return;
            }

            var result = await getRecommendation(recommendationDate, username);
            if (result.IsSuccessful != true)
            {
                TempData["MessageFailed"] = result.Content;
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
                return;
            }

            outTransactionHistoryList = new List<TransactionHistory>();
            var listResultHistory = JsonConvert.DeserializeObject<IList<TransactionHistory>>(result2.Content);


            foreach (var item in listResultHistory)
            {
                var itemName = _context.tb_product.Where(x => x.item_code == item.ITEM_CODE).FirstOrDefault().item_desc.ToString();
                outTransactionHistoryList.Add(new TransactionHistory { 
                    ITEM_CODE = item.ITEM_CODE, 
                    ITEM_NAME = itemName,
                    SALES_QTY = item.SALES_QTY,
                    GROSS_SALES_AMOUNT = item.GROSS_SALES_AMOUNT,
                    TRX_DATE = item.TRX_DATE
                });
            };

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
