#r "System.Runtime.Serialization.dll"
#r "../.AnFake/AnFake.Api.v1.dll"
#r "../.AnFake/AnFake.Core.dll"
#r "../.AnFake/AnFake.Fsx.dll"
#r "../.AnFake/AnFake.Plugins.Tfs2012.dll"
#r "../.AnFake/AnFake.Plugins.NHibernate.dll"
#r "../.AnFake/NHibernate.dll"

open System
open System.Linq
open System.Runtime.Serialization
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012
open AnFake.Plugins.NHibernate

Tfs.PlugIn()
Nh.PlugIn()

let out = ~~".out"
let binOut = out / "bin"

[<DataContract>]
type PerformanceReport () =
    [<DataMember>] member val ChangesetId: int = 0 with get, set
    [<DataMember>] member val ElapsedTime: double = 0.0 with get, set
    [<DataMember>] member val BytesProcessed: int64 = 0L with get, set

"Test.Performance" => (fun _ ->
    let buildDefName = MyBuild.GetProp("productBuild")
    let goodBuild = TfsBuild.QueryByQuality(buildDefName, "Unit-Tests Passed", 1).First()
    
    Folders.Clean(binOut)
    Files.Copy(goodBuild.GetDropLocationOf(ArtifactType.Deliverables) % "*", binOut)
    
    let reportPath = out / "PerfMeter.report"
    let args = 
        (new Args("--", " "))            
            .Option("threads", 4)
            .Option("report", reportPath)
            .ToString()

    Process.Run(fun p -> 
        p.FileName <- binOut / "PerfMeter.exe"
        p.Arguments <- args        
    ).FailIfExitCodeNonZero("PerfMeter.exe FAILED.") 
    |> ignore

    let report = Json.ReadAs<PerformanceReport>(reportPath.AsFile())
    report.ChangesetId <- VersionControl.CurrentChangesetId

    Nh.MapClass<PerformanceReport>()

    Nh.DoWork(fun uow ->
        let prevReports = 
            uow.Query("from PerformanceReport order by id desc")
                .SetMaxResults(5)
                .List<PerformanceReport>()

        uow.Save(report)
        uow.Commit()

        if prevReports.Count = 5 then        
            let prevSpeeds = prevReports.Select(fun rep -> (double)rep.BytesProcessed / rep.ElapsedTime)
            let avg = prevSpeeds.Average()
            let sig2 = prevSpeeds.Average(fun x -> (x - avg)*(x - avg))

            let speed = (double)report.BytesProcessed / report.ElapsedTime
            let threshold = avg - Math.Sqrt(sig2);
            if speed < threshold then
                MyBuild.Failed(
                    "The last reported speed {0:F2} KB/s is under threshold {1:F2} KB/s.", 
                    speed / 1024.0, threshold / 1024.0
                )
    )
)