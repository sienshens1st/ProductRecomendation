using System;
using System.ComponentModel.DataAnnotations;

namespace ProductRecomendation.Models
{

    public class tb_product
    {
        [Key]
        public int item_id { get; set; }
        public string item_code { get; set; }
        public string item_desc { get; set; }

        public string product_family_desc { get; set; }

        public string flag_active { get; set; }

        public string lastupdate_by { get; set; }

        public DateTime lastupdate_date { get; set; }
    }
}
