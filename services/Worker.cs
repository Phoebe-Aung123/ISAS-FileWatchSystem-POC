using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography;
using ChoETL;
using FileWatcherService.services;

namespace FileWatcherService;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private FileSystemWatcher watcher; 
    private readonly ISendToApi _sendToApi;
    private FileSystemWatcher processWatcher; 
    private IConfiguration _configuration;
    private readonly string _rootDirectory;
    private readonly string _sourceDirectory;
    private readonly string _errorDirectory; 
    private readonly string _processingDirectory;
    private readonly string _archiveDirectory; 
    private readonly int _dataProviderId; 


    // private readonly string rootDirectory = "/Users/phoebeaung/Documents/IASAS";
    // private readonly string sourceDirectory = "/Users/phoebeaung/Documents/IASAS/FileWatcherPOC/InputFolder";
    // private readonly string errorDirectory = "/Users/phoebeaung/Documents/IASAS/FileWatcherPOC/ErrorFolder";
    // private readonly string processingDirectory = "/Users/phoebeaung/Documents/IASAS/FileWatcherPOC/ProcessingFolder";
    // private readonly string archiveDirectory = "/Users/phoebeaung/Documents/IASAS/FileWatcherPOC/ArchiveFolder";
    

    public Worker(ILogger<Worker> logger, ISendToApi sentToApi, IConfiguration configuration)
    {
        _logger = logger;
        _sendToApi = sentToApi; 
        _configuration = configuration;

        var directorySettings = _configuration.GetSection("ClientSettings:DirectorySettings");
        _rootDirectory = directorySettings["RootDirectory"];
        _sourceDirectory = directorySettings["SourceDirectory"];
        _errorDirectory = directorySettings["ErrorDirectory"];
        _processingDirectory = directorySettings["ProcessingDirectory"];
        _archiveDirectory = directorySettings["ArchiveDirectory"];

     }

    public void onChanged(object source, FileSystemEventArgs e){

        if (e.Name.EndsWith(".DS_Store") || e.Name.EndsWith(".tmp")){
            return;
        }

        if (e.ChangeType == WatcherChangeTypes.Created){
            Console.WriteLine(e.Name + " " + e.ChangeType);

            if (source == watcher){

                string extension = getExtension(e.Name);

                if (isCsv(extension)){
                //rename the file according to the data provider
                    string renamedFile = renameFile(e, 2);
                    moveFolder(Path.Combine(getDirectory(e), renamedFile), _processingDirectory);
                }
                else {
                    Console.WriteLine(e.FullPath);
                    moveFolder(e.FullPath, _errorDirectory);
                }
        }
            else if (source == processWatcher){
                if(e.Name.EndsWith(".csv")){
                    try {
                        processWatcher.EnableRaisingEvents = false;
                        Task.Run(async () => {
                            convertToJson(e);
                            await Task.Delay(200);
                            moveFolder(e.FullPath, _archiveDirectory);}
                            );  
                       
                        processWatcher.EnableRaisingEvents = true;
                    }
                    catch(Exception ex){
                        Console.WriteLine(ex.Message);
                    }
                
            }
            }

        }
            
        // else if (e.ChangeType == WatcherChangeTypes.Changed){
        //     Console.WriteLine("There has been a change in the file " + e.Name);
        // }

        // else if (e.ChangeType == WatcherChangeTypes.Renamed){
        //     Console.WriteLine("The file has been renamed to " + e.Name);
        // }
        else if (e.ChangeType == WatcherChangeTypes.Deleted){
            Console.WriteLine(e.Name + " has been deleted from {0} ", getDirectory(e));
        }
        
    }
    //this method is called when the FileSystem detects an error
    public void onError(object source, ErrorEventArgs e){
        //indicates that an error has been detected
        Console.WriteLine("The FileSystemWatcher has detected an error.");
    }
 
    public string getExtension(string name){
        string extension = Path.GetExtension(name);
        return extension;
    }
    public string renameFile(FileSystemEventArgs e, int DataProviderID){
        try {

            //get the extension 
            string extension = getExtension(e.Name);
            //generate new name with suffices
            string newName = changeName(e.Name, DataProviderID);

            //get the directory of the file location 
            string directory = getDirectory(e);

            //get the full path of the file
            string oldPath = e.FullPath;

            //get the new path with the newName
            string newPath = Path.Combine(_sourceDirectory, newName);

            //rename the file with the newName
            File.Move(oldPath, newPath);

            return newPath;    
        }
        catch (Exception ex){
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public string getDirectory(FileSystemEventArgs e){
        string directory = Path.GetDirectoryName(e.FullPath);
        return directory;
    }


    public string changeName(string fileName, int DataProviderID){
        try {
            //get the time created
            DateTime dateCreated = DateTime.Now;
            DateTime dateOnly = dateCreated.Date;

            //convert date to string
            string dateToString = dateOnly.ToString("MMddyyyy");

            string suffix = "";

            //get the suffix of data provider
            if (DataProviderID == 1) {
                suffix = "_M_";
            }
            else if (DataProviderID == 2){
            suffix = "_Z_";
            }
            else {
                suffix = "___";
            }

            string extension = getExtension(fileName);

            fileName = Path.GetFileNameWithoutExtension(fileName) + suffix + dateToString + extension;

            return fileName;
        }
        catch(Exception ex){
            Console.WriteLine(ex.Message);
            return null;
        }
    }
    public void convertToJson(FileSystemEventArgs e){
        try { 
            string directory = getDirectory(e);
            string newJson = Path.Combine(getDirectory(e),Path.GetFileNameWithoutExtension(e.Name) + ".json");
            // Console.WriteLine(directory, " ", newJson, " is created");

            using (var jw = new ChoJSONWriter(newJson)){
                using (var cr = new ChoCSVReader(e.FullPath).WithFirstLineHeader()){
                    jw.Write(cr);
                }
            }
            Console.WriteLine("The file {0} has been successfully converted to JSON. ", e.Name);
        }
        catch (Exception ex){
            Console.WriteLine($"Error: {ex.Message}");
        }
        
    }

    public void moveFolder(string sourcePath, string directory){
        string destinationPath = Path.Combine(directory, Path.GetFileName(sourcePath));

        try{
            if (!File.Exists(sourcePath)){
                using(FileStream fs = File.Create(sourcePath)) {}
            }
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            File.Move(sourcePath, destinationPath);
            Console.Write("The file has been moved to the {0}. ", directory);

            if (File.Exists(sourcePath)){
                Console.WriteLine("The original file still exisits.");
            }
            else
            {
                Console.WriteLine("The original file no longer exists.");
            }
            
        }
        catch (Exception ex){
            Console.WriteLine("The process failed {0}", ex.ToString()); 
        }
    }

    public bool isCsv(string extension){
        try{
            if (extension.Equals(".csv")){
                return true;
            }
            else {
                Console.WriteLine("The file is not in a .csv format");
                return false;
            }
        }
        catch(Exception ex){
            Console.WriteLine(ex.Message);
            return false;
        }
    }
    

    public Task StartAsync(CancellationToken cancellationToken)
    {
        watcher = new FileSystemWatcher();
        processWatcher = new FileSystemWatcher();

        watcher.Path = _sourceDirectory;

        processWatcher.Path = _processingDirectory; 

        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //}
    
        watcher.IncludeSubdirectories = false;
        processWatcher.IncludeSubdirectories = false; 
            
        watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime 
            | NotifyFilters.FileName | NotifyFilters.Size;

        processWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime 
            | NotifyFilters.FileName | NotifyFilters.Size;

        watcher.Filter = "*.*";
        processWatcher.Filter = "*.*";

        //register event handler for watcher
        watcher.Changed += new FileSystemEventHandler(onChanged);
        watcher.Created += new FileSystemEventHandler(onChanged);
        watcher.Deleted += new FileSystemEventHandler(onChanged);
        watcher.Renamed += new RenamedEventHandler(onChanged);
        watcher.Error += new ErrorEventHandler(onError);

        watcher.EnableRaisingEvents = true;

            //register event handler for watcher
        processWatcher.Changed += new FileSystemEventHandler(onChanged);
        processWatcher.Created += new FileSystemEventHandler(onChanged);
        processWatcher.Deleted += new FileSystemEventHandler(onChanged);
        processWatcher.Renamed += new RenamedEventHandler(onChanged);
        processWatcher.Error += new ErrorEventHandler(onError);

        processWatcher.EnableRaisingEvents = true;

            //start monitoring
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
