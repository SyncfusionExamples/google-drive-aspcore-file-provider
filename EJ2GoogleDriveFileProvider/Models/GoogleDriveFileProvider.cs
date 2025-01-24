using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Syncfusion.EJ2.FileManager.Base;
using Microsoft.AspNetCore.Http;
using Google.Apis.Download;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using File = Google.Apis.Drive.v2.Data.File;
using GoogleDriveOAuth2;
using System.Net;
using System.IO.Compression;
using System.Text.Json;

namespace EJ2FileManagerService.Models
{
    public class GoogleDriveFileProvider
    {
        static Dictionary<string, Google.Apis.Drive.v2.Data.File> files = new Dictionary<string, Google.Apis.Drive.v2.Data.File>();
        long sizeValue = 0;
        private List<string> path = new List<string>();
        private List<string> idValues = new List<string>();

        // Gets the service authentication
        public static DriveService GetService() { return new OAuthHelper().GetAuth(); }
        // Search for file(s) or folder(s)
        public virtual FileManagerResponse Search(string path, string searchString, bool showHiddenItems = false, bool caseSensitive = false, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse searchResponse = new FileManagerResponse();
            DriveService service = GetService();
            File fileData = service.Files.Get(data[0].Id).Execute();
            FileManagerDirectoryContent cwd = new FileManagerDirectoryContent();
            cwd.IsFile = fileData.MimeType == "application/vnd.google-apps.folder" ? false : true;
            cwd.Size = fileData.FileSize != null ? long.Parse(fileData.FileSize.ToString()) : 0;
            cwd.DateCreated = Convert.ToDateTime(fileData.ModifiedDate);
            cwd.FilterPath = obtainFilterPath(fileData, true) + @"\";
            cwd.DateModified = Convert.ToDateTime(fileData.ModifiedDate);
            cwd.Type = fileData.FileExtension == null ? "folder" : fileData.FileExtension;
            searchResponse.CWD = cwd;
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Fields = "nextPageToken, files(*)";
            List<Google.Apis.Drive.v2.Data.File> result = new List<Google.Apis.Drive.v2.Data.File>();
            FilesResource.ListRequest request = service.Files.List();
            request.Q = "title='" + searchString.Replace("*", "") + "'";
            IList<Google.Apis.Drive.v2.Data.File> searchList = request.ExecuteAsync().Result.Items;
            List<FileManagerDirectoryContent> rootFileList = searchList.Select(x => new FileManagerDirectoryContent()
            {
                Id = x.Id,
                Name = x.Title,
                Size = x.FileSize != null ? long.Parse(x.FileSize.ToString()) : 0,
                DateCreated = Convert.ToDateTime(x.CreatedDate),
                DateModified = Convert.ToDateTime(x.ModifiedDate),
                Type = x.FileExtension,
                HasChild = getChildrenById(x.Id),
                FilterId = obtainFilterId(x),
                IsFile = x.MimeType == "application/vnd.google-apps.folder" ? false : true,
                FilterPath = (x.MimeType == "application/vnd.google-apps.folder" ? getFilterPath(x, true) : getFilterPath(service.Files.Get(x.Parents[0].Id).Execute(), true) + @"\" + service.Files.Get(x.Parents[0].Id).Execute().Title) + @"\",
            }).ToList();
            searchResponse.Files = rootFileList;
            return searchResponse;
        }
        // Gets the child file(s) or directories within directory
        protected bool getChildrenById(string id)
        {
            DriveService service = GetService();
            List<Google.Apis.Drive.v2.Data.File> result = new List<Google.Apis.Drive.v2.Data.File>();
            FilesResource.ListRequest request = service.Files.List();
            request.Q = "trashed=false";
            request.Q += string.Format(" and '{0}' in parents", id);
            FileList files = request.Execute();
            result = files.Items.ToList<Google.Apis.Drive.v2.Data.File>();
            return result.Where(x => x.MimeType == "application/vnd.google-apps.folder").Count() > 0 ? true : false;
        }

        // Gets the file details
        private FileManagerDirectoryContent getFileDetails(string id)
        {
            File filedata = GetService().Files.Get(id).Execute();
            return new FileManagerDirectoryContent
            {
                Name = filedata.Title,
                IsFile = filedata.MimeType == "application/vnd.google-apps.folder" ? false : true,
                Size = filedata.FileSize != null ? long.Parse(filedata.FileSize.ToString()) : 0,
                DateCreated = Convert.ToDateTime(filedata.ModifiedDate),
                FilterPath = obtainFilterPath(filedata, true) + @"\",
                Id = filedata.Id,
                FilterId = obtainFilterId(filedata),
                DateModified = Convert.ToDateTime(filedata.ModifiedDate),
                Type = filedata.FileExtension == null ? "folder" : filedata.FileExtension,
            };
        }

        // Deletes file(s) or folder(s)
        public virtual FileManagerResponse Delete(string path, string[] names, FileManagerDirectoryContent[] data)
        {
            DriveService service = GetService();
            FileManagerResponse deleteResponse = new FileManagerResponse();
            foreach (FileManagerDirectoryContent file in data)
            {
                deleteResponse.Files = new[] { getFileDetails(file.Id) };
                service.Files.Delete(file.Id).Execute();
            }
            return deleteResponse;
        }
        // Creates a newFolder
        public virtual FileManagerResponse Create(string path, string name, params FileManagerDirectoryContent[] data)
        {
            DriveService service = GetService();
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Fields = "nextPageToken, files(*)";
            List<Google.Apis.Drive.v2.Data.File> result = new List<Google.Apis.Drive.v2.Data.File>();
            FilesResource.ListRequest req = service.Files.List();
            IList<Google.Apis.Drive.v2.Data.File> files = req.Execute().Items;
            FileManagerResponse readResponse = new FileManagerResponse();
            FileManagerResponse createResponse = new FileManagerResponse();
            FileManagerDirectoryContent CreateData = new FileManagerDirectoryContent();
            var fileMetaData = new File()
            {
                Title = name,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<ParentReference> { new ParentReference() { Id = data[0].Id } }
            };
            FilesResource.InsertRequest request = service.Files.Insert(fileMetaData);
            request.Fields = "id";
            CreateData.Name = name;
            CreateData.Id = request.Execute().Id;
            CreateData.IsFile = false;
            CreateData.Size = 0;
            CreateData.DateModified = new DateTime();
            CreateData.DateCreated = new DateTime();
            CreateData.HasChild = false;
            CreateData.Type = "folder";
            createResponse.Files = new FileManagerDirectoryContent[] { CreateData };
            return createResponse;
        }
        // Download file(s) or folder(s)
        public virtual FileStreamResult Download(string path, string[] names, FileManagerDirectoryContent[] data)
        {
            FileStreamResult fileStreamResult = null;
            List<String> files = new List<String> { };
            DriveService service = GetService();
            foreach (FileManagerDirectoryContent item in data)
            {
                File fileProperties = service.Files.Get(item.Id).Execute();
                byte[] fileContent = null;
                if (item.IsFile)
                {
                    fileContent = service.HttpClient.GetByteArrayAsync(fileProperties.DownloadUrl).Result;

                    if (System.IO.File.Exists(Path.Combine(Path.GetTempPath(), item.Name)))
                        System.IO.File.Delete(Path.Combine(Path.GetTempPath(), item.Name));
                    using (Stream file = System.IO.File.OpenWrite(Path.Combine(Path.GetTempPath(), item.Name)))
                    {
                        file.Write(fileContent, 0, fileContent.Length);
                    }
                }
                else Directory.CreateDirectory(Path.GetTempPath() + item.Name);
                if (files.IndexOf(item.Name) == -1) files.Add(item.Name);
            }
            if (files.Count == 1 && data[0].IsFile)
            {
                try
                {
                    FileStream fileStreamInput = new FileStream(Path.Combine(Path.GetTempPath(), files[0]), FileMode.Open, FileAccess.Read);
                    fileStreamResult = new FileStreamResult(fileStreamInput, "APPLICATION/octet-stream");
                    fileStreamResult.FileDownloadName = files[0];
                }
                catch (Exception ex) { throw ex; }
            }
            else
            {
                ZipArchiveEntry zipEntry;
                ZipArchive archive;
                using (archive = ZipFile.Open(Path.Combine(Path.GetTempPath(), "files.zip"), ZipArchiveMode.Update))
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        if (!data[i].IsFile)
                        {
                            zipEntry = archive.CreateEntry(data[i].Name + "/");
                            DownloadFolderFiles(data[i].Id, data[i].Name, archive, zipEntry);
                        }
                        else zipEntry = archive.CreateEntryFromFile(Path.GetTempPath() + files[i], files[i], CompressionLevel.Fastest);
                    }
                    archive.Dispose();
                    FileStream fileStreamInput = new FileStream(Path.Combine(Path.GetTempPath(), "files.zip"), FileMode.Open, FileAccess.Read, FileShare.Delete);
                    fileStreamResult = new FileStreamResult(fileStreamInput, "APPLICATION/octet-stream");
                    fileStreamResult.FileDownloadName = "files.zip";
                    if (System.IO.File.Exists(Path.Combine(Path.GetTempPath(), "files.zip")))
                        System.IO.File.Delete(Path.Combine(Path.GetTempPath(), "files.zip"));
                }
            }
            return fileStreamResult;
        }

