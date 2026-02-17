using AISupportAssist.API.Data;
using AISupportAssist.API.Interfaces;
using AISupportAssist.API.Mappings;
using AISupportAssist.API.Models.DTOs.Admin;
using AISupportAssist.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections;


namespace AISupportAssist.API.Services
{
    public class FaqService : IFaqService
    {
        private readonly AppDbContext _context;
        private DbSet<Faq> _dbset;

        public FaqService(AppDbContext context)
        {
            _context = context;
            _dbset = _context.Set<Faq>();
        }

        public async Task<FaqDto?> GetByIdAsync(int id)
        {
            var faq = await _dbset.FindAsync(id);
            if (faq != null)
            {
                return FaqMapping.MapToDto(faq);
            }
            return null;
        }

        public async Task<IEnumerable<FaqDto>> GetAllAsync()
        {
            var faqs = await _dbset.AsNoTracking().ToListAsync();
            return FaqMapping.MapToDtos(faqs);
        }

        public async Task AddAsync(FaqDto faqDto)
        {

            await _dbset.AddAsync(FaqMapping.MapToEntity(faqDto));
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(FaqDto faqDto)
        {
            _dbset.Update(FaqMapping.MapToEntity(faqDto));
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var faq = await _dbset.FindAsync(id);
            if (faq != null)
            {
                _dbset.Remove(faq);
                await _context.SaveChangesAsync();
            }
        }

        
    }
}
