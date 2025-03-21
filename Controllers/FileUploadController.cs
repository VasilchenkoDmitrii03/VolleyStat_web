using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ActionsLib;
using ActionsLib.ActionTypes;
using WebApplication1.Models;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using System.Reflection;
namespace WebApplication1.Controllers
{
    public class FileUploadController : Controller
    {
        static Game game { get; set; } = new Game(new List<int>(), new ActionsMetricTypes("test"), new Team());
        public static ActionsMetricTypes AMT { get; set; } = new ActionsMetricTypes("empty");
        public static List<ActionTypeFilters> BasicFilters = new List<ActionTypeFilters>();
        public static List<VolleyActionType> VolleyActionTypes = new List<VolleyActionType>() { VolleyActionType.Serve, VolleyActionType.Reception, VolleyActionType.Set, VolleyActionType.Attack, VolleyActionType.Block, VolleyActionType.Defence, VolleyActionType.FreeBall, VolleyActionType.Transfer };
        public IActionResult Index()
        {
            BaseModel model = new BaseModel();

            return View(model);
        }

        [HttpPost]
        public IActionResult UploadFile(IFormFile file)
        {
            try
            {
                if (file != null && file.Length > 0)
                {
                    using var reader = new StreamReader(file.OpenReadStream());
                    
                    BasicFilters.Clear();
                    List<ActionTypeFilters> actionTypeFiltersList = BasicFilters;
                    actionTypeFiltersList.Add(createPlayersFilter(game.Team));
                    foreach (VolleyActionType actType in AMT.Keys)
                    {
                        ActionTypeFilters actionTypeFilters1 = new ActionTypeFilters();
                        actionTypeFilters1.Filters = new List<Filter>();
                        actionTypeFilters1.ActionType = actType.ToString();
                        foreach (MetricType mType in AMT[actType])
                        {
                            Filter filter = new Filter();
                            filter.Name = mType.Name;
                            filter.Options = new List<string>(mType.AcceptableValuesNames.Values);
                            actionTypeFilters1.Filters.Add(filter);
                        }
                        actionTypeFiltersList.Add(actionTypeFilters1);
                    }



                    // Десериализуем JSON-файл в структуру фильтров

                    List<TimedData> timedDatas = convertDataToTimedDataFormat(game.getVolleyActionSequence());
                    //TempData["Filters"] = JsonConvert.SerializeObject(actionTypeFiltersList);
                   // TempData["FilteredData"] = JsonConvert.SerializeObject(timedDatas);
                    BaseModel model = new BaseModel
                    {
                        Filters = actionTypeFiltersList,
                       TimedData = timedDatas
                    };
                    return View("Index", model);
                }
                return RedirectToAction("Index", new BaseModel());
            }
            catch
            {
                BaseModel model_ = new BaseModel();
                return RedirectToAction("Index", model_);
            }
            
        }
        private ActionTypeFilters createPlayersFilter(Team team)
        {
            ActionTypeFilters playerFilter = new ActionTypeFilters();
            playerFilter.ActionType = "Player";
            playerFilter.Filters = new List<Filter>();
            Filter filter = new Filter();
            filter.Name = team.Name;
            filter.Options = new List<string>();
            foreach (Player p in team.Players) filter.Options.Add(p.ToString());
            playerFilter.Filters.Add(filter);
            return playerFilter;
        }
        private List<TimedData> convertDataToTimedDataFormat(VolleyActionSequence sequence)
        {
            List<TimedData> result = new List<TimedData>();
            foreach(ActionsLib.Action a in sequence)
            {
                if(a.AuthorType == ActionAuthorType.Player) result.Add(new TimedData((PlayerAction)a, AMT));
            }
            return result;
        }

        public IActionResult Filters()
        {
            var filtersJson = TempData["Filters"] as string;
            if (filtersJson != null)
            {
                var filters = JsonConvert.DeserializeObject<List<ActionTypeFilters>>(filtersJson);
                return View(filters);
            }
            BaseModel model = new BaseModel();
            return RedirectToAction("Index", model);
        }

        [HttpPost]
        public IActionResult UseFilters(IFormCollection form)
        {
            BaseModel model = new BaseModel();
            if (form == null) return View("Index", model);
            var selectedFilters = new Dictionary<string, List<string>>();

            foreach (var key in form.Keys)
            {
                List<string> selectedValues = new List<string>();
                for (int i = 0; i < form[key].Count; i++) if(form[key][i] != null) selectedValues.Add(form[key][i]);
                if (selectedValues.Any())
                {
                    if(selectedValues != null) selectedFilters[key] = selectedValues;
                }
            }
            if(ActualFilters != null && PlayerFilter != null)
            {
                Dictionary<VolleyActionType, Dictionary<MetricType, List<string>>> data = reconvertData(selectedFilters);
                updateFiltersHolder(data);
                updatePlayerFilter(selectedFilters);
                model.Filters = BasicFilters;
                VolleyActionSequence seq = PlayerFilter.ProcessSequence(ActualFilters.ProcessSequence(game.getVolleyActionSequence()));
                model.TimedData = convertDataToTimedDataFormat(seq);
            }
            return View("Index", model);
        }

        FiltersHolder ActualFilters = new FiltersHolder();
        PlayersFiltersHolder PlayerFilter = new PlayersFiltersHolder(new Team());

        private Dictionary<VolleyActionType, Dictionary<MetricType, List<string>>> reconvertData(Dictionary<string, List<string>> data)
        {
            Dictionary<VolleyActionType, Dictionary<MetricType, List<string>>> result = new Dictionary<VolleyActionType, Dictionary<MetricType, List<string>>>();
            List<string> selectedActionTypes = new List<string>();
            foreach(var actionType in data.Keys)
            {
                string[] tmp = actionType.Split('$');
                if (tmp[0] == "CheckBox") selectedActionTypes.Add(tmp[1]);
            }

            foreach(string actionType in selectedActionTypes)
            {
                VolleyActionType type = convert(actionType);
                result.Add(type, new Dictionary<MetricType, List<string>>());
                
                foreach (var key in data.Keys)
                {
                    string[] tmp = key.Split('$');
                    if (tmp[0] == "ComboBox" && tmp[1] == actionType)
                    {
                        result[type].Add(AMT.getByName(type, tmp[2]), data[key]);
                    }
                }
            }
            return result;
        }
        private void updatePlayerFilter(Dictionary<string, List<string>> data) {
            foreach(string key in data.Keys)
            {
                string[] tmp = key.Split('$');
                if (tmp[0] == "ComboBox" && tmp[1] == "Player")
                {
                    PlayerFilter = new PlayersFiltersHolder(game.Team);
                    List<string> numbers = new List<string>();
                    foreach (string str in data[key])
                    {
                        numbers.Add(str.Substring(1));
                    }
                    PlayerFilter.update(numbers);
                }
            }
        
        }
        private void updateFiltersHolder(Dictionary<VolleyActionType, Dictionary<MetricType, List<string>>> data)
        {
            ActualFilters = new FiltersHolder(AMT);
            foreach(VolleyActionType actionType in data.Keys)
            {
                foreach(MetricType metricType in data[actionType].Keys)
                {
                    ActualFilters.update(actionType, metricType, data[actionType][metricType]);
                }
            }
        }
        private VolleyActionType convert(string str)
        {
            foreach(VolleyActionType act in VolleyActionTypes)
            {
                if(act.ToString() == str) return act;
            }
            return VolleyActionType.Defence;
        }
        
        
    }




}
