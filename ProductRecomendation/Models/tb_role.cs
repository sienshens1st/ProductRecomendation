using System;
using System.ComponentModel.DataAnnotations;

namespace ProductRecomendation.Models
{

    public class tb_role
    {
        [Key]
        public int role_id { get; set; }
        public string role_name { get; set; }
    }
}
