using egitlab_PotionNetCore.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductRecomendation.Data;
using ProductRecomendation.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ProductRecomendation.Pages
{
    [AuthorizeCustom(Role.admin)]
    public class ProductManagementModel : PageModel
    {

        private readonly ApplicationDbContext _context;

        public ProductManagementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public tb_product InputAddUProduct { get; set; }
        [BindProperty]
        public tb_product InputEditProduct { get; set; }

        public IList<tb_product> tb_Products { get; set; }

        public async Task OnGet()
        {
            await loadProducts();
        }

        private async Task loadProducts()
        {
            var products = _context.tb_product.ToListAsync();

            tb_Products = await products;
        }

        public async Task<IActionResult> OnPostAddProductAsync(IFormFile fileInputAdd)
        {
            try
            {
                bool isProductExist = _context.tb_product.Where(x => x.item_code == InputAddUProduct.item_code).Any();
                if (isProductExist)
                {
                    TempData["MessageFailed"] = "Product Exists!";
                    return RedirectToPage("/ProductManagement");
                }


                string datetime = DateTime.Now.ToString("yyyyMMddHHmmss");
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\ProductImage");
                var uniquefilename = datetime + "-" + ContentDispositionHeaderValue.Parse(fileInputAdd.ContentDisposition).FileName.Trim('"');
                var filepath = Path.Combine(uploadsFolder, uniquefilename);
                
                using (var fileStream = new FileStream(filepath, FileMode.Create))
                {
                    await fileInputAdd.CopyToAsync(fileStream);
                }


                InputAddUProduct.product_image = uniquefilename;
                InputAddUProduct.flag_active = "Y";
                InputAddUProduct.lastupdate_by = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "username").Value;
                InputAddUProduct.lastupdate_date = DateTime.Now;

                _context.tb_product.Add(InputAddUProduct);
                _context.SaveChanges();

                TempData["MessageSuccess"] = "Product Added!";
            }
            catch (Exception ex)
            {
                TempData["MessageFailed"] = ex.Message;
                return RedirectToPage("/ProductManagement");
            }

            return RedirectToPage("/ProductManagement");
        }


        public async Task<IActionResult> OnPostEditProductAsync(IFormFile fileInputEdit)
        {
            try
            {
                var item = _context.tb_product.Where(x => x.item_id == InputEditProduct.item_id).FirstOrDefault();


                if (fileInputEdit != null)
                {
                    string datetime = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\ProductImage");
                    var uniquefilename = datetime + "-" + ContentDispositionHeaderValue.Parse(fileInputEdit.ContentDisposition).FileName.Trim('"');
                    var filepath = Path.Combine(uploadsFolder, uniquefilename);

                    using (var fileStream = new FileStream(filepath, FileMode.Create))
                    {
                        await fileInputEdit.CopyToAsync(fileStream);
                    }
                    item.product_image = uniquefilename;
                }



                item.item_desc = InputEditProduct.item_desc;
                item.product_family_desc = InputEditProduct.product_family_desc;
                item.flag_active = "Y";
                item.lastupdate_by = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "username").Value;
                item.lastupdate_date = DateTime.Now;
                //GAPAKE ENTITY STATE MODIFIED GPP ? GA BIKIN NULL 
                //_context.tb_user.Update(InputEditProduct);// ini kalo ga pake user id di context nanti dia bikin user baru jadinya, dan ini harus pake as no tracking biar bisa overide id yg udah ad 
                _context.SaveChanges();
                TempData["MessageSuccess"] = "Product Edited!";
            }
            catch (Exception ex)
            {
                TempData["MessageFailed"] = ex.Message;
                return RedirectToPage("/ProductManagement");
            }

            return RedirectToPage("/ProductManagement");
        }

        public async Task<IActionResult> OnPostDeactivateAsync(string itemid_Deactivate)
        {
            if (ModelState.IsValid)
            {
                tb_product item = await _context.tb_product.FirstOrDefaultAsync(x => x.item_id == int.Parse(itemid_Deactivate));
                item.flag_active = "N";
                item.lastupdate_date = DateTime.Now;
                item.lastupdate_by = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "username").Value;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    TempData["MessageFailed"] = ex.Message;
                    return RedirectToPage("/ProductManagement");
                }
            }

            TempData["MessageSuccess"] = "Product Deactivated!";

            return RedirectToPage("/ProductManagement");
        }


        public async Task<IActionResult> OnPostActivateAsync(string itemid_Activate)
        {
            if (ModelState.IsValid)
            {
                tb_product item = await _context.tb_product.FirstOrDefaultAsync(x => x.item_id == int.Parse(itemid_Activate));
                item.flag_active = "Y";
                item.lastupdate_date = DateTime.Now;
                item.lastupdate_by = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "username").Value;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    TempData["MessageFailed"] = ex.Message;
                    return RedirectToPage("/ProductManagement");
                }
            }

            TempData["MessageSuccess"] = "Product Activated!";

            return RedirectToPage("/ProductManagement");

        }


    }//end class
}
