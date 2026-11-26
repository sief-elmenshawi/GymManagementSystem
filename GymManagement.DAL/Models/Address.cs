using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Models
{
    [Owned]
    public class Address
    {
        [Required, MaxLength(50)]
        public string Street { get; set; }
        [Required , MaxLength(50)]
        public string City { get; set; }
        public int BuildingNumber { get; set; }
    }
}
