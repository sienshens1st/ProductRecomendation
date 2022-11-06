using egitlab_PotionNetCore.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductRecomendation.Data;

namespace ProductRecomendation.Pages
{
    [AuthorizeCustom(Role.admin)]
    public class UploadTransactionModel : PageModel
    {


        private readonly ApplicationDbContext _context;

        public UploadTransactionModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string transactionDate { get; set; }

        
        public void OnGet()
        {
        }


        public void OnPost(IFormFile uploadedFile) { 
        
        
        }
    }
}
