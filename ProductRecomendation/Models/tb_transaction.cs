using System;
using System.ComponentModel.DataAnnotations;

namespace ProductRecomendation.Models
{

    public class tb_transaction
    {
        [Key]
        public int transaction_id { get; set; }
        public DateTime transaction_date { get; set; }
        public string file_location { get; set; }
    }
}
