using PasswordManager.Models;

namespace PasswordManager.WinUi.Models
{
    public class NavigationFilterData
    {
        public IServiceProvider ServiceProvider { get; set; }
        public ItemType? FilterType { get; set; }
        public string? FilterName { get; set; }
        public bool? ShowFavorites { get; set; }
        public bool? ShowArchived { get; set; }
        public bool? ShowDeleted { get; set; }

        public NavigationFilterData(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
    }
}