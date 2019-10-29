# ej2-file-manager-google-drive-core-service

This repository contains the ASP.NET Core Google drive file system providers for the Essential JS 2 File Manager component.

## Key Features

Google Drive file system provider serves the file system support for the FileManager component with the Google Drive. Uses the Google APIs to read the file and uses the OAuth 2.0 protocol for authentication and authorization.

The following actions can be performed with Google drive file system provider.

| **Actions** | **Description** |
| --- | --- |
| Read      | Read the files from Google drive API's. |
| Details   | Gets a file's metadata or content by ID which is Type, Size, Location and Modified date. |
| Download  | Download the selected file or folder from the Google drive. |
| Upload    | Upload's the file to the google drive. It accepts uploaded media with the following characteristics: <ul><li>Maximum file size:  30MB</li><li>Accepted Media MIME types: `*/*` </li></ul> |
| Create    | Create a New Folder. |
| Delete    | Permanently deletes a file owned by the user without moving it to the trash. |
| Copy      | Currently this support is not availabe. |
| Move      | Currently this support is not availabe. |
| Rename    | Rename a folder or file. |
| Search    | Search a file or folder in Google drive. |

## How to run this application?

To run this application, you need to first clone the `ej2-google-drive-aspcore-file-provider` repository and then navigate to its appropriate path where it has been located in your system.

To do so, open the command prompt and run the below commands one after the other.

```
git clone https://github.com/SyncfusionExamples/ej2-google-drive-aspnet-core-file-provider  ej2-google-drive-aspcore-file-provider

cd ej2-google-drive-aspcore-file-provider

```

## Updating client secret data.

* Generate the client_secret JSON from Google Authorization Server.

* Copy the JSON content to below specified JSON files
    
    * EJ2FileManagerService > credentials > client_secret.json
    * GoogleOAuth2.0Base > credentials > client_secret.json

## Running application

Once cloned, open solution file in visual studio.Then build the project and run it after restoring the nuget packages.

## File Manager AjaxSettings

To access the basic actions such as Read, Delete, Copy, Move, Rename, Search, and Get Details of File Manager using Azure service, just map the following code snippet in the Ajaxsettings property of File Manager.

Here, the `hostUrl` will be your locally hosted port number.

```
  var hostUrl = http://localhost:62870/;
  ajaxSettings: {
        url: hostUrl + 'api/GoogleDriveProvider/GoogleDriveFileOperations'
  }
```

## File download AjaxSettings

To perform download operation, initialize the `downloadUrl` property in ajaxSettings of the File Manager component.

```
  var hostUrl = http://localhost:62870/;
  ajaxSettings: {
        url: hostUrl + 'api/GoogleDriveProvider/GoogleDriveFileOperations',
        downloadUrl: hostUrl +'api/GoogleDriveProvider/GoogleDriveDownload'
  }
```

## File upload AjaxSettings

To perform upload operation, initialize the `uploadUrl` property in ajaxSettings of the File Manager component.

```
  var hostUrl = http://localhost:62870/;
  ajaxSettings: {
        url: hostUrl + 'api/GoogleDriveProvider/GoogleDriveFileOperations',
        uploadUrl: hostUrl +'api/GoogleDriveProvider/GoogleDriveUpload'
  }
```

## File image preview AjaxSettings

To perform image preview support in the File Manager component, initialize the `getImageUrl` property in ajaxSettings of the File Manager component.

```
  var hostUrl = http://localhost:62870/;
  ajaxSettings: {
        url: hostUrl + 'api/GoogleDriveProvider/GoogleDriveFileOperations',
         getImageUrl: hostUrl +'api/GoogleDriveProvider/GoogleDriveGetImage'
  }
```

The FileManager will be rendered as the following.

![File Manager](https://ej2.syncfusion.com/products/images/file-manager/readme.gif)

## Support

Product support is available for through following mediums.

* Creating incident in Syncfusion [Direct-trac](https://www.syncfusion.com/support/directtrac/incidents?utm_source=npm&utm_campaign=filemanager) support system or [Community forum](https://www.syncfusion.com/forums/essential-js2?utm_source=npm&utm_campaign=filemanager).
* New [GitHub issue](https://github.com/syncfusion/ej2-javascript-ui-controls/issues/new).
* Ask your query in [Stack Overflow](https://stackoverflow.com/?utm_source=npm&utm_campaign=filemanager) with tag `syncfusion` and `ej2`.

## License

Check the license detail [here](https://github.com/syncfusion/ej2-javascript-ui-controls/blob/master/license).

## Changelog

Check the changelog [here](https://github.com/syncfusion/ej2-javascript-ui-controls/blob/master/controls/filemanager/CHANGELOG.md)

© Copyright 2019 Syncfusion, Inc. All Rights Reserved. The Syncfusion Essential Studio license and copyright applies to this distribution.
