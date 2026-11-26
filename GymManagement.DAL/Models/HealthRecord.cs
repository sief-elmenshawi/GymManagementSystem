using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Models
{
    public class HealthRecord : BaseEntity
    {
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        [MaxLength(500)]
        public string? Note { get; set; }
        [Required, MaxLength(5)]
        public string BloodType { get; set; } = default!;

        public Member Member { get; set; } = null!;
        public int MemberId { get; set; }   
    }
}
