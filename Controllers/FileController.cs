using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadsFolder;
        public FileController(ApplicationDbContext context)
        {
            _context = context;
            _uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        }

        // Страница загрузки файла
        [HttpGet]
        public IActionResult UploadFile()
        {
            //_context.Database.EnsureDeleted();
            //_context.Database.Migrate();
            return View();
        }

        // Обработка загрузки файла

        // Страница отображения всех файлов
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var files = await _context.Files.ToListAsync();
            return View(files);
        }


        [HttpPost]
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
    }
}
