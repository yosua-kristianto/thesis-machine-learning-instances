open Microsoft.Extensions.DependencyInjection;
open Microsoft.EntityFrameworkCore;

open Config;
open Facade.EnvironmentVariable;
open Model.Entity;
open Repository;
open Handler.CommandHandler;

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


        
let ConfigurationBuilder =

    // Step 2
    Config.DatabaseInitializer.Initialzr;

    // Create service collection
    let services = new ServiceCollection()

    // Step 3

    // Configuring DI
    // Configure services
    ConfigureServices services
    let serviceProvider = services.BuildServiceProvider()
    
    // Registering DI
    let ctx = serviceProvider.GetService<DatabaseContext>()

    ctx;


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
        Before upload is conducted, for each files information that will be uploaded must be saved 
        to the SQLite database. In this endeavour, the Entity Framework Core SQLite is employed. 
        Therefore, a dependency injection pattern, and OO approach is required to be conducted.

    4. Upload handler
        This handler have some algorithm below:
            Algorithm below are applied to do the job:

            4.1. Collect all datas within csv file to be uploaded
            4.2. For every datas within the csv file:
            4.2.1 Navigate to the directory within the csv file
            4.2.2 For every files within the Directory
            4.2.2.1 If the folder code include SRGAN
            4.2.2.1.1 -> Randomize whether the data will be treated as Train / Test / Val -> 7 : 1.5 : 1.5
            4.2.2.1.2 -> Setup the target upload folder ID to the decided Train / Test / Val
            4.2.2.2 Save the file to the Database as an unuploaded state.
            4.2.3 For every saved files
            4.2.3.1 Try to upload the file using GDrive Integrator script 
            4.2.3.2 If the upload is successful
            4.2.3.3 Save the file state as success within some persistance (Undetermined. May use SQLite)

--------------------------------------------------------------------------------------------------------
@since 20240327
--------------------------------------------------------------------------------------------------------
Add additional argument variable to limit the action into non-united operation:

--register => Will enable the code to do file registrations
--upload => Will enable the code invoke upload
*)
[<EntryPoint>]
let Main (args: string array) = 
    printfn "Argument: %s" args[0]

    AppBanner

    // Step 1
    let folderSettings: DataFrame array = FolderSettingExtractor.ExtractCsv;

    // Step 2 and 3
    let ctx: DatabaseContext = ConfigurationBuilder;

    // Connecting to SQL
    let repository: IRegisteredFileRepository = new RegisteredFileRepository(ctx);

    // Step 4
    if Array.contains "--register" args then
        printfn "Found register arguments. Running file registration.";
        for folder in folderSettings do
            CommandParser folder repository |> ignore;
    
    if Array.contains "--upload" args then
        printfn "Found register arguments. Running upload.";
        InvokeUpload repository |> ignore;
    
    0;