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
        [HttpPost]
        public IActionResult UploadFile(IFormFile file, string description, string team)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Выберите файл для загрузки.");
                return View();
            }

            // Создаём папку для хранения файлов, если её ещё нет
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }

            // Генерируем уникальное имя для файла
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(_uploadsFolder, fileName);

            // Сохраняем файл на сервере
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // Сохраняем метаданные файла в базе данных
            var fileMetadata = new FileMetadata
            {
                FileName = file.FileName,
                FilePath = filePath,
                Description = description,
                DateUploaded = DateTime.UtcNow,
                UploadedBy = User.Identity.Name ?? "Anonymous", // Имя пользователя, если есть
                Team = team
            };

            _context.Files.Add(fileMetadata);
            _context.SaveChanges();

            return RedirectToAction("Index", "Home"); // Перенаправляем на главную страницу или страницу с файлами
        }
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
