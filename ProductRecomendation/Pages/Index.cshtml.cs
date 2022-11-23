using egitlab_PotionNetCore.Data;
using egitlab_PotionNetCore.Pages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductRecomendation.Data;
using ProductRecomendation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ProductRecomendation.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContext;


        public IndexModel(ApplicationDbContext context, IHttpContextAccessor httpContext)
        {
            _context = context;
            _httpContext = httpContext;
        }


        public class LoginModel
        {
            public string username { get; set; }
            public string password { get; set; }
        }


        [BindProperty]
        public LoginModel Input { get; set; }

        UrlString conf = new UrlString();
        Helper helper = new Helper();

        public void OnGet()
        {
            if (User.Claims.FirstOrDefault(c => c.Type == "username")?.Value != null)
            {
                Response.Redirect("/LandingPage");
                return;
            }
        }


        public void OnPost()
        {
            if (Input.username == null || Input.password == null)
            {
                TempData["MessageFailed"] = "Please fill in all fields.";
            }
            else
            {
                IsAuthenticated(Input.username, Input.password);
            }
        }

        private void IsAuthenticated(string userName, string password)
        {
            tb_user user = _context.tb_user.Where(x => x.username == userName).FirstOrDefault();

            if (user == null)
            {
                TempData["MessageFailed"] = "User is not registered.";
                return;
            }

            if (user.flag_active == "N")
            {
                TempData["MessageFailed"] = "User is not active. Please contact admin.";
                return;
            }

            string encryptedPassword = helper.EncryptString(conf.KeyEncrpyt, password.Trim());


            if(user.password != encryptedPassword)
            {
                TempData["MessageFailed"] = "The password you entered is incorrect. Please try again.";
                return;
            }


            var roleName = _context.tb_role.Where(x => x.role_id == user.role_id).FirstOrDefault().role_name.ToString();

            var claims = new List<Claim>
                        {
                            new Claim("username", user.username),
                            new Claim("rayon_exp_code",user.rayon_exp_code.ToString()),
                            new Claim("role",roleName),
                            new Claim("roleId",user.role_id.ToString()),
                        };

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddHours(23)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            //return RedirectToPage("/LandingPage");
            Response.Redirect("/LandingPage");

        }






    }
}
