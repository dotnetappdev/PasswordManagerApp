using PasswordManager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PasswordManager.Services.Interfaces;

public interface ICustomFieldService
{
    Task<List<CustomField>> GetByPasswordItemIdAsync(int passwordItemId);
    Task<CustomField> CreateAsync(CustomField customField);
    Task<CustomField> UpdateAsync(CustomField customField);
    Task DeleteAsync(int id);
    Task<List<CustomField>> GetAllAsync();
}