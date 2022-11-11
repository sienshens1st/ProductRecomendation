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
        }
        


        [BindProperty]
        public InputRec Input { get; set; }

        public IList<tb_user> DdlCustomer { get; set; }

        public IList<OutputRecommendation> outRecommendationList { get; set; }



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
                var itemName = _context.tb_product.Where(x => x.item_code == item).FirstOrDefault().item_desc.ToString();
                outRecommendationList.Add(new OutputRecommendation { item_code = item, item_desc = itemName });
            };

            Console.Write(outRecommendationList);

            await loadDdlCustomer(role, rayon);
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
