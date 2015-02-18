#r "System.Runtime.Serialization.dll"
#r "../.AnFake/AnFake.Api.v1.dll"
#r "../.AnFake/AnFake.Core.dll"
#r "../.AnFake/AnFake.Fsx.dll"
#r "../.AnFake/Plugins/AnFake.Plugins.NHibernate.dll"
#r "../.AnFake/Plugins/NHibernate.dll"

open System
open System.Linq
open System.Runtime.Serialization
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.NHibernate
open NHibernate.Mapping.ByCode

Nh.PlugIn()

[<DataContract>]
type PerformanceReport () =
    [<DataMember>] member val Name: string = null with get, set
    [<DataMember>] member val ElapsedTime: double = 0.0 with get, set
    [<DataMember>] member val BytesProcessed: int64 = 0L with get, set

"Report" => (fun _ ->
    let report = Json.Read<PerformanceReport>("report.json".AsFile())

    Trace.InfoFormat("{0} processing speed {1:F2}MB/s", report.Name, (double)report.BytesProcessed / report.ElapsedTime / 1024.0)

    Nh.MapClass<PerformanceReport>(
        fun map ->
            map.Property(
                "Name",                
                fun p -> p.Index("IDX_Name")
            )            
    )

    let uow = Nh.BeginWork()

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
        if (speed - avg)*(speed - avg) > sig2 then
            MyBuild.Failed("Reported speed {0:F2}MB/s is out of tolerance interval [{1:F2}, {2:F2}] ", 
                speed / 1024.0, (avg - Math.Sqrt(sig2)) / 1024.0, (avg + Math.Sqrt(sig2)) / 1024.0)
)