using System.Collections.Generic;

namespace StudentTrackingCoach.Models.ViewModels
{
    public class AdminUserListViewModel
    {
        // What the admin typed in the search box
        public string? Search { get; set; }

        // List of users shown on the admin page
        public List<AdminUserListItemViewModel> Users { get; set; }
            = new List<AdminUserListItemViewModel>();
    }
}
