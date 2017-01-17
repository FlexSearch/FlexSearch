namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FlexSearch Server")>]
[<assembly: AssemblyDescriptionAttribute("FlexSearch Server")>]
[<assembly: AssemblyProductAttribute("FlexSearch")>]
[<assembly: AssemblyCopyrightAttribute("Copyright (C) 2010 - 2017 - FlexSearch")>]
[<assembly: AssemblyFileVersionAttribute("0.8.4")>]
[<assembly: AssemblyVersionAttribute("0.8.4")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.8.4"
    let [<Literal>] InformationalVersion = "0.8.4"
