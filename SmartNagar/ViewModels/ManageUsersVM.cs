using System.Collections.Generic;

namespace SmartNagar.ViewModels
{
    public class ManageUsersVM
    {
        public List<UserRow> Users { get; set; } = new();

        public class UserRow
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public string Role { get; set; } = "";
            public bool IsActive { get; set; }
        }
    }
}
