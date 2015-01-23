#r "../.AnFake/AnFake.Api.v1.dll"
#r "../.AnFake/AnFake.Core.dll"
#r "../.AnFake/AnFake.Fsx.dll"
#r "../.AnFake/Plugins/AnFake.Plugins.Tfs2012.dll"
#r "../.AnFake/Plugins/AnFake.Plugins.StringTemplate.dll"

open System
open System.Linq
open AnFake.Api
open AnFake.Core
open AnFake.Fsx.Dsl
open AnFake.Plugins.Tfs2012
open AnFake.Plugins.StringTemplate

Tfs.PlugIn()
ST.PlugIn()

"Generate.ReleaseNotes" => (fun _ ->
    let wiql = 
        "select [System.Id], [System.State], [System.Title]" + 
        " from WorkItems" +
        " where" +
        "  [System.TeamProject] = @project" +         
        "  and ([System.WorkItemType] = 'Bug' or [System.WorkItemType] = 'Task')" +
        "  and [System.State] = 'Closed'" +
        "  and [System.ChangedDate] > @from"

    let itemGroups = 
        TfsWorkItem.ExecQuery(wiql, "from", (DateTime.Now - TimeSpan.FromDays(30.0)).Date)
            .GroupBy(fun x -> x.Type)

    ST.Render("releasenotes.stg".AsFile(), "releasenotes.html".AsFile(), itemGroups)
)