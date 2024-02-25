namespace Model.Entity

open System;
open System.ComponentModel.DataAnnotations;
open System.ComponentModel.DataAnnotations.Schema;

[<Table("fla_tbl_registered_file")>]
type RegisteredFile () = 

    [<Key>]
    [<Column("file_id")>]
    member val FileId: string = "" with get, set

    [<Column("file_original_path")>]
    member val FileOriginalPath: string = "" with get, set

    [<Column("folder_code")>]
    member val FolderCode: string = "" with get, set

    [<Column("folder_target")>]
    member val FolderTarget: string = "" with get, set

    [<Column("uploaded_at")>]
    member val UploadedAt: Nullable<DateTime> = new System.Nullable<DateTime>() with get, set

    [<Column("created_at")>]
    member val CreatedAt: DateTime = new DateTime() with get, set

    [<Column("deleted_at")>]
    member val DeletedAt: Nullable<DateTime> = new System.Nullable<DateTime>() with get, set
