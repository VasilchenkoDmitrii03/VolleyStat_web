using ActionsLib;

namespace WebApplication1.Models
{
    public class ActionTypeFilters
    {
        public string ActionType { get; set; } = "";
        public List<Filter> Filters { get; set; } = new List<Filter>();
    }

    public class Filter
    {
        public string Name { get; set; } = "";
        public List<string> Options { get; set; } = new List<string>();
    }
}

