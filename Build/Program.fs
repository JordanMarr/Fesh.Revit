﻿module Program 

open System
open Fake.IO
open Fake.IO.Globbing.Operators // !! and ++
open Fake.DotNet
open Fake.Core
open Fun.Build
open Fun.Result

let path xs = System.IO.Path.Combine(Array.ofList xs)
let rootDir = Files.findParent __SOURCE_DIRECTORY__ "Fesh.Revit2024.fsproj";
let buildDir = path [ rootDir; ".build" ]
let addinProjDir = rootDir
let installerProjDir = path [ rootDir; "Installer" ]
let installerProj = path [ installerProjDir; "Installer.fsproj" ]
let installerBinReleaseDir = path [ installerProjDir; "bin"; "Release" ]

let supportedRevitYears = [ 2024 ]

pipeline "Fesh.Revit" {

    stage "Clean" {
        run (fun _ ->
            Shell.cleanDir buildDir
            Shell.cleanDir (path [ addinProjDir; "bin" ])
            Shell.cleanDir (path [ installerProjDir; "bin" ])
        )
    }
    
    stage "Restore Packages" {        
        run $"dotnet restore {rootDir}/Fesh.Revit.sln"
    }
    
    stage "Restore Tools" {
        //run $"dotnet tool restore" $"--tool-manifest {rootDir}/.config/dotnet-tools.json --configfile {__SOURCE_DIRECTORY__}/nuget.config"
        run "dotnet tool install --global wix --version 5.0.0"
    }

    stage "Build Addin" {
        run (fun ctx -> asyncResult {
            for year in supportedRevitYears do
                do! ctx.RunCommand $"dotnet build {addinProjDir}\\Fesh.Revit%i{year}.fsproj -c Release -p:RevitVersion=%i{year}"
        })        
    }
    
    stage "Build Installer" {
        run $"dotnet build {installerProj} -c Release"
    }

    stage "Copy Installer to Build Dir" {
        run (fun _ -> 
            !! $"{installerBinReleaseDir}/*.msi"
            |> Shell.copyFiles buildDir
        )
    }

    runIfOnlySpecified false
}

tryPrintPipelineCommandHelp ()
