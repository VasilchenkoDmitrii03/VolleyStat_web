namespace WebApplication1.Models
{
    public class FileMetadata
    {
        public int Id { get; set; }
        public string FileName {  get; set; }
        public string FilePath {  get; set; }
        public string Description { get ; set; }
        public DateTime DateUploaded { get; set; }
        public string UploadedBy { get; set; }
        public string Team {  get; set; }
    }
}
