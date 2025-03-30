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
using Microsoft.EntityFrameworkCore;
namespace WebApplication1.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadsFolder;
        public FileUploadController(ApplicationDbContext context)
        {
            _context = context;
            _uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        }
        public IActionResult Index()
        {

            return View();
        }

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

    }




}
