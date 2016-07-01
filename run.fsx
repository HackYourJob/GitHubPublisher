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

let repositoryDirectoryName = "repository"
let repositoryFullPath = __SOURCE_DIRECTORY__ </> repositoryDirectoryName
let dockerFullPath = __SOURCE_DIRECTORY__ </> "www.hackyourtraining.com"

let initializeRepository () =
    if TestDir repositoryFullPath |> not
    then Git.Repository.clone __SOURCE_DIRECTORY__ repositoryUrl repositoryDirectoryName

let exec commandName arguments workingDirectory timeout =
    let result = ExecProcess (fun info ->
        info.FileName <- commandName 
        info.WorkingDirectory <- workingDirectory
        info.Arguments <- arguments) timeout
    if result <> 0 
    then failwith (sprintf "Publish script failed with result %i" result)

let updateRepository () =
    Git.Reset.hard repositoryFullPath "HEAD" null
    Git.Branches.pull repositoryFullPath "origin" "master"

let publishDocker () =
    exec "sh" "publish.sh" repositoryFullPath (TimeSpan.FromMinutes 15.0)

let restartDocker () =
    exec "docker-restart" "" repositoryDirectoryName (TimeSpan.FromMinutes 5.0)

let checkSecret action secret =
    match secret with
    | Choice1Of2 s when s = expectedSecret ->
        action ()
        OK "Deployed"
    | _ -> BAD_REQUEST "Bad secret"

let deploy = checkSecret (updateRepository >> publishDocker >> restartDocker)

let app : WebPart =
    choose 
        [ 
            POST >=> choose
                [ path "/" >=> request (fun req -> deploy (req.queryParam "secret")) ]   
            RequestErrors.NOT_FOUND "Page not found." 
        ]

let run () = 
    startWebServer { defaultConfig with 
                        bindings = [ HttpBinding.mk HTTP IPAddress.Any 80us ] } app
    

initializeRepository >> run
