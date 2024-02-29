namespace Repository

open System;
open Model.Entity;
open Config;

type RegisteredFileRepository(ctx: DatabaseContext) =

    interface IRegisteredFileRepository with
        member this.GetRegisteredFileById(id: Guid): RegisteredFile = 
            let query: Linq.IQueryable<RegisteredFile> = query {
                for file in ctx.DataFile do
                where (file.FileId = id)
                where (file.DeletedAt = new System.Nullable<DateTime>())
                select file
            }

            let resultSet = query |> Seq.toArray;
            resultSet.[0];

        member this.CreateRegisteredFile(originalPath: string, folderCode: string): RegisteredFile = 
            
            let mutable newId: Guid = Guid.NewGuid();
            let mutable flag: bool = false;

            while not flag do
                try
                    // If this throw exception, then the id is not yet used.
                    (this :> IRegisteredFileRepository).GetRegisteredFileById newId |> ignore;
                    newId <- (Guid.NewGuid())
                with
                | ex -> flag <- true
                    
            let file: RegisteredFile = new RegisteredFile();
            file.FileId <- newId;
            file.FileOriginalPath <- originalPath.Replace("\\", "//");
            file.FolderCode <- folderCode;

            ctx.DataFile.Add(file) |> ignore;
            ctx.SaveChanges() |> ignore;

            (this :> IRegisteredFileRepository).GetRegisteredFileById file.FileId;

        member this.GetAllUnuploadedRegisteredFiles () =
            let query: Linq.IQueryable<RegisteredFile> = query {
                for file in ctx.DataFile do
                where (file.DeletedAt = new System.Nullable<DateTime>())
                where (file.UploadedAt = new System.Nullable<DateTime>())
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

        member this.UpdateUploadedAtByRegisteredFile (id: Guid, folderTarget: string) =
            let entity: RegisteredFile = (this :> IRegisteredFileRepository).GetRegisteredFileById id;

            entity.UploadedAt <- new DateTime();
            entity.FolderTarget <- folderTarget;

            ctx.DataFile.Update(entity) |> ignore;
            ctx.SaveChanges() |> ignore;
