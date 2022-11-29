using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using egitlab_PotionNetCore.Security;
using LINQtoCSV;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ProductRecomendation.Data;
using ProductRecomendation.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ProductRecomendation.Pages.TransactionModel;

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


        public class TransactionCSVPropertyColumn
        {
            [Name("TRX_DATE")]
            public string TRX_DATE { get; set; }
            [Name("TRX_NUMBER")]
            public string TRX_NUMBER { get; set; }
            [Name("BRANCH_CODE")]
            public string BRANCH_CODE { get; set; }
            [Name("ITEM_CODE")]
            public string ITEM_CODE { get; set; }
            [Name("SALES_QTY")]
            public string SALES_QTY { get; set; }
            [Name("GROSS_SALES_AMOUNT")]
            public string GROSS_SALES_AMOUNT { get; set; }
            [Name("SHIP_TO_ID")]
            public string SHIP_TO_ID { get; set; }
            [Name("RAYON_EXP_CODE")]
            public string RAYON_EXP_CODE { get; set; }
            [Name("RAYON_EXP_DESC")]
            public string RAYON_EXP_DESC { get; set; }
            [Name("SALES_CHANNEL_CODE")]
            public string SALES_CHANNEL_CODE { get; set; }
            [Name("SALES_CHANNEL_DESC")]
            public string SALES_CHANNEL_DESC { get; set; }
            [Name("PRODUCT_FAMILY_CODE")]
            public string PRODUCT_FAMILY_CODE { get; set; }
            [Name("PRODUCT_FAMILY_DESC")]
            public string PRODUCT_FAMILY_DESC { get; set; }
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

                using (var stream = new MemoryStream())
                {
                    await uploadedFile.CopyToAsync(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    using (var reader = new StreamReader(stream))
                    {
                        using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
                        {
                            csvReader.Read();

                            csvReader.Context.RegisterClassMap<TransactionCSVPropertyColumnMap>();
                            try
                            {
                                var records = csvReader.GetRecord<TransactionCSVPropertyColumn>();
                            }
                            catch
                            {
                                TempData["MessageFailed"] = "Upload Error! Invalid document format detected. Please fix your file and try again.";
                                return RedirectToPage();
                            }

                        }
                    }
                }

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

    }//end class



    public class TransactionCSVPropertyColumnMap : ClassMap<TransactionCSVPropertyColumn>
    {
        public TransactionCSVPropertyColumnMap()
        {
            Map(m => m.TRX_DATE).Name("TRX_DATE");
            Map(m => m.TRX_NUMBER).Name("TRX_NUMBER");
            Map(m => m.BRANCH_CODE).Name("BRANCH_CODE");
            Map(m => m.ITEM_CODE).Name("ITEM_CODE");
            Map(m => m.SALES_QTY).Name("SALES_QTY");
            Map(m => m.GROSS_SALES_AMOUNT).Name("GROSS_SALES_AMOUNT");
            Map(m => m.SHIP_TO_ID).Name("SHIP_TO_ID");
            Map(m => m.RAYON_EXP_CODE).Name("RAYON_EXP_CODE");
            Map(m => m.RAYON_EXP_DESC).Name("RAYON_EXP_DESC");
            Map(m => m.SALES_CHANNEL_CODE).Name("SALES_CHANNEL_CODE");
            Map(m => m.SALES_CHANNEL_DESC).Name("SALES_CHANNEL_DESC");
            Map(m => m.PRODUCT_FAMILY_CODE).Name("PRODUCT_FAMILY_CODE");
            Map(m => m.PRODUCT_FAMILY_DESC).Name("PRODUCT_FAMILY_DESC");
        }

    }


}

