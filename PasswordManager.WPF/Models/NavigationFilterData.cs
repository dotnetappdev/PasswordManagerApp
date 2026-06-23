using System;
using PasswordManager.Models;

namespace PasswordManager.WPF.Models
{
    public class NavigationFilterData
    {
        public IServiceProvider ServiceProvider { get; set; }
        public ItemType? FilterType { get; set; }
        public string? FilterName { get; set; }
        public bool? ShowFavorites { get; set; }
        public bool? ShowArchived { get; set; }
        public bool? ShowDeleted { get; set; }
        public string? TagName { get; set; }
        public string? FilterCategoryName { get; set; } // Filter by category name
        public int? FilterVaultId { get; set; } // Filter items belonging to a specific vault
        public string? FilterVaultName { get; set; } // Display name of the vault being filtered (shown in the header, never the id)

        public NavigationFilterData(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
    }
}
