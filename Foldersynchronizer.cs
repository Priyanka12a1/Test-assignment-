using System;
using System.IO;
using System.Linq;
using System.Timers;
class FolderSynchronizer
{
   private static string sourceFolder;
   private static string replicaFolder;
   private static string logFilePath;
   private static int syncTimeInterval;
   static void Main(string[] args)
   {
      if(args.Length != 4)
      {
         Console.WriteLine($"Use FolderSynchronizer by giving correct parameter
          <sourceFolder> <replicaFolder> <syncTimeInterval> <logFilePath>");
         return;
      }
      sourceFolder = args[0];
      replicaFolder = args[1];
      syncTimeInterval = int.Parse( args[2]);
      logFilePath = args[3];
      if(!Directory.Exists(sourceFolder))
      {
           Console.WriteLine($"Source folder : {sourceFolder} does not exist ");
           return; //return if source directory doesn't exist
      }
      if(!Directory.Exists(replicaFolder))
      {
          Directory.CreateDirectory(replicaFolder);
          Log($" Replica folder is created : {replicaFolder}");
      }
      SynchronizeFolders();
      // Set up Timer for periodic synchronization
      Timer timer = new Timer(syncTimeInterval * 1000);
      timer.Elapsed += (sender, e) => SynchronizeFolders();
      time.Start();
      Console.WriteLine("Press [Enter] to Exit");
      Console.ReadLine();
   }
   private static void Log(string message)
   {
       var logMessage = $"{DateTime.Now} : {message}";
       Console.WriteLine(logMessage);
       File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
   }
   private static void SynchronizeFolders()
   {
      try
      {
           var sourceFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
           var replicaFiles = Directory.GetFiles(replicaFolder, "*", SearchOption.AllDirectories);
           foreach(var sourceFile in sourceFiles)
           {
                try
                 {
                     var relativePath = Path.GetRelativePath(sourceFolder, sourceFile);
                     var replicaFile = Path.Combine(replicaFolder, relativePath);
                     if(! File.Exists(replicaFile) || File.GetLastWriteTime(sourceFile) >
                        File.GetLastWriteTime(replicaFile))
                      {
                          Directory.CreateDirectory(Path.GetDirectoryName(replicaFile));
                          File.Copy(sourceFile, replicaFile, true);
                          Log($" Successfully copied /updated : {relativePath}");
                      }
                }
                catch(Exception ex)
                {
                      Log($"Error copying file : {sourceFile} : {ex.Message}");
                }
           }
           foreach(var replicaFile in replicaFiles)
           {
                try
                {
                    var relativePath = Path.GetRelativePath(replicaFolder, replicaFile);
                    var sourceFile = Path.Combine(sourceFolder, relativePath);
                    if(!File.Exists(sourceFile))
                    {
                         File.Delete(replicaFile);
                         Log($"Deleted from replica folder : {relativePath}");
                    }
                }
                catch(Exception e)
                {
                     Log($"Error deleting file {replicaFile} : {e.Message}");
                }
           }
           SynchronizeDirectories(sourceFolder, replicaFolder);
    catch(Exception e)
    {
        Log($"Error occured during synchronization : {e.Message}");
    }
}
private static void SynchronizeDirectories(string sourceDir, string replicaDir)
{
     var sourceSubDirs = Directory.GetDirectories(sourceDir);
     var replicaSubDirs = Directory.GetDirectories(replicaDir);
     foreach(var replicaSubDir in replicaSubDirs)
     {
        try
        {
            var relativePath = Path.GetRelativePath(replicaFolder, replicaSubDir);
            var sourceSubDir = Path.Combine(sourceFolder, relativePath);
            if(!Directory.Exists(sourceSubDir))
            {
                Directory.Delete(replicaSubDir, true);
                Log($"Deleted directory: {relativePath}");
            }
        }
        catch(Exception e)
        {
            Log($"Error occured during deleting directory : {replicaSubDir} : {e.Message}");
        }
     }
     foreach(var sourceSubDir in sourceSubDirs)
     {
        try
        {
            var relativePath = Path.GetRelativePath(sourceFolder, sourceSubDir);
            var replicaSubDir = Path.Combine(replicaFolder, relativePath);
            if(!Directory.Exists(replicaSubDir))
            {
                 Directory.CreateDirectory(replicaSubDir);
                 Log($"Directory created : {relativePath}");
            }
            SynchronizeDirectories(sourceSubDir, replicaSubDir);
       }
       catch(Exception e)
       {
            Log($"Error occured during snchronizing directories : {sourceSubDir} : {e.Message}");
       }
    }
  }
}


/*A general explanation-
Program ensures that replica folder is an exact and up-to-date copy of the source folder. It performs
initial synchronization and sets up periodic synchronization using timer.

Approach-
1. First program checks that correct number of arguments are provided. It expects source folder path,
replica folder path, time interval in which folder should be synchronized and a path where the log file will
be saved. It ensures the source folder exists and replica folder is created if doesnt exists.
2. SynchronizeFolders() - This method calls to perform an initial synchronization between source and
replica folders.It iterates over the files in the source directory , if a file is not present in the replica folder
or the file has been modified in the source , it copies the file to the replica folder and logs it.
It iterates over the files in the replica folder .If a file is not present in the source directory, it deletes the
file from the replica folder and logs it.
It calls SynchronizeDirectories() to ensure the folder structure matches and removes the empty folders in
the replica.
3.SynchronizeDirectories() - This method ensures that directories structure is synchronized.
It iterates through directories in replica folder. If the directory is not present in the source folder, it deletes
the folder from the replica folder.It iterates over the directories in the source folder, if the directory in the
replica folder is not present , it creates the directory and logs it.
It calls itself resursively to synchronize between subdirectories.
4.Log()- This simply logs messages to both console and to the specified log file path.
5.I didn't use Hash calculation(md5) for comparison because it makes the program complex(higher CPU
and memory usage) and it slows down the system whereas File.GetLastWriteTime() is efficient and
straightforward, checking the last write time of a file is a fast operation.

How to Run-
1.Copy the program in a code editor i.e. visual studio
2.Ensure all dependencies are installed and build the project
3.Take .exe from Release folder and copy it in a vm or any test machine
4.Call exe through cmdline and give parameter for source folder, replica folder, sync time 30 sec and
path where the log file needs to be.
5.Tested with small set of testdata , it looks fine
Test cases derived for the usecase -
1.Symbolic links, hardlinks, junction point - If source directory contains these paths , it will throw error in
case of exception
2.Network path - If source and replica directories contains network path it may lead to some latency
issues or connectivity issues. it will throw error in case of exception
3.If case sensitivity is enabled in directories , it might cause conflict. If case sensitivity is enabled in 
onefolder then in this case check if programs errors out.
4.Renamed or deleted source directory - During execution of test , if the directories are renamed or
deleted . it will lead to error in copying and deletion . It will throw error in case of such exception.
5.File locking- If files are in locked state (meaning opened for write or in use) then copying or deleting will
fail in this case. Program will throw error in case of this exception. Needed retry logic for locked file can
be enhanced further.
6.File access permission - To test if there is not enough permission to read or write file . Program will
handle such exceptions
7.Large data set - To test the response time when program runs against the large numbers of data/files
8.Empty source - If source directory is empty, check the handling of exception is correct
9.Interrupted synchronization - If the synchronization process is interrrupted , replica will be in
inconsistent state. State transition is not handled by the program.For example if file is not deleted 100%
and it leaves behind a .tmp file. It can be handled by renaming them and successful coping them later
can reduce the risk of incomplete copies.
10.Disk space issues- The replica might run out of disk space during snchronization. Need to see how
program behaves in this case if user get BSOD or unresponsive machine in such cases*/ 



