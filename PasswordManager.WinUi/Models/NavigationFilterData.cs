using PasswordManager.Models;

namespace PasswordManager.WinUi.Models
{
    public class NavigationFilterData
    {
        public IServiceProvider ServiceProvider { get; set; }
        public ItemType? FilterType { get; set; }
        public string? FilterName { get; set; }
        // Optional search text passed when navigating from a global search
        public string? SearchText { get; set; }
        public bool? ShowFavorites { get; set; }
        public bool? ShowArchived { get; set; }
        public bool? ShowDeleted { get; set; }
        // Filter by specific category ID
        public int? FilterCategoryId { get; set; }

        public NavigationFilterData(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
    }
}