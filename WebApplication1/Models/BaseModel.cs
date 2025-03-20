using ActionsLib;

namespace WebApplication1.Models
{
    public class BaseModel
    {
        public List<ActionTypeFilters> Filters { get; set; } = new List<ActionTypeFilters>();
        public List<TimedData> TimedData { get; set; } = new List<TimedData> { };

    
    }
}
