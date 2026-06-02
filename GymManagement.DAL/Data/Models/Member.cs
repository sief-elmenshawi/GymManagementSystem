using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Data.Models
{
    public class Member : GymUser
    {
        public string? Photo { get; set; }

        public HealthRecord HealthRecord { get; set; } = null!;

        public ICollection<Membership> Memberships { get; set; } = new HashSet<Membership>();

        public ICollection<Booking> Bookings { get; set; } = new HashSet<Booking>();

    }
}
