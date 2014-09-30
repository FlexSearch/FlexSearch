// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open System.Linq
open System.Reflection
open org.apache.lucene.codecs
open org.apache.lucene.util

module ModuleInitializer = 
    /// <summary>
    /// Used by the ModuleInit. All code inside the Initialize method is ran as soon as the assembly is loaded.
    /// </summary>
    let Initialize() = 
        let asm = typeof<FlexSearch.Java.FlexCodec410>.Assembly
        // Some codec are written in Java and needs to be reloaded 
        // for them to get picked up by the engine
        let classLoader = ikvm.runtime.AssemblyClassLoader(asm)
        Codec.reloadCodecs (classLoader)
        let availableCodec = Codec.availableCodecs()
        assert (availableCodec.contains ("FlexCodec410") = true)
        ()
