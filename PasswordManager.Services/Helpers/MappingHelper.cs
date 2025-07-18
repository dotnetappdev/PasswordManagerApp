using PasswordManager.Models;
using PasswordManager.Models.DTOs;

namespace PasswordManager.Services.Helpers;

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
        entity.Title = dto.Title ?? entity.Title;
        entity.Description = dto.Description ?? entity.Description;
        entity.IsFavorite = dto.IsFavorite ?? entity.IsFavorite;
        entity.CategoryId = dto.CategoryId.HasValue ? dto.CategoryId.Value : entity.CategoryId;
        entity.CollectionId = dto.CollectionId.HasValue ? dto.CollectionId.Value : entity.CollectionId;
        entity.LastModified = DateTime.UtcNow;
    }

    // LoginItem mappings
    public static LoginItemDto ToDto(this LoginItem entity)
    {
        return new LoginItemDto
        {
            Id = entity.Id,
            Username = entity.Username,
            Password = entity.Password,
            Website = entity.Website,
            Notes = entity.Notes,
            LastAutoFill = entity.LastAutoFill,
            RequiresMasterPassword = entity.RequiresMasterPassword,
            PasswordId = entity.PasswordId
        };
    }

    public static LoginItem ToEntity(this CreateLoginItemDto dto)
    {
        return new LoginItem
        {
            Username = dto.Username,
            Password = dto.Password,
            Website = dto.Website,
            Notes = dto.Notes,
            RequiresMasterPassword = dto.RequiresMasterPassword
        };
    }

    public static void UpdateFromDto(this LoginItem entity, UpdateLoginItemDto dto)
    {
        entity.Username = dto.Username ?? entity.Username;
        entity.Password = dto.Password ?? entity.Password;
        entity.Website = dto.Website ?? entity.Website;
        entity.Notes = dto.Notes ?? entity.Notes;
        if (dto.RequiresMasterPassword)
            entity.RequiresMasterPassword = dto.RequiresMasterPassword;
    }

    // CreditCardItem mappings
    public static CreditCardItemDto ToDto(this CreditCardItem entity)
    {
        return new CreditCardItemDto
        {
            Id = entity.Id,
            CardholderName = entity.CardholderName,
            CardNumber = entity.CardNumber,
            ExpirationMonth = entity.ExpirationMonth,
            ExpirationYear = entity.ExpirationYear,
            SecurityCode = entity.SecurityCode,
            CardType = entity.CardType,
            Notes = entity.Notes,
            RequiresMasterPassword = entity.RequiresMasterPassword,
            PasswordId = entity.PasswordId,
            EncryptedCardNumber = entity.EncryptedCardNumber,
            CardNumberNonce = entity.CardNumberNonce,
            CardNumberAuthTag = entity.CardNumberAuthTag,
            EncryptedCvv = entity.EncryptedCvv,
            CvvNonce = entity.CvvNonce,
            CvvAuthTag = entity.CvvAuthTag
        };
    }

    public static CreditCardItem ToEntity(this CreateCreditCardItemDto dto)
    {
        return new CreditCardItem
        {
            CardholderName = dto.CardholderName,
            CardNumber = dto.CardNumber,
            ExpirationMonth = dto.ExpirationMonth,
            ExpirationYear = dto.ExpirationYear,
            SecurityCode = dto.SecurityCode,
            CardType = dto.CardType,
            Notes = dto.Notes,
            RequiresMasterPassword = dto.RequiresMasterPassword
        };
    }

    public static void UpdateFromDto(this CreditCardItem entity, UpdateCreditCardItemDto dto)
    {
        entity.CardholderName = dto.CardholderName ?? entity.CardholderName;
        entity.CardNumber = dto.CardNumber ?? entity.CardNumber;
        entity.ExpirationMonth = dto.ExpirationMonth ?? entity.ExpirationMonth;
        entity.ExpirationYear = dto.ExpirationYear ?? entity.ExpirationYear;
        entity.SecurityCode = dto.SecurityCode ?? entity.SecurityCode;
        entity.CardType = dto.CardType;
        entity.Notes = dto.Notes ?? entity.Notes;
        if (dto.RequiresMasterPassword)
            entity.RequiresMasterPassword = dto.RequiresMasterPassword;
    }

    // SecureNoteItem mappings
    public static SecureNoteItemDto ToDto(this SecureNoteItem entity)
    {
        return new SecureNoteItemDto
        {
            Id = entity.Id,
            Content = entity.Content,
            RequiresMasterPassword = entity.RequiresMasterPassword,
            PasswordId = entity.PasswordId,
            EncryptedContent = entity.EncryptedContent,
            ContentNonce = entity.ContentNonce,
            ContentAuthTag = entity.ContentAuthTag
        };
    }

    public static SecureNoteItem ToEntity(this CreateSecureNoteItemDto dto)
    {
        return new SecureNoteItem
        {
            Content = dto.Content,
            RequiresMasterPassword = dto.RequiresMasterPassword
        };
    }

    public static void UpdateFromDto(this SecureNoteItem entity, UpdateSecureNoteItemDto dto)
    {
        entity.Content = dto.Content ?? entity.Content;
        if (dto.RequiresMasterPassword)
            entity.RequiresMasterPassword = dto.RequiresMasterPassword;
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
            Notes = entity.Notes,
            RequiresMasterPassword = entity.RequiresMasterPassword,
            PasswordId = entity.PasswordId
        };
    }

    public static WiFiItem ToEntity(this CreateWiFiItemDto dto)
    {
        return new WiFiItem
        {
            NetworkName = dto.NetworkName,
            Password = dto.Password,
            SecurityType = dto.SecurityType,
            Notes = dto.Notes,
            RequiresMasterPassword = dto.RequiresMasterPassword
        };
    }

    public static void UpdateFromDto(this WiFiItem entity, UpdateWiFiItemDto dto)
    {
        entity.NetworkName = dto.NetworkName ?? entity.NetworkName;
        entity.Password = dto.Password ?? entity.Password;
        entity.SecurityType = dto.SecurityType;
        entity.Notes = dto.Notes ?? entity.Notes;
        if (dto.RequiresMasterPassword)
            entity.RequiresMasterPassword = dto.RequiresMasterPassword;
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
            LastModified = entity.LastModified
        };
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
            ParentCollectionId = entity.ParentCollectionId,
            ParentCollection = entity.ParentCollection != null ? entity.ParentCollection.ToDto() : null,
            Children = entity.Children?.Select(c => c.ToDto()).ToList() ?? new List<CollectionDto>(),
            Categories = entity.Categories?.Select(c => c.ToDto()).ToList() ?? new List<CategoryDto>(),
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            IsDefault = entity.IsDefault,
            UserId = entity.UserId
        };
    }

    // Tag mappings
    public static TagDto ToDto(this Tag entity)
    {
        return new TagDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Color = entity.Color,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified
        };
    }
}
