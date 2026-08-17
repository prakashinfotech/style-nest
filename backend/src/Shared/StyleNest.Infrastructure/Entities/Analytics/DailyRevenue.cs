using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Analytics;

public class DailyRevenue : BaseEntity<Guid>
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
    public int ItemCount { get; set; }
    public decimal AverageOrderValue { get; set; }
}
