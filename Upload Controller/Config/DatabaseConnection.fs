namespace Config

open Microsoft.EntityFrameworkCore;

open Model.Entity;
open Facade.EnvironmentVariable;

type DatabaseContext(options: DbContextOptions<DatabaseContext>) = 
    inherit DbContext(options);

    let mutable dataFileCtx: DbSet<RegisteredFile> = base.Set<RegisteredFile>()

    member this.DataFile
        with get() = dataFileCtx
        and set s = dataFileCtx <- s

    override this.OnConfiguring (optionsBuilder: DbContextOptionsBuilder) = 
        let connectionString = sprintf "Data Source=%s" EnvironmentVariable.DATABASE_CONNECTION;
        optionsBuilder.UseSqlite(connectionString) |> ignore;

    override this.OnModelCreating(modelBuilder: ModelBuilder) =
        base.OnModelCreating(modelBuilder);