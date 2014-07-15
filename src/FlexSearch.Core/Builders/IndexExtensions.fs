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

open FlexSearch.Api
open FlexSearch.Core
open FlexSearch.Utility
open System.Collections.Generic
open System
open Validator
open FlexSearch.Api.Message
open FlexSearch.Core.Services

[<AutoOpen>]
module IndexExtensions = 
    type IndexConfiguration with
        /// <summary>
        /// Validator to validate index configuration
        /// </summary>
        member this.Validate() = 
            maybe { 
                do! ("CommitTimeSec", this.CommitTimeSec) |> GreaterThanOrEqualTo 60
                do! ("RefreshTimeMilliSec", this.RefreshTimeMilliSec) |> GreaterThanOrEqualTo 25
                do! ("RamBufferSizeMb", this.RamBufferSizeMb) |> GreaterThanOrEqualTo 100
            }
    
    type Index with
        /// <summary>
        /// Validate Index properties
        /// </summary>
        /// <param name="factoryCollection"></param>
        member this.Validate(factoryCollection : IFactoryCollection) = 
            maybe { 
                do! this.IndexName.ValidatePropertyValue("IndexName")
                do! this.IndexConfiguration.Validate()
                let! analyzers = AnalyzerProperties.Build(this.Analyzers, factoryCollection)
                let! scriptManager = ScriptProperties.Build(this.Scripts, factoryCollection)
                let! fields = FieldProperties.Build(this.Fields, analyzers, this.Scripts, factoryCollection)
//                do! IterExitOnFailure (Seq.toList (this.Scripts)) (fun x -> 
//                        maybe { 
//                            do! x.Key.ValidatePropertyValue("ScriptName")
//                            do! x.Value.Validate(factoryCollection)
//                        })
                return! Choice1Of2()
            }
