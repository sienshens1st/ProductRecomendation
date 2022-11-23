using egitlab_PotionNetCore.Data;
using egitlab_PotionNetCore.Pages;
using egitlab_PotionNetCore.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProductRecomendation.Data;
using ProductRecomendation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductRecomendation.Pages
{
    [AuthorizeCustom(Role.admin)]
    public class UserManagementModel : PageModel
    {

        private readonly ApplicationDbContext _context;

        public UserManagementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public tb_user InputAddUser { get; set; }
        [BindProperty]
        public tb_user InputEditUser { get; set; }

        public IList<tb_user> tb_Users { get; set; }


        UrlString conf = new UrlString();
        Helper helper = new Helper();

        public async Task OnGet()
        {
            await loadUsers();
        }

        private async Task loadUsers()
        {
            var users = _context.tb_user.ToListAsync();

            tb_Users = await users;
        }


        public IActionResult OnPostAddUser()
        {
            try
            {
                bool isUserExist = _context.tb_user.Where(x=> x.username == InputAddUser.username).Any();
                if (isUserExist)
                {
                    TempData["MessageFailed"] = "User Exists!";
                    return RedirectToPage("/UserManagement");
                }

                InputAddUser.flag_active = "Y";
                InputAddUser.lastupdate_by = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "username").Value;
                InputAddUser.lastupdate_date = DateTime.Now;
                InputAddUser.password = helper.EncryptString(conf.KeyEncrpyt, InputAddUser.password.Trim());

                _context.tb_user.Add(InputAddUser);
                _context.SaveChanges();

                TempData["MessageSuccess"] = "User Added!";
            }
            catch (Exception ex)
            {
                TempData["MessageFailed"] = ex.Message;
                return RedirectToPage("/UserManagement");
            }

            return RedirectToPage("/UserManagement");
        }


        public IActionResult OnPostEditUser()
        {
            try
            {
                var user = _context.tb_user.Where(x => x.user_id == InputEditUser.user_id).FirstOrDefault();
                if (String.IsNullOrEmpty(InputEditUser.password)){
                    InputEditUser.password = user.password;
                }

                user.username = InputEditUser.username;
                user.password = helper.EncryptString(conf.KeyEncrpyt, InputEditUser.password.Trim()); 
                user.rayon_exp_code = InputEditUser.rayon_exp_code;
                user.role_id = InputEditUser.role_id;
                user.flag_active = "Y";
                user.lastupdate_by = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "username").Value;
                user.lastupdate_date = DateTime.Now;
                //GAPAKE ENTITY STATE MODIFIED GPP ? GA BIKIN NULL 
              
                //_context.tb_user.Update(InputEditUser);// ini kalo ga pake user id di context nanti dia bikin user baru jadinya, dan ini harus pake as no tracking biar bisa overide id yg udah ad 
                _context.SaveChanges();
                TempData["MessageSuccess"] = "User Edited!";
            }
            catch (Exception ex)
            {
                TempData["MessageFailed"] = ex.Message;
                return RedirectToPage("/UserManagement");
            }

            return RedirectToPage("/UserManagement");
        }

        public async Task<IActionResult> OnPostDeactivateAsync(string userid_Deactivate)
        {
            if (ModelState.IsValid)
            {
                tb_user user = await _context.tb_user.FirstOrDefaultAsync(x => x.user_id == int.Parse(userid_Deactivate));
                user.flag_active = "N";
                user.lastupdate_date = DateTime.Now;
                user.lastupdate_by = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "username").Value;


                _context.Attach(user).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    TempData["MessageFailed"] = ex.Message;
                    return RedirectToPage("/UserManagement");
                }
            }

            TempData["MessageSuccess"] = "User Deactivated!";

            return RedirectToPage("/UserManagement");
        }


        public async Task<IActionResult> OnPostActivateAsync(string userid_Activate)
        {
            if (ModelState.IsValid)
            {
                tb_user user = await _context.tb_user.FirstOrDefaultAsync(x => x.user_id == int.Parse(userid_Activate));
                user.flag_active = "Y";
                user.lastupdate_date = DateTime.Now;
                user.lastupdate_by = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "username").Value;


                _context.Attach(user).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    TempData["MessageFailed"] = ex.Message;
                    return RedirectToPage("/UserManagement");
                }
            }

            TempData["MessageSuccess"] = "User Activated!";

            return RedirectToPage("/UserManagement");

        }


    }//end class
}
