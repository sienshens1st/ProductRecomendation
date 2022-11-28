using System;
using System.ComponentModel.DataAnnotations;

namespace ProductRecomendation.Models
{

    public class tb_rayon
    {
        [Key]
        public int rayon_exp_id { get; set; }
        public string rayon_exp_code { get; set; }
        public string flag_active { get; set; }
    }
}
