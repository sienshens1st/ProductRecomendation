using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProductRecomendation.Models;
using System;
using System.Collections.Generic;
using System.Text;


namespace ProductRecomendation.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
           : base(options)
        {
        }

        public DbSet<tb_user> tb_user { get; set; }
    }
}
