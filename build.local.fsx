#I "packages/FAKE/tools" //used for linux build
#r "packages/FAKE/tools/FakeLib.dll"
#r "packages/Suave/lib/net40/Suave.dll"

open Fake
open System
open System.IO
open System.Diagnostics
open System.Net
open Suave
open Suave.Web
open Suave.Http
open Suave.Successful
open Suave.Redirection
open Suave.Files
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors

let repositoryUrl = Environment.GetEnvironmentVariable "HACKYOURTRAINING_REPOSITORY_URL"
let expectedSecret = Environment.GetEnvironmentVariable "HACKYOURTRAINING_DEPLOY_SECRET"

let repositoryDir = __SOURCE_DIRECTORY__ </> "repository"
let dockerDir = __SOURCE_DIRECTORY__ </> "www.hackyourtraining.com"
Git.Repository.clone __SOURCE_DIRECTORY__ repositoryUrl "repository"

let deploy secret =
    match secret with
    | Choice1Of2 s when s = expectedSecret ->
        Git.Reset.hard repositoryDir "HEAD" null
        Git.Branches.pull repositoryDir "origin" "master"
        let result = ExecProcess (fun info ->
            info.FileName <- "sh" 
            info.WorkingDirectory <- repositoryDir
            info.Arguments <- "publish.sh") (TimeSpan.FromMinutes 15.0)
        if result <> 0 
        then BAD_REQUEST (sprintf "Publish script failed with result %i" result)
        else
            let result = ExecProcess (fun info ->
                info.FileName <- "docker-restart" 
                info.WorkingDirectory <- "repositoryDir") (TimeSpan.FromMinutes 5.0)
            if result <> 0 
            then BAD_REQUEST (sprintf "Docker restart failed with result %i" result)
            else OK "Deployed"
    | _ -> BAD_REQUEST "Bad secret"

let app : WebPart =
    choose 
        [ 
            POST >=> choose
                [ path "/" >=> request (fun req -> deploy (req.queryParam "secret")) ]   
            RequestErrors.NOT_FOUND "Page not found." 
        ]

let runAndForget () = 
    startWebServer { defaultConfig with 
                        bindings = [ HttpBinding.mk HTTP IPAddress.Any 80us ] } app
    
let stop () = killProcess "HackYourTraining"

let reload = stop >> ignore >> runAndForget

let waitUserStopRequest () = 
    () |> traceLine |> traceLine
    traceImportant "Press any key to stop."
    () |> traceLine |> traceLine

    System.Console.ReadLine() |> ignore
    
let watchSource action =
    !! (__SOURCE_DIRECTORY__ </> "*.fs") 
        |> WatchChanges (fun _ -> action ())
        |> ignore

let reloadOnChange () =
    watchSource reload

let askStop = waitUserStopRequest >> stop

Target "run" (runAndForget >> askStop)

Target "watch" (runAndForget >> reloadOnChange >> askStop)

RunTargetOrDefault "run"