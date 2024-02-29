namespace Facade

open System;
open System.IO;

(*
    LogWriter

    This module contains the log writer. Whether any configurations regarding the logging format changes, 
    this module is the key.
*)
module internal LogWriter =

    // Configure this to change the log path
    let PATH_TO_LOG: string = "../../../../logs/";

    (*
        Write

        This function helps the log formatting and log writing process. The log within this service is as follow:
        
        `[YYYY-MM-DD HH:ii:ss][LOG_TYPE][DATA_UPLOADER] The Log Message`
    *)
    let Write (message: string) (logType: string) = 
        // Log formatting
        let currentTime = DateTime.Now;
        let currentTimeInString = currentTime.ToString("yyyy-MM-dd H:m:s.FFFF zzz")
        let currentDateInString = currentTime.ToString("yyyyMMdd")
        let logToBeWritten = sprintf "[%s][%s][DATA_UPLOADER] %s" currentTimeInString logType message
        
        // Path setting
        let pathToLog = Path.Combine(PATH_TO_LOG, sprintf "%s.log" currentDateInString);

        // Create the path if not exist
        if not (File.Exists(pathToLog)) then
            if not (Directory.Exists(PATH_TO_LOG)) then
                Directory.CreateDirectory(PATH_TO_LOG) |> ignore;
            use stream = new StreamWriter(pathToLog, false);

            stream.Write("");
            stream.Close();

        // Write it as file.
        File.AppendAllText(pathToLog, logToBeWritten + "\n") |> ignore;

        logToBeWritten;

(*
    Log

    This module helps the logging.
*)
module Log =

    (*
        I
        Stands for Info Log
    *)
    let I (message: string) = 
        LogWriter.Write message "INFO" |> ignore;

    (*
        E
        Stands for Error log
    *)
    let E (message: string) =
        printfn "%s" (LogWriter.Write message "ERROR");

    (*
        D
        Stands for Debug log
    *)
    let D (message: string) = 
        printfn "%s" (LogWriter.Write message "DEBUG");

    (*
        V
        Stands for Verbose log
    *)
    let V (message: string) =
        LogWriter.Write message "VERBOSE" |> ignore;

    (*
        W
        Stands for Warning log
    *)
    let W (message: string) =
        LogWriter.Write message "WARNING" |> ignore;
        




