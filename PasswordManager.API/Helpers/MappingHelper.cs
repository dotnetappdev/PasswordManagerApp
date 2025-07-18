using PasswordManager.Models;
using PasswordManager.Models.DTOs;

namespace PasswordManager.API.Helpers;

public static class MappingHelper
{
    // PasswordItem mappings
    public static PasswordItemDto ToDto(this PasswordItem entity)
    {
        return new PasswordItemDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Type = entity.Type,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            IsFavorite = entity.IsFavorite,
            IsArchived = entity.IsArchived,
            IsDeleted = entity.IsDeleted,
            CategoryId = entity.CategoryId,
            CollectionId = entity.CollectionId,
            Category = entity.Category?.ToDto(),
            Collection = entity.Collection?.ToDto(),
            LoginItem = entity.LoginItem?.ToDto(),
            CreditCardItem = entity.CreditCardItem?.ToDto(),
            SecureNoteItem = entity.SecureNoteItem?.ToDto(),
            WiFiItem = entity.WiFiItem?.ToDto(),
            Tags = entity.Tags?.Select(t => t.ToDto()).ToList() ?? new List<TagDto>()
        };
    }

    public static PasswordItem ToEntity(this CreatePasswordItemDto dto)
    {
        return new PasswordItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Type = dto.Type,
            IsFavorite = dto.IsFavorite,
            CategoryId = dto.CategoryId,
            CollectionId = dto.CollectionId,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            IsDeleted = false,
            IsArchived = false
        };
    }

    public static void UpdateFromDto(this PasswordItem entity, UpdatePasswordItemDto dto)
    {
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.IsFavorite = dto.IsFavorite ?? entity.IsFavorite;
        entity.IsArchived = dto.IsArchived;
        entity.CategoryId = dto.CategoryId;
        entity.CollectionId = dto.CollectionId;
        entity.LastModified = DateTime.UtcNow;
    }

    // LoginItem mappings
    public static LoginItemDto ToDto(this LoginItem entity)
    {
        return new LoginItemDto
        {
            Id = entity.Id,
            Username = entity.Username,
            Email = entity.Email,
            Password = entity.Password,
            Website = entity.Website,
            TotpSecret = entity.TotpSecret,
            Notes = entity.Notes,
            PasswordItemId = entity.PasswordItemId
        };
    }

    public static LoginItem ToEntity(this CreateLoginItemDto dto)
    {
        return new LoginItem
        {
            Username = dto.Username,
            Email = dto.Email,
            Password = dto.Password,
            Website = dto.Website,
            TotpSecret = dto.TotpSecret,
            Notes = dto.Notes
        };
    }

    public static void UpdateFromDto(this LoginItem entity, UpdateLoginItemDto dto)
    {
        entity.Username = dto.Username;
        entity.Email = dto.Email;
        entity.Password = dto.Password;
        entity.Website = dto.Website;
        entity.TotpSecret = dto.TotpSecret;
        entity.Notes = dto.Notes;
    }

    // CreditCardItem mappings
    public static CreditCardItemDto ToDto(this CreditCardItem entity)
    {
        return new CreditCardItemDto
        {
            Id = entity.Id,
            CardholderName = entity.CardholderName,
            CardNumber = entity.CardNumber,
            CVV = entity.CVV,
            ExpiryDate = entity.ExpiryDate,
            PIN = entity.PIN,
            CardType = entity.CardType,
            Notes = entity.Notes,
            PasswordItemId = entity.PasswordItemId
        };
    }

    public static CreditCardItem ToEntity(this CreateCreditCardItemDto dto)
    {
        return new CreditCardItem
        {
            CardholderName = dto.CardholderName,
            CardNumber = dto.CardNumber,
            CVV = dto.CVV,
            ExpiryDate = dto.ExpiryDate,
            PIN = dto.PIN,
            CardType = dto.CardType,
            Notes = dto.Notes
        };
    }

    public static void UpdateFromDto(this CreditCardItem entity, UpdateCreditCardItemDto dto)
    {
        entity.CardholderName = dto.CardholderName;
        entity.CardNumber = dto.CardNumber;
        entity.CVV = dto.CVV;
        entity.ExpiryDate = dto.ExpiryDate;
        entity.PIN = dto.PIN;
        entity.CardType = dto.CardType;
        entity.Notes = dto.Notes;
    }

    // SecureNoteItem mappings
    public static SecureNoteItemDto ToDto(this SecureNoteItem entity)
    {
        return new SecureNoteItemDto
        {
            Id = entity.Id,
            Content = entity.Content,
            PasswordItemId = entity.PasswordItemId
        };
    }

    public static SecureNoteItem ToEntity(this CreateSecureNoteItemDto dto)
    {
        return new SecureNoteItem
        {
            Content = dto.Content
        };
    }

    public static void UpdateFromDto(this SecureNoteItem entity, UpdateSecureNoteItemDto dto)
    {
        entity.Content = dto.Content;
    }

    // WiFiItem mappings
    public static WiFiItemDto ToDto(this WiFiItem entity)
    {
        return new WiFiItemDto
        {
            Id = entity.Id,
            NetworkName = entity.NetworkName,
            Password = entity.Password,
            SecurityType = entity.SecurityType,
            Frequency = entity.Frequency,
            RouterPassword = entity.RouterPassword,
            Notes = entity.Notes,
            PasswordItemId = entity.PasswordItemId
        };
    }

    public static WiFiItem ToEntity(this CreateWiFiItemDto dto)
    {
        return new WiFiItem
        {
            NetworkName = dto.NetworkName,
            Password = dto.Password,
            SecurityType = dto.SecurityType,
            Frequency = dto.Frequency,
            RouterPassword = dto.RouterPassword,
            Notes = dto.Notes
        };
    }

    public static void UpdateFromDto(this WiFiItem entity, UpdateWiFiItemDto dto)
    {
        entity.NetworkName = dto.NetworkName;
        entity.Password = dto.Password;
        entity.SecurityType = dto.SecurityType;
        entity.Frequency = dto.Frequency;
        entity.RouterPassword = dto.RouterPassword;
        entity.Notes = dto.Notes;
    }

    // Category mappings
    public static CategoryDto ToDto(this Category entity)
    {
        return new CategoryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Icon = entity.Icon,
            Color = entity.Color,
            CreatedAt = entity.CreatedAt,
            CollectionId = entity.CollectionId,
            Collection = entity.Collection?.ToDto()
        };
    }

    public static Category ToEntity(this CreateCategoryDto dto)
    {
        return new Category
        {
            Name = dto.Name,
            Description = dto.Description,
            Icon = dto.Icon,
            Color = dto.Color,
            CollectionId = dto.CollectionId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static void UpdateFromDto(this Category entity, UpdateCategoryDto dto)
    {
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Icon = dto.Icon;
        entity.Color = dto.Color;
        entity.CollectionId = dto.CollectionId;
    }

    // Collection mappings
    public static CollectionDto ToDto(this Collection entity)
    {
        return new CollectionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Icon = entity.Icon,
            Color = entity.Color,
            CreatedAt = entity.CreatedAt,
            IsDefault = entity.IsDefault,
            ParentCollectionId = entity.ParentCollectionId,
            ParentCollection = entity.ParentCollection?.ToDto(),
            Children = entity.Children?.Select(c => c.ToDto()).ToList() ?? new List<CollectionDto>(),
            Categories = entity.Categories?.Select(c => c.ToDto()).ToList() ?? new List<CategoryDto>()
        };
    }

    public static Collection ToEntity(this CreateCollectionDto dto)
    {
        return new Collection
        {
            Name = dto.Name,
            Description = dto.Description,
            Icon = dto.Icon,
            Color = dto.Color,
            IsDefault = dto.IsDefault,
            ParentCollectionId = dto.ParentCollectionId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static void UpdateFromDto(this Collection entity, UpdateCollectionDto dto)
    {
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Icon = dto.Icon;
        entity.Color = dto.Color;
        entity.IsDefault = dto.IsDefault;
        entity.ParentCollectionId = dto.ParentCollectionId;
    }

    // Tag mappings
    public static TagDto ToDto(this Tag entity)
    {
        return new TagDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Color = entity.Color,
            IsSystemTag = entity.IsSystemTag,
            CreatedAt = entity.CreatedAt
        };
    }

    public static Tag ToEntity(this CreateTagDto dto)
    {
        return new Tag
        {
            Name = dto.Name,
            Color = dto.Color,
            IsSystemTag = dto.IsSystemTag,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static void UpdateFromDto(this Tag entity, UpdateTagDto dto)
    {
        entity.Name = dto.Name;
        entity.Color = dto.Color;
        entity.IsSystemTag = dto.IsSystemTag;
    }

    // Helper method for collections
    public static IEnumerable<T> ToDto<T>(this IEnumerable<object> entities) where T : class
    {
        return entities.Cast<T>();
    }
}
