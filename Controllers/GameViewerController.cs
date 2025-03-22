using ActionsLib;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using ActionsLib;
using ActionsLib.ActionTypes;
using System.Text.RegularExpressions;

namespace WebApplication1.Controllers
{
    public class GameViewerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private ActionsMetricTypes AMT;
        private Game game;
        private List<ActionTypeFilters> BasicFilters = new List<ActionTypeFilters>();
        private  List<VolleyActionType> VolleyActionTypes = new List<VolleyActionType>() { VolleyActionType.Serve, VolleyActionType.Reception, VolleyActionType.Set, VolleyActionType.Attack, VolleyActionType.Block, VolleyActionType.Defence, VolleyActionType.FreeBall, VolleyActionType.Transfer };
        public GameViewerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Страница с информацией о выбранной игре
        public IActionResult GamePage(string gameName)
        {
            // Ищем файл по имени игры
            var file = _context.Files.FirstOrDefault(f => f.FileName == gameName);
            if (file == null)
            {
                return NotFound();
            }

            // Возвращаем представление с информацией о выбранной игре
            return View(file);
        }
        
        public IActionResult Filters(string gameName)
        {
            HttpContext.Session.SetString("gameName", gameName);
            LoadData(gameName);
                List<TimedData> timedDatas = convertDataToTimedDataFormat(game.getVolleyActionSequence());
            string substr = GetYouTubeVideoID(game.URL);
            BaseModel model = new BaseModel
            {
                Filters = BasicFilters,
                TimedData = timedDatas,
                YoutubeURL = substr
            };
                return View("Filters", model);

        }
        private string GetYouTubeVideoID(string url)
        {
            // Регулярное выражение для поиска ID видео по разным форматам URL
            string pattern = @"(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})";

            Regex regex = new Regex(pattern);
            Match match = regex.Match(url);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return null;
        }
        private void LoadData(string gameName)
        {
            var file = _context.Files.FirstOrDefault(f => f.FileName == gameName);
            using (StreamReader sr = new StreamReader(file.FilePath))
            {
                game = Game.Load(sr);
            }
            AMT = game.ActionsMetricTypes;
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
            foreach (ActionsLib.Action a in sequence)
            {
                if (a.AuthorType == ActionAuthorType.Player) result.Add(new TimedData((PlayerAction)a, AMT));
            }
            return result;
        }

        #region FilterApplyign
        [HttpPost]
        public IActionResult UseFilters(IFormCollection form)
        {
            var gameName = HttpContext.Session.GetString("gameName");
            LoadData(gameName);
            BaseModel model = new BaseModel();
            if (form == null) return View("Filters", model);
            var selectedFilters = new Dictionary<string, List<string>>();

            foreach (var key in form.Keys)
            {
                List<string> selectedValues = new List<string>();
                for (int i = 0; i < form[key].Count; i++) if (form[key][i] != null) selectedValues.Add(form[key][i]);
                if (selectedValues.Any())
                {
                    if (selectedValues != null) selectedFilters[key] = selectedValues;
                }
            }
            if (ActualFilters != null && PlayerFilter != null)
            {
                Dictionary<VolleyActionType, Dictionary<MetricType, List<string>>> data = reconvertData(selectedFilters);
                updateFiltersHolder(data);
                updatePlayerFilter(selectedFilters);
                model.Filters = BasicFilters;
                VolleyActionSequence seq = PlayerFilter.ProcessSequence(ActualFilters.ProcessSequence(game.getVolleyActionSequence()));
                model.TimedData = convertDataToTimedDataFormat(seq);
                model.YoutubeURL = GetYouTubeVideoID(game.URL);
            }
            return View("Filters", model);
        }

        FiltersHolder ActualFilters = new FiltersHolder();
        PlayersFiltersHolder PlayerFilter = new PlayersFiltersHolder(new Team());

        private Dictionary<VolleyActionType, Dictionary<MetricType, List<string>>> reconvertData(Dictionary<string, List<string>> data)
        {
            Dictionary<VolleyActionType, Dictionary<MetricType, List<string>>> result = new Dictionary<VolleyActionType, Dictionary<MetricType, List<string>>>();
            List<string> selectedActionTypes = new List<string>();
            foreach (var actionType in data.Keys)
            {
                string[] tmp = actionType.Split('$');
                if (tmp[0] == "CheckBox") selectedActionTypes.Add(tmp[1]);
            }

            foreach (string actionType in selectedActionTypes)
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
        private void updatePlayerFilter(Dictionary<string, List<string>> data)
        {
            foreach (string key in data.Keys)
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
            foreach (VolleyActionType actionType in data.Keys)
            {
                foreach (MetricType metricType in data[actionType].Keys)
                {
                    ActualFilters.update(actionType, metricType, data[actionType][metricType]);
                }
            }
        }
        private VolleyActionType convert(string str)
        {
            foreach (VolleyActionType act in VolleyActionTypes)
            {
                if (act.ToString() == str) return act;
            }
            return VolleyActionType.Defence;
        }


        #endregion
    }
}
