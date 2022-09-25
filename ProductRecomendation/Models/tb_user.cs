using System;
using System.ComponentModel.DataAnnotations;

namespace ProductRecomendation.Models
{

    public class tb_user
    {
        [Key]
        public int user_id { get; set; }
        public int role_id { get; set; }
        public string username { get; set; }

        public string password { get; set; }

        public string rayon_exp_code { get; set; }
        public string flag_active { get; set; }

        public string lastupdate_by { get; set; }

        public DateTime lastupdate_date { get; set; }
    }
}
