using GymManagement.DAL.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Models
{
    public abstract class GymUser : BaseEntity
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = default!;
        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; } = default!;
        [Required, MaxLength(11)]
        [RegularExpression(@"^(010|011|012|015)\d{8}$",
        ErrorMessage = "Egyptian phone format only.")]
        public string Phone { get; set; } = default!;
        public DateOnly DateOfBirth { get; set; }

        // Gender 
        public Gender Gender { get; set; }
        //Address
        public Address Address { get; set; }
    }
}
