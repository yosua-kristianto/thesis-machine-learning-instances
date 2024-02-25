open Config;
open Model.Entity;
open Microsoft.Extensions.DependencyInjection;
open Microsoft.EntityFrameworkCore;
open Facade.EnvironmentVariable;
open SQLitePCL;

open Repository;

(*

# Data Uploader

This script is dedicated to helps uploading file by folders. 

Background:
Found that Google Drive API have no capabilities to upload folder. Either we zip the thingy out, 
and then upload the file to the Google Drive, or we upload the folder through web. But, this approach is uncomfy, 
especially tracking which files are successfully uploaded or not is huge task for 500K of data. 

The main goal of this notebook / script is to upload the file, by giving folder path as its parameter to run upload script recursively. 
Also, tracking whether the file is successfully uploaded or not to prevent any duplications. 

*)

let AppBanner = 
    printfn " +-+-+-+-+-+-+ +-+-+-+-+-+-+-+-+-+-+
 |U|p|l|o|a|d| |C|o|n|t|r|o|l|l|e|r|
 +-+-+-+-+-+-+ +-+-+-+-+-+-+-+-+-+-+\n\n
                                                                                                                                           ";

let ConfigureServices (services : IServiceCollection) =
    // Register DatabaseContext with DI container
    services.AddDbContext<DatabaseContext>(fun (optionsBuilder: DbContextOptionsBuilder) ->
        let connectionString = sprintf "Data Source=%s" EnvironmentVariable.DATABASE_CONNECTION
        optionsBuilder.UseSqlite(connectionString) |> ignore
        ()
    ) |> ignore;

(*
    ## Main Script
    This is where the management and uploading algorithm doing some works. 
    Remember well that an fsx file script must be executed from its folder hierarchy. 
    There will be several steps to be fulfilled:

    1. Import folder setting pre-processing
        This folder setting stored as data.csv in the root of repository.

    2. Define commands pattern for the upload process
        This is command handler. According to the notes within the handler file, 
        this handler used to control the treatment of the file.
        
    3. SQLite database integration
        Before upload is conducted, for each files that want to be uploaded must be saved 
        to the SQLite database. This prepare the file t

    4. Upload handler
*)
[<EntryPointAttribute("")>]
let Main (argv) =
    AppBanner

    printfn "After AppBanner"

    // Step 1
    let folderSettings: DataFrame array = FolderSettingExtractor.ExtractCsv;

    // Step 2
    SQLitePCL.Batteries.Init();

    // Create service collection
    let services = new ServiceCollection()

    // Configure services
    ConfigureServices services

    // Build service provider
    let serviceProvider = services.BuildServiceProvider()

    // Resolve DatabaseContext
    let ctx = serviceProvider.GetService<DatabaseContext>()

    let repository: IRegisterdFileRepository = new RegisteredFileRepository(ctx);

    let resultSet = repository.GetAllUnuploadedRegisteredFiles();

    for i in resultSet do
        printfn "Data %s" i.FileId

    0;

Main "";

(*
    Algorithm below are applied to do the job:

    1. Collect all datas within csv file to be uploaded
    2. For every datas within the csv file:
    2.1 Navigate to the directory within the csv file
    2.2 For every files within the Directory
    2.2.1 Try to upload the file using GDrive Integrator script 
    2.2.2 If the upload is successful
    2.2.3 Save the file state as success within some persistance (Undetermined. May use SQLite)
    2.2.4 
*)

// let ProgressBarFactory (total : int) =
//     let updateProgress (current : int) =
//         let progress = float current / float total * 100.
//         printf "\rProgress: [%-20s] %.2f%%" (String.replicate (current * 20 / total) "#") progress
//         Console.Out.Flush() |> ignore

//     let dispose() =
//         printfn ""

//     { new System.IDisposable with
//         member this.Dispose() = dispose() }, updateProgress

// let totalIterations = 100
// let disposable, updateProgress = ProgressBarFactory totalIterations

// for e = 1 to 100 do
//     printfn "Doing iteration for process %d" e;
//     for i = 1 to totalIterations do
//         // Do your iteration work here
//         Thread.Sleep(100);
//         updateProgress i
//     disposable.Dispose()