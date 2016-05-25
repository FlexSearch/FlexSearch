namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FlexSearch Core Library")>]
[<assembly: AssemblyDescriptionAttribute("FlexSearch Core Library")>]
[<assembly: AssemblyProductAttribute("FlexSearch")>]
[<assembly: AssemblyCopyrightAttribute("Copyright (C) 2010 - 2016 - FlexSearch")>]
[<assembly: AssemblyFileVersionAttribute("0.6.10")>]
[<assembly: AssemblyVersionAttribute("0.6.10")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.6.10"
    let [<Literal>] InformationalVersion = "0.6.10"
