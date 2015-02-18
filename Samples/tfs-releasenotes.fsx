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

"Generate" => (fun _ ->
    let wiql = 
        "select [System.Id], [System.State], [System.Title]" + 
        " from WorkItems" +
        " where" +
        "  [System.TeamProject] = @project" +         
        "  and ([System.WorkItemType] = 'Bug' or [System.WorkItemType] = 'Task')" +
        "  and [System.State] = 'Closed'" +
        "  and [System.ChangedDate] > @from"

    let items = 
        TfsWorkItem.ExecQuery(wiql, "from", (DateTime.Now - TimeSpan.FromDays(30.0)).Date);

    let releaseNotes = 
        ReleaseNotes.Create(
            "My Product", 
            "1.0".AsVersion(), 
            items,
            fun ticket note ->                
                note.Category <- ticket.Type
                note.Ordinal <- ticket.NativeId
        )

    let tmplFile = "releasenotes.stg".AsFile()
    let notesFile = "releasenotes.html".AsFile()
    ST.Render(tmplFile, notesFile, releaseNotes)
)