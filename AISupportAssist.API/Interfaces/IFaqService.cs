using AISupportAssist.API.Models.DTOs.Admin;
using AISupportAssist.API.Models.Entities;

namespace AISupportAssist.API.Interfaces
{
    public interface IFaqService
    {
        Task<FaqDto?> GetByIdAsync(int id);
        Task<IEnumerable<FaqDto>> GetAllAsync();
        Task AddAsync(FaqDto faq);
        Task UpdateAsync(FaqDto faq);
        Task DeleteAsync(int id);

    }
}
