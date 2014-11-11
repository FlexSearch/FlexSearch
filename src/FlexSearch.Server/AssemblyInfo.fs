namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FlexSearch Server")>]
[<assembly: AssemblyDescriptionAttribute("FlexSearch Server")>]
[<assembly: AssemblyProductAttribute("FlexSearch")>]
[<assembly: AssemblyCopyrightAttribute("(c) Seemant Rajvanshi, 2012 - 2014")>]
[<assembly: AssemblyFileVersionAttribute("0.23.0.0")>]
[<assembly: AssemblyVersionAttribute("0.23.0.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.23.0.0"
