namespace Services

open System;
open System.Text;
open System.Net.Http;
open Newtonsoft.Json;

open Model.Dto;
open Facade.EnvironmentVariable;

(*
    TelegramService

    This module helps the integration with Telegram. 
*)
module TelegramService =

    let SendMessage (message: string) =
    
        // Request manipulation

        let currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        let requestMessage = "["+currentDateTime+"] "+message;
        let request: TelegramRequestDTO = { text = requestMessage; chat_id = EnvironmentVariable.CHAT_ID; };


        use httpRequest = new HttpClient();
        let json = JsonConvert.SerializeObject request;
        use content = new StringContent (json, Encoding.UTF8, "application/json")

        async {
            httpRequest.PostAsync("https://api.telegram.org/bot"+EnvironmentVariable.TELEGRAM_BOT_ID+"/sendMessage", content) |> Async.AwaitTask |> ignore;
        } |> Async.RunSynchronously;