
using Syncfusion.EJ2.FileManager.Base;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using GoogleDriveOAuth2;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using EJ2FileManagerService.Models;

namespace EJ2FileManagerServices.Controllers
{

    [Route("api/[controller]")]
    [EnableCors("AllowAllOrigins")]
    public class GoogleDriveProviderController : Controller
    {
        public GoogleDriveFileProvider googleDrive = new GoogleDriveFileProvider();

        [Route("GoogleDriveFileOperations")]
        public object GoogleDriveFileOperations([FromBody] FileManagerDirectoryContent args)
        {

            if (args.Action == "delete" || args.Action == "rename")
            {
                if ((args.TargetPath == null) && (args.Path == ""))
                {
                    FileManagerResponse response = new FileManagerResponse();
                    response.Error = new ErrorDetails { Code = "403", Message = "Restricted to modify the root folder." };
                    return googleDrive.ToCamelCase(response);
                }
            }
            switch (args.Action)
            {
                case "read":
                    // reads the file(s) or folder(s) from the given path.
                    return googleDrive.ToCamelCase(googleDrive.GetFiles(args.Path, false, args.Data));
                case "delete":
                    // deletes the selected file(s) or folder(s) from the given path.
                    return googleDrive.ToCamelCase(googleDrive.Delete(args.Path, args.Names, args.Data));
                case "details":
                    // gets the details of the selected file(s) or folder(s).
                    return googleDrive.ToCamelCase(googleDrive.Details(args.Path, args.Names, args.Data));
                case "create":
                    // creates a new folder in a given path.
                    return googleDrive.ToCamelCase(googleDrive.Create(args.Path, args.Name, args.Data));
                case "search":
                    // creates a new folder in a given path. // creates a new folder in a given path.
                    return googleDrive.ToCamelCase(googleDrive.Search(args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive, args.Data));
                case "rename":
                    // renames a file or folder.
                    return googleDrive.ToCamelCase(googleDrive.Rename(args.Path, args.Name, args.NewName, false, args.Data));
                case "copy":
                    // copies the selected file(s) or folder(s) from a path and then pastes them into a given target path.
                    return googleDrive.ToCamelCase(googleDrive.Copy(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData, args.Data));
                case "move":
                    // cuts the selected file(s) or folder(s) from a path and then pastes them into a given target path.
                    return googleDrive.ToCamelCase(googleDrive.Move(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData, args.Data));
            }
            return null;
        }

        // uploads the file(s) into a specified path
        [Route("GoogleDriveUpload")]
        public IActionResult GoogleDriveUpload(string path, IList<IFormFile> uploadFiles, string action, string data)
        {
            FileManagerResponse uploadResponse;
            FileManagerDirectoryContent FileData = JsonConvert.DeserializeObject<FileManagerDirectoryContent>(data);
            uploadResponse = googleDrive.Upload(path, uploadFiles, action, FileData);
            if (uploadResponse.Error != null)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.StatusCode = 204;
                Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = uploadResponse.Error.Message;
            }
            return Content("");
        }

        // downloads the selected file(s) and folder(s)
        [Route("GoogleDriveDownload")]
        public IActionResult GoogleDriveDownload(string downloadInput)
        {
            FileManagerDirectoryContent args = JsonConvert.DeserializeObject<FileManagerDirectoryContent>(downloadInput);
            args.Path = (args.Path);
            return googleDrive.Download(args.Path, args.Names, args.Data);
        }

        // gets the image(s) from the given path
        [Route("GoogleDriveGetImage")]
        public IActionResult GoogleDriveGetImage(FileManagerDirectoryContent args)
        {
            return googleDrive.GetImage(args.Path, args.Id, true, null, null);
        }
    }
}
