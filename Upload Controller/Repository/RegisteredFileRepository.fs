namespace Repository

open System;
open Model.Entity;
open Config;

type RegisteredFileRepository(ctx: DatabaseContext) =

    interface IRegisterdFileRepository with
        member this.GetAllUnuploadedRegisteredFiles () =
            let query: Linq.IQueryable<RegisteredFile> = query {
                for file in ctx.DataFile do
                where (file.DeletedAt = new System.Nullable<DateTime>())
                select file
            }

            query |> Seq.toArray;

        member this.GetRegisteredFilesByCodeAndName (folderCode: string, fileName: string): RegisteredFile =
            let query: Linq.IQueryable<RegisteredFile> = query {
                for file in ctx.DataFile do
                where (file.DeletedAt = new System.Nullable<DateTime>())
                where (file.FolderCode = folderCode)
                where (file.FileOriginalPath.Contains(folderCode))
                select file
            }

            let resultSet = query |> Seq.toArray;
            resultSet.[0];

        member this.UpdateUploadedAtByRegisteredFile (id: string, folderTarget: string) =
            let data = ctx.DataFile.Find([| id |]);

            let queriedEntity: RegisteredFile = {
                UploadedAt = new DateTime();
                FileId = data.FileId;
                FileOriginalPath = data.FileOriginalPath;
                FolderCode = data.FolderCode;
                CreatedAt = data.CreatedAt;
                FolderTarget = folderTarget;
                DeletedAt = new System.Nullable<DateTime>();
            }

            ctx.DataFile.Update(queriedEntity) |> ignore;
            ctx.SaveChanges() |> ignore;
