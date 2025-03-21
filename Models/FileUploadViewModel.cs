namespace WebApplication1.Models
{
    public class FileUploadViewModel
    {
        public IFormFile File { get; set; }  // Загружаемый файл
        public string Description { get; set; }  // Описание файла
        public string AdditionalInfo { get; set; }  // Дополнительная информация
    }
}
