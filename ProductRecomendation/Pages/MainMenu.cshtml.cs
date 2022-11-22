using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using egitlab_PotionNetCore.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace egitlab_PotionNetCore.Pages
{
    [AuthorizeCustom(Role.admin,Role.salesman)]
    public class MainMenuModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