        // Download files within the folder
        private void DownloadFolderFiles(string Id, string Name, ZipArchive archive, ZipArchiveEntry zipEntry)
        {
            DriveService service = GetService();
            DirectoryInfo info = new DirectoryInfo(Path.GetTempPath() + Name);
            ChildrenResource.ListRequest request = service.Children.List(Id);
            ChildList children = request.Execute();
            List<Google.Apis.Drive.v2.Data.ChildReference> childFileList = children.Items.ToList();
            foreach (ChildReference child in childFileList)
            {
                if (service.Files.Get(child.Id).Execute().MimeType == "application/vnd.google-apps.folder")
                {
                    info.CreateSubdirectory(service.Files.Get(child.Id).Execute().Title);
                    zipEntry = archive.CreateEntry(Name + "\\" + service.Files.Get(child.Id).Execute().Title + "/");
                    DownloadFolderFiles(child.Id, Name + "\\" + service.Files.Get(child.Id).Execute().Title, archive, zipEntry);
                }
                else
                {
                    Stream file;
                    File fileProperties = service.Files.Get(child.Id).Execute();
                    if (System.IO.File.Exists(Path.Combine(Path.GetTempPath() + Name, fileProperties.Title)))
                    {
                        System.IO.File.Delete(Path.Combine(Path.GetTempPath() + Name, fileProperties.Title));
                    }
                    byte[] subFileContent = service.HttpClient.GetByteArrayAsync(fileProperties.DownloadUrl).Result;
                    using (file = System.IO.File.OpenWrite(Path.Combine(Path.GetTempPath() + Name, fileProperties.Title)))
                    {
                        file.Write(subFileContent, 0, subFileContent.Length);
                        file.Close();
                        zipEntry = archive.CreateEntryFromFile(Path.Combine(Path.GetTempPath() + Name, fileProperties.Title), Name + "\\" + fileProperties.Title, CompressionLevel.Fastest);
                    }
                }
            }
            if (System.IO.File.Exists(Path.Combine(Path.GetTempPath(), Name)))
                System.IO.File.Delete(Path.Combine(Path.GetTempPath(), Name));
        }
        // Writes the content of the file
        private static void SaveStream(MemoryStream stream, string FilePath)
        {
            using (System.IO.FileStream file = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite)) { stream.WriteTo(file); }
        }
        // Uploads the file(s)
        public virtual FileManagerResponse Upload(string path, IList<IFormFile> uploadFiles, string action, params FileManagerDirectoryContent[] data)
        {
            DriveService service = GetService();
            FileManagerResponse uploadResponse = new FileManagerResponse();
            FileManagerDirectoryContent CreateData = new FileManagerDirectoryContent();
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Fields = "nextPageToken, files(*)";
            List<Google.Apis.Drive.v2.Data.File> result = new List<Google.Apis.Drive.v2.Data.File>();
            FilesResource.ListRequest req = service.Files.List();
            IList<Google.Apis.Drive.v2.Data.File> files = req.Execute().Items;
            FileManagerResponse readResponse = new FileManagerResponse();
            FilesResource.InsertMediaUpload request;
            int fileIndex = 0;
            List<string> existFiles = new List<string>();
            FilesResource.ListRequest requ = service.Files.List();
            FileList list = requ.Execute();
            List<string> fileList = list.Items.Select(x => x.OriginalFilename).ToList();
            List<string> id = list.Items.Select(x => x.Id).ToList();
            foreach (IFormFile uploadFile in uploadFiles)
            {
                string filename = Path.GetFileName(uploadFile.FileName);
                if (action == "save")
                {
                    foreach (string obtainedFileName in fileList)
                    {
                        if (filename == obtainedFileName)
                        {
                            existFiles.Add(filename);
                            fileIndex = fileList.IndexOf(obtainedFileName);
                        }
                    }
                    if (existFiles.Count == 0)
                    {
                        using (FileStream fsSource = new FileStream(Path.Combine(Path.GetTempPath(), uploadFile.FileName), FileMode.Create))
                        {
                            uploadFiles[0].CopyTo(fsSource);
                        }
                        var fileMetadata = new File()
                        {
                            Title = uploadFile.FileName,
                            Parents = new List<ParentReference> { new ParentReference() { Id = data[0].Id } }
                        };
                        using (FileStream stream = new System.IO.FileStream(Path.GetTempPath() + uploadFile.FileName, System.IO.FileMode.Open))
                        {
                            request = service.Files.Insert(fileMetadata, stream, "application/");
                            request.Fields = "id";
                            request.Upload();
                        }
                    }

                }
                else if (action == "keepboth")
                {
                    string name = uploadFile.FileName;
                    string fullName = Path.Combine(Path.GetTempPath(), name);
                    string newName = fullName;
                    string newFileName = uploadFile.FileName;
                    int index = fullName.LastIndexOf(".");
                    int indexValue = newFileName.LastIndexOf(".");
                    if (index >= 0)
                    {
                        newName = fullName.Substring(0, index);
                        newFileName = newFileName.Substring(0, indexValue);
                    }
                    int fileCount = 0;
                    while (System.IO.File.Exists(newName + (fileCount > 0 ? "(" + fileCount.ToString() + ")" + Path.GetExtension(name) : Path.GetExtension(name))))
                    {
                        fileCount++;
                    }
                    newName = newFileName + (fileCount > 0 ? "(" + fileCount.ToString() + ")" : "") + Path.GetExtension(name);
                    using (FileStream fsSource = new FileStream(Path.Combine(Path.GetTempPath(), newName), FileMode.Create))
                    {
                        uploadFile.CopyTo(fsSource);
                    }
                    var fileMetadata = new File()
                    {
                        Title = newName,
                        Parents = new List<ParentReference> { new ParentReference() { Id = data[0].Id } }
                    };
                    using (FileStream stream = new System.IO.FileStream(Path.GetTempPath() + newName, System.IO.FileMode.Open))
                    {
                        request = service.Files.Insert(fileMetadata, stream, "application/");
                        request.Fields = "id";
                        request.Upload();
                    }
                }
                else if (action == "replace")
                {
                    foreach (string fileId in id)
                    {
                        if (fileId == files[fileIndex].Id)
                        {
                            service.Files.Delete(fileId).Execute();
                        }
                    }
                    using (FileStream fsSource = new FileStream(Path.Combine(Path.GetTempPath(), uploadFile.FileName), FileMode.Create))
                    {
                        uploadFile.CopyTo(fsSource);
                    }
                    var fileMetadata = new File()
                    {
                        Title = uploadFile.FileName,
                        Parents = new List<ParentReference> { new ParentReference() { Id = data[0].Id } }
                    };
                    using (FileStream stream = new System.IO.FileStream(Path.GetTempPath() + uploadFile.FileName, System.IO.FileMode.Open))
                    {
                        request = service.Files.Insert(fileMetadata, stream, "application/");
                        request.Fields = "id";
                        request.Upload();
                    }
                }
            }
            if (existFiles.Count != 0)
            {
                ErrorDetails er = new ErrorDetails();
                er.Code = "400";
                er.Message = "File already exists.";
                uploadResponse.Error = er;
            }
            return uploadResponse;
        }

        // Renames a file or folder
        public FileManagerResponse Rename(string path, string name, string newName, bool replace, FileManagerDirectoryContent[] data)
        {
            FileManagerResponse renameResponse = new FileManagerResponse();
            DriveService service = GetService();
            try
            {
                File file = new File();
                file.Title = newName;
                // Rename the file.
                FilesResource.PatchRequest request = service.Files.Patch(file, data[0].Id);
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
                return null;
            }
            renameResponse.Files = new[] { getFileDetails(data[0].Id) };
            return renameResponse;
        }

        // Calculates the folder size value
        private void getFolderSize(string folderdId)
        {
            DriveService service = GetService();
            ChildrenResource.ListRequest request = service.Children.List(folderdId);
            ChildList children = request.Execute();
            List<Google.Apis.Drive.v2.Data.ChildReference> childFileList = children.Items.ToList();
            foreach (var child in childFileList)
            {
                if (service.Files.Get(children.Items[0].Id).Execute().MimeType == "application/vnd.google-apps.folder")
                    getFolderSize(child.Id);
                else
                    sizeValue = sizeValue + long.Parse(service.Files.Get(children.Items[0].Id).Execute().FileSize.ToString());
            }

        }
        // Gets the details of the file(s) or folder(s)
        public FileManagerResponse Details(string path, string[] names, params FileManagerDirectoryContent[] data)
        {
            DriveService service = GetService();
            FileManagerResponse detailsResponse = new FileManagerResponse();
            FileDetails fileDetails = new FileDetails();
            long size = 0;
            if (data.Length == 1)
            {
                File fileData = service.Files.Get(data[0].Id).Execute();
                fileDetails.Name = fileData.Title;
                fileDetails.IsFile = fileData.MimeType == "application/vnd.google-apps.folder" ? false : true;
                if (fileDetails.IsFile)
                {
                    this.path = new List<string>();
                    fileDetails.Size = fileData.FileSize.ToString() == "" ? "0" : byteConversion(long.Parse(fileData.FileSize.ToString()));
                    File parent = service.Files.Get(fileData.Parents[0].Id).Execute();
                    if (fileData.Parents.Count > 0 && (bool)fileData.Parents[0].IsRoot)
                    {
                        fileDetails.Location = getFilterPath(parent, false) + @"\" + data[0].Name;
                    }
                    else
                    {
                        fileDetails.Location = getFilterPath(parent, false) + @"\" + (parent.Title + @"\") + data[0].Name;
                    }
                }
                else
                {
                    this.path = new List<string>();
                    fileDetails.Location = getFilterPath(fileData, false) + (fileData.Parents.Count > 0 ? @"\" + data[0].Name : "");
                    this.getFolderSize(fileData.Id);
                    fileDetails.Size = byteConversion(sizeValue);
                    sizeValue = 0;
                }
                fileDetails.Created = Convert.ToDateTime(fileData.ModifiedDate);
                fileDetails.Modified = Convert.ToDateTime(fileData.ModifiedDate);
            }
            else
            {
                string[] itemsName = new string[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    File fileData = service.Files.Get(data[i].Id).Execute();
                    if (fileData.MimeType != "application/vnd.google-apps.folder")
                        size = long.Parse((size + fileData.FileSize).ToString());
                    else
                    {
                        this.getFolderSize(data[i].Id);
                        size = long.Parse((size + sizeValue).ToString());
                        sizeValue = 0;
                    }
                    itemsName[i] = fileData.Title;
                    fileDetails.Name = string.Join(", ", itemsName);
                    fileDetails.MultipleFiles = true;
                    fileDetails.Size = size.ToString() == "0" ? "0" : byteConversion(long.Parse(size.ToString()));
                    if (i == data.Length - 1)
                    {
                        this.path = new List<string>();
                        fileDetails.Location = fileData.MimeType == "application/vnd.google-apps.folder" ? getFilterPath(service.Files.Get(fileData.Id).Execute(), false) + @"\" : getFilterPath(service.Files.Get(fileData.Parents[0].Id).Execute(), false) + @"\";
                    }
                    detailsResponse.Details = fileDetails;
                }
            }
            size = 0;
            detailsResponse.Details = fileDetails;
            return detailsResponse;
        }

        private string getFilterPath(Google.Apis.Drive.v2.Data.File dir, bool isSearhPath)
        {
            DriveService service = GetService();
            if (isSearhPath && dir.Parents.Count > 0 && !(bool)dir.Parents[0].IsRoot)
            {
                path.Add(service.Files.Get(dir.Parents[0].Id).Execute().Title);
                getFilterPath(service.Files.Get(dir.Parents[0].Id).Execute(), isSearhPath);
            }
            else
            {
                if (!isSearhPath)
                {
                    if (dir.Parents.Count > 0)
                    {
                        path.Add(service.Files.Get(dir.Parents[0].Id).Execute().Title);
                        if (!(bool)dir.Parents[0].IsRoot) getFilterPath(service.Files.Get(dir.Parents[0].Id).Execute(), isSearhPath);
                    }
                    else path.Add(dir.Title);
                }
            }
            return ((isSearhPath ? @"\" : "") + string.Join(@"\", path.ToArray().Reverse()));
        }

        private string getFilterId(Google.Apis.Drive.v2.Data.File dir)
        {
            DriveService service = GetService();
            if (dir.Parents.Count > 0)
            {
                idValues.Add(service.Files.Get(dir.Parents[0].Id).Execute().Id);
                getFilterId(service.Files.Get(dir.Parents[0].Id).Execute());
            }
            return (string.Join(@"\", idValues.ToArray().Reverse()) + @"\");
        }

        private string obtainFilterId(Google.Apis.Drive.v2.Data.File file)
        {
            DriveService service = GetService();
            string value = file.MimeType == "application/vnd.google-apps.folder" ? getFilterId(file) : (getFilterId(service.Files.Get(file.Parents[0].Id).Execute()) + service.Files.Get(file.Parents[0].Id).Execute().Id + @"\");
            idValues = new List<string>();
            return value;
        }

        private string obtainFilterPath(Google.Apis.Drive.v2.Data.File details, bool isSearch)
        {
            DriveService service = GetService();
            return details.MimeType == "application/vnd.google-apps.folder" ? getFilterPath(details, isSearch) : (getFilterPath(service.Files.Get(details.Parents[0].Id).Execute(), isSearch) + @"\" + service.Files.Get(details.Parents[0].Id).Execute().Title);
        }

        // Reads the file(s) and folder(s)
        public FileManagerResponse GetFiles(string path, bool showHiddenItems, params FileManagerDirectoryContent[] data)
        {
            // Check if the path is the root directory ("/"). If so, set id to null, otherwise, get the id from the provided data
            string id = (path == "/") ? null : data[0].Id;
            // Define the fields to retrieve for each file
            string fields = "items(parents,id,title,fileSize,mimeType,createdDate,modifiedDate,fileExtension)";
            // Set the maximum number of results to retrieve per request
            int result = 5000;
            // Create Drive API service.
            DriveService service = GetService();
            // Initialize a list to store Google Drive files and create a response object
            IList<Google.Apis.Drive.v2.Data.File> files = new List<Google.Apis.Drive.v2.Data.File>();
            FileManagerResponse readResponse = new FileManagerResponse();
            // If the path is the root directory ("/"), retrieve the list of files at the root level.
            if (path == "/")
            {
                FilesResource.ListRequest req = service.Files.List();
                req.Fields = fields;
                req.MaxResults = result;
                files = req.Execute().Items;
            }
            if (id != null || (files != null && files.Count > 0))
            {
                // Create a FileManagerDirectoryContent object to represent the current working directory (CWD)
                FileManagerDirectoryContent cwd = new FileManagerDirectoryContent();
                Google.Apis.Drive.v2.Data.File directory = (id == null) ? service.Files.Get(files.Where(a => a.Parents.Any(c => (bool)c.IsRoot == true)).ToList()[0].Parents[0].Id).Execute() :
                    service.Files.Get(id).Execute();
                // Populate the CWD object with information about the directory
                cwd.Name = directory.Title;
                cwd.Size = directory.FileSize != null ? long.Parse(directory.FileSize.ToString()) : 0;
                cwd.IsFile = directory.MimeType == "application/vnd.google-apps.folder" ? false : true;
                cwd.DateModified = Convert.ToDateTime(directory.ModifiedDate);
                cwd.DateCreated = Convert.ToDateTime(directory.CreatedDate);
                cwd.Id = directory.Id;
                cwd.HasChild = true;
                cwd.Type = "Folder";
                cwd.FilterId = directory.Parents.Count == 0 ? "" : directory.Parents[0].Id + @"\";
                this.path = new List<string>();
                cwd.FilterPath = directory.Parents.Count == 0 ? "" : data[0].FilterPath;
                if (id == null)
                {
                    FilesResource.ListRequest req = service.Files.List();
                    req.Q = "'root' in parents";
                    req.Fields = fields;
                    req.MaxResults = result;
                    FileList rootFiles = req.Execute();
                    List<FileManagerDirectoryContent> rootFileList = new List<FileManagerDirectoryContent>();
                    // Iterate through the root files and create FileManagerDirectoryContent objects for each file.
                    foreach (File details in rootFiles.Items)
                    {
                        bool isFile = details.MimeType == "application/vnd.google-apps.folder" ? false : true;
                        FileManagerDirectoryContent content = new FileManagerDirectoryContent()
                        {
                            Id = details.Id,
                            Name = details.Title,
                            Size = details.FileSize != null ? long.Parse(details.FileSize.ToString()) : 0,
                            DateCreated = Convert.ToDateTime(details.CreatedDate),
                            DateModified = Convert.ToDateTime(details.ModifiedDate),
                            Type = details.FileExtension == null ? "folder" : details.FileExtension,
                            FilterPath = @"\",
                            FilterId = cwd.Id + @"\",
                            IsFile = isFile,
                            HasChild = !isFile
                        };
                        rootFileList.Add(content);
                        readResponse.Files = rootFileList;
                    }
                }
                else
                {
                    // Retrieve child files (files within the current directory)
                    FilesResource.ListRequest filesRequest = service.Files.List();
                    filesRequest.Q = string.Format("'{0}' in parents", id);
                    filesRequest.Fields = fields;
                    filesRequest.MaxResults = result;
                    FileList childFiles = filesRequest.Execute();
                    List<FileManagerDirectoryContent> childFileList = new List<FileManagerDirectoryContent>();
                    // Iterate through the child files and create FileManagerDirectoryContent objects for each file
                    foreach (File details in childFiles.Items)
                    {
                        bool isFile = details.MimeType == "application/vnd.google-apps.folder" ? false : true;
                        FileManagerDirectoryContent content = new FileManagerDirectoryContent()
                        {
                            Id = details.Id,
                            Name = details.Title,
                            Size = details.FileSize != null ? long.Parse(details.FileSize.ToString()) : 0,
                            DateCreated = Convert.ToDateTime(details.CreatedDate),
                            DateModified = Convert.ToDateTime(details.ModifiedDate),
                            Type = details.FileExtension,
                            FilterPath = data.Length != 0 ? cwd.FilterPath + cwd.Name + @"\" + details.Title + @"\" : @"\",
                            FilterId = cwd.FilterId + cwd.Id + @"\" + details.Id + @"\",
                            IsFile = isFile,
                            HasChild = !isFile
                        };
                        childFileList.Add(content);
                    }
                    readResponse.Files = childFileList;
                }
                // Set the CWD in the response and return it.
                readResponse.CWD = cwd;
                return readResponse;
            }
            // If no data is found, return an empty response.
            return readResponse;
        }

        private void copyFolderItems(FileManagerDirectoryContent item, string targetID)
        {
            DriveService service = GetService();
            ChildrenResource.ListRequest request = service.Children.List(item.Id);
            ChildList children = request.Execute();
            foreach (ChildReference child in children.Items)
            {
                var childDetails = service.Files.Get(child.Id).Execute();
                if (childDetails.MimeType == "application/vnd.google-apps.folder")
                {
                    File file = new File()
                    {
                        Title = childDetails.Title,
                        Parents = new List<ParentReference> { new ParentReference() { Id = targetID } },
                        MimeType = "application/vnd.google-apps.folder"
                    };
                    request.Fields = "id";
                    File SubFolder = (service.Files.Insert(file)).Execute();
                    FileManagerDirectoryContent FileDetail = new FileManagerDirectoryContent();
                    FileDetail.Name = childDetails.Title;
                    FileDetail.Id = childDetails.Id;
                    FileDetail.IsFile = false;
                    FileDetail.Type = "folder";
                    FileDetail.FilterId = obtainFilterId(childDetails);
                    FileDetail.HasChild = getChildrenById(childDetails.Id);
                    FileDetail.DateCreated = Convert.ToDateTime(childDetails.ModifiedDate);
                    FileDetail.DateModified = Convert.ToDateTime(childDetails.ModifiedDate);
                    copyFolderItems(FileDetail, SubFolder.Id);
                }
                else
                {
                    File SubFile = new File()
                    {
                        Title = childDetails.Title,
                        Parents = new List<ParentReference> { new ParentReference() { Id = targetID } }
                    };
                    FilesResource.CopyRequest subFilerequest = service.Files.Copy(SubFile, child.Id);
                    subFilerequest.Execute();
                }
            }
        }

        public FileManagerResponse Copy(string path, string targetPath, string[] names, string[] replacedItemNames, FileManagerDirectoryContent TargetData, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse copyResponse = new FileManagerResponse();
            DriveService service = GetService();
            List<FileManagerDirectoryContent> copyFiles = new List<FileManagerDirectoryContent>();
            ChildrenResource.ListRequest childRequest = service.Children.List(data[0].Id);
            ChildList children = childRequest.Execute();
            List<string> childFileList = children.Items.Select(x => x.Id).ToList();
            if (childFileList.IndexOf(TargetData.Id) != -1)
            {
                ErrorDetails er = new ErrorDetails();
                er.Code = "400";
                er.Message = "The destination folder is the subfolder of the source folder.";
                copyResponse.Error = er;
                return copyResponse;
            }
            foreach (FileManagerDirectoryContent item in data)
            {
                File copyFile;
                try
                {
                    File file = new File()
                    {
                        Title = item.Name,
                        Parents = new List<ParentReference> { new ParentReference() { Id = TargetData.Id } }
                    };
                    if (item.IsFile)
                    {
                        // Copy the file
                        FilesResource.CopyRequest request = service.Files.Copy(file, item.Id);
                        copyFile = request.Execute();
                    }
                    else
                    {
                        file.MimeType = "application/vnd.google-apps.folder";
                        FilesResource.InsertRequest request = service.Files.Insert(file);
                        request.Fields = "id";
                        copyFile = request.Execute();
                        copyFolderItems(item, copyFile.Id);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return null;
                }
                File filedetails = service.Files.Get(copyFile.Id).Execute();
                FileManagerDirectoryContent FileDetail = new FileManagerDirectoryContent();
                FileDetail.Name = filedetails.Title;
                FileDetail.Id = filedetails.Id;
                FileDetail.IsFile = filedetails.MimeType == "application/vnd.google-apps.folder" ? false : true;
                FileDetail.Type = filedetails.FileExtension == null ? "folder" : filedetails.FileExtension;
                FileDetail.HasChild = getChildrenById(filedetails.Id);
                FileDetail.FilterId = obtainFilterId(filedetails);
                FileDetail.Size = item.Size;
                FileDetail.FilterPath = targetPath.Replace("/", @"\");
                FileDetail.DateCreated = Convert.ToDateTime(filedetails.ModifiedDate);
                FileDetail.DateModified = Convert.ToDateTime(filedetails.ModifiedDate);
                copyFiles.Add(FileDetail);
            }
            copyResponse.Files = copyFiles;
            return copyResponse;
        }

        public FileManagerResponse Move(string path, string targetPath, string[] names, string[] replacedItemNames, FileManagerDirectoryContent TargetData, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse moveResponse = new FileManagerResponse();
            DriveService service = GetService();
            List<FileManagerDirectoryContent> moveFiles = new List<FileManagerDirectoryContent>();
            ChildrenResource.ListRequest request = service.Children.List(data[0].Id);
            ChildList children = request.Execute();
            List<string> childFileList = children.Items.Select(x => x.Id).ToList();
            if (childFileList.IndexOf(TargetData.Id) != -1)
            {
                ErrorDetails er = new ErrorDetails();
                er.Code = "400";
                er.Message = "The destination folder is the subfolder of the source folder.";
                moveResponse.Error = er;
                return moveResponse;
            }
            foreach (FileManagerDirectoryContent item in data)
            {
                FilesResource.GetRequest fileRequest = service.Files.Get(item.Id);
                fileRequest.Fields = "parents";
                var file = fileRequest.Execute();
                string previousParents = String.Join(",", file.Parents.Select(t => t.Id));
                FilesResource.UpdateRequest moveRequest = service.Files.Update(new File(), item.Id);
                moveRequest.Fields = "id, parents";
                moveRequest.AddParents = TargetData.Id;
                moveRequest.RemoveParents = previousParents;
                file = moveRequest.Execute();
                File filedetails = service.Files.Get(file.Id).Execute();
                FileManagerDirectoryContent FileDetail = new FileManagerDirectoryContent();
                FileDetail.Name = filedetails.Title;
                FileDetail.Id = filedetails.Id;
                FileDetail.IsFile = filedetails.MimeType == "application/vnd.google-apps.folder" ? false : true;
                FileDetail.Type = filedetails.FileExtension == null ? "folder" : filedetails.FileExtension;
                FileDetail.HasChild = getChildrenById(filedetails.Id);
                FileDetail.Size = item.Size;
                FileDetail.FilterPath = targetPath.Replace("/", @"\");
                FileDetail.FilterId = obtainFilterId(filedetails);
                FileDetail.DateCreated = Convert.ToDateTime(filedetails.ModifiedDate);
                FileDetail.DateModified = Convert.ToDateTime(filedetails.ModifiedDate);
                moveFiles.Add(FileDetail);
            }
            moveResponse.Files = moveFiles;
            return moveResponse;
        }

        // Returns the image
        public FileStreamResult GetImage(string path, string id, bool allowCompress, ImageSize size, params FileManagerDirectoryContent[] data)
        {
            DriveService service = GetService();
            File file = service.Files.Get(id).Execute();
            return new FileStreamResult(new MemoryStream((service.HttpClient.GetByteArrayAsync(file.DownloadUrl).Result)), "APPLICATION/octet-stream");
        }

        public string ToCamelCase(FileManagerResponse userData)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            return JsonSerializer.Serialize(userData, options);
        }

        // Converts the byte value to appropriate size value
        private string byteConversion(long fileSize)
        {
            try
            {
                string[] index = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
                if (fileSize == 0) return "0 " + index[0];
                int loc = Convert.ToInt32(Math.Floor(Math.Log(Math.Abs(fileSize), 1024)));
                return (Math.Sign(fileSize) * Math.Round(Math.Abs(fileSize) / Math.Pow(1024, loc), 1)).ToString() + " " + index[loc];
            }
            catch (Exception e) { throw e; }
        }
    }
}
