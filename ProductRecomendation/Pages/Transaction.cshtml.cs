using egitlab_PotionNetCore.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductRecomendation.Data;
using ProductRecomendation.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProductRecomendation.Pages
{
    [AuthorizeCustom(Role.admin)]
    public class TransactionModel : PageModel
    {


        private readonly ApplicationDbContext _context;

        public TransactionModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class transactionViewModel
        {
            public int transaction_id { get; set; }
            public string transaction_date { get; set; }
            public string file_location { get; set; }
            public int dateordering { get; set; }
        }


        [BindProperty]
        public tb_transaction Input { get; set; }
        [BindProperty]
        public string transactionDate { get; set; }

        public IList<transactionViewModel> tb_Transactions { get; set; }

        public async Task OnGet()
        {
            var tb_transactions = await _context.tb_transaction.ToListAsync();

            var transactions = (from tb in tb_transactions
                                select new transactionViewModel
                                {
                                    transaction_id = tb.transaction_id,
                                    transaction_date = tb.transaction_date,
                                    file_location = tb.file_location,
                                    dateordering = int.Parse(tb.transaction_date.Split("-")[1] + tb.transaction_date.Split("-")[0])
                                }).OrderByDescending(x => x.dateordering).ToList();

            tb_Transactions = transactions;


        }

        public async Task<IActionResult> OnPostAsync(IFormFile uploadedFile)
        {
            try
            {
                string extension = Path.GetExtension(uploadedFile.FileName);
                string filename = transactionDate.ToString() + extension;

                string saveDirectory = @"D:\User\Desktop\Program Skripsi Dennis\API\Files\";
                Directory.CreateDirectory(saveDirectory);

                string path = saveDirectory + filename;

                using (Stream stream = new FileStream(path, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(stream);
                }

                bool isDataExist = _context.tb_transaction.Where(x => x.transaction_date == transactionDate).Any();
                if (!isDataExist)
                {
                    Input.transaction_date = transactionDate;
                    Input.file_location = path;

                    _context.tb_transaction.Add(Input);
                    _context.SaveChanges();
                }
                TempData["MessageSuccess"] = "Upload Success!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["MessageFailed"] = ex.Message;
                return RedirectToPage();
            }
        }

    }//end modal
}

