namespace Repository

open System;

open Model.Entity;

(*
    RegisteredFileRepository

    This repository contains every integration within RegistedFile table.
*)
type IRegisteredFileRepository =
    
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

        @param Guid
        -> This parameter refers to FileId.

        @param string
        -> This parameter refers to the folder id of the location where the file is uploaded.
        Storing drive's ID.
    *)
    abstract member UpdateUploadedAtByRegisteredFile: Guid * string -> unit

    (*
        CreateRegisteredFile

        This function run query to do INSERT statement for registered file object.

        @param string
        -> This parameter refers to FileOriginalPath

        @param string
        -> This parameter refers to FolderCode

        See RegisteredFile for more details.
    *)
    abstract member CreateRegisteredFile: string * string -> RegisteredFile

    (*
        GetRegisteredFileById

        This function run query to get a registered file object by its id.

        @param string
        -> Refers to FileId
    *)
    abstract member GetRegisteredFileById: Guid -> RegisteredFile