#r "bin/Debug/AnFake.Api.dll"
#r "bin/Debug/AnFake.Core.dll"
#r "bin/Debug/AnFake.Fsx.dll"

open System.Linq
open AnFake.Core
open AnFake.Fsx.Dsl

Target.Define("Clean", (fun _ ->
    Folders.Clean "txt"
))

Target.Define("Test.PrepareData", (fun _ ->
    let dlpBin = "dlp"
    let officeFiles = !!"office/*"
    let collectionDef = 
        [|
            1024L, 100L*1024L, 300
            100L*1024L, 1024L*1024L, 150
            1024L*1024L, 20L*1024L*1024L, 20
        |]

    let indexOfDefinition (len: int64) =        
        let mutable idx = -1
        for i = 0 to collectionDef.Length - 1 do
            let (a, b, _) = collectionDef.[i]
            if (len >= a && len < b) then idx <- i
        idx

    Logger.Debug "Converting office files..."

    /////////////////////////////////////////////

    let documents = !!"office/*"
    let doc2text = dlpBin / "isys_doc2text.exe"
    let args = new Args("--", " ")
        .Option("options", "EXCELMODE=CSV;PDFPHYSLAYOUT=True")
        .Option("silent")
        .MacroOption("output", fun f -> "txt/" + f.Name + ".txt")
        .MacroValue(fun f -> "office/" + f.Name)

    documents.Using(doc2text, args).DoForEach();

    /////////////////////////////////////////////

    /////////////////////////////////////////////

    let documents = !!"office/*"
    let doc2text = dlpBin / "isys_doc2text.exe"    

    for f in documents do        
        let args = new Args("--", " ")
            .Option("options", "EXCELMODE=CSV;PDFPHYSLAYOUT=True")
            .Option("silent")
            .Option("output", "txt/" + f.Name + ".txt")
            .QuotedValue("office/" + f.Name)

        Process.Run(fun p -> 
            p.FileName <- doc2text
            p.Arguments <- args.ToString())        
            |> ignore

    /////////////////////////////////////////////

    /////////////////////////////////////////////

    product.UsingMsBuild().BuildDebug();

    /////////////////////////////////////////////

    /////////////////////////////////////////////

    MsBuild.BuildDebug(product);

    /////////////////////////////////////////////

    /////////////////////////////////////////////

    product.UsingDotCover().InstrumentThenRun(tests.UsingMsTest());

    /////////////////////////////////////////////

    /////////////////////////////////////////////

    let coverage = DotCover.Instrument(product)
    MsTest.Run(tests)
    coverage.GetReport()

    /////////////////////////////////////////////

    /////////////////////////////////////////////

    let out = "out".AsPath()
    out.AsFolder().Clean()

    /////////////////////////////////////////////

    /////////////////////////////////////////////

    let out = "out".AsPath()
    Folders.Clean(out)

    /////////////////////////////////////////////


    Folders.Create "txt"

    for f in officeFiles do
        if (not <| Files.Exists("txt" @@ f.Name + ".txt")) then
            Logger.DebugFormat("  {0}", f.Name)

            Process.Run(fun p -> 
                p.FileName <- dlpBin @@ "isys_doc2text.exe"
                p.Arguments <- "--options EXCELMODE=CSV;PDFPHYSLAYOUT=True -s --output txt/" + f.Name + ".txt office/" + f.Name)        
                |> ignore

    Logger.Debug "Compounding test collection..."

    Folders.Clean "compound"

    let txtFiles = (!!"txt/*").ToList()
    let weights: float array = Array.zeroCreate collectionDef.Length

    for f in txtFiles do
        let idx = indexOfDefinition f.Length
        if (idx >= 0) then weights.[idx] <- weights.[idx] + 1.0

    for i = 0 to collectionDef.Length - 1 do
        let (_, _, c) = collectionDef.[i]
        weights.[i] <- float c / weights.[i]

    let counts: float array = Array.zeroCreate collectionDef.Length

    for f in txtFiles do
        let idx = indexOfDefinition f.Length
        if (idx >= 0) then 
            counts.[idx] <- counts.[idx] + weights.[idx]

            if (counts.[idx] >= 1.0) then
                counts.[idx] <- counts.[idx] - 1.0
                Files.Copy(f.FullPath, "compound" @@ f.Name)

    for i = 0 to collectionDef.Length - 1 do
        let (a, b, c) = collectionDef.[i]

        if (counts.[i] > 1.0) then        
            MyBuild.WarnFormat("{0} - {1}KB: {2} files missed", a/1024L, b/1024L, counts.[i])

        if (counts.[i] = 0.0) then        
            MyBuild.ErrorFormat("{0} - {1}KB: {2} files missed", a/1024L, b/1024L, c)
))

Target.Run "Test.PrepareData"