﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using Ionic.Zip;
using System.Text.RegularExpressions;
namespace ZibomodInstaller
{
    class DriveAPI //API parser
    {
        public Dictionary<string,dynamic> GetDriveFolderList(string DriveFolderID)
        {
            Dictionary<string, dynamic> folderContentData = null; //Define data storage for parsed JSON content
            JavaScriptSerializer jsonParser = new JavaScriptSerializer(); 
            using (WebClient DriveClient = new WebClient()) //WebClient can be scrapped after the file has been downloaded into memory.
            {
                string jsonData = DriveClient.DownloadString("https://www.googleapis.com/drive/v2beta/files?openDrive=true&reason=102&syncType=0&errorRecovery=false&q=trashed = false and '" + DriveFolderID + "' in parents&fields=kind,nextPageToken,items(kind,title,mimeType,createdDate,modifiedDate,modifiedByMeDate,lastViewedByMeDate,fileSize,owners(kind,permissionId,displayName,picture),lastModifyingUser(kind,permissionId,displayName,picture),hasThumbnail,thumbnailVersion,iconLink,id,shared,sharedWithMeDate,userPermission(role),explicitlyTrashed,quotaBytesUsed,shareable,copyable,fileExtension,sharingUser(kind,permissionId,displayName,picture),spaces,editable,version,teamDriveId,hasAugmentedPermissions,trashingUser(kind,permissionId,displayName,picture),trashedDate,parents(id),labels(starred,hidden,trashed,restricted,viewed),capabilities(canCopy,canDownload,canEdit,canAddChildren,canDelete,canRemoveChildren,canShare,canTrash,canRename,canReadTeamDrive,canMoveTeamDriveItem)),incompleteSearch&appDataFilter=NO_APP_DATA&spaces=drive&maxResults=50&orderBy=folder,title_natural asc&key=AIzaSyCTF8x5DeVXllRTPMrtIwnY5DaBjbjKts8"); //Key at the end
                folderContentData = jsonParser.Deserialize<Dictionary<string, dynamic>>(jsonData); //Convert downloaded JSON data to data which is readable by C#
            }
            return folderContentData;
        }
        public void DownloadFile(string ID, string DownloadLocation)
        {
            using (WebClient DriveClient = new WebClient())
            {
                DriveClient.DownloadFile("https://www.googleapis.com/drive/v3/files/" + ID + "?alt=media&key=AIzaSyCTF8x5DeVXllRTPMrtIwnY5DaBjbjKts8", DownloadLocation);
            }
        }
    }
    class InstallActions
    {
        public static void ZiboPrepareDir(string xplaneDir)
        {
            if(!Directory.Exists(xplaneDir + @"Aircraft\B737-800X")) 
            {
                DirectoryCopy(xplaneDir + @"Aircraft\Laminar Research\Boeing B737-800", xplaneDir + @"Aircraft\B737-800X", true);
            }
        }
        public static string FindLatestFile(string FolderID)
        {
            string DownloadID = "";
            DriveAPI ZiboDrive = new DriveAPI(); //Import the API parser
            Dictionary<string,dynamic> folderContentData = ZiboDrive.GetDriveFolderList(FolderID); //Get list of items in folder
            List<string> folderItemName = new List<string>(); //Define lists for item properties
            List<double> folderItemAddedDate = new List<double>();
            List<string> folderItemDriveID = new List<string>();
            List<int> potentialFiles = new List<int>(); //Define list of zip candidates for installation
            List<double> potentialFilesAddedDate = new List<double>(); // Define list of zip candidates with date to find newest of the few.

            for (int i = 0; i < folderContentData["items"].Count; i++) //Add a global index of files and directories in drive folder, so that we can search in it.
            {
                folderItemName.Add(folderContentData["items"][i]["title"]); //Add title to list
                folderItemAddedDate.Add(Convert.ToDouble(Convert.ToDateTime(folderContentData["items"][i]["createdDate"]).Ticks)); //Add the file added date to list
                folderItemDriveID.Add(folderContentData["items"][i]["id"]);

                if (folderItemName[i].Contains(".zip"))
                {
                     potentialFiles.Add(i); //Add to list of zip files as ID, so that we don't have to look up later when we're downloading it
                }
            }
            for (int i = 0; i < potentialFiles.Count; i++)
            {
                potentialFilesAddedDate.Add(folderItemAddedDate[potentialFiles[i]]); //Add potentialfiles' last modified date so that they get the same ID.
            }
            for (int i = 0; i < potentialFiles.Count; i++)
            {
                int NewestFile = potentialFiles[potentialFilesAddedDate.IndexOf(potentialFilesAddedDate.Max())]; //Find which file ID is the newest file.
                if (potentialFiles[i] == NewestFile) //If the current file is the newest one, then do:
                {
                    int selectedDownload;
                    selectedDownload = potentialFiles[i];
                    DownloadID = folderItemDriveID[selectedDownload]; //Select DriveID for downloading the file
                }
            }
            return DownloadID;
        }
        //ZiboMod
        public static void ZiboDownload(string DownloadID)
        {
            DriveAPI ZiboDrive = new DriveAPI();
            ZiboDrive.DownloadFile(DownloadID, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BoeingDL.zip"); //Downloads file to %Appdata%
        }
        public static void ZiboExtract(string xplaneDir)
        {
            using (ZipFile BoeingDL = ZipFile.Read(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BoeingDL.zip"))
            {
                BoeingDL.ExtractAll(xplaneDir + @"Aircraft\B737-800X", ExtractExistingFileAction.OverwriteSilently);
            }
        }
        //AudioBird
        public static void AudioDownload(string DownloadID)
        {
            DriveAPI AudioDrive = new DriveAPI();
            AudioDrive.DownloadFile(DownloadID, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\AXP-Immersion.zip"); //Downloads file to %Appdata%
        }
        public static void AudioExtract(string xplaneDir)
        {
            using (ZipFile AudioDL = ZipFile.Read(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\AXP-Immersion.zip"))
            {
                AudioDL.ExtractAll(xplaneDir + @"Aircraft\B737-800X", ExtractExistingFileAction.OverwriteSilently);
            }
        }
        //public static void AudioInstall(string xplaneDir)
        //{
        //    string[] dirs = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AXP IMM*");
        //    DirectoryCopy(dirs[0]+"\\fmod",xplaneDir + @"Aircraft\B737-800X\fmod",true);
        //}
        //RGMod
        public static string FindLatestRG()
        {
            using (WebClient VK = new WebClient())
            {
                string DownloadID = "";
                VK.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1");
                string VKGroup = VK.DownloadString(@"https://vk.com/xplane11rgmod");
                MatchCollection posts = Regex.Matches(VKGroup, "div id=\"post-.*?_(.*?)\" class=\"_post[\\s\\S\\n]*?title=\"https:\\/\\/drive\\.google\\.com\\/file\\/d\\/(.*?)\\/view");
                DownloadID = posts[1].Groups[2].Value;
                return DownloadID;
            }
        }
        public static void RGDownload(string ID)
        {
            DriveAPI RGDrive = new DriveAPI();
            RGDrive.DownloadFile(ID, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RG-Mod.zip");
        }
        public static void RGExtract(bool isTextureOnly, string xPlanePath)
        {
            if (!isTextureOnly)
            {
                using (ZipFile RGMod = ZipFile.Read(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RG-Mod.zip"))
                {
                    RGMod.ExtractAll(xPlanePath + "\\Aircraft\\B737-800X", ExtractExistingFileAction.OverwriteSilently);
                }
            } else
            {
                using (ZipFile RGMod = ZipFile.Read(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RG-Mod.zip"))
                {
                    RGMod.ExtractSelectedEntries("name = *", "objects", xPlanePath+"\\Aircraft\\B737-800X", ExtractExistingFileAction.OverwriteSilently);
                }
            }

        }
        public static void CleanUp()
        {
            MainForm mainForm = new MainForm();
            if(File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BoeingDL.zip"))
            {
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BoeingDL.zip");
            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\AXP-Immersion.zip"))
            {
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\AXP-Immersion.zip");
                string[] dirs = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AXP IMM*");
                Directory.Delete(dirs[0], true);
            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RG-Mod.zip"))
            {
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RG-Mod.zip");
            }
        }
        //
        //
        //
        //DirectoryCopy
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException();
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
