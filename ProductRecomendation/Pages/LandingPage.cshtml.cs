using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

namespace ProductRecomendation.Pages
{
    public class LandingPageModel : PageModel
    {
        public void OnGet()
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (role == "admin")
            {
                Response.Redirect("MainMenu");
                return;
            }
        }
    }
}
