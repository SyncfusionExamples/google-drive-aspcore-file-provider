using System;
using System.Collections.Generic;
using System.Linq;
#if EJ2_DNX
using System.Web;
#endif

namespace Syncfusion.EJ2.FileManager.Base
{

    public class FileManagerResponse
    {
        public FileManagerDirectoryContent CWD { get; set; }
        public IEnumerable<FileManagerDirectoryContent> Files { get; set; }

        public ErrorProperty Error { get; set; }

        public FileDetails Details { get; set; }

    }

}