﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FlexSearch Common Utilities")>]
[<assembly: AssemblyDescriptionAttribute("FlexSearch Common Utilities")>]
[<assembly: AssemblyProductAttribute("FlexSearch")>]
[<assembly: AssemblyCopyrightAttribute("(c) Seemant Rajvanshi, 2012 - 2014")>]
[<assembly: AssemblyFileVersionAttribute("0.23.1.1")>]
[<assembly: AssemblyVersionAttribute("0.23.1.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.23.1.1"