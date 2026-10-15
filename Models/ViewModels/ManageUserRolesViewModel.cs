namespace AssignmentProg.Models.ViewModels
{
    public class ManageUserRolesViewModel
    {
        internal bool isSelected;

        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool IsSelected { get; set; } // FIXED from isSelected
    }
}