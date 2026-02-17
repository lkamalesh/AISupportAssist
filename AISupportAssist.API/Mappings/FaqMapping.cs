using AISupportAssist.API.Models.DTOs.Admin;
using AISupportAssist.API.Models.Entities;

namespace AISupportAssist.API.Mappings
{
    public static class FaqMapping
    {
        public static Faq MapToEntity(FaqDto faq)
        {
            return new Faq
            {
                Question = faq.Question,
                Answer = faq.Answer
            };
        }

        public static FaqDto MapToDto(this Faq faq)
        {

            return new FaqDto
            {
                Id = faq.Id,
                Question = faq.Question,
                Answer = faq.Answer
            };
        }

        public static IEnumerable<FaqDto> MapToDtos(IEnumerable<Faq> faqs)
        {
            return faqs.Select(s => s.MapToDto());
        }
    }
}
