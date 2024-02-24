namespace Repository

open Model.Entity;

(*
    RegisteredFileRepository

    This repository contains every integration within RegistedFile table.
*)
type IRegisterdFileRepository =
    
    (*
        GetAllUnuploadedRegisteredFiles

        This function run query to get all data from the RegisteredFile table 

        @return RegisteredFile array
    *)
    abstract member GetAllUnuploadedRegisteredFiles: unit -> RegisteredFile array

    (*
        GetRegisteredFilesByCodeAndName

        This function run query to get a RegisteredFile by it folder code and name.

        @param string
        -> This parameter refers to FolderCode parameter. 

        @param string
        -> This parameter refers to the file name of the intended searching file.
    *)
    abstract member GetRegisteredFilesByCodeAndName: string * string -> RegisteredFile
    
    (*
        UpdateUploadedAtByRegisteredFile

        This function run query to mark whether the registered file has been uploaded successfully.

        @param string
        -> This parameter refers to FileId.

        @param string
        -> This parameter refers to the folder id of the location where the file is uploaded.
        Storing drive's ID.
    *)
    abstract member UpdateUploadedAtByRegisteredFile: string * string -> unit