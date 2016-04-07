using Prism.Mvvm;
using System.IO;

namespace MinimalEmailClient.ViewModels
{
    public class AttachmentInfoViewModel : BindableBase
    {
        private string filePath = string.Empty;
        public string FilePath
        {
            get { return this.filePath; }
            private set
            {
                SetProperty(ref this.filePath, value);
                FileName = Path.GetFileName(this.filePath);
                FileInfo fileInfo = new FileInfo(Path.GetFullPath(this.filePath));
                FileSizeBytes = fileInfo.Length;
            }
        }

        private string fileName = string.Empty;
        public string FileName
        {
            get { return this.fileName; }
            private set { SetProperty(ref this.fileName, value); }
        }

        private long fileSizeBytes = 0;
        public long FileSizeBytes
        {
            get { return this.fileSizeBytes; }
            private set { SetProperty(ref this.fileSizeBytes, value); }
        }

        public AttachmentInfoViewModel(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);
            if (File.Exists(fullPath))
            {
                FilePath = fullPath;
            }
            else
            {
                throw new FileNotFoundException();
            }
        }
    }
}