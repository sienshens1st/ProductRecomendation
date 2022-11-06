using ProductRecomendation;
using System.Collections.Generic;

namespace egitlab_PotionNetCore.Pages
{


    public class UrlString
    {
        readonly public string PythonApiUrl = Startup._configuration["UrlString:PythonApi"];
        readonly public string KeyEncrpyt = Startup._configuration["UrlString:KeyEncrpyt"];
    }

    public static class MenuData
    {
        public static IList<MenuList> menuList = null;
    }

    public static class GlobalClass {
        public static IList<string> shipToIdList;
        public static IList<Outlet> outletShipId;
    }

    public class Outlet
    {
        public string shipToId { get; set; }
        public string outletName { get; set; }

    }

    public class MenuList
    {
        public string menuId { get; set; }
        public string menuDescription { get; set; }
        public bool isAvailable { get; set; }

    }

    public class OutletModel
    {
        public string groupId { get; set; }
        public string outletId { get; set; }
        public string outletName { get; set; }
        public string shipId { get; set; }
        public string branchName { get; set; }
        public string address { get; set; }
    }


    public class RoleModel
    {
        public string id { get; set; }
        public string roleName { get; set; }
    }




}
