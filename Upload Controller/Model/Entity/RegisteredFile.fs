namespace Model.Entity

open System;
open System.ComponentModel.DataAnnotations;
open System.ComponentModel.DataAnnotations.Schema;

[<Table("fla_tbl_registered_file")>]
type RegisteredFile = {

    [<Key>]
    [<Column("file_id")>]
    FileId: string

    [<Column("file_original_path")>]
    FileOriginalPath: string

    [<Column("folder_code")>]
    FolderCode: string

    [<Column("folder_target")>]
    FolderTarget: string

    [<Column("uploaded_at")>]
    UploadedAt: Nullable<DateTime>

    [<Column("created_at")>]
    CreatedAt: DateTime

    [<Column("deleted_at")>]
    DeletedAt: Nullable<DateTime>
};