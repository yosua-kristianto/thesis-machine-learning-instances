namespace Facade

open System.IO;
open Newtonsoft.Json;

open Model.Dto;

(*
    EnvironmentVariable

    This file used to help integrate config.json
*)
module EnvironmentVariable =
    let EnvironmentVariable: RegisteredKeys  =
    
        let envPath: string = "config.json";
    
        let environmentVariableJsonFile = File.ReadAllText(envPath);

        let configurationValue: RegisteredKeys = JsonConvert.DeserializeObject<RegisteredKeys>(environmentVariableJsonFile);
 
        configurationValue;