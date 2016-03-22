namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FlexSearch Server")>]
[<assembly: AssemblyDescriptionAttribute("FlexSearch Server")>]
[<assembly: AssemblyProductAttribute("FlexSearch")>]
[<assembly: AssemblyCopyrightAttribute("Copyright (C) 2010 - 2016 - FlexSearch")>]
[<assembly: AssemblyFileVersionAttribute("0.5.1")>]
[<assembly: AssemblyVersionAttribute("0.5.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.5.1"
