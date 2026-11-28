using System;
using System.ComponentModel.DataAnnotations;

namespace GymManagement.BLL.ViewModels.MembershipViewModels
{
    public class CreateMembershipViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid plan is required.")]
        public int PlanId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid member is required.")]
        public int MemberId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? StartDate { get; set; }
    }
}
